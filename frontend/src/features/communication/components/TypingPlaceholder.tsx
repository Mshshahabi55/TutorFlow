import { Box, Stack } from "@mui/material";

/**
 * RC5.1 Step 3 asked for a "typing placeholder" and ADR-022 accepted it
 * literally: a static, non-functional UI element, not a real typing
 * indicator — no presence/typing signal exists anywhere in this API (no
 * WebSockets/SignalR, per the ADR's "No new infrastructure" decision), so
 * there is nothing real to wire this up to. It never animates and carries
 * `aria-hidden` so it never tells a screen reader user someone is typing
 * when nobody verifiably is — purely the same visual affordance a chat UI
 * is expected to reserve space for.
 */
export function TypingPlaceholder() {
  return (
    <Stack direction="row" justifyContent="flex-start" aria-hidden="true">
      <Box
        sx={{
          display: "flex",
          gap: 0.5,
          bgcolor: "action.selected",
          borderRadius: 2,
          px: 2,
          py: 1.25,
          opacity: 0.5,
        }}
      >
        {[0, 1, 2].map((dot) => (
          <Box
            key={dot}
            sx={{
              width: 6,
              height: 6,
              borderRadius: "50%",
              bgcolor: "text.secondary",
            }}
          />
        ))}
      </Box>
    </Stack>
  );
}
