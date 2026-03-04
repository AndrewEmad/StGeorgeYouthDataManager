export interface User {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  photoPath?: string | null;
}
