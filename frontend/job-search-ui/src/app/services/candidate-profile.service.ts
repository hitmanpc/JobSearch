import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CandidateProfileRequest, CandidateProfileResponse } from '../models/candidate-profile';

@Injectable({
  providedIn: 'root'
})
export class CandidateProfileService {
  private readonly baseUrl = 'http://localhost:5000/api/candidate-profile';

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<CandidateProfileResponse> {
    return this.http.get<CandidateProfileResponse>(this.baseUrl);
  }

  saveProfile(profile: CandidateProfileRequest): Observable<void> {
    return this.http.put<void>(this.baseUrl, profile);
  }
}
