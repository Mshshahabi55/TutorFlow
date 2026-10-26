'use client';

import { useState } from 'react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { Checkbox } from '@/components/ui/Checkbox';
import { FormField } from '@/components/ui/FormField';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { useSubjects } from '@/features/catalog/useCatalog';
import { ApiError } from '@/lib/api/client';
import { uploadCertificateDocument, withdrawCertificateDocument } from '../api';
import { CertificateEntryDraft, TutorApplication, UpdateDraftCommand, toCertificateEntryDraftInput } from '../types';
import { StepShell } from './StepShell';
import styles from './CertificationStep.module.css';

export interface CertificationStepProps {
  application: TutorApplication;
  onContinue: (patch: UpdateDraftCommand) => void;
  onBack: () => void;
  isSaving: boolean;
  error: string | null;
}

const MAX_CERTIFICATE_ENTRIES = 10;
const PRESENT_VALUE = 'present';
const ACCEPTED_DOCUMENT_TYPES = ['image/jpeg', 'image/png'];
const MAX_DOCUMENT_SIZE_BYTES = 20 * 1024 * 1024;

const EMPTY_CERTIFICATE: CertificateEntryDraft = {
  id: null,
  name: '',
  issuingOrganization: '',
  description: null,
  subjectId: null,
  startYear: null,
  endYear: null,
  hasDocument: false,
  verificationStatus: 'None',
  rejectionReason: null,
};

function buildYearOptions(): { value: string; label: string }[] {
  const currentYear = new Date().getFullYear();
  const years: { value: string; label: string }[] = [];
  for (let year = currentYear + 1; year >= 1950; year--) {
    years.push({ value: String(year), label: String(year) });
  }
  return years;
}

const START_YEAR_OPTIONS = buildYearOptions();
const END_YEAR_OPTIONS = [{ value: PRESENT_VALUE, label: 'Present' }, ...START_YEAR_OPTIONS];

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.problem.detail ?? error.message;
  }
  return "Couldn't complete that action. Please try again.";
}

/** Preply's evidenced "Teaching certification" step (step 3 of 8 —
 * docs/product/preply-reverse-engineering/evidence/Preply_Certifications). Stage 1: freeform
 * entry (Subject, Certificate name, Description, Issued by, Start/End year) — no certificate
 * catalog (no evidenced data source for one — deferred). Stage 2 adds the optional document
 * upload + admin verification lifecycle (None → PendingReview → Verified/Rejected). A null
 * `endYear` means "Present". An entry's document-upload control only appears once it has a
 * server-assigned `id` (i.e. after at least one save) — this wizard has no "save without
 * advancing" action for any step, so a brand-new entry must be saved once before a document can
 * be attached to it; this is communicated inline rather than silently hidden. */
