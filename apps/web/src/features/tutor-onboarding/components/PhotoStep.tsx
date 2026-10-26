'use client';

import { useEffect, useRef, useState, type DragEvent } from 'react';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Avatar } from '@/components/ui/Avatar';
import { FlagIcon } from '@/components/ui/FlagIcon';
import { useLanguages, useSubjects } from '@/features/catalog/useCatalog';
import { uploadTutorPhoto } from '../api';
import { TutorApplication, UpdateDraftCommand } from '../types';
import { StepShell } from './StepShell';
import styles from './PhotoStep.module.css';

export interface PhotoStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const ACCEPTED_TYPES = ['image/jpeg', 'image/png', 'image/webp'];
const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024;

// Exact wording and order from the live Preply "What your photo needs" checklist
// (docs/product/preply-reverse-engineering/evidence/Preply_Photo/Screenshot 2026-10-19 083953.png).
const GUIDANCE_TIPS = [
  'You should be facing forward',
  'Frame your head and shoulders',
  'You should be centered and upright',
  'Your face and eyes should be visible (except for religious reasons)',
  'You should be the only person in the photo',
  'Use a color photo with high resolution and no filters',
  'Avoid logos or contact information',
];

/** Real royalty-free portrait photos (Unsplash License — free for commercial use, no permission
 * required: https://unsplash.com/license; not Unsplash+/paid content), downloaded locally into
 * `public/images/photo-guidance/` so they work offline and in production without hotlinking a
 * third party. Four good examples, no "avoid" counter-examples — matches the real Preply
 * wizard's own "What your photo needs" grid exactly (four good reference photos, no bad ones,
 * no captions underneath them —
 * docs/product/preply-reverse-engineering/evidence/Preply_Photo/Screenshot 2026-10-19
 * 083953.png). Attribution: `public/images/photo-guidance/CREDITS.md`. */
const EXAMPLES: { src: string; alt: string }[] = [
  {
    src: '/images/photo-guidance/good-face-forward.jpg',
    alt: 'Example: a man facing the camera directly, centered in frame',
  },
  {
    src: '/images/photo-guidance/good-head-shoulders.jpg',
    alt: 'Example: a woman smiling, head and shoulders framed in the shot',
  },
  {
    src: '/images/photo-guidance/good-smiling-woman.jpg',
    alt: 'Example: a woman with a warm, natural smile facing the camera',
  },
  {
    src: '/images/photo-guidance/good-professional-man.jpg',
    alt: 'Example: a man photographed against a plain, uncluttered background',
  },
];

/** Real upload: the dropzone posts to `POST /api/tutors/me/photo` (JPEG/PNG/WebP, max 5 MB —
 * matches the server's own validation, re-checked client-side first so an obviously-bad file
 * never leaves the browser), which returns an absolute URL saved via the existing draft-save
 * path exactly like a pasted URL always was — `profilePhotoReference` itself is still just an
 * opaque string (ADR-0026 §6 unchanged). The URL field is a collapsed, secondary fallback for a
 * photo that's already hosted elsewhere. The preview card follows Preply's own information
 * hierarchy (photo, name, country flag, subjects, languages —
 * docs/product/preply-reverse-engineering/18-screenshot-evidence.md, SS02), using only fields
 * already on the application; in TutorFlow's current step order (`about → basics → photo →
 * teaching → …`), Subject and Languages are captured on the About step, before Photo, so they're
 * normally already present here — still omitted rather than fabricated on the rare path where
 * they aren't (e.g. a tutor jumping directly to this step via the progress nav). */
