import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./guest/landing/landing-view.component').then((m) => m.LandingViewComponent),
  },
  {
    path: 'post/:id',
    loadComponent: () =>
      import('./guest/post-detail/post-detail-view.component').then(
        (m) => m.PostDetailViewComponent
      ),
  },
  {
    path: 'admin-auth',
    loadComponent: () =>
      import('./admin/admin-auth/admin-auth.component').then((m) => m.AdminAuthComponent),
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./admin/admin-layout/admin-layout.component').then((m) => m.AdminLayoutComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
