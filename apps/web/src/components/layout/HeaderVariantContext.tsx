'use client';

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';

export type HeaderVariant = 'full' | 'minimal' | 'home';

interface HeaderVariantContextValue {
  variant: HeaderVariant;
  setVariant: (variant: HeaderVariant) => void;
}

// Defaults to "full" with a no-op setter so SiteHeader renders its normal variant when used
// outside a HeaderVariantProvider too (e.g. component tests that render it standalone).
const defaultValue: HeaderVariantContextValue = { variant: 'full', setVariant: () => {} };
const HeaderVariantContext = createContext<HeaderVariantContextValue>(defaultValue);

export function HeaderVariantProvider({ children }: { children: ReactNode }) {
  const [variant, setVariant] = useState<HeaderVariant>('full');
  const value = useMemo(() => ({ variant, setVariant }), [variant]);
  return <HeaderVariantContext.Provider value={value}>{children}</HeaderVariantContext.Provider>;
}

function useHeaderVariantContext(): HeaderVariantContextValue {
  return useContext(HeaderVariantContext);
}

export function useHeaderVariant(): HeaderVariant {
  return useHeaderVariantContext().variant;
}

/** Switches the site header to `variant` for as long as the calling component stays mounted,
 * restoring "full" on unmount — lets a focused flow (e.g. the tutor onboarding wizard) request a
 * minimal header without SiteHeader needing to know about that flow's route or domain. */
export function useSetHeaderVariant(variant: HeaderVariant): void {
  const { setVariant } = useHeaderVariantContext();
  useEffect(() => {
    setVariant(variant);
    return () => setVariant('full');
  }, [variant, setVariant]);
}
