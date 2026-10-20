import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/login/login.component').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'work-orders' },
      {
        path: 'work-orders',
        loadComponent: () =>
          import('./features/work-orders/work-order-list.component').then((m) => m.WorkOrderListComponent),
      },
      {
        path: 'work-orders/new',
        canActivate: [roleGuard('Admin', 'Planner')],
        loadComponent: () =>
          import('./features/work-orders/work-order-create.component').then((m) => m.WorkOrderCreateComponent),
      },
      {
        path: 'work-orders/:id',
        loadComponent: () =>
          import('./features/work-orders/work-order-detail.component').then((m) => m.WorkOrderDetailComponent),
      },
    ],
  },
  { path: '**', redirectTo: 'work-orders' },
];
