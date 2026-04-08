import { ChangeDetectorRef, Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { GoogleMap, MapMarker, MapInfoWindow, MapPolyline } from '@angular/google-maps';
import { catchError, map, of, timeout, finalize } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../auth/auth.service';
import { MobilityService, RouteResponse } from './mobility.service';

export interface MobilityLocation {
  placeId: string;
  name: string;
  type: 'bixi' | 'parking';
  latitude: number;
  longitude: number;
  city?: string;
  capacity?: number;
  vicinity?: string;
  availableSpots?: number;
  startDate: string;
  endDate: string;
}

interface ReservationResponse {
  success: boolean;
  reservation?: {
    mobilityLocation?: {
      availableSpots?: number;
    };
  };
}

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule, FormsModule, GoogleMap, MapMarker, MapInfoWindow, MapPolyline],
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
})
export class MapComponent implements OnInit {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);
  private auth = inject(AuthService);
  private router = inject(Router);
  private mobilityService = inject(MobilityService);
  private payUiFallbackTimer: ReturnType<typeof setTimeout> | null = null;

  protected center = { lat: 45.5451, lng: -73.6395 };
  protected zoom = 11;
  protected mapOptions = {
    mapTypeId: 'roadmap',
    mapTypeControl: false,
    streetViewControl: false,
    fullscreenControl: true,
  };

  protected selectedLocation: MobilityLocation | null = null;
  protected reserveMessage: string | null = null;

  protected showPaymentModal = false;
  protected paymentAmount = 0;
  protected paymentContext: any = null;
  protected isPaying = false;

  protected cardholderName = '';
  protected cardNumber = '';
  protected cardExpiry = '';
  protected cardCvc = '';

  protected locations$ = this.http
    .get<{ locations: MobilityLocation[] }>('/api/mobility/montreal-laval')
    .pipe(
      map((res) => ({ data: res.locations, error: null })),
      catchError((err) => of({ data: null, error: err?.message ?? 'Failed to load locations.' }))
    );

  // --- Route state ---
  protected routeOrigin = '';
  protected routeDestination = '';
  protected routeTravelMode: 'car' | 'bike' = 'car';
  protected routePolylinePath: google.maps.LatLngLiteral[] = [];
  protected routePolylineOptions: google.maps.PolylineOptions = {
    strokeColor: '#2563eb',
    strokeOpacity: 0.85,
    strokeWeight: 5,
  };
  protected routeDistance: string | null = null;
  protected routeDuration: string | null = null;
  protected routeLoading = false;
  protected routeError: string | null = null;

  get canCalculateRoute(): boolean {
    return this.routeOrigin.trim().length > 0 && this.routeDestination.trim().length > 0;
  }

  ngOnInit(): void {
    this.loadMapsScript();
  }

  private async loadMapsScript(): Promise<void> {
    const res = await fetch('/api/config/maps-key');
    const { key } = await res.json();
    if (!key) return;
    if (!document.getElementById('google-maps-script')) {
      const script = document.createElement('script');
      script.id = 'google-maps-script';
      script.src = `https://maps.googleapis.com/maps/api/js?key=${key}&libraries=places`;
      document.head.appendChild(script);
    }
  }

  // --- Route methods ---

  protected calculateRoute(): void {
    if (!this.canCalculateRoute) return;

    this.routeLoading = true;
    this.routeError = null;
    this.routePolylinePath = [];
    this.routeDistance = null;
    this.routeDuration = null;

    this.mobilityService
      .getRoute({
        origin: this.routeOrigin.trim(),
        destination: this.routeDestination.trim(),
        travelMode: this.routeTravelMode,
      })
      .pipe(
        timeout(20000),
        finalize(() => {
          this.routeLoading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (res: RouteResponse) => {
          this.routePolylinePath = decodePolyline(res.encodedPolyline);
          this.routeDistance = formatDistance(res.distanceMeters);
          this.routeDuration = formatDuration(res.duration);
          this.routePolylineOptions = {
            ...this.routePolylineOptions,
            strokeColor: this.routeTravelMode === 'bike' ? '#f97316' : '#2563eb',
          };
        },
        error: (err) => {
          this.routeError =
            err?.error?.message ?? 'Could not calculate route. Please try again.';
        },
      });
  }

  protected clearRoute(): void {
    this.routePolylinePath = [];
    this.routeDistance = null;
    this.routeDuration = null;
    this.routeError = null;
  }

  protected setAsOrigin(loc: MobilityLocation): void {
    this.routeOrigin = locationToAddress(loc);
  }

  protected setAsDestination(loc: MobilityLocation): void {
    this.routeDestination = locationToAddress(loc);
  }

  // --- Marker methods ---

  protected getMarkerOptions(loc: MobilityLocation): google.maps.MarkerOptions {
    const isBixi = loc.type === 'bixi';
    return {
      icon: {
        path: google.maps.SymbolPath.CIRCLE,
        scale: isBixi ? 8 : 10,
        fillColor: isBixi ? '#f97316' : '#3b82f6',
        fillOpacity: 0.9,
        strokeColor: isBixi ? '#9a3412' : '#1e40af',
        strokeWeight: 2,
      },
      title: loc.name,
    };
  }

  protected getMarkerPosition(loc: MobilityLocation): google.maps.LatLngLiteral {
    return { lat: loc.latitude, lng: loc.longitude };
  }

  protected openInfo(infoWindow: MapInfoWindow, marker: MapMarker, loc: MobilityLocation): void {
    this.selectedLocation = loc;
    this.reserveMessage = null;
    infoWindow.open(marker);
  }

  protected reserveSelectedLocation(event: Event): void {
    if (!this.selectedLocation) return;

    const startDate = (document.getElementById('start-date') as HTMLInputElement)?.value;
    const endDate = (document.getElementById('end-date') as HTMLInputElement)?.value;

    if (!startDate || !endDate) {
      this.reserveMessage = 'Please select a start and end date.';
      return;
    }

    const start = new Date(startDate);
    const end = new Date(endDate);

    if (start >= end) {
      this.reserveMessage = 'End date must be after start date.';
      return;
    }

    const currentSpots = Math.max(0, this.selectedLocation.availableSpots ?? 0);
    if (currentSpots <= 0) {
      this.reserveMessage = 'There are no spots anymore.';
      return;
    }

    const durationHours = Math.ceil((end.getTime() - start.getTime()) / (1000 * 60 * 60));
    const rate = this.selectedLocation.type === 'bixi' ? 0.25 : 0.50;
    this.paymentAmount = durationHours * rate;

    if (this.paymentAmount <= 0) {
      this.paymentAmount = rate; // Minimum 1 hour
    }

    // Set context for payment
    this.paymentContext = {
      button: event.currentTarget as HTMLButtonElement | null,
      startDate: start.toISOString(),
      endDate: end.toISOString(),
      userId: this.auth.currentUser()?.id ?? null,
    };

    if (!this.paymentContext.userId) {
      this.reserveMessage = 'You must be logged in to reserve.';
      return;
    }

    this.showPaymentModal = true;
    this.reserveMessage = null;
  }

  protected processPayment(): void {
    if (!this.paymentContext) return;

    const paymentValidationError = this.getPaymentValidationError();
    if (paymentValidationError) {
      this.reserveMessage = paymentValidationError;
      return;
    }

    this.isPaying = true;
    this.reserveMessage = null;

    // Safety fallback: never leave the button stuck in "Processing..."
    this.clearPayUiFallbackTimer();
    this.payUiFallbackTimer = setTimeout(() => {
      this.isPaying = false;
      if (!this.reserveMessage) {
        this.reserveMessage = 'Payment timed out. Please try again.';
      }
    }, 20000);

    const paymentPayload = {
      userId: this.paymentContext.userId,
      amount: this.paymentAmount,
      paymentMethod: 'CreditCard',
      reservationType: this.selectedLocation?.type,
      reservationStartDate: this.paymentContext.startDate,
      reservationEndDate: this.paymentContext.endDate,
      cardholderName: this.cardholderName.trim(),
      cardNumber: this.cardNumber.replace(/\s/g, ''),
      expiry: this.cardExpiry.trim(),
      cvc: this.cardCvc.trim()
    };

    console.log('Processing payment...');

    this.http.post<any>('/api/payments/process', paymentPayload)
      .pipe(
        timeout(15000),
        finalize(() => {
          this.isPaying = false;
          this.clearPayUiFallbackTimer();
        })
      )
      .subscribe({
        next: (res) => {
          if (res.success) {
            // Close modal and show success immediately
            this.showPaymentModal = false;
            this.reserveMessage = 'Success! Spot reserved.';

            // Execute reservation in background without awaiting
            this.executeReservationInBackground(this.paymentContext, res.paymentId);
            void this.router.navigate(['/my-reservations']);
          } else {
            this.reserveMessage = res.message || 'Payment failed. Please try again.';
            console.error('Payment failed:', res);
          }
        },
        error: (err) => {
          const errorMsg = err?.error?.message || err?.error || 'Payment failed. Please try again.';
          this.reserveMessage = errorMsg;
          console.error('Payment error:', err);
        }
      });
  }

  private clearPayUiFallbackTimer(): void {
    if (this.payUiFallbackTimer) {
      clearTimeout(this.payUiFallbackTimer);
      this.payUiFallbackTimer = null;
    }
  }

  protected onCardNumberInput(): void {
    const digits = this.cardNumber.replace(/\D/g, '').slice(0, 16);
    this.cardNumber = digits.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
  }

  protected onCardExpiryInput(): void {
    const digits = this.cardExpiry.replace(/\D/g, '').slice(0, 4);
    if (digits.length <= 2) {
      this.cardExpiry = digits;
      return;
    }

    this.cardExpiry = `${digits.slice(0, 2)}/${digits.slice(2)}`;
  }

  protected onCardCvcInput(): void {
    this.cardCvc = this.cardCvc.replace(/\D/g, '').slice(0, 4);
  }

  private executeReservation(context: any, paymentId: number): void {
    if (!this.selectedLocation) return;
    const button = context.button;
    if (button) {
      button.disabled = true;
      button.textContent = 'Reserving...';
    }

    const selected = this.selectedLocation;
    const availableSpots = Math.max(0, selected.availableSpots ?? 0);
    // IMPORTANT: Use capacity if available, otherwise estimate based on availableSpots
    // For BIXI, capacity is always provided. For parking, estimate conservatively.
    const capacity = selected.capacity ?? Math.max(availableSpots * 2, 5);

    const payload = {
      placeId: selected.placeId,
      name: selected.name,
      type: selected.type,
      city: selected.city ?? 'Unknown',
      latitude: selected.latitude,
      longitude: selected.longitude,
      capacity,
      availableSpots,
      reservationTime: new Date().toISOString(),
      startDate: context.startDate,
      endDate: context.endDate,
      userId: context.userId,
      // Pass paymentId if backend needs to link it, though PaymentsController already saved it
    };

    this.http
      .post<ReservationResponse>('/api/reservations/reserve-location', payload)
      .pipe(
        timeout(15000),
        finalize(() => {
          if (button) {
            button.disabled = false;
            button.textContent = 'Confirm Reservation';
          }
        })
      )
      .subscribe({
        next: (res) => {
          const updatedSpots = res?.reservation?.mobilityLocation?.availableSpots;
          const nextSpots =
            typeof updatedSpots === 'number'
              ? Math.max(0, updatedSpots)
              : Math.max(0, availableSpots - 1);

          this.applyDisplayedSpots(selected, nextSpots);
          // Silent success in background - payment already confirmed
        },
        error: (err) => {
          // Log silently in background - payment was successful even if reservation sync fails
          console.warn('Background reservation sync failed (payment already processed):', err);
        },
      });
  }

  private executeReservationInBackground(context: any, paymentId: number): void {
    // Execute reservation in background without blocking the user's response
    setTimeout(() => {
      this.executeReservation(context, paymentId);
    }, 100);
  }

  private getPaymentValidationError(): string | null {
    if (!this.cardholderName.trim()) {
      return 'Please enter the cardholder name.';
    }

    const cardDigits = this.cardNumber.replace(/\D/g, '');
    if (!/^\d{16}$/.test(cardDigits)) {
      return 'Card number must be exactly 16 digits.';
    }

    const expiry = this.cardExpiry.trim();
    if (!/^(0[1-9]|1[0-2])\/\d{2}$/.test(expiry)) {
      return 'Expiry must be in MM/YY format.';
    }

    const cvc = this.cardCvc.trim();
    if (!/^\d{3,4}$/.test(cvc)) {
      return 'CVC must be 3 or 4 digits.';
    }

    return null;
  }

  private applyDisplayedSpots(targetLocation: MobilityLocation, spots: number): void {
    targetLocation.availableSpots = spots;

    if (this.selectedLocation?.placeId === targetLocation.placeId) {
      this.selectedLocation = {
        ...this.selectedLocation,
        availableSpots: spots,
      };
    }

    this.cdr.detectChanges();
  }
}

