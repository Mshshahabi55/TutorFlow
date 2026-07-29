import { useState } from "react";
import { IconButton, Stack, Tooltip, Typography } from "@mui/material";
import ContentCopyRoundedIcon from "@mui/icons-material/ContentCopyRounded";
import CheckRoundedIcon from "@mui/icons-material/CheckRounded";
import { monoFontFamily } from "@/app/theme";

export interface CopyableIdProps {
  id: string;
}

/**
 * Displays a record's id with a copy-to-clipboard action. Used on every
 * registration/creation success screen across every feature module: the
 * returned id is often still the only way to reference this specific record
 * elsewhere (sharing a Student id for a Relationship invite, an Admin
 * lookup), so it must be easy to copy rather than only readable — a real
 * signed-in user's own id no longer needs this (`useOwnId`), but every id
 * belonging to *someone else* still does. Promoted here from
 * features/identity once features/scheduling needed the same pattern
 * (Sprint 7).
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
      <Typography variant="body1" fontFamily={monoFontFamily} sx={{ wordBreak: "break-all" }}>
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
