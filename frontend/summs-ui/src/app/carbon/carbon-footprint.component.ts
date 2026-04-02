﻿﻿import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../auth/auth.service';
import { CarbonFootprintService, CarbonFootprintData, UserLeaderboardEntry } from './carbon-footprint.service';

@Component({
  selector: 'app-carbon-footprint',
  standalone: true,
  imports: [CommonModule],
  host: {
    'style': 'display: block; height: 100%; overflow: hidden;'
  },
  template: `
    <div class="carbon-container">
      <h1>🌍 Carbon Footprint Tracker</h1>

      <div class="content">
        <!-- User's Carbon Stats -->
        <section class="user-stats">
          <h2>Your Carbon Impact</h2>
          @if (loading()) {
            <p class="loading">Loading your data...</p>
          } @else if (error()) {
            <p class="error">{{ error() }}</p>
          } @else if (userFootprint()) {
            <div class="stats-card">
              <div class="stat-item">
                <div class="stat-value">{{ (userFootprint()!.totalCarbonKg).toFixed(2) }}</div>
                <div class="stat-label">kg CO₂ Emitted</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ emissionsAvoidedKg().toFixed(2) }}</div>
                <div class="stat-label">kg CO₂ Emissions Avoided</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ netEmissionsKg().toFixed(2) }}</div>
                <div class="stat-label">kg CO₂ Net Emissions</div>
              </div>
              <div class="stat-item">
                <div class="stat-value">{{ userFootprint()!.tripsCompleted }}</div>
                <div class="stat-label">Trips Completed</div>
              </div>
              @if (userRank()) {
                <div class="stat-item">
                  <div class="stat-value">#{{ userRank() }}</div>
                  <div class="stat-label">Your Rank</div>
                </div>
              }
            </div>
            <div class="last-updated">
              Last updated: {{ formatDate(userFootprint()!.lastUpdated) }}
            </div>
            <div class="last-updated">
              Emissions avoided are calculated from BIXI trip distances entered in My Reservations.
            </div>
          } @else {
            <p class="no-data">No carbon footprint data yet. Start making trips to track your impact!</p>
          }
        </section>

        <!-- Leaderboard -->
        <section class="leaderboard-section">
          <h2>🏆 Carbon Leaderboard</h2>
          @if (loadingLeaderboard()) {
            <p class="loading">Loading leaderboard...</p>
          } @else if (leaderboardError()) {
            <p class="error">{{ leaderboardError() }}</p>
          } @else if (leaderboard().length > 0) {
            <div class="leaderboard-table">
              <div class="leaderboard-header">
                <div class="rank-col">Rank</div>
                <div class="name-col">User</div>
                <div class="carbon-col">Carbon (kg)</div>
                <div class="trips-col">Trips</div>
              </div>
              @for (entry of leaderboard(); track entry.userId) {
                <div class="leaderboard-row" [class.user-highlight]="entry.userId === currentUserId()">
                  <div class="rank-col">
                    @switch(entry.rank) {
                      @case(1) { <span class="medal">🥇</span> }
                      @case(2) { <span class="medal">🥈</span> }
                      @case(3) { <span class="medal">🥉</span> }
                      @default { <span class="rank-number">{{ entry.rank }}</span> }
                    }
                  </div>
                  <div class="name-col">{{ entry.userName }}</div>
                  <div class="carbon-col">{{ (entry.totalCarbonKg).toFixed(2) }}</div>
                  <div class="trips-col">{{ entry.tripsCompleted }}</div>
                </div>
              }
            </div>
          } @else {
            <p class="no-data">No leaderboard data available yet.</p>
          }
        </section>

        <!-- Carbon Tips -->
        <section class="tips-section">
          <h2>💡 Reduce Your Carbon Footprint</h2>
          <div class="tips-grid">
            <div class="tip-card">
              <div class="tip-icon">🚲</div>
              <h3>Use BIXI Bikes</h3>
              <p>Zero emissions! Perfect for short trips around the city.</p>
            </div>
            <div class="tip-card">
              <div class="tip-icon">🚌</div>
              <h3>Public Transit</h3>
              <p>Buses produce 89g CO₂/km - much lower than cars!</p>
            </div>
            <div class="tip-card">
              <div class="tip-icon">⚡</div>
              <h3>E-Scooters</h3>
              <p>Only 22g CO₂/km - a great eco-friendly option.</p>
            </div>
            <div class="tip-card">
              <div class="tip-icon">📊</div>
              <h3>Track Progress</h3>
              <p>Monitor your emissions and challenge yourself to reduce them!</p>
            </div>
          </div>
        </section>
      </div>
    </div>
  `,
  styleUrl: './carbon-footprint.component.scss',
})
export class CarbonFootprintComponent implements OnInit {
  private static readonly AVOIDED_STORAGE_PREFIX = 'carbonAvoidedByReservation:user:';

  private auth = inject(AuthService);
  private carbonService = inject(CarbonFootprintService);

  protected userFootprint = signal<CarbonFootprintData | null>(null);
  protected leaderboard = signal<UserLeaderboardEntry[]>([]);
  protected currentUserId = signal<number | null>(null);
  protected userRank = signal<number | null>(null);
  protected emissionsAvoidedKg = signal(0);

  protected loading = signal(true);
  protected loadingLeaderboard = signal(true);
  protected error = signal<string | null>(null);
  protected leaderboardError = signal<string | null>(null);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (user) {
      this.currentUserId.set(user.id);
      this.loadAvoidedEmissions(user.id);
      this.loadUserFootprint(user.id);
      this.loadUserRank(user.id);
    } else {
      this.loading.set(false);
    }

    this.loadLeaderboard();
  }

  private loadUserFootprint(userId: number): void {
    this.carbonService.getUserCarbonFootprint(userId).subscribe({
      next: (response) => {
        if (response.success) {
          this.userFootprint.set(response.data);
        }
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load carbon footprint', err);
        this.error.set('Failed to load your carbon footprint data.');
        this.loading.set(false);
      },
    });
  }

  private loadLeaderboard(): void {
    this.carbonService.getLeaderboard(10).subscribe({
      next: (response) => {
        if (response.success) {
          this.leaderboard.set(response.data);
        }
        this.loadingLeaderboard.set(false);
      },
      error: (err) => {
        console.error('Failed to load leaderboard', err);
        this.leaderboardError.set('Failed to load leaderboard.');
        this.loadingLeaderboard.set(false);
      },
    });
  }

  private loadUserRank(userId: number): void {
    this.carbonService.getUserRank(userId).subscribe({
      next: (response) => {
        if (response.success) {
          this.userRank.set(response.data.rank);
        }
      },
      error: (err) => {
        console.error('Failed to load user rank', err);
      },
    });
  }

  protected formatDate(dateString: string): string {
    return new Date(dateString).toLocaleString('en-CA', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  protected netEmissionsKg(): number {
    const totalEmitted = this.userFootprint()?.totalCarbonKg ?? 0;
    return totalEmitted - this.emissionsAvoidedKg();
  }

  private loadAvoidedEmissions(userId: number): void {
    const key = `${CarbonFootprintComponent.AVOIDED_STORAGE_PREFIX}${userId}`;

    try {
      const parsed = JSON.parse(localStorage.getItem(key) ?? '{}') as Record<string, unknown>;
      const totalAvoided = Object.values(parsed)
        .map((value) => Number(value))
        .filter((value) => Number.isFinite(value) && value >= 0)
        .reduce((sum, value) => sum + value, 0);

      this.emissionsAvoidedKg.set(totalAvoided);
    } catch {
      this.emissionsAvoidedKg.set(0);
    }
  }
}


