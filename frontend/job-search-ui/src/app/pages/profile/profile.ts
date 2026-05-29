import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { JobImportStatus } from '../../models/candidate-profile';
import { CandidateProfileService } from '../../services/candidate-profile.service';

@Component({
  selector: 'app-profile',
  imports: [FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css'
})
export class ProfileComponent implements OnInit {
  private readonly profileService = inject(CandidateProfileService);

  protected readonly resumeText = signal('');
  protected readonly jobImportStatus = signal<JobImportStatus | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly savedMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    this.profileService.getProfile().subscribe({
      next: profile => {
        this.resumeText.set(profile.resumeText);
        this.jobImportStatus.set(profile.jobImportStatus);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load profile.');
        this.isLoading.set(false);
      }
    });
  }

  protected save(): void {
    this.isSaving.set(true);
    this.savedMessage.set(null);
    this.errorMessage.set(null);

    this.profileService.saveProfile({ resumeText: this.resumeText() }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.savedMessage.set('Resume saved. It will be used on the next score call.');
      },
      error: () => {
        this.isSaving.set(false);
        this.errorMessage.set('Failed to save resume.');
      }
    });
  }

  protected formatDateTime(value: string | null): string {
    if (!value) {
      return 'Not available';
    }

    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
      return value;
    }

    return new Intl.DateTimeFormat(undefined, {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(parsed);
  }

  protected formatResult(status: JobImportStatus): string {
    return status.lastResult || 'Not available';
  }

  protected formatErrorMessage(status: JobImportStatus): string {
    return status.lastErrorMessage || 'None';
  }
}
