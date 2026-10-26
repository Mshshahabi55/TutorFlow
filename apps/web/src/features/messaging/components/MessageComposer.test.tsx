import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { axe } from 'vitest-axe';
import { ApiError } from '@/lib/api/client';
import { MessageComposer } from './MessageComposer';

describe('MessageComposer', () => {
  it('rejects a whitespace-only message without calling onSend', () => {
    const onSend = vi.fn();
    render(<MessageComposer isSending={false} sendError={null} onSend={onSend} />);

    fireEvent.change(screen.getByLabelText('Message'), { target: { value: '   ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Send' }));

    expect(onSend).not.toHaveBeenCalled();
    expect(screen.getByRole('alert')).toHaveTextContent('Message must not be empty.');
  });

  it('rejects a message over 5000 characters without calling onSend', () => {
    const onSend = vi.fn();
    render(<MessageComposer isSending={false} sendError={null} onSend={onSend} />);

    const tooLong = 'a'.repeat(5001);
    fireEvent.change(screen.getByLabelText('Message'), { target: { value: tooLong } });
    fireEvent.click(screen.getByRole('button', { name: 'Send' }));

    expect(onSend).not.toHaveBeenCalled();
  });

  it('trims and sends a valid message, then clears the composer', () => {
    const onSend = vi.fn();
    render(<MessageComposer isSending={false} sendError={null} onSend={onSend} />);

    const textarea = screen.getByLabelText('Message') as HTMLTextAreaElement;
    fireEvent.change(textarea, { target: { value: '  Hello tutor  ' } });
    fireEvent.click(screen.getByRole('button', { name: 'Send' }));

    expect(onSend).toHaveBeenCalledWith('Hello tutor');
    expect(textarea.value).toBe('');
  });

  it('disables the textarea and Send button while a send is pending', () => {
    render(<MessageComposer isSending sendError={null} onSend={vi.fn()} />);

    expect(screen.getByLabelText('Message')).toBeDisabled();
    expect(screen.getByRole('button', { name: 'Send' })).toBeDisabled();
  });

  it('shows an accessible server error message when the send mutation fails', () => {
    const error = new ApiError(500, { title: 'Something broke' });
    render(<MessageComposer isSending={false} sendError={error} onSend={vi.fn()} />);

    expect(screen.getByRole('alert')).toHaveTextContent('Something broke');
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<MessageComposer isSending={false} sendError={null} onSend={vi.fn()} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});
