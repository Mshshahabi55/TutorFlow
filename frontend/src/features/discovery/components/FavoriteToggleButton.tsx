import { IconButton, Tooltip } from "@mui/material";
import FavoriteRoundedIcon from "@mui/icons-material/FavoriteRounded";
import FavoriteBorderRoundedIcon from "@mui/icons-material/FavoriteBorderRounded";
import { useFavoriteTutors } from "@/features/discovery/hooks/useFavoriteTutors";

export interface FavoriteToggleButtonProps {
  tutorId: string;
  size?: "small" | "medium";
}

/** A heart toggle for `useFavoriteTutors` — event.stopPropagation() so it never triggers a parent card's own click-through-to-profile handler. */
export function FavoriteToggleButton({ tutorId, size = "small" }: FavoriteToggleButtonProps) {
  const { isFavorite, toggleFavorite } = useFavoriteTutors();
  const favorited = isFavorite(tutorId);

  return (
    <Tooltip title={favorited ? "Remove from favorites" : "Add to favorites"}>
      <IconButton
        aria-label={favorited ? "Remove from favorites" : "Add to favorites"}
        aria-pressed={favorited}
        size={size}
        onClick={(event) => {
          event.preventDefault();
          event.stopPropagation();
          toggleFavorite(tutorId);
        }}
        sx={{ color: favorited ? "error.main" : "text.secondary" }}
      >
        {favorited ? <FavoriteRoundedIcon fontSize={size} /> : <FavoriteBorderRoundedIcon fontSize={size} />}
      </IconButton>
    </Tooltip>
  );
}
