﻿import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../auth/auth.service';
import { CarbonFootprintService } from '../carbon/carbon-footprint.service';
import { FormsModule } from '@angular/forms';

export interface ReservationSummary {
  id: number;
  city: string;
  type: string;
  startDate: string;
  endDate: string;
  reservationTime: string;
  mobilityLocation?: {
    name: string;
    type: string;
    city: string;
  };
}

@Component({
  selector: 'app-my-reservations',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './my-reservations.component.html',
  styleUrl: './my-reservations.component.scss',
})
export class MyReservationsComponent implements OnInit {
  protected auth = inject(AuthService);
  private http   = inject(HttpClient);
  private carbonService = inject(CarbonFootprintService);

  protected reservations = signal<ReservationSummary[]>([]);
  protected loading      = signal(true);
  protected error        = signal<string | null>(null);

  // Carbon calculation state
  protected expandedReservationId = signal<number | null>(null);
  protected distanceInput = signal<{ [key: number]: string }>({});
  protected calculatingId = signal<number | null>(null);
  protected calculationSuccess = signal<{ [key: number]: boolean }>({});

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
          this.reservations.set(res.reservations ?? []);
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
    return new Date(d).toLocaleString('en-CA', {
      dateStyle: 'medium',
      timeStyle: 'short',
    });
  }

  protected toggleExpand(reservationId: number): void {
    const expanded = this.expandedReservationId();
    this.expandedReservationId.set(expanded === reservationId ? null : reservationId);
  }

  protected calculateCarbon(reservationId: number): void {
    const distanceKm = parseFloat(this.distanceInput()[reservationId] || '0');

    if (!distanceKm || distanceKm <= 0) {
      alert('Please enter a valid distance in kilometers');
      return;
    }

    this.calculatingId.set(reservationId);

    this.carbonService.calculateTripCarbonFootprint(reservationId, distanceKm).subscribe({
      next: (response) => {
        if (response.success) {
          const success = { ...this.calculationSuccess() };
          success[reservationId] = true;
          this.calculationSuccess.set(success);

          // Reset input after success
          const inputs = { ...this.distanceInput() };
          inputs[reservationId] = '';
          this.distanceInput.set(inputs);

          // Close after 2 seconds
          setTimeout(() => {
            this.expandedReservationId.set(null);
          }, 2000);
        }
        this.calculatingId.set(null);
      },
      error: (err) => {
        console.error('Failed to calculate carbon footprint', err);
        alert('Failed to calculate carbon footprint. Please try again.');
        this.calculatingId.set(null);
      },
    });
  }
}
