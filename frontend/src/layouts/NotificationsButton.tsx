import { useState } from "react";
import { IconButton, Popover, Typography, Box } from "@mui/material";
import NotificationsRoundedIcon from "@mui/icons-material/NotificationsRounded";

/**
 * UI only (per the shell brief): no badge/count — there is nothing real
 * to count — and opening it shows an honest "no notifications" message
 * rather than a button that visibly does nothing on click.
 */
export function NotificationsButton() {
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  return (
    <>
      <IconButton
        aria-label="Notifications"
        size="small"
        onClick={(event) => setAnchorEl(event.currentTarget)}
      >
        <NotificationsRoundedIcon fontSize="small" />
      </IconButton>
      <Popover
        open={Boolean(anchorEl)}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "right" }}
        transformOrigin={{ vertical: "top", horizontal: "right" }}
      >
        <Box sx={{ p: 2, maxWidth: 260 }}>
          <Typography variant="body2" color="text.secondary">
            No notifications yet.
          </Typography>
        </Box>
      </Popover>
    </>
  );
}
