import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { axe } from 'vitest-axe';
import { ExternalLoginButtons } from './ExternalLoginButtons';

describe('ExternalLoginButtons', () => {
  it('links to the backend Google challenge endpoint with returnTo encoded', () => {
    render(<ExternalLoginButtons returnTo="/become-a-tutor" />);

    expect(screen.getByRole('link', { name: 'Continue with Google' })).toHaveAttribute(
      'href',
      'http://localhost:5148/api/identity/external/google?returnTo=%2Fbecome-a-tutor',
    );
  });

  it('links to the backend Facebook challenge endpoint with returnTo encoded', () => {
    render(<ExternalLoginButtons returnTo="/tutors/jane-dev" />);

    expect(screen.getByRole('link', { name: 'Continue with Facebook' })).toHaveAttribute(
      'href',
      'http://localhost:5148/api/identity/external/facebook?returnTo=%2Ftutors%2Fjane-dev',
    );
  });

  it('has no detectable axe violations', async () => {
    const { container } = render(<ExternalLoginButtons returnTo="/" />);
    expect((await axe(container)).violations).toEqual([]);
  });
});
