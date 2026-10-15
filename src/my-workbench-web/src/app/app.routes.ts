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
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./modules/invoices/pages/home/invoices-home.component').then(
            (m) => m.InvoicesPageComponent,
          ),
        title: 'Invoices',
      },
      {
        path: 'templates/:id/design',
        loadComponent: () =>
          import('./modules/invoices/pages/invoices-template-design/invoices-template-design.component').then(
            (m) => m.InvoicesTemplateDesignComponent,
          ),
        title: 'Design Template',
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];

