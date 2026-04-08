import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';

export interface ReservationSummary {
  id: number;
  city: string;
  type: string;
  startDate: string;
  endDate: string;
  reservationTime: string;
  status?: string | number;
  isDeleted?: boolean;
  mobilityLocation?: {
    name: string;
    type: string;
    city: string;
  };
}

@Component({
  selector: 'app-my-reservations',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-reservations.component.html',
  styleUrl: './my-reservations.component.scss',
})
export class MyReservationsComponent implements OnInit {
  protected auth = inject(AuthService);
  private http   = inject(HttpClient);

  protected reservations = signal<ReservationSummary[]>([]);
  protected loading      = signal(true);
  protected error        = signal<string | null>(null);

  protected activeReservations = signal<ReservationSummary[]>([]);
  protected expiredReservations = signal<ReservationSummary[]>([]);

  ngOnInit(): void {
    const user = this.auth.currentUser();
    if (!user) {
      this.loading.set(false);
      return;
    }

    this.http
      .get<{ success: boolean; reservations: ReservationSummary[] }>(
        `/api/reservations/by-user/${user.id}`
      )
      .subscribe({
        next: (res) => {
          const allReservations = res.reservations ?? [];
          const now = new Date().getTime();

          const active = allReservations.filter(r => {
            const isPast = new Date(this.normalizeApiDate(r.endDate)).getTime() < now;
            const validStatus = r.status === 0 || r.status === 'Active' || r.status === undefined;
            return !r.isDeleted && validStatus && !isPast;
          });

          const expired = allReservations.filter(r => {
            const isPast = new Date(this.normalizeApiDate(r.endDate)).getTime() < now;
            const validStatus = r.status === 0 || r.status === 'Active' || r.status === undefined;
            return r.isDeleted || !validStatus || isPast;
          }).sort((a, b) => new Date(this.normalizeApiDate(b.startDate)).getTime() - new Date(this.normalizeApiDate(a.startDate)).getTime());

          this.reservations.set(allReservations);
          this.activeReservations.set(active);
          this.expiredReservations.set(expired);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Failed to load reservations. Please try again.');
          this.loading.set(false);
        },
      });
  }

  protected typeLabel(type: string): string {
    return type === 'bixi' ? '🚲 BIXI' : '🅿️ Parking';
  }

  protected formatDate(d: string): string {
    const normalized = this.normalizeApiDate(d);
    return new Date(normalized).toLocaleString('en-CA', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  private normalizeApiDate(value: string): string {
    // Some API dates can arrive without timezone suffix; interpret those as UTC.
    if (!value) {
      return value;
    }

    const hasTimezone = /(?:Z|[+-]\d{2}:\d{2})$/i.test(value);
    return hasTimezone ? value : `${value}Z`;
  }

}
