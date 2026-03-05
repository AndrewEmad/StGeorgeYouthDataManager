/**
 * Response from login endpoint.
 */
export interface LoginResponse {
  token: string;
  refreshToken: string;
  fullName: string;
  role: string;
  userId: string;
  requiresPasswordChange?: boolean;
  requiresProfileCompletion?: boolean;
}

/**
 * User profile from Profile API.
 */
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

/**
 * Credentials for login.
 */
export interface LoginCredentials {
  username: string;
  password: string;
}

/**
 * Payload for profile update.
 */
export interface UpdateProfileDto {
  fullName?: string;
  email?: string;
  phone?: string;
  address?: string;
}

/**
 * Response from profile update (may include new token).
 */
export interface UpdateProfileResponse {
  token?: string;
}
