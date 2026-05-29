import { Component, OnInit, computed, inject, signal } from '@angular/core';
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
  protected readonly remotiveCategory = signal('');
  protected readonly remotiveSearchText = signal('');
  protected readonly remotiveLimit = signal<number | null>(null);
  protected readonly jobImportStatus = signal<JobImportStatus | null>(null);
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly savedMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly remotiveLimitError = computed(() => {
    const limit = this.remotiveLimit();

    if (limit === null) {
      return null;
    }

    return Number.isInteger(limit) && limit > 0
      ? null
      : 'Limit must be a positive whole number.';
  });

  ngOnInit(): void {
    this.profileService.getProfile().subscribe({
      next: profile => {
        this.resumeText.set(profile.resumeText);
        this.remotiveCategory.set(profile.remotiveCategory ?? '');
        this.remotiveSearchText.set(profile.remotiveSearchText ?? '');
        this.remotiveLimit.set(profile.remotiveLimit);
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
    if (this.remotiveLimitError()) {
      this.errorMessage.set('Fix the Remotive limit before saving.');
      return;
    }

    this.isSaving.set(true);
    this.savedMessage.set(null);
    this.errorMessage.set(null);

    this.profileService.saveProfile({
      resumeText: this.resumeText(),
      remotiveCategory: this.toNullableText(this.remotiveCategory()),
      remotiveSearchText: this.toNullableText(this.remotiveSearchText()),
      remotiveLimit: this.remotiveLimit()
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.savedMessage.set('Profile saved. Resume and Remotive preferences will be used on the next score or import run.');
      },
      error: () => {
        this.isSaving.set(false);
        this.errorMessage.set('Failed to save profile.');
      }
    });
  }

  protected setRemotiveLimit(value: number | string | null): void {
    if (value === null || value === '') {
      this.remotiveLimit.set(null);
      return;
    }

    this.remotiveLimit.set(Number(value));
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

  private toNullableText(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length === 0 ? null : trimmed;
  }
}
