export type AuthRole = 'Student' | 'Tutor' | 'Admin';

export interface Session {
  userId: string;
  email: string;
  roles: AuthRole[];
}

export interface RegisterInput {
  email: string;
  password: string;
}

export interface LoginInput {
  email: string;
  password: string;
}
