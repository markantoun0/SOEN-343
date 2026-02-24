import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <!-- Top nav bar -->
    <nav class="app-nav">
      <span class="app-brand">SUMMS</span>
    </nav>

    <!-- Menu bar (space reserved for future pages) -->
    <div class="app-menu">
      <a routerLink="/map" routerLinkActive="active">🗺 Map</a>
      <!-- Future links go here, e.g.:
      <a routerLink="/directions" routerLinkActive="active">🧭 Directions</a>
      <a routerLink="/parking"    routerLinkActive="active">🅿️ Parking</a>
      -->
    </div>

    <!-- Page content -->
    <main>
      <router-outlet />
    </main>
  `,
  styleUrl: './app.scss',
})
export class App {}
