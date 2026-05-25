import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CandidateProfile } from '../models/candidate-profile';

@Injectable({
  providedIn: 'root'
})
export class CandidateProfileService {
  private readonly baseUrl = 'http://localhost:5000/api/candidate-profile';

  constructor(private readonly http: HttpClient) {}

  getProfile(): Observable<CandidateProfile> {
    return this.http.get<CandidateProfile>(this.baseUrl);
  }

  saveProfile(profile: CandidateProfile): Observable<void> {
    return this.http.put<void>(this.baseUrl, profile);
  }
}
