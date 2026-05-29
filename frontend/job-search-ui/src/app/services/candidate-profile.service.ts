import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { CandidateProfileRequest, CandidateProfileResponse } from '../models/candidate-profile';

@Injectable({
  providedIn: 'root'
})
export class CandidateProfileService {
  private readonly baseUrl = 'http://localhost:5000/api/candidate-profile';

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<CandidateProfileResponse> {
    return this.http.get<CandidateProfileResponse>(this.baseUrl).pipe(
      map(profile => ({
        resumeText: profile.resumeText,
        remotiveCategory: profile.remotiveCategory,
        remotiveSearchText: profile.remotiveSearchText,
        remotiveLimit: profile.remotiveLimit,
        jobImportStatus: {
          workerEnabled: profile.jobImportStatus.workerEnabled,
          configuredIntervalMinutes: profile.jobImportStatus.configuredIntervalMinutes,
          lastRunStartedAt: profile.jobImportStatus.lastRunStartedAt,
          lastRunCompletedAt: profile.jobImportStatus.lastRunCompletedAt,
          lastRunSucceeded: profile.jobImportStatus.lastRunSucceeded,
          lastResult: profile.jobImportStatus.lastResult,
          lastErrorMessage: profile.jobImportStatus.lastErrorMessage,
          nextExpectedRunAt: profile.jobImportStatus.nextExpectedRunAt
        }
      }))
    );
  }

  saveProfile(profile: CandidateProfileRequest): Observable<void> {
    return this.http.put<void>(this.baseUrl, profile);
  }
}
