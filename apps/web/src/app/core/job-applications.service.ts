import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { ApplicationInput, JobApplication, PagedResponse, StatusCount } from './models';

export interface ApplicationQuery {
  page: number; pageSize: number; search?: string; status?: string;
  sortBy: string; sortDirection: string;
}
@Injectable({ providedIn: 'root' })
export class JobApplicationsService {
  private readonly http = inject(HttpClient);
  private readonly url = '/api/JobApplications';
  list(query: ApplicationQuery) {
    let params = new HttpParams().set('page', query.page).set('pageSize', query.pageSize)
      .set('sortBy', query.sortBy).set('sortDirection', query.sortDirection);
    if (query.search) params = params.set('search', query.search);
    if (query.status) params = params.set('status', query.status);
    return this.http.get<PagedResponse<JobApplication>>(this.url, { params });
  }
  dashboard() { return this.http.get<StatusCount[]>(`${this.url}/dashboard`); }
  create(input: ApplicationInput) { return this.http.post<JobApplication>(this.url, input); }
  update(id: number, input: ApplicationInput) { return this.http.put<JobApplication>(`${this.url}/${id}`, input); }
  delete(id: number) { return this.http.delete<void>(`${this.url}/${id}`); }
}
