import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';

type PreferredCity = 'montreal' | 'laval';
type PreferredMobilityType = 'bixi' | 'parking';

interface PreferencesResponse {
  success: boolean;
  preferences?: {
    preferredCity?: PreferredCity;
    preferredMobilityType?: PreferredMobilityType;
  };
  message?: string;
}

@Component({
  selector: 'app-preferences',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './preferences.component.html',
  styleUrl: './preferences.component.scss',
})
export class PreferencesComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly http = inject(HttpClient);

  protected preferredCity: PreferredCity = 'montreal';
  protected preferredMobilityType: PreferredMobilityType = 'bixi';
  protected loading = signal(true);
  protected saving = signal(false);
  protected error = signal<string | null>(null);
  protected success = signal<string | null>(null);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (!user) {
      this.loading.set(false);
      this.error.set('Please sign in to manage preferences.');
      return;
    }

    this.http
      .get<PreferencesResponse>(`/api/users/${user.id}/preferences`)
      .subscribe({
        next: (res) => {
          const city = res.preferences?.preferredCity;
          if (city === 'montreal' || city === 'laval') {
            this.preferredCity = city;
          }
          const mobilityType = res.preferences?.preferredMobilityType;
          if (mobilityType === 'bixi' || mobilityType === 'parking') {
            this.preferredMobilityType = mobilityType;
          }
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load your preferences.');
          this.loading.set(false);
        }
      });
  }

  protected savePreferences(): void {
    const user = this.auth.currentUser();
    if (!user) {
      this.error.set('Please sign in to save preferences.');
      return;
    }

    this.error.set(null);
    this.success.set(null);
    this.saving.set(true);

    this.http
      .patch<PreferencesResponse>(`/api/users/${user.id}/preferences`, {
        preferredCity: this.preferredCity,
        preferredMobilityType: this.preferredMobilityType
      })
      .subscribe({
        next: (res) => {
          if (!res.success) {
            this.error.set(res.message ?? 'Unable to save preferences.');
            this.saving.set(false);
            return;
          }

          this.auth.updateCurrentUserPreferences(this.preferredCity, this.preferredMobilityType);
          this.success.set('Your preferences were saved.');
          this.saving.set(false);
        },
        error: (err) => {
          this.error.set(err?.error?.message ?? 'Unable to save preferences.');
          this.saving.set(false);
        }
      });
  }
}
