import { Suspense } from 'react';
import { LoginPageContent } from './LoginPageContent';

export const metadata = { title: 'Log in | TutorFlow' };

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginPageContent />
    </Suspense>
  );
}
