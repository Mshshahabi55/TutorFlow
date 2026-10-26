import { describe, expect, it } from 'vitest';
import { sanitizeReturnTo } from './returnTo';

describe('sanitizeReturnTo', () => {
  it('passes through a normal same-origin path', () => {
    expect(sanitizeReturnTo('/tutors/jane-dev')).toBe('/tutors/jane-dev');
  });

  it('passes through a same-origin path with a query string', () => {
    expect(sanitizeReturnTo('/messages/conv-1?tab=open')).toBe('/messages/conv-1?tab=open');
  });

  it('falls back to "/" for null', () => {
    expect(sanitizeReturnTo(null)).toBe('/');
  });

  it('falls back to "/" for an empty string', () => {
    expect(sanitizeReturnTo('')).toBe('/');
  });

  it('rejects an absolute external URL (open-redirect attempt)', () => {
    expect(sanitizeReturnTo('https://evil.example.com')).toBe('/');
  });

  it('rejects a scheme-relative path missing only the scheme', () => {
    expect(sanitizeReturnTo('evil.example.com/path')).toBe('/');
  });

  it('rejects a protocol-relative URL (open-redirect attempt)', () => {
    expect(sanitizeReturnTo('//evil.example.com')).toBe('/');
  });

  it('rejects a backslash-prefixed URL some browsers normalize to protocol-relative', () => {
    expect(sanitizeReturnTo('/\\evil.example.com')).toBe('/');
  });

  it('rejects a javascript: pseudo-URL', () => {
    expect(sanitizeReturnTo('javascript:alert(1)')).toBe('/');
  });
});
