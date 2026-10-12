import { Modal } from '@/components/ui/Modal';
import { ButtonLink } from '@/components/ui/ButtonLink';
import styles from './AuthGateModal.module.css';

export interface AuthGateModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Describes the gated action in the user's own words, e.g. "message this tutor". */
  action: string;
  /**
   * Path to return to after a successful login/signup (ADR-0023's "return to intended action"
   * flow). Optional and caller-supplied rather than read from `usePathname()` here — keeps this
   * component decoupled from routing, and existing callers that omit it still link to a plain
   * `/login`/`/signup`.
   */
  returnTo?: string;
}

/**
 * Shared, domain-agnostic gate for any action that requires a Student account (message,
 * favorite, book a trial). Links to the real `/login`/`/signup` pages (ADR-0023) rather than
 * growing its own inline form — the modal's job is "explain why, send you to the right place."
 */
export function AuthGateModal({ open, onOpenChange, action, returnTo }: AuthGateModalProps) {
  const suffix = returnTo ? `?returnTo=${encodeURIComponent(returnTo)}` : '';

  return (
    <Modal open={open} onOpenChange={onOpenChange} title="Log in to continue">
      <div className={styles.body}>
        <p>You&apos;ll need a TutorFlow account to {action}.</p>
        <div className={styles.actions}>
          <ButtonLink href={`/login${suffix}`} variant="primary">
            Log in
          </ButtonLink>
          <ButtonLink href={`/signup${suffix}`} variant="secondary">
            Sign up
          </ButtonLink>
        </div>
      </div>
    </Modal>
  );
}
