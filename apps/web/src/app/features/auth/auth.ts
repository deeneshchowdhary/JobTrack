import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-auth',
  imports: [ReactiveFormsModule],
  templateUrl: './auth.html',
  styleUrl: './auth.scss'
})
export class Auth {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly mode = signal<'login' | 'register'>('login');
  readonly loading = signal(false);
  readonly error = signal('');
  readonly form = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]]
  });

  setMode(mode: 'login' | 'register'): void {
    this.mode.set(mode);
    this.error.set('');
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.loading.set(true);
    this.error.set('');
    const { email, password } = this.form.getRawValue();
    const request = this.mode() === 'login'
      ? this.auth.login(email, password)
      : this.auth.register(email, password);
    request.pipe(finalize(() => this.loading.set(false))).subscribe({
      next: () => void this.router.navigate(['/']),
      error: response => this.error.set(this.messageFor(response))
    });
  }

  private messageFor(response: any): string {
    const errors = response?.error?.errors;
    if (errors) return Object.values(errors).flat().join(' ');
    return response?.error?.detail ?? 'Unable to continue. Please try again.';
  }
}
