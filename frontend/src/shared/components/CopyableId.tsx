import { useState } from "react";
import { IconButton, Stack, Tooltip, Typography } from "@mui/material";
import ContentCopyRoundedIcon from "@mui/icons-material/ContentCopyRounded";
import CheckRoundedIcon from "@mui/icons-material/CheckRounded";

export interface CopyableIdProps {
  id: string;
}

/**
 * Displays a record's id with a copy-to-clipboard action. Used on every
 * registration/creation success screen across every feature module: since
 * no authentication exists (ADR-011), the returned id is often the only way
 * the user can find this record again, so it must be easy to copy rather
 * than only readable. Promoted here from features/identity once
 * features/scheduling needed the same pattern (Sprint 7).
 */
export function CopyableId({ id }: CopyableIdProps) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    await navigator.clipboard.writeText(id);
    setCopied(true);
    window.setTimeout(() => setCopied(false), 2000);
  }

  return (
    <Stack direction="row" alignItems="center" spacing={1}>
      <Typography variant="body1" fontFamily="ui-monospace, monospace" sx={{ wordBreak: "break-all" }}>
        {id}
      </Typography>
      <Tooltip title={copied ? "Copied" : "Copy id"}>
        <IconButton size="small" aria-label="Copy id" onClick={() => void handleCopy()}>
          {copied ? <CheckRoundedIcon fontSize="small" /> : <ContentCopyRoundedIcon fontSize="small" />}
        </IconButton>
      </Tooltip>
    </Stack>
  );
}
