import { Card, CardContent, Skeleton, Stack } from "@mui/material";

/** Mirrors `ConversationListItem`'s layout exactly, so resolving the Inbox never shifts the page. */
export function ConversationListItemSkeleton() {
  return (
    <Card variant="outlined" data-testid="conversation-list-item-skeleton">
      <CardContent>
        <Stack direction="row" justifyContent="space-between" alignItems="center" mb={1}>
          <Skeleton variant="text" width="55%" height={28} />
          <Skeleton variant="rounded" width={70} height={24} />
        </Stack>
        <Skeleton variant="text" width="80%" />
        <Skeleton variant="text" width="30%" />
      </CardContent>
    </Card>
  );
}
