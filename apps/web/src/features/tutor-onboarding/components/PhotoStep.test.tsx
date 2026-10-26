import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { describe, expect, it, vi, beforeEach } from 'vitest';
import { axe } from 'vitest-axe';
import { PhotoStep } from './PhotoStep';
import { TutorApplication } from '../types';

const mockUploadTutorPhoto = vi.fn();
vi.mock('../api', () => ({
  uploadTutorPhoto: (...args: unknown[]) => mockUploadTutorPhoto(...args),
}));

const mockUseSubjects = vi.fn();
const mockUseLanguages = vi.fn();
vi.mock('@/features/catalog/useCatalog', () => ({
  useSubjects: () => mockUseSubjects(),
  useLanguages: () => mockUseLanguages(),
}));

const baseApplication = {
  displayName: 'Ada Lovelace',
  headline: null,
  profilePhotoReference: null,
  countryOfBirthCode: null,
  subjectIds: [],
  spokenLanguages: [],
} as unknown as TutorApplication;

function selectFile(input: HTMLElement, file: File) {
  fireEvent.change(input, { target: { files: [file] } });
}

function getFileInput(container: HTMLElement) {
  return container.querySelector('input[type="file"]') as HTMLInputElement;
}

describe('PhotoStep', () => {
  beforeEach(() => {
    mockUploadTutorPhoto.mockReset();
    mockUseSubjects.mockReturnValue({ data: [] });
    mockUseLanguages.mockReturnValue({ data: [] });
    // jsdom doesn't implement these — stub them so the component's optimistic local-preview
    // path (URL.createObjectURL) doesn't throw "Not implemented" during tests.
    vi.stubGlobal('URL', {
      ...URL,
      createObjectURL: vi.fn(() => 'blob:mock-preview-url'),
      revokeObjectURL: vi.fn(),
    });
  });

  it('disables Continue until a photo is set', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeDisabled();
  });

  it('shows "Upload new photo" as the primary surface for a fresh step, with the URL field collapsed', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: /upload new photo/i })).toBeInTheDocument();
    expect(screen.queryByLabelText('Photo URL')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: /paste a link to an already-hosted photo/i })).toBeInTheDocument();
  });

  // Regression for a real, confirmed interaction defect: the control used to be a <label htmlFor>
  // wrapping a visually-hidden <input type="file">. File inputs carry an implicit "button" ARIA
  // role, so anything resolving the control by accessible role+name (assistive tech, automated
  // tests, some browser extensions) could resolve to the *input* rather than the *label* — and a
  // direct, realistic click aimed at that resolved element fails, because the label visually sits
  // on top of the (clip-rect-hidden) input and intercepts the pointer event. Confirmed live via
  // Playwright: a role-based click targeting the input timed out; a click on the label text
  // succeeded. The fix makes a real <button> the sole interactive/focusable control, which
  // programmatically triggers the now fully inert (aria-hidden, tabIndex=-1) input via ref — this
  // test proves that mechanism actually fires.
  it('clicking the Upload new photo button (by role, the realistic way a user or assistive tech activates it) triggers the file input', () => {
    const clickSpy = vi.spyOn(HTMLInputElement.prototype, 'click').mockImplementation(() => {});
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Upload new photo' }));

    expect(clickSpy).toHaveBeenCalledTimes(1);
    clickSpy.mockRestore();
  });

  it('the file input is not independently focusable/announced — the button is the sole accessible control', () => {
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const input = getFileInput(container);
    expect(input).toHaveAttribute('aria-hidden', 'true');
    expect(input).toHaveAttribute('tabindex', '-1');
  });

  it('reveals the URL field when the fallback toggle is clicked, and lets a pasted URL enable Continue', () => {
    const onContinue = vi.fn();
    render(<PhotoStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: /paste a link to an already-hosted photo/i }));
    fireEvent.change(screen.getByLabelText('Photo URL'), { target: { value: 'https://example.com/photo.jpg' } });
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));

    expect(onContinue).toHaveBeenCalledWith({ profilePhotoReference: 'https://example.com/photo.jpg' });
  });

  it('starts with the URL field already expanded when a photo reference is already saved', () => {
    const application = { ...baseApplication, profilePhotoReference: 'https://example.com/photo.jpg' };
    render(<PhotoStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByLabelText('Photo URL')).toHaveValue('https://example.com/photo.jpg');
  });

  it('shows "Change photo" on the upload surface once a photo reference exists', () => {
    const application = { ...baseApplication, profilePhotoReference: 'https://example.com/photo.jpg' };
    render(<PhotoStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('button', { name: /change photo/i })).toBeInTheDocument();
  });

  it('uploading a valid JPEG calls uploadTutorPhoto, fills the URL from the response, and shows a success message', async () => {
    mockUploadTutorPhoto.mockResolvedValue({ url: 'https://api.tutorflow.local/uploads/tutor-photos/abc.jpg' });
    const onContinue = vi.fn();
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const file = new File(['fake-bytes'], 'photo.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), file);

    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledWith(file));
    expect(await screen.findByText('✓ Photo updated')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled());

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));
    expect(onContinue).toHaveBeenCalledWith({
      profilePhotoReference: 'https://api.tutorflow.local/uploads/tutor-photos/abc.jpg',
    });
  });

  it('creates an instant local preview of the selected file before the upload resolves, and revokes it once settled', async () => {
    let resolveUpload!: (value: { url: string }) => void;
    mockUploadTutorPhoto.mockReturnValue(new Promise((resolve) => (resolveUpload = resolve)));
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const file = new File(['fake-bytes'], 'photo.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), file);

    // Created synchronously on selection — before the server round-trip even starts — which is
    // exactly what makes the preview update instantly rather than waiting for the response.
    // (Radix's Avatar.Image never reaches its "loaded" state in jsdom — no real image decode —
    // so asserting on a rendered <img> isn't reliable here; this asserts the same guarantee at
    // the data-flow level instead.)
    expect(URL.createObjectURL).toHaveBeenCalledWith(file);
    expect(URL.revokeObjectURL).not.toHaveBeenCalled();

    resolveUpload({ url: 'https://api.tutorflow.local/uploads/tutor-photos/abc.jpg' });
    await screen.findByText('✓ Photo updated');
    await waitFor(() => expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:mock-preview-url'));
  });

  it('REGRESSION: a different second file after a successful upload is accepted without a refresh', async () => {
    mockUploadTutorPhoto
      .mockResolvedValueOnce({ url: 'https://api.tutorflow.local/uploads/tutor-photos/first.jpg' })
      .mockResolvedValueOnce({ url: 'https://api.tutorflow.local/uploads/tutor-photos/second.jpg' });
    const onContinue = vi.fn();
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const first = new File(['fake-bytes-1'], 'first.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), first);
    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledWith(first));
    await screen.findByText('✓ Photo updated');

    const second = new File(['fake-bytes-2'], 'second.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), second);
    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledWith(second));
    expect(mockUploadTutorPhoto).toHaveBeenCalledTimes(2);

    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));
    expect(onContinue).toHaveBeenCalledWith({
      profilePhotoReference: 'https://api.tutorflow.local/uploads/tutor-photos/second.jpg',
    });
  });

  it('REGRESSION: selecting the exact same file again after a successful upload re-triggers the upload', async () => {
    mockUploadTutorPhoto.mockResolvedValue({ url: 'https://api.tutorflow.local/uploads/tutor-photos/same.jpg' });
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const file = new File(['fake-bytes'], 'same.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), file);
    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledTimes(1));
    await screen.findByText('✓ Photo updated');

    selectFile(getFileInput(container), file);
    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledTimes(2));
  });

  it('REGRESSION: a failed replacement upload keeps the previous photo usable', async () => {
    mockUploadTutorPhoto
      .mockResolvedValueOnce({ url: 'https://api.tutorflow.local/uploads/tutor-photos/good.jpg' })
      .mockRejectedValueOnce(new Error('network error'));
    const onContinue = vi.fn();
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={onContinue} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const good = new File(['fake-bytes-1'], 'good.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), good);
    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledWith(good));
    await screen.findByText('✓ Photo updated');

    const bad = new File(['fake-bytes-2'], 'bad.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), bad);
    expect(await screen.findByText("Couldn't upload your photo. Please try again.")).toBeInTheDocument();

    // Continue is still enabled, and still saves the earlier, successfully-uploaded URL — the
    // failed replacement never overwrote it.
    expect(screen.getByRole('button', { name: 'Save & continue' })).toBeEnabled();
    fireEvent.click(screen.getByRole('button', { name: 'Save & continue' }));
    expect(onContinue).toHaveBeenCalledWith({
      profilePhotoReference: 'https://api.tutorflow.local/uploads/tutor-photos/good.jpg',
    });
  });

  it('dropping a valid file on the dropzone uploads it too', async () => {
    mockUploadTutorPhoto.mockResolvedValue({ url: 'https://api.tutorflow.local/uploads/tutor-photos/dropped.jpg' });
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    const file = new File(['fake-bytes'], 'dropped.jpg', { type: 'image/jpeg' });
    const dropzone = screen.getByRole('button', { name: 'Upload new photo' });
    expect(dropzone).not.toBeNull();

    fireEvent.drop(dropzone as HTMLElement, { dataTransfer: { files: [file] } });

    await waitFor(() => expect(mockUploadTutorPhoto).toHaveBeenCalledWith(file));
  });

  it('rejects an unsupported file type client-side, without calling the upload API', async () => {
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const file = new File(['fake-bytes'], 'document.pdf', { type: 'application/pdf' });
    selectFile(getFileInput(container), file);

    expect(await screen.findByText('Photo must be a JPEG, PNG, or WebP image.')).toBeInTheDocument();
    expect(mockUploadTutorPhoto).not.toHaveBeenCalled();
  });

  it('rejects an oversized file client-side, without calling the upload API', async () => {
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const oversized = new File([new Uint8Array(5 * 1024 * 1024 + 1)], 'big.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), oversized);

    expect(await screen.findByText('Photo must be 5 MB or smaller.')).toBeInTheDocument();
    expect(mockUploadTutorPhoto).not.toHaveBeenCalled();
  });

  it('shows the tutor\'s country flag, subjects, and languages in the preview card when available', () => {
    mockUseSubjects.mockReturnValue({ data: [{ id: 's1', code: 'en', displayName: 'English' }] });
    mockUseLanguages.mockReturnValue({ data: [{ code: 'en', displayName: 'English' }] });
    const application = {
      ...baseApplication,
      countryOfBirthCode: 'FR',
      subjectIds: ['s1'],
      spokenLanguages: [{ languageCode: 'en', proficiency: 'Native' as const }],
    } as unknown as TutorApplication;

    render(<PhotoStep application={application} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByRole('img', { name: 'France' })).toBeInTheDocument();
    expect(screen.getByText('Teaches English')).toBeInTheDocument();
    expect(screen.getByText('Speaks English (Native)')).toBeInTheDocument();
  });

  it('omits subjects/languages/flag gracefully when not yet available, without fabricating them', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.queryByText(/Teaches/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Speaks/)).not.toBeInTheDocument();
  });

  it('shows the subheading copy evidenced by the live Preply Photo step', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByText('Choose a photo that will help learners get to know you.')).toBeInTheDocument();
  });

  it('shows exactly four "What your photo needs" example photos with no captions underneath them', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    expect(screen.getByText('What your photo needs')).toBeInTheDocument();
    expect(screen.getAllByRole('img', { name: /^Example/ })).toHaveLength(4);
    expect(screen.queryByText('Face forward, centered')).not.toBeInTheDocument();
    expect(screen.queryByText('Head & shoulders framed')).not.toBeInTheDocument();
    expect(screen.queryByText('Natural, friendly smile')).not.toBeInTheDocument();
    expect(screen.queryByText('Clean, simple background')).not.toBeInTheDocument();
  });

  it('shows the exact seven-item guidance checklist, in order, matching the live Preply Photo step', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />);

    const items = screen.getAllByRole('listitem').map((item) => item.textContent);
    const expected = [
      'You should be facing forward',
      'Frame your head and shoulders',
      'You should be centered and upright',
      'Your face and eyes should be visible (except for religious reasons)',
      'You should be the only person in the photo',
      'Use a color photo with high resolution and no filters',
      'Avoid logos or contact information',
    ];
    for (const tip of expected) {
      expect(items).toContain(tip);
    }
    const tipIndexes = expected.map((tip) => items.indexOf(tip));
    expect(tipIndexes).toEqual([...tipIndexes].sort((a, b) => a - b));
  });

  it('surfaces an upload failure from the server', async () => {
    mockUploadTutorPhoto.mockRejectedValue(new Error('network error'));
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );

    const file = new File(['fake-bytes'], 'photo.jpg', { type: 'image/jpeg' });
    selectFile(getFileInput(container), file);

    expect(await screen.findByText("Couldn't upload your photo. Please try again.")).toBeInTheDocument();
  });

  it('calls onBack when Back is clicked', () => {
    const onBack = vi.fn();
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={onBack} isSaving={false} error={null} />);

    fireEvent.click(screen.getByRole('button', { name: 'Back' }));

    expect(onBack).toHaveBeenCalledTimes(1);
  });

  it('surfaces a save error from the parent', () => {
    render(<PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error="Couldn't save." />);

    expect(screen.getByText("Couldn't save.")).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(
      <PhotoStep application={baseApplication} onContinue={vi.fn()} onBack={vi.fn()} isSaving={false} error={null} />,
    );
    expect((await axe(container)).violations).toEqual([]);
  });
});
