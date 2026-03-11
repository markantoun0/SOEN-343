import { Component, inject } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './auth/auth.service';
import { AuthDialogComponent } from './auth/auth-dialog/auth-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, CommonModule, AuthDialogComponent],
  template: `
    <!-- Top nav bar -->
    <nav class="app-nav">
      <span class="app-brand">SUMMS</span>

      <!-- Right-side auth area -->
      <div class="nav-auth">
        @if (auth.currentUser(); as user) {
          <!-- Logged-in: show name initial + dropdown -->
          <div class="user-menu" (click)="toggleUserMenu()" #userMenuTrigger>
            <span class="user-avatar">{{ user.name.charAt(0).toUpperCase() }}</span>
            <span class="user-name">{{ user.name }}</span>
            <span class="chevron">▾</span>
          </div>
          @if (menuOpen) {
            <div class="user-dropdown">
              <a routerLink="/my-reservations" class="dropdown-item" (click)="menuOpen = false">
                🗓️ My Reservations
              </a>
              <button class="dropdown-item danger" (click)="logout()">
                🚪 Sign Out
              </button>
            </div>
          }
        } @else {
          <!-- Logged-out: show login button -->
          <button class="login-btn" (click)="auth.openDialog()">
            👤 Sign In
          </button>
        }
      </div>
    </nav>

    <!-- Menu bar -->
    <div class="app-menu">
      <a routerLink="/map" routerLinkActive="active">🗺 Map</a>
      @if (auth.currentUser()) {
        <a routerLink="/my-reservations" routerLinkActive="active">🗓 My Reservations</a>
      }
    </div>

    <!-- Page content -->
    <main (click)="closeMenuOnOutside($event)">
      <router-outlet />
    </main>

    <!-- Auth dialog overlay -->
    @if (auth.showDialog()) {
      <app-auth-dialog />
    }
  `,
  styleUrl: './app.scss',
})
export class App {
  protected auth = inject(AuthService);
  protected menuOpen = false;

  protected toggleUserMenu(): void {
    this.menuOpen = !this.menuOpen;
  }

  protected logout(): void {
    this.auth.logout();
    this.menuOpen = false;
  }

  protected closeMenuOnOutside(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.user-menu') && !target.closest('.user-dropdown')) {
      this.menuOpen = false;
    }
  }
}