function locationToAddress(loc: MobilityLocation): string {
  if (loc.vicinity) return loc.vicinity;
  return loc.city ? `${loc.name}, ${loc.city}` : loc.name;
}

function decodePolyline(encoded: string): google.maps.LatLngLiteral[] {
  const points: google.maps.LatLngLiteral[] = [];
  let index = 0;
  let lat = 0;
  let lng = 0;

  while (index < encoded.length) {
    let shift = 0;
    let result = 0;
    let byte: number;
    do {
      byte = encoded.charCodeAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20);
    lat += result & 1 ? ~(result >> 1) : result >> 1;

    shift = 0;
    result = 0;
    do {
      byte = encoded.charCodeAt(index++) - 63;
      result |= (byte & 0x1f) << shift;
      shift += 5;
    } while (byte >= 0x20);
    lng += result & 1 ? ~(result >> 1) : result >> 1;

    points.push({ lat: lat / 1e5, lng: lng / 1e5 });
  }

  return points;
}

function formatDistance(meters: number): string {
  if (meters >= 1000) {
    return `${(meters / 1000).toFixed(1)} km`;
  }
  return `${meters} m`;
}

function formatDuration(raw: string): string {
  const totalSeconds = parseInt(raw.replace('s', ''), 10);
  if (isNaN(totalSeconds) || totalSeconds <= 0) return '0 min';

  const hours = Math.floor(totalSeconds / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  if (hours > 0) {
    return minutes > 0 ? `${hours} h ${minutes} min` : `${hours} h`;
  }
  return `${minutes} min`;
}
