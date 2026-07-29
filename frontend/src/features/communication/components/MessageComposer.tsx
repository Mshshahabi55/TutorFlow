import { useState, type FormEvent, type KeyboardEvent } from "react";
import { IconButton, Stack, TextField } from "@mui/material";
import SendRoundedIcon from "@mui/icons-material/SendRounded";

const MAX_BODY_LENGTH = 4000; // Domain.Communication.Message.MaxBodyLength

export interface MessageComposerProps {
  onSend: (body: string) => void;
  isSending: boolean;
}

/** Enter sends, Shift+Enter inserts a newline — the conventional chat-composer keybinding. */
export function MessageComposer({ onSend, isSending }: MessageComposerProps) {
  const [body, setBody] = useState("");

  const trimmed = body.trim();
  const canSend = trimmed.length > 0 && !isSending;

  function submit() {
    if (!canSend) {
      return;
    }

    onSend(trimmed);
    setBody("");
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    submit();
  }

  function handleKeyDown(event: KeyboardEvent<HTMLDivElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      submit();
    }
  }

  return (
    <Stack
      component="form"
      direction="row"
      spacing={1}
      alignItems="flex-end"
      onSubmit={handleSubmit}
      sx={{ p: 2, borderTop: "1px solid", borderColor: "divider" }}
    >
      <TextField
        label="Message"
        placeholder="Write a message…"
        value={body}
        onChange={(event) => setBody(event.target.value)}
        onKeyDown={handleKeyDown}
        multiline
        maxRows={6}
        fullWidth
        size="small"
        slotProps={{ htmlInput: { maxLength: MAX_BODY_LENGTH } }}
      />
      <IconButton
        type="submit"
        color="primary"
        aria-label="Send message"
        disabled={!canSend}
        sx={{ flexShrink: 0 }}
      >
        <SendRoundedIcon />
      </IconButton>
    </Stack>
  );
}
