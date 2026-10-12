/**
 * `ProfilePhotoReference`/`IntroVideoReference` are opaque strings on the Tutors domain — no
 * object-storage/media abstraction exists anywhere in this codebase yet (confirmed: no
 * IFileStorage/IMediaStorage/S3/MinIO abstraction, no provisioned object-storage service). This
 * is a real, pre-existing gap this feature surfaces but does not solve (out of scope — see the
 * completion report's "known limitations"). Until a resolver exists, treat the reference as
 * usable only when it already looks like a servable absolute URL; otherwise render nothing
 * rather than a broken image/video.
 */
export function resolveMediaUrl(reference: string | null | undefined): string | null {
  if (!reference) {
    return null;
  }
  return /^https?:\/\//i.test(reference) ? reference : null;
}
