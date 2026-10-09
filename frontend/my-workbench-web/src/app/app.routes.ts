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
      import('./modules/invoices/invoices-page/invoices-page').then(
        (m) => m.InvoicesPageComponent,
      ),
    title: 'Invoices',
  },
  {
    path: '**',
    redirectTo: '',
  },
];
