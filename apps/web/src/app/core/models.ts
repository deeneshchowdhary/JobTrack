export const APPLICATION_STATUSES = ['Saved', 'Applied', 'Interview', 'Offer', 'Rejected', 'Withdrawn'] as const;
export type ApplicationStatus = typeof APPLICATION_STATUSES[number];

export interface JobApplication {
  id: number; company: string; position: string; status: ApplicationStatus;
  appliedDate: string; salary: number | null; notes: string | null;
}
export interface ApplicationInput {
  company: string; position: string; status: ApplicationStatus;
  appliedDate: string; salary: number | null; notes: string | null;
}
export interface PagedResponse<T> {
  items: T[]; page: number; pageSize: number; totalItems: number; totalPages: number;
}
export interface StatusCount { status: ApplicationStatus; count: number; }
export interface AuthenticationResponse {
  accessToken: string; tokenType: string; expiresIn: number; email: string;
}
