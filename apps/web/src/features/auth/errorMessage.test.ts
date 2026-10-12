import { describe, expect, it } from 'vitest';
import { ApiError } from '@/lib/api/client';
import { getAuthErrorMessage } from './errorMessage';

describe('getAuthErrorMessage', () => {
  it('joins the flat string-array shape used by Identity business failures', () => {
    const error = new ApiError(401, { errors: ['Invalid email or password.'] });
    expect(getAuthErrorMessage(error)).toBe('Invalid email or password.');
  });

  it('flattens the field-keyed object shape used by FluentValidation failures', () => {
    const error = new ApiError(400, { errors: { Email: ['must not be empty'], Password: ['too short'] } });
    expect(getAuthErrorMessage(error)).toBe('must not be empty too short');
  });

  it('falls back to detail, then a generic message, never a raw status code', () => {
    expect(getAuthErrorMessage(new ApiError(500, { detail: 'Database unreachable' }))).toBe('Database unreachable');
    expect(getAuthErrorMessage(new ApiError(500, {}))).toBe('Something went wrong. Please try again.');
    expect(getAuthErrorMessage(new Error('network down'))).toBe('Something went wrong. Please try again.');
  });
});
