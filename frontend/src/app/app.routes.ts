import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/election-dashboard').then((m) => m.ElectionDashboardComponent),
  },
  {
    path: 'states/:uf',
    loadComponent: () =>
      import('./features/state-details/state-details').then((m) => m.StateDetailsComponent),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: '**', redirectTo: 'dashboard' },
];
