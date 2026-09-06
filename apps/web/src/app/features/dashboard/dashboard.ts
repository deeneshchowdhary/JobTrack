import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { JobApplicationsService } from '../../core/job-applications.service';
import { APPLICATION_STATUSES, ApplicationInput, ApplicationStatus, JobApplication, StatusCount } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [ReactiveFormsModule, CurrencyPipe, DatePipe],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
  private readonly api = inject(JobApplicationsService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);

  readonly statuses = APPLICATION_STATUSES;
  readonly email = this.auth.email;
  readonly applications = signal<JobApplication[]>([]);
  readonly counts = signal<StatusCount[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly view = signal<'board' | 'list'>('board');
  readonly page = signal(1);
  readonly totalPages = signal(0);
  readonly totalItems = signal(0);
  readonly search = signal('');
  readonly statusFilter = signal('');
  readonly sortBy = signal('appliedDate');
  readonly sortDirection = signal('desc');
  readonly editorOpen = signal(false);
  readonly editing = signal<JobApplication | null>(null);
  readonly selected = signal<JobApplication | null>(null);
  readonly responseRate = computed(() => {
    const total = this.totalItems();
    if (!total) return 0;
    const unanswered = this.countFor('Saved') + this.countFor('Applied');
    return Math.round(((total - unanswered) / total) * 100);
  });

  readonly form = this.fb.group({
    company: ['', [Validators.required, Validators.maxLength(150)]],
    position: ['', [Validators.required, Validators.maxLength(150)]],
    status: ['Applied' as ApplicationStatus, Validators.required],
    appliedDate: [this.today(), Validators.required],
    salary: [null as number | null, Validators.min(0)],
    notes: ['', Validators.maxLength(1000)]
  });

  constructor() { this.load(); }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      page: this.api.list({
        page: this.page(), pageSize: this.view() === 'board' ? 100 : 10,
        search: this.search(), status: this.statusFilter(),
        sortBy: this.sortBy(), sortDirection: this.sortDirection()
      }),
      counts: this.api.dashboard()
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: result => {
        this.applications.set(result.page.items);
        this.totalItems.set(result.page.totalItems);
        this.totalPages.set(result.page.totalPages);
        this.counts.set(result.counts);
      },
      error: () => this.error.set('We could not load your applications. Try again.')
    });
  }

  countFor(status: string): number {
    return this.counts().find(item => item.status === status)?.count ?? 0;
  }
  applicationsFor(status: string): JobApplication[] {
    return this.applications().filter(item => item.status === status);
  }
  setView(view: 'board' | 'list'): void { this.view.set(view); this.page.set(1); this.load(); }
  applyFilters(): void { this.page.set(1); this.load(); }
  clearFilters(): void { this.search.set(''); this.statusFilter.set(''); this.page.set(1); this.load(); }
  goToPage(page: number): void { this.page.set(page); this.load(); }

  openCreate(): void {
    this.editing.set(null);
    this.form.reset({ company: '', position: '', status: 'Applied', appliedDate: this.today(), salary: null, notes: '' });
    this.editorOpen.set(true);
  }
  openEdit(application: JobApplication): void {
    this.selected.set(null);
    this.editing.set(application);
    this.form.reset({
      company: application.company, position: application.position,
      status: application.status, appliedDate: application.appliedDate.slice(0, 10),
      salary: application.salary, notes: application.notes ?? ''
    });
    this.editorOpen.set(true);
  }
  closeEditor(): void { this.editorOpen.set(false); this.editing.set(null); }

  save(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const value = this.form.getRawValue();
    const input: ApplicationInput = {
      company: value.company!, position: value.position!, status: value.status!,
      appliedDate: new Date(`${value.appliedDate}T00:00:00.000Z`).toISOString(),
      salary: value.salary,
      notes: value.notes?.trim() || null
    };
    this.saving.set(true);
    this.error.set('');
    const current = this.editing();
    const request = current ? this.api.update(current.id, input) : this.api.create(input);
    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: () => { this.closeEditor(); this.load(); },
      error: response => this.error.set(response?.error?.detail ?? 'Unable to save this application.')
    });
  }

  move(application: JobApplication, status: ApplicationStatus): void {
    if (application.status === status) return;
    this.api.update(application.id, this.toInput(application, status)).subscribe({
      next: () => this.load(), error: () => this.error.set('Unable to update the status.')
    });
  }
  remove(application: JobApplication): void {
    if (!confirm(`Delete the ${application.position} application at ${application.company}?`)) return;
    this.api.delete(application.id).subscribe({
      next: () => { this.selected.set(null); this.load(); },
      error: () => this.error.set('Unable to delete this application.')
    });
  }
  logout(): void { this.auth.logout(); void this.router.navigate(['/auth']); }
  trackById(_: number, item: JobApplication): number { return item.id; }

  private toInput(application: JobApplication, status: ApplicationStatus): ApplicationInput {
    return { company: application.company, position: application.position, status,
      appliedDate: application.appliedDate, salary: application.salary, notes: application.notes };
  }
  private today(): string { return new Date().toISOString().slice(0, 10); }
}
