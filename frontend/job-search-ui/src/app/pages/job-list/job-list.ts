import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  CreateJobOpportunityRequest,
  JobOpportunity,
  RemoteType,
  applicationStatuses,
  remoteTypes
} from '../../models/job-opportunity';
import { JobOpportunityService } from '../../services/job-opportunity.service';

@Component({
  selector: 'app-job-list',
  imports: [FormsModule, RouterLink],
  templateUrl: './job-list.html',
  styleUrl: './job-list.css'
})
export class JobListComponent implements OnInit {
  private readonly jobService = inject(JobOpportunityService);

  protected readonly jobs = signal<JobOpportunity[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly isSaving = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly remoteTypes = remoteTypes;
  protected readonly applicationStatuses = applicationStatuses;
  protected readonly sortedJobs = computed(() =>
    [...this.jobs()].sort((left, right) => {
      const leftScore = left.fitScore ?? -1;
      const rightScore = right.fitScore ?? -1;
      return rightScore - leftScore || left.company.localeCompare(right.company);
    })
  );

  protected form: CreateJobOpportunityRequest = this.createEmptyForm();

  ngOnInit(): void {
    this.loadJobs();
  }

  protected addJob(): void {
    this.errorMessage.set(null);
    this.isSaving.set(true);

    const request: CreateJobOpportunityRequest = {
      ...this.form,
      company: this.form.company.trim(),
      title: this.form.title.trim(),
      location: this.emptyToNull(this.form.location),
      url: this.emptyToNull(this.form.url),
      description: this.emptyToNull(this.form.description),
      fitScore: this.normalizeFitScore(this.form.fitScore)
    };

    this.jobService.createJob(request).subscribe({
      next: (job) => {
        this.jobs.update((jobs) => [job, ...jobs]);
        this.form = this.createEmptyForm();
        this.isSaving.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not add this job. Check the backend and try again.');
        this.isSaving.set(false);
      }
    });
  }

  protected statusChanged(job: JobOpportunity, status: string): void {
    const nextStatus = status as JobOpportunity['status'];

    this.jobService.updateStatus(job.id, nextStatus).subscribe({
      next: (updatedJob) => {
        this.jobs.update((jobs) => jobs.map((current) => (current.id === updatedJob.id ? updatedJob : current)));
      },
      error: () => this.errorMessage.set('Could not update status. Check the backend and try again.')
    });
  }

  protected trackByJobId(index: number, job: JobOpportunity): string {
    return job.id;
  }

  private loadJobs(): void {
    this.jobService.getJobs().subscribe({
      next: (jobs) => {
        this.jobs.set(jobs);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load jobs from http://localhost:5000/api.');
        this.isLoading.set(false);
      }
    });
  }

  private createEmptyForm(): CreateJobOpportunityRequest {
    return {
      company: '',
      title: '',
      location: '',
      remoteType: 'Unknown' satisfies RemoteType,
      url: '',
      description: '',
      fitScore: null
    };
  }

  private emptyToNull(value: string | null): string | null {
    const trimmed = value?.trim();
    return trimmed ? trimmed : null;
  }

  private normalizeFitScore(value: number | null): number | null {
    if (value === null || Number.isNaN(value)) {
      return null;
    }

    return Math.min(100, Math.max(0, value));
  }
}
