import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApplicationStatus, JobOpportunity, applicationStatuses } from '../../models/job-opportunity';
import { JobOpportunityService } from '../../services/job-opportunity.service';

@Component({
  selector: 'app-job-detail',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './job-detail.html',
  styleUrl: './job-detail.css'
})
export class JobDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly jobService = inject(JobOpportunityService);

  protected readonly job = signal<JobOpportunity | null>(null);
  protected readonly applicationStatuses = applicationStatuses;
  protected readonly isLoading = signal(true);
  protected readonly isScoring = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly generatedMessage = signal('');
  protected readonly isGeneratingMessage = signal(false);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (!id) {
      this.errorMessage.set('Job id was not provided.');
      this.isLoading.set(false);
      return;
    }

    this.jobService.getJob(id).subscribe({
      next: (job) => {
        this.job.set(job);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load this job.');
        this.isLoading.set(false);
      }
    });
  }

  protected updateStatus(status: string): void {
    const currentJob = this.job();

    if (!currentJob) {
      return;
    }

    this.jobService.updateStatus(currentJob.id, status as ApplicationStatus).subscribe({
      next: (updatedJob) => this.job.set(updatedJob),
      error: () => this.errorMessage.set('Could not update status.')
    });
  }

  protected scoreFit(): void {
    const currentJob = this.job();

    if (!currentJob) {
      return;
    }

    this.isScoring.set(true);
    this.errorMessage.set(null);

    this.jobService.scoreFit(currentJob.id).subscribe({
      next: (fitScoreResult) => {
        this.job.set({
          ...currentJob,
          fitScore: fitScoreResult.fitScore,
          fitScoreResult
        });
        this.isScoring.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not score this job.');
        this.isScoring.set(false);
      }
    });
  }

  protected generateMessage(): void {
    const currentJob = this.job();
    if (!currentJob) {
      return;
    }

    this.isGeneratingMessage.set(true);
    this.errorMessage.set(null);

    this.jobService.generateMessage(currentJob.id).subscribe({
      next: (response) => {
        this.generatedMessage.set(response.message);
        this.isGeneratingMessage.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not generate recruiter message.');
        this.isGeneratingMessage.set(false);
      }
    });
  }

  protected copyGeneratedMessage(): void {
    const message = this.generatedMessage().trim();
    if (!message) {
      return;
    }

    navigator.clipboard.writeText(message).catch(() => {
      this.errorMessage.set('Could not copy the generated message.');
    });
  }
}
