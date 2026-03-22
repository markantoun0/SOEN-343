import { Routes } from '@angular/router';
import { MapComponent } from './map/map.component';
import { MyReservationsComponent } from './reservations/my-reservations.component';
import { AdminLoginComponent } from './admin/admin-login.component';
import { AdminSignupComponent } from './admin/admin-signup.component';
import { AdminDashboardComponent } from './admin/admin-dashboard.component';
import { adminAuthGuard } from './auth/admin-auth.guard';

export const routes: Routes = [
  { path: 'map',              component: MapComponent },
  { path: 'my-reservations',  component: MyReservationsComponent },
  { path: 'admin/login',      component: AdminLoginComponent },
  { path: 'admin/signup',     component: AdminSignupComponent },
  {
    path: 'admin/dashboard',
    component: AdminDashboardComponent,
    canActivate: [adminAuthGuard]
  },
  { path: '',                 redirectTo: 'map', pathMatch: 'full' },
  { path: '**',               redirectTo: 'map' },
];
