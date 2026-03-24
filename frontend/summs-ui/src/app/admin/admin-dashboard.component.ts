import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './admin-dashboard.component.html',
  styleUrl: './admin-dashboard.component.scss'
})
export class AdminDashboardComponent implements OnInit {
  protected auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  analytics = signal<any>(null);
  loading = signal(false);
  errorMsg = signal<string | null>(null);

  ngOnInit(): void {
    this.loadAnalytics();
  }

  private loadAnalytics(): void {
    this.loading.set(true);
    this.errorMsg.set(null);

    this.http.get<any>('/api/admins/analytics')
      .subscribe({
        next: (data) => {
          this.analytics.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          console.error('Failed to load analytics', err);
          this.errorMsg.set(err?.error?.message ?? 'Failed to load analytics.');
          this.loading.set(false);
        }
      });
  }

  protected logout(): void {
    this.auth.logout();
    this.router.navigate(['/admin/login']);
  }
}
