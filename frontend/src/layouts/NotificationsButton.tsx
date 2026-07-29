import { useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  Badge,
  Box,
  Button,
  Divider,
  IconButton,
  List,
  ListItemButton,
  ListItemText,
  Popover,
  Typography,
} from "@mui/material";
import NotificationsRoundedIcon from "@mui/icons-material/NotificationsRounded";
import { useMyNotifications } from "@/features/communication/hooks/useNotificationQueries";
import {
  useMarkAllNotificationsRead,
  useMarkNotificationRead,
} from "@/features/communication/hooks/useNotificationMutations";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import { paths } from "@/routes/paths";
import { NotificationType } from "@/services/api/dtos";
import type { NotificationDto } from "@/services/api/dtos";

const RECENT_NOTIFICATIONS_LIMIT = 10;

/**
 * RC5.1 (docs/adr/ADR-022-...): RelatedEntityId's meaning is
 * NotificationType-specific — a Session for booking/cancellation, a
 * Conversation for a reply/new message, a Relationship for a confirmation.
 * `AvailabilityChanged` is named in the enum but never actually raised
 * (ADR-022's Non-Goals — no recipient-resolution mechanism exists), so it
 * has no destination here either.
 */
function notificationHref(notification: NotificationDto): string | undefined {
  if (!notification.relatedEntityId) {
    return undefined;
  }

  switch (notification.type) {
    case NotificationType.BookingConfirmed:
    case NotificationType.LessonCancelled:
      return paths.scheduling.sessionDetail(notification.relatedEntityId);
    case NotificationType.TutorReplied:
    case NotificationType.NewMessage:
      return paths.messages.conversationDetail(notification.relatedEntityId);
    case NotificationType.ParentConfirmed:
      return paths.identity.relationships;
    default:
      return undefined;
  }
}

export function NotificationsButton() {
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);
  const navigate = useNavigate();
  const notificationsQuery = useMyNotifications();
  const markRead = useMarkNotificationRead();
  const markAllRead = useMarkAllNotificationsRead();

  const notifications = notificationsQuery.data ?? [];
  const unreadCount = notifications.filter((notification) => !notification.readAtUtc).length;
  const recent = notifications.slice(0, RECENT_NOTIFICATIONS_LIMIT);

  function handleOpen(notification: NotificationDto) {
    if (!notification.readAtUtc) {
      markRead.mutate(notification.notificationId);
    }

    const href = notificationHref(notification);
    if (href) {
      setAnchorEl(null);
      void navigate(href);
    }
  }

  return (
    <>
      <IconButton aria-label="Notifications" onClick={(event) => setAnchorEl(event.currentTarget)}>
        <Badge badgeContent={unreadCount} color="error" max={99}>
          <NotificationsRoundedIcon />
        </Badge>
      </IconButton>
      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <Box sx={{ width: 320, maxWidth: "90vw" }}>
          <Box sx={{ px: 2, py: 1.5, display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <Typography variant="subtitle2" fontWeight={600}>
              Notifications
            </Typography>
            {unreadCount > 0 ? (
              <Button
                size="small"
                onClick={() => markAllRead.mutate()}
                disabled={markAllRead.isPending}
              >
                Mark all read
              </Button>
            ) : null}
          </Box>
          <Divider />
          {recent.length === 0 ? (
            <Box sx={{ p: 2 }}>
              <Typography variant="body2" color="text.secondary">
                No notifications yet.
              </Typography>
            </Box>
          ) : (
            <List dense disablePadding sx={{ maxHeight: 400, overflowY: "auto" }}>
              {recent.map((notification) => (
                <ListItemButton
                  key={notification.notificationId}
                  onClick={() => handleOpen(notification)}
                  sx={{ alignItems: "flex-start" }}
                >
                  <ListItemText
                    primary={notification.summary}
                    secondary={toTehranDisplay(notification.createdAtUtc)}
                    slotProps={{
                      primary: { fontWeight: notification.readAtUtc ? 400 : 700 },
                    }}
                  />
                </ListItemButton>
              ))}
            </List>
          )}
        </Box>
      </Popover>
    </>
  );
}
