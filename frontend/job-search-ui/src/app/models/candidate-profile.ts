export interface CandidateProfileRequest {
  resumeText: string;
  remotiveCategory: string | null;
  remotiveSearchText: string | null;
  remotiveLimit: number | null;
}

export interface CandidateProfileResponse {
  resumeText: string;
  remotiveCategory: string | null;
  remotiveSearchText: string | null;
  remotiveLimit: number | null;
  jobImportStatus: JobImportStatus;
}

export interface JobImportStatus {
  workerEnabled: boolean;
  configuredIntervalMinutes: number;
  lastRunStartedAt: string | null;
  lastRunCompletedAt: string | null;
  lastRunSucceeded: boolean | null;
  lastResult: string;
  lastErrorMessage: string | null;
  nextExpectedRunAt: string | null;
}
