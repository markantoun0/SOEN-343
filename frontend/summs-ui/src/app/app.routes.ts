import { Routes } from '@angular/router';
import { MapComponent } from './map/map.component';
import { MyReservationsComponent } from './reservations/my-reservations.component';

export const routes: Routes = [
  { path: 'map',              component: MapComponent },
  { path: 'my-reservations',  component: MyReservationsComponent },
  { path: '',                 redirectTo: 'map', pathMatch: 'full' },
];
