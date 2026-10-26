import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { CertificationStep } from './CertificationStep';
import { CertificateEntryDraft, TutorApplication } from '../types';

const mockUseSubjects = vi.fn();
vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => mockUseSubjects(),
}));

const mockUploadCertificateDocument = vi.fn();
const mockWithdrawCertificateDocument = vi.fn();
vi.mock('../api', () => ({
  uploadCertificateDocument: (...args: unknown[]) => mockUploadCertificateDocument(...args),
  withdrawCertificateDocument: (...args: unknown[]) => mockWithdrawCertificateDocument(...args),
}));

const SUBJECTS = [
  { id: 'subject-english', code: 'en', displayName: 'English' },
  { id: 'subject-spanish', code: 'es', displayName: 'Spanish' },
];

const baseApplication = {
  certificateEntries: [],
} as unknown as TutorApplication;

function entry(overrides: Partial<CertificateEntryDraft> = {}): CertificateEntryDraft {
  return {
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
    ...overrides,
  };
}

describe('CertificationStep', () => {
  beforeEach(() => {
    mockUseSubjects.mockReturnValue({ data: SUBJECTS, isLoading: false });
    mockUploadCertificateDocument.mockReset();
    mockWithdrawCertificateDocument.mockReset();
  });

  it('is entirely optional — Continue is enabled with no entries at all', () => {
    render(<CertificationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
    expect(screen.queryByLabelText('Certificate')).not.toBeInTheDocument();
  });

  it('"Add another certificate" adds one empty, editable entry', () => {
    render(<CertificationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add another certificate' }));

    expect(screen.getByLabelText('Certificate')).toHaveValue('');
    expect(screen.getByLabelText('Issued by')).toHaveValue('');
    expect(screen.getByLabelText('Description')).toHaveValue('');
  });

  it('a brand-new (unsaved) entry shows a hint instead of the document upload control', () => {
    render(<CertificationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add another certificate' }));

    expect(screen.getByText(/Save & continue, then come back/i)).toBeInTheDocument();
    expect(screen.queryByLabelText('Upload a certificate document')).not.toBeInTheDocument();
  });

  it('filling in a new certificate entry and continuing strips server-controlled fields from the payload', () => {
    const onContinue = vi.fn();
    render(<CertificationStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Add another certificate' }));
    fireEvent.change(screen.getByLabelText('Certificate'), { target: { value: 'TEFL' } });
    fireEvent.change(screen.getByLabelText('Issued by'), { target: { value: 'ITA' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    // Only the user-editable fields — id/hasDocument/verificationStatus/rejectionReason must
    // never be part of what this component sends, even though the backend would ignore them.
    expect(onContinue).toHaveBeenCalledWith({
      certificateEntries: [
        {
          id: null,
          name: 'TEFL',
          issuingOrganization: 'ITA',
          description: null,
          subjectId: null,
          startYear: null,
          endYear: null,
        },
      ],
    });
  });

  it('"Remove" deletes that specific entry, not all entries', () => {
    const application = {
      certificateEntries: [entry({ id: 'cert-a', name: 'Certificate A', issuingOrganization: 'Org A' }), entry({ id: 'cert-b', name: 'Certificate B', issuingOrganization: 'Org B' })],
    } as unknown as TutorApplication;
    render(<CertificationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getAllByRole('button', { name: 'Remove' })[0]);

    expect(screen.queryByDisplayValue('Certificate A')).not.toBeInTheDocument();
    expect(screen.getByDisplayValue('Certificate B')).toBeInTheDocument();
  });

  it('pre-fills existing certificate entries', () => {
    const application = {
      certificateEntries: [entry({ id: 'cert-a', name: 'TEFL', issuingOrganization: 'ITA', startYear: 2020 })],
    } as unknown as TutorApplication;
    render(<CertificationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByDisplayValue('TEFL')).toBeInTheDocument();
    expect(screen.getByText('2020')).toBeInTheDocument();
    expect(screen.getByText('Present')).toBeInTheDocument();
  });

  it('caps at 10 entries — "Add another certificate" disables at the limit', () => {
    // Rendering 10 cards, each with two 78-option year Selects, is slow in jsdom — a real
    // render-cost artifact of this test's scale, not a hang.
    const tenEntries = Array.from({ length: 10 }, (_, i) => entry({ id: `cert-${i}`, name: `Cert ${i}`, issuingOrganization: 'Org' }));
    const application = { certificateEntries: tenEntries } as unknown as TutorApplication;
    render(<CertificationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Add another certificate' })).toBeDisabled();
  }, 20000);

  it('checking "I don\'t have a teaching certificate" hides and clears the entry list', () => {
    const application = {
      certificateEntries: [entry({ id: 'cert-a', name: 'TEFL', issuingOrganization: 'ITA' })],
    } as unknown as TutorApplication;
    const onContinue = vi.fn();
    render(<CertificationStep application={application} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('checkbox', { name: /I don.t have a teaching certificate/i }));

    expect(screen.queryByDisplayValue('TEFL')).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Add another certificate' })).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));
    expect(onContinue).toHaveBeenCalledWith({ certificateEntries: [] });
  });

  it('unchecking "I don\'t have a teaching certificate" restores an empty, editable form — not the previous entries', () => {
    const application = {
      certificateEntries: [entry({ id: 'cert-a', name: 'TEFL', issuingOrganization: 'ITA' })],
    } as unknown as TutorApplication;
    render(<CertificationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    const checkbox = screen.getByRole('checkbox', { name: /I don.t have a teaching certificate/i });
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    expect(screen.queryByDisplayValue('TEFL')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add another certificate' })).toBeInTheDocument();
  });

  it('rejects a start year after the end year before calling onContinue', () => {
    const application = {
      certificateEntries: [entry({ id: 'cert-a', name: 'TEFL', issuingOrganization: 'ITA', startYear: 2024, endYear: 2020 })],
    } as unknown as TutorApplication;
    const onContinue = vi.fn();
    render(<CertificationStep application={application} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).not.toHaveBeenCalled();
    expect(screen.getByText('Start year must not be after end year.')).toBeInTheDocument();
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<CertificationStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('surfaces a save error from the parent', () => {
    render(<CertificationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Couldn't save." />);

    expect(screen.getByText("Couldn't save.")).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <CertificationStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });

  describe('Stage 2 — document upload lifecycle', () => {
    function renderWithSavedEntry(overrides: Partial<ReturnType<typeof entry>> = {}) {
      const application = {
        certificateEntries: [entry({ id: 'cert-a', name: 'TEFL', issuingOrganization: 'ITA', ...overrides })],
      } as unknown as TutorApplication;
      return render(<CertificationStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);
    }

    it('a saved entry with no document shows the upload control', () => {
      renderWithSavedEntry();
      expect(screen.getByLabelText('Upload a certificate document')).toBeInTheDocument();
    });

    it('selecting a valid file uploads it and shows the PendingReview banner from the server response', async () => {
      mockUploadCertificateDocument.mockResolvedValue({ certificateId: 'cert-a', verificationStatus: 'PendingReview' });
      renderWithSavedEntry();
      const file = new File(['x'], 'cert.jpg', { type: 'image/jpeg' });

      fireEvent.change(screen.getByLabelText('Upload a certificate document'), { target: { files: [file] } });

      await waitFor(() => expect(screen.getByText(/reviewing your document/i)).toBeInTheDocument());
      expect(mockUploadCertificateDocument).toHaveBeenCalledWith('cert-a', file);
    });

    it('rejects an invalid file type client-side without calling the API', () => {
      renderWithSavedEntry();
      const file = new File(['x'], 'cert.gif', { type: 'image/gif' });

      fireEvent.change(screen.getByLabelText('Upload a certificate document'), { target: { files: [file] } });

      expect(screen.getByText('Document must be a JPEG or PNG image.')).toBeInTheDocument();
      expect(mockUploadCertificateDocument).not.toHaveBeenCalled();
    });

    it('rejects an oversized file client-side without calling the API', () => {
      renderWithSavedEntry();
      const big = new File([new Uint8Array(20 * 1024 * 1024 + 1)], 'cert.jpg', { type: 'image/jpeg' });

      fireEvent.change(screen.getByLabelText('Upload a certificate document'), { target: { files: [big] } });

      expect(screen.getByText('Document must be 20 MB or smaller.')).toBeInTheDocument();
      expect(mockUploadCertificateDocument).not.toHaveBeenCalled();
    });

    it('shows the verified badge only when the loaded entry\'s own verificationStatus is Verified — never from the upload response alone', () => {
      renderWithSavedEntry({ verificationStatus: 'Verified' });

      expect(screen.getByText('✓ Certificate verified')).toBeInTheDocument();
      // No upload control once verified — a verified document was already deleted server-side;
      // editing details (not re-uploading) is the only way off Verified.
      expect(screen.queryByLabelText('Upload a certificate document')).not.toBeInTheDocument();
    });

    it('shows the rejection reason and an upload control to resubmit when Rejected', () => {
      renderWithSavedEntry({ verificationStatus: 'Rejected', rejectionReason: 'Document is illegible.' });

      expect(screen.getByText(/Document is illegible\./)).toBeInTheDocument();
      expect(screen.getByLabelText('Upload a certificate document')).toBeInTheDocument();
    });

    it('withdrawing a pending document calls the API and resets the entry to None', async () => {
      mockWithdrawCertificateDocument.mockResolvedValue(undefined);
      renderWithSavedEntry({ verificationStatus: 'PendingReview' });

      fireEvent.click(screen.getByRole('button', { name: 'Withdraw document' }));

      await waitFor(() => expect(mockWithdrawCertificateDocument).toHaveBeenCalledWith('cert-a'));
      await waitFor(() => expect(screen.getByLabelText('Upload a certificate document')).toBeInTheDocument());
    });

    it('surfaces an upload API failure without crashing', async () => {
      mockUploadCertificateDocument.mockRejectedValue(new Error('network down'));
      renderWithSavedEntry();
      const file = new File(['x'], 'cert.jpg', { type: 'image/jpeg' });

      fireEvent.change(screen.getByLabelText('Upload a certificate document'), { target: { files: [file] } });

      await waitFor(() => expect(screen.getByText("Couldn't complete that action. Please try again.")).toBeInTheDocument());
    });
  });
});
