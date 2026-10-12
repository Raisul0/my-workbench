import { Routes } from '@angular/router';
import { DashboardPageComponent } from './dashboard/dashboard-page';

export const routes: Routes = [
  {
    path: '',
    component: DashboardPageComponent,
    title: 'Dashboard',
  },
  {
    path: 'invoices',
    loadComponent: () =>
      import('./modules/invoices/pages/home/invoices-home.component').then(
        (m) => m.InvoicesPageComponent,
      ),
    title: 'Invoices',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
