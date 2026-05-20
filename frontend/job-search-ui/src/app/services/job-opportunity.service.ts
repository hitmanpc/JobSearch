import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApplicationStatus, CreateJobOpportunityRequest, FitScoreResult, JobOpportunity } from '../models/job-opportunity';

@Injectable({
  providedIn: 'root'
})
export class JobOpportunityService {
  private readonly baseUrl = 'http://localhost:5000/api/jobs';

  constructor(private readonly http: HttpClient) {}

  getJobs(): Observable<JobOpportunity[]> {
    return this.http.get<JobOpportunity[]>(this.baseUrl);
  }

  getJob(id: string): Observable<JobOpportunity> {
    return this.http.get<JobOpportunity>(`${this.baseUrl}/${id}`);
  }

  createJob(request: CreateJobOpportunityRequest): Observable<JobOpportunity> {
    return this.http.post<JobOpportunity>(this.baseUrl, request);
  }

  updateStatus(id: string, status: ApplicationStatus): Observable<JobOpportunity> {
    return this.http.patch<JobOpportunity>(`${this.baseUrl}/${id}/status`, { status });
  }

  scoreFit(id: string): Observable<FitScoreResult> {
    return this.http.post<FitScoreResult>(`${this.baseUrl}/${id}/score`, {});
  }
}
