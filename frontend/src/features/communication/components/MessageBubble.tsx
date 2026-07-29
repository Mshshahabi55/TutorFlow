import { Box, Stack, Typography } from "@mui/material";
import DoneRoundedIcon from "@mui/icons-material/DoneRounded";
import DoneAllRoundedIcon from "@mui/icons-material/DoneAllRounded";
import { toTehranDisplay } from "@/shared/time/tehranTime";
import type { MessageDto } from "@/services/api/dtos";

export interface MessageBubbleProps {
  message: MessageDto;
  isOwn: boolean;
}

/**
 * One Message bubble. Delivery state (Sent/Read) is only meaningful for a
 * Message the viewer themselves sent — `readAtUtc` reflects whether the
 * *recipient* has read it, which has no meaning to render on a Message the
 * viewer received (RC5.1 Step 3: "Delivery state").
 */
export function MessageBubble({ message, isOwn }: MessageBubbleProps) {
  return (
    <Stack
      direction="row"
      justifyContent={isOwn ? "flex-end" : "flex-start"}
      role="listitem"
    >
      <Box
        sx={{
          maxWidth: { xs: "85%", sm: "70%" },
          bgcolor: isOwn ? "primary.main" : "action.selected",
          color: isOwn ? "primary.contrastText" : "text.primary",
          borderRadius: 2,
          px: 2,
          py: 1,
        }}
      >
        <Typography variant="body2" sx={{ whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
          {message.body}
        </Typography>
        <Stack direction="row" spacing={0.5} justifyContent="flex-end" alignItems="center" mt={0.5}>
          <Typography
            variant="caption"
            sx={{ opacity: 0.8, color: isOwn ? "primary.contrastText" : "text.secondary" }}
          >
            {toTehranDisplay(message.sentAtUtc)}
          </Typography>
          {isOwn ? (
            message.readAtUtc ? (
              <DoneAllRoundedIcon fontSize="inherit" aria-label="Read" titleAccess="Read" sx={{ opacity: 0.8 }} />
            ) : (
              <DoneRoundedIcon fontSize="inherit" aria-label="Sent" titleAccess="Sent" sx={{ opacity: 0.8 }} />
            )
          ) : null}
        </Stack>
      </Box>
    </Stack>
  );
}