export function CertificationStep({ application, onContinue, onBack, isSaving, error }: CertificationStepProps) {
  const [certificates, setCertificates] = useState<CertificateEntryDraft[]>(application.certificateEntries);
  // No separately persisted flag — this mirrors the same fact an empty `certificateEntries`
  // collection already represents (ADR-0026 §3 precedent: no new invariant without a real one to
  // protect). Reinitialized to `false` on every mount rather than derived from an empty list, so a
  // tutor who simply hasn't added a certificate yet isn't mistaken for one who explicitly declared
  // they have none — the two are genuinely indistinguishable once saved, and that's an accepted
  // consequence of not inventing a second persisted field for this.
  const [noCertificate, setNoCertificate] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);
  const [documentState, setDocumentState] = useState<Record<number, { busy: boolean; error: string | null }>>({});
  const { data: subjects } = useSubjects();

  const updateEntry = (index: number, patch: Partial<CertificateEntryDraft>) => {
    setCertificates((prev) => prev.map((entry, i) => (i === index ? { ...entry, ...patch } : entry)));
  };

  const setDocumentBusy = (index: number, busy: boolean, docError: string | null = null) => {
    setDocumentState((prev) => ({ ...prev, [index]: { busy, error: docError } }));
  };

  const handleNoCertificateChange = (checked: boolean) => {
    setNoCertificate(checked);
    if (checked) {
      setCertificates([]);
      setLocalError(null);
    }
  };

  const handleDocumentSelected = async (index: number, file: File | undefined) => {
    if (!file) {
      return;
    }
    const certificateId = certificates[index].id;
    if (!certificateId) {
      return;
    }

    if (!ACCEPTED_DOCUMENT_TYPES.includes(file.type)) {
      setDocumentBusy(index, false, 'Document must be a JPEG or PNG image.');
      return;
    }
    if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
      setDocumentBusy(index, false, 'Document must be 20 MB or smaller.');
      return;
    }

    setDocumentBusy(index, true);
    try {
      const result = await uploadCertificateDocument(certificateId, file);
      // Never show "Verified" from a client-side assumption — this is exactly the server's own
      // confirmed status from the upload response (always "PendingReview" on a fresh upload, per
      // the backend's own guarantee that an upload never auto-verifies).
      updateEntry(index, { verificationStatus: result.verificationStatus as CertificateEntryDraft['verificationStatus'], rejectionReason: null });
      setDocumentBusy(index, false);
    } catch (err) {
      setDocumentBusy(index, false, errorMessage(err));
    }
  };

  const handleWithdrawDocument = async (index: number) => {
    const certificateId = certificates[index].id;
    if (!certificateId) {
      return;
    }
    setDocumentBusy(index, true);
    try {
      await withdrawCertificateDocument(certificateId);
      updateEntry(index, { verificationStatus: 'None', rejectionReason: null });
      setDocumentBusy(index, false);
    } catch (err) {
      setDocumentBusy(index, false, errorMessage(err));
    }
  };

  const handleContinue = () => {
    const invalid = certificates.some(
      (entry) => entry.startYear !== null && entry.endYear !== null && entry.startYear > entry.endYear,
    );
    if (invalid) {
      setLocalError('Start year must not be after end year.');
      return;
    }
    setLocalError(null);
    onContinue({ certificateEntries: certificates.map(toCertificateEntryDraftInput) });
  };

  return (
    <StepShell
      title="Teaching certification"
      description="Do you have teaching certificates? If so, describe them to enhance your profile credibility and get more students."
      onBack={onBack}
      onContinue={handleContinue}
      isSaving={isSaving}
      error={localError ?? error}
    >
      <Checkbox checked={noCertificate} onCheckedChange={handleNoCertificateChange}>
        I don&apos;t have a teaching certificate
      </Checkbox>

      {!noCertificate && (
        <div className={styles.entries}>
          {certificates.map((entry, index) => {
            const doc = documentState[index];
            return (
              <Card key={index} className={styles.entry}>
                <FormField label="Subject">
                  {(props) => (
                    <Select
                      {...props}
                      value={entry.subjectId ?? ''}
                      onValueChange={(value) => updateEntry(index, { subjectId: value })}
                      options={(subjects ?? []).map((s) => ({ value: s.id, label: s.displayName }))}
                      placeholder="Choose subject…"
                    />
                  )}
                </FormField>
                <FormField label="Certificate">
                  {(props) => (
                    <Input {...props} value={entry.name} onChange={(e) => updateEntry(index, { name: e.target.value })} />
                  )}
                </FormField>
                <FormField label="Description" helperText="Optional">
                  {(props) => (
                    <Input
                      {...props}
                      value={entry.description ?? ''}
                      onChange={(e) => updateEntry(index, { description: e.target.value || null })}
                    />
                  )}
                </FormField>
                <FormField label="Issued by">
                  {(props) => (
                    <Input
                      {...props}
                      value={entry.issuingOrganization}
                      onChange={(e) => updateEntry(index, { issuingOrganization: e.target.value })}
                    />
                  )}
                </FormField>
                <div className={styles.yearRange}>
                  <FormField label="Start year">
                    {(props) => (
                      <Select
                        {...props}
                        value={entry.startYear !== null ? String(entry.startYear) : ''}
                        onValueChange={(value) => updateEntry(index, { startYear: Number(value) })}
                        options={START_YEAR_OPTIONS}
                        placeholder="Select"
                      />
                    )}
                  </FormField>
                  <FormField label="End year">
                    {(props) => (
                      <Select
                        {...props}
                        value={entry.endYear !== null ? String(entry.endYear) : PRESENT_VALUE}
                        onValueChange={(value) =>
                          updateEntry(index, { endYear: value === PRESENT_VALUE ? null : Number(value) })
                        }
                        options={END_YEAR_OPTIONS}
                        placeholder="Select"
                      />
                    )}
                  </FormField>
                </div>

                <div className={styles.documentSection}>
                  {!entry.id && (
                    <p className={styles.documentHint}>Save & continue, then come back to attach a document for this certificate.</p>
                  )}

                  {entry.id && entry.verificationStatus === 'Verified' && (
                    <p className={styles.verifiedBadge} role="status">
                      ✓ Certificate verified
                    </p>
                  )}

                  {entry.id && entry.verificationStatus === 'PendingReview' && (
                    <>
                      <p className={styles.pendingBanner} role="status">
                        We&apos;re reviewing your document. This may take a few days.
                      </p>
                      <Button type="button" variant="ghost" size="sm" disabled={doc?.busy} onClick={() => handleWithdrawDocument(index)}>
                        Withdraw document
                      </Button>
                    </>
                  )}

                  {entry.id && entry.verificationStatus === 'Rejected' && (
                    <p className={styles.rejectedBanner} role="alert">
                      {entry.rejectionReason ?? "This document couldn't be verified."} Upload a new document below.
                    </p>
                  )}

                  {entry.id && (entry.verificationStatus === 'None' || entry.verificationStatus === 'Rejected') && (
                    <FormField label="Upload a certificate document" helperText="Optional — JPEG or PNG, up to 20 MB">
                      {(props) => (
                        <input
                          {...props}
                          type="file"
                          accept="image/jpeg,image/png"
                          disabled={doc?.busy}
                          onChange={(e) => handleDocumentSelected(index, e.target.files?.[0])}
                        />
                      )}
                    </FormField>
                  )}

                  {doc?.busy && (
                    <p className={styles.status} role="status">
                      Uploading…
                    </p>
                  )}
                  {doc?.error && (
                    <p className={styles.uploadError} role="alert">
                      {doc.error}
                    </p>
                  )}
                </div>

                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setCertificates((prev) => prev.filter((_, i) => i !== index))}
                >
                  Remove
                </Button>
              </Card>
            );
          })}
          <Button
            type="button"
            variant="secondary"
            size="sm"
            disabled={certificates.length >= MAX_CERTIFICATE_ENTRIES}
            onClick={() => setCertificates((prev) => [...prev, { ...EMPTY_CERTIFICATE }])}
          >
            Add another certificate
          </Button>
        </div>
      )}
    </StepShell>
  );
}
