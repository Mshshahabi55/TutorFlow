import { Avatar, Stack, Typography } from "@mui/material";
import VideocamRoundedIcon from "@mui/icons-material/VideocamRounded";
import { MeetingProviderOption } from "@/services/api/dtos";

// No real Google/Microsoft/Zoom logo asset is embedded here (trademark —
// this app has no license to reproduce any of them); a distinct color per
// provider plus its real name is an honest, non-infringing "logo-like"
// visual distinction, the same spirit as TutorProfileHero's generic
// person-icon Avatar standing in for a photo this API has none of.
const PROVIDER_LABELS: Record<MeetingProviderOption, string> = {
  [MeetingProviderOption.GoogleMeet]: "Google Meet",
  [MeetingProviderOption.MicrosoftTeams]: "Microsoft Teams",
  [MeetingProviderOption.Zoom]: "Zoom",
  [MeetingProviderOption.Mock]: "Mock Meeting (dev only)",
};

const PROVIDER_COLORS: Record<MeetingProviderOption, string> = {
  [MeetingProviderOption.GoogleMeet]: "#1a73e8",
  [MeetingProviderOption.MicrosoftTeams]: "#5b5fc7",
  [MeetingProviderOption.Zoom]: "#2d8cff",
  [MeetingProviderOption.Mock]: "#757575",
};

function providerLabel(provider: MeetingProviderOption): string {
  return PROVIDER_LABELS[provider] ?? "Online meeting";
}

export function ProviderBadge({ provider }: { provider: MeetingProviderOption }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <Avatar sx={{ width: 28, height: 28, bgcolor: PROVIDER_COLORS[provider] ?? "action.selected" }}>
        <VideocamRoundedIcon sx={{ fontSize: 16 }} aria-hidden="true" />
      </Avatar>
      <Typography variant="body2" fontWeight={600}>
        {providerLabel(provider)}
      </Typography>
    </Stack>
  );
}
