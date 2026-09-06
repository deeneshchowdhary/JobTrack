import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { tap } from 'rxjs';
import { AuthenticationResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly session = signal<AuthenticationResponse | null>(null);
  readonly token = computed(() => this.session()?.accessToken ?? null);
  readonly email = computed(() => this.session()?.email ?? '');
  readonly isAuthenticated = computed(() => this.session() !== null);

  register(email: string, password: string) {
    return this.http.post<AuthenticationResponse>('/api/auth/register', { email, password })
      .pipe(tap(response => this.session.set(response)));
  }
  login(email: string, password: string) {
    return this.http.post<AuthenticationResponse>('/api/auth/login', { email, password })
      .pipe(tap(response => this.session.set(response)));
  }
  logout(): void { this.session.set(null); }
}