export function PhotoStep({ application, onContinue, onBack, isSaving, error }: PhotoStepProps) {
  const [photoUrl, setPhotoUrl] = useState(application.profilePhotoReference ?? '');
  const [previewObjectUrl, setPreviewObjectUrl] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadSucceeded, setUploadSucceeded] = useState(false);
  const [isDraggingOver, setIsDraggingOver] = useState(false);
  // Collapsed by default so device upload reads as the primary action for a fresh step — but
  // expanded from the start if a reference is already saved, so existing data stays visible
  // rather than hidden behind an extra click.
  const [showUrlField, setShowUrlField] = useState(Boolean(application.profilePhotoReference));
  // Bumped after every completed attempt (success or failure) to force a fresh <input> element —
  // strictly more robust than resetting `.value` alone, which some browsers don't reliably honor
  // for re-selecting the same file. This, not the value reset, is what actually lets a user pick
  // another photo (or the same one again) right after an upload without refreshing the page.
  const [inputResetKey, setInputResetKey] = useState(0);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data: subjects } = useSubjects();
  const { data: languages } = useLanguages();

  // Revokes the object URL once it's no longer the active preview (replaced by the next
  // selection, or cleared after an upload settles) — also on unmount, in case the component
  // unmounts mid-upload before the `finally` block below gets a chance to clear it itself.
  useEffect(() => {
    if (!previewObjectUrl) {
      return;
    }
    return () => URL.revokeObjectURL(previewObjectUrl);
  }, [previewObjectUrl]);

  const resolvedPhotoUrl = /^https?:\/\//i.test(photoUrl) ? photoUrl : null;
  const avatarSrc = previewObjectUrl ?? resolvedPhotoUrl;

  const countryCode = application.countryOfBirthCode;

  const subjectNames = application.subjectIds
    .map((id) => subjects?.find((s) => s.id === id)?.displayName)
    .filter((name): name is string => Boolean(name));
  const subjectsLine = subjectNames.length > 0 ? `Teaches ${subjectNames.join(', ')}` : null;

  const languageParts = application.spokenLanguages
    .map((spoken) => {
      const name = languages?.find((l) => l.code === spoken.languageCode)?.displayName;
      return name ? `${name} (${spoken.proficiency})` : null;
    })
    .filter((part): part is string => Boolean(part));
  const languagesLine = languageParts.length > 0 ? `Speaks ${languageParts.join(', ')}` : null;

  const handleFile = async (file: File | undefined) => {
    setUploadError(null);
    setUploadSucceeded(false);
    if (!file) {
      return;
    }

    if (!ACCEPTED_TYPES.includes(file.type)) {
      setUploadError('Photo must be a JPEG, PNG, or WebP image.');
      return;
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
      setUploadError('Photo must be 5 MB or smaller.');
      return;
    }

    // Instant local feedback — the chosen file renders immediately, before the network request
    // even starts. The previously-saved photo (`photoUrl`) is left untouched underneath it, so
    // it's still what's used if this upload fails.
    setPreviewObjectUrl(URL.createObjectURL(file));

    setIsUploading(true);
    try {
      const { url } = await uploadTutorPhoto(file);
      setPhotoUrl(url);
      setUploadSucceeded(true);
    } catch {
      setUploadError("Couldn't upload your photo. Please try again.");
    } finally {
      setIsUploading(false);
      setPreviewObjectUrl(null);
      setInputResetKey((key) => key + 1);
    }
  };

  const handleDragOver = (event: DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    if (!isUploading) {
      setIsDraggingOver(true);
    }
  };

  const handleDrop = (event: DragEvent<HTMLButtonElement>) => {
    event.preventDefault();
    setIsDraggingOver(false);
    if (!isUploading) {
      void handleFile(event.dataTransfer.files?.[0]);
    }
  };

  return (
    <StepShell
      title="Profile photo"
      description="Choose a photo that will help learners get to know you."
      onBack={onBack}
      onContinue={() => onContinue({ profilePhotoReference: photoUrl })}
      continueDisabled={!photoUrl.trim() || isUploading}
      isSaving={isSaving}
      error={error}
    >
      <div className={styles.previewCard}>
        <Avatar src={avatarSrc} name={application.displayName ?? '?'} size="xl" shape="square" />
        <div className={styles.previewInfo}>
          <div className={styles.previewNameRow}>
            <span className={styles.previewName}>{application.displayName || 'Your name'}</span>
            {countryCode && <FlagIcon code={countryCode} />}
          </div>
          {application.headline && <p className={styles.previewHeadline}>{application.headline}</p>}
          {subjectsLine && <p className={styles.previewMeta}>{subjectsLine}</p>}
          {languagesLine && <p className={styles.previewMeta}>{languagesLine}</p>}
        </div>
      </div>

      {/* Plain outlined button, not a dashed dropzone box — matches the real Preply wizard's
          "Upload new photo" control (same evidence as above). A real <button> that programmatically
          triggers the hidden file input via ref, not a <label htmlFor> wrapper — the file input's
          own native implicit "button" ARIA role made the label+input pair ambiguous for anything
          that resolves an element by accessible role/name rather than relying on native
          label-click forwarding (confirmed live: a role-based click aimed at the input itself
          fails, because the label visually sits on top of it; a click on the label text succeeds).
          The button is now unambiguously the one interactive, focusable control; the input is
          purely a programmatic target (aria-hidden, not tab-reachable). Drag-and-drop still works
          — the button still carries the drag handlers. */}
      <button
        type="button"
        className={styles.uploadButton}
        data-dragging={isDraggingOver || undefined}
        data-disabled={isUploading || undefined}
        disabled={isUploading}
        onClick={() => fileInputRef.current?.click()}
        onDragOver={handleDragOver}
        onDragLeave={() => setIsDraggingOver(false)}
        onDrop={handleDrop}
      >
        {avatarSrc ? 'Change photo' : 'Upload new photo'}
      </button>
      <input
        key={inputResetKey}
        ref={fileInputRef}
        className="visually-hidden"
        type="file"
        accept="image/jpeg,image/png,image/webp"
        tabIndex={-1}
        aria-hidden="true"
        disabled={isUploading}
        onChange={(event) => handleFile(event.target.files?.[0])}
      />
      <p className={styles.uploadHint}>or drag and drop — JPEG, PNG, or WebP, up to 5 MB</p>

      {isUploading && (
        <p className={styles.status} role="status">
          Uploading…
        </p>
      )}
      {!isUploading && uploadSucceeded && (
        <p className={styles.success} role="status">
          ✓ Photo updated
        </p>
      )}
      {uploadError && (
        <p className={styles.uploadError} role="alert">
          {uploadError}
        </p>
      )}

      <div className={styles.guide}>
        <p className={styles.guideTitle}>What your photo needs</p>
        <ul className={styles.exampleGrid}>
          {EXAMPLES.map((example) => (
            <li key={example.src} className={styles.exampleTile}>
              <div className={styles.exampleIllustration}>
                <img src={example.src} alt={example.alt} className={styles.exampleImage} />
              </div>
            </li>
          ))}
        </ul>
        <ul className={styles.guideList}>
          {GUIDANCE_TIPS.map((tip) => (
            <li key={tip}>{tip}</li>
          ))}
        </ul>
      </div>

      <div className={styles.urlFallback}>
        <button type="button" className={styles.urlToggle} onClick={() => setShowUrlField((prev) => !prev)}>
          {showUrlField ? 'Hide link field' : 'Or paste a link to an already-hosted photo instead'}
        </button>
        {showUrlField && (
          <FormField
            label="Photo URL"
            helperText="If your photo is already online somewhere, paste the link here instead of uploading."
          >
            {(props) => (
              <Input
                {...props}
                type="url"
                placeholder="https://…"
                value={photoUrl}
                onChange={(event) => {
                  setPhotoUrl(event.target.value);
                  setUploadSucceeded(false);
                }}
              />
            )}
          </FormField>
        )}
      </div>
    </StepShell>
  );
}
