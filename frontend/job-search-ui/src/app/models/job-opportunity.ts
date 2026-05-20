export type RemoteType = 'Unknown' | 'Remote' | 'Hybrid' | 'Onsite';

export type ApplicationStatus =
  | 'Found'
  | 'Interested'
  | 'Applied'
  | 'Interviewing'
  | 'Rejected'
  | 'Archived';

export interface JobOpportunity {
  id: string;
  company: string;
  title: string;
  location: string | null;
  remoteType: RemoteType;
  url: string | null;
  description: string | null;
  status: ApplicationStatus;
  fitScore: number | null;
  dateFound: string;
  dateApplied: string | null;
}

export interface CreateJobOpportunityRequest {
  company: string;
  title: string;
  location: string | null;
  remoteType: RemoteType;
  url: string | null;
  description: string | null;
  fitScore: number | null;
}

export const remoteTypes: readonly RemoteType[] = ['Unknown', 'Remote', 'Hybrid', 'Onsite'];

export const applicationStatuses: readonly ApplicationStatus[] = [
  'Found',
  'Interested',
  'Applied',
  'Interviewing',
  'Rejected',
  'Archived'
];
