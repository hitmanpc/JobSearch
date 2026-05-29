export interface CandidateProfileRequest {
  resumeText: string;
}

export interface CandidateProfileResponse {
  resumeText: string;
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
