export interface LoginResponse {
  token: string;
  refreshToken: string;
  fullName: string;
  role: string;
  userId: string;
  requiresPasswordChange?: boolean;
  requiresProfileCompletion?: boolean;
}

export interface ProfileDto {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  phone: string;
  address?: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}
