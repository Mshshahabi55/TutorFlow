import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { axe } from 'vitest-axe';
import { ConversationCard } from './ConversationCard';
import { ConversationSummary } from '../types';

const BASE: ConversationSummary = {
  id: 'conv-1',
  studentUserId: 'student-1',
  tutorUserId: 'tutor-1',
  createdAtUtc: '2026-01-01T00:00:00.000Z',
  lastMessageAtUtc: '2026-01-14T10:00:00.000Z',
  myLastReadAtUtc: null,
  tutor: { displayName: 'Jane Dev', profilePhotoReference: null, publicSlug: 'jane-dev' },
};

describe('ConversationCard', () => {
  it('shows the real Tutor display name when the viewer is the Student side', () => {
    render(<ConversationCard conversation={BASE} currentUserId="student-1" />);
    expect(screen.getByText('Jane Dev')).toBeInTheDocument();
  });

  it('shows the generic "Student" label, never a real name, when the viewer is the Tutor side', () => {
    render(<ConversationCard conversation={BASE} currentUserId="tutor-1" />);
    expect(screen.getByText('Student')).toBeInTheDocument();
    expect(screen.queryByText('Jane Dev')).not.toBeInTheDocument();
  });

  it("falls back to a generic 'Tutor' label when the Tutor summary is unresolvable", () => {
    render(<ConversationCard conversation={{ ...BASE, tutor: null }} currentUserId="student-1" />);
    expect(screen.getByText('Tutor')).toBeInTheDocument();
  });

  it('links to the conversation thread', () => {
    render(<ConversationCard conversation={BASE} currentUserId="student-1" />);
    expect(screen.getByRole('link')).toHaveAttribute('href', '/messages/conv-1');
  });

  it('shows "No messages yet" when the conversation has no activity', () => {
    render(<ConversationCard conversation={{ ...BASE, lastMessageAtUtc: null }} currentUserId="student-1" />);
    expect(screen.getByText('No messages yet')).toBeInTheDocument();
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<ConversationCard conversation={BASE} currentUserId="student-1" />);
    expect((await axe(container)).violations).toEqual([]);
  });
});
