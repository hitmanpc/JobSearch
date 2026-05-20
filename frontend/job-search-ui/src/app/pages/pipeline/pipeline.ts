import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ApplicationStatus, JobOpportunity, applicationStatuses } from '../../models/job-opportunity';
import { JobOpportunityService } from '../../services/job-opportunity.service';

interface PipelineColumn {
  status: ApplicationStatus;
  jobs: JobOpportunity[];
}

@Component({
  selector: 'app-pipeline',
  imports: [RouterLink],
  templateUrl: './pipeline.html',
  styleUrl: './pipeline.css'
})
export class PipelineComponent implements OnInit {
  private readonly jobService = inject(JobOpportunityService);

  protected readonly jobs = signal<JobOpportunity[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly columns = computed<PipelineColumn[]>(() =>
    applicationStatuses.map((status) => ({
      status,
      jobs: this.jobs().filter((job) => job.status === status)
    }))
  );

  ngOnInit(): void {
    this.jobService.getJobs().subscribe({
      next: (jobs) => {
        this.jobs.set(jobs);
        this.isLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load the pipeline.');
        this.isLoading.set(false);
      }
    });
  }
}
