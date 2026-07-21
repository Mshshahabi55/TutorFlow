import { Box, Button, Typography } from "@mui/material";
import { Link as RouterLink } from "react-router-dom";
import { paths } from "@/routes/paths";

export function NotFoundPage() {
  return (
    <Box display="flex" flexDirection="column" alignItems="flex-start" gap={2} py={6}>
      <Typography variant="h4">Page not found</Typography>
      <Typography variant="body1" color="text.secondary">
        The page you're looking for doesn't exist.
      </Typography>
      <Button component={RouterLink} to={paths.home} variant="contained">
        Back to Home
      </Button>
    </Box>
  );
}
