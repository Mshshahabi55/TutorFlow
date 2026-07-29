import { isNotFoundError } from "@/services/api/errorClassification";
import { ErrorState } from "@/shared/components/feedback/ErrorState";
import { UnavailableState } from "@/shared/components/feedback/UnavailableState";
import { useAuth } from "@/shared/hooks/useAuth";
import { paths } from "@/routes/paths";

export interface IdentityLookupErrorStateProps {
  error: unknown;
  onRetry: () => void;
  onChooseAgain: () => void;
}

/**
 * The recovery UI for any `useOwnId`-driven view (a Dashboard, My Lessons,
 * My Students, Manage Your Schedule) whose id fails to load. A real signed-in
 * user's id comes from their own session, never something they entered, so
 * a 404 for them means "your profile itself couldn't load" (Retry / Go
 * Home / Contact Support) — never "choose a different id," which only
 * makes sense for the dev-only "Acting as" preview's remembered-id
 * fallback (`onChooseAgain`, unreachable once really signed in).
 */
export function IdentityLookupErrorState({ error, onRetry, onChooseAgain }: IdentityLookupErrorStateProps) {
  const { isAuthenticated } = useAuth();

  if (!isNotFoundError(error)) {
    return <ErrorState error={error} onRetry={onRetry} />;
  }

  if (isAuthenticated) {
    return (
      <UnavailableState
        title="We couldn't load your profile"
        description="Something went wrong loading your account. Please try again."
        actions={[
          { label: "Retry", onClick: onRetry },
          { label: "Go to Home", to: paths.home, variant: "contained" },
          { label: "Contact Support", to: paths.messages.inbox },
        ]}
        headingComponent="h2"
      />
    );
  }

  return (
    <UnavailableState
      title="We couldn't find your saved profile"
      description="Please choose it again."
      actions={[{ label: "Choose again", onClick: onChooseAgain, variant: "contained" }]}
      headingComponent="h2"
    />
  );
}
