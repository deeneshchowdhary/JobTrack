import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth.guard';

export const routes: Routes = [
  { path: 'auth', canActivate: [guestGuard], loadComponent: () => import('./features/auth/auth').then(m => m.Auth) },
  { path: '', canActivate: [authGuard], loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },
  { path: '**', redirectTo: '' }
];
