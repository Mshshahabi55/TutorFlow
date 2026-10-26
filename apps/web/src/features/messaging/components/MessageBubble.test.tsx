import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { axe } from 'vitest-axe';
import { MessageBubble } from './MessageBubble';
import { MessageDto } from '../types';

const MESSAGE: MessageDto = {
  id: 'm1',
  senderUserId: 'other-user',
  body: 'Hello there',
  createdAtUtc: '2026-01-14T10:00:00.000Z',
  sequenceNumber: 1,
};

describe('MessageBubble', () => {
  it("renders the other party's message aligned as 'other'", () => {
    render(<MessageBubble message={MESSAGE} isOwnMessage={false} />);
    expect(screen.getByText('Hello there')).toBeInTheDocument();
  });

  it("renders the caller's own message distinctly from the other party's", () => {
    const { container: otherContainer } = render(<MessageBubble message={MESSAGE} isOwnMessage={false} />);
    const { container: ownContainer } = render(<MessageBubble message={{ ...MESSAGE, id: 'm2' }} isOwnMessage />);

    const otherClassName = otherContainer.firstElementChild?.className ?? '';
    const ownClassName = ownContainer.firstElementChild?.className ?? '';
    expect(otherClassName).not.toBe(ownClassName);
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<MessageBubble message={MESSAGE} isOwnMessage={false} />);
    expect((await axe(container)).violations).toEqual([]);
  });
});
