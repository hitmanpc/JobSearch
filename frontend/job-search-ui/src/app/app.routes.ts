import { Routes } from '@angular/router';
import { JobDetailComponent } from './pages/job-detail/job-detail';
import { JobListComponent } from './pages/job-list/job-list';
import { PipelineComponent } from './pages/pipeline/pipeline';

export const routes: Routes = [
  { path: '', component: JobListComponent, title: 'Jobs' },
  { path: 'jobs/:id', component: JobDetailComponent, title: 'Job detail' },
  { path: 'pipeline', component: PipelineComponent, title: 'Pipeline' },
  { path: '**', redirectTo: '' }
];
