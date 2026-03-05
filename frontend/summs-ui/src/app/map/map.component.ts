import { ChangeDetectorRef, Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { GoogleMap, MapMarker, MapInfoWindow } from '@angular/google-maps';
import { catchError, map, of, timeout } from 'rxjs';

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
  imports: [CommonModule, GoogleMap, MapMarker, MapInfoWindow],
  templateUrl: './map.component.html',
  styleUrl: './map.component.scss',
})
export class MapComponent {
  private http = inject(HttpClient);
  private cdr = inject(ChangeDetectorRef);

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

  // Single observable — Angular async pipe handles subscribe/unsubscribe & change detection
  protected locations$ = this.http
    .get<{ locations: MobilityLocation[] }>('/api/mobility/montreal-laval')
    .pipe(
      map((res) => ({ data: res.locations, error: null })),
      catchError((err) => of({ data: null, error: err?.message ?? 'Failed to load locations.' }))
    );

  ngOnInit(): void {
    this.loadMapsScript();
  }

  private async loadMapsScript(): Promise<void> {
    // Fetch the API key from backend
    const res = await fetch('/api/config/maps-key');
    const { key } = await res.json();
    if (!key) return;
    // Inject the script if not already present
    if (!document.getElementById('google-maps-script')) {
      const script = document.createElement('script');
      script.id = 'google-maps-script';
      script.src = `https://maps.googleapis.com/maps/api/js?key=${key}&libraries=places`;
      document.head.appendChild(script);
    }
  }

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
    if (!this.selectedLocation) {
      return;
    }

    const currentSpots = Math.max(0, this.selectedLocation.availableSpots ?? 0);
    if (currentSpots <= 0) {
      this.reserveMessage = 'There are no spots anymore.';
      return;
    }

    const button = event.currentTarget as HTMLButtonElement | null;
    if (button) {
      button.disabled = true;
      button.textContent = 'Reserving...';

      setTimeout(() => {
        button.disabled = false;
        button.textContent = 'Reserve';
      }, 1000);
    }

    this.reserveMessage = null;

    const selected = this.selectedLocation;
    const availableSpots = Math.max(0, selected.availableSpots ?? 0);
    const capacity = Math.max(selected.capacity ?? availableSpots, availableSpots);

    const payload = {
      placeId: selected.placeId,
      name: selected.name,
      type: selected.type,
      city: selected.city ?? 'Unknown',
      latitude: selected.latitude,
      longitude: selected.longitude,
      capacity,
      availableSpots,
      reservationTime: new Date().toISOString()
    };

    this.http
      .post<ReservationResponse>('/api/reservations/reserve-location', payload)
      .pipe(timeout(15000))
      .subscribe({
        next: (res) => {
          const updatedSpots = res?.reservation?.mobilityLocation?.availableSpots;
          const nextSpots = typeof updatedSpots === 'number'
            ? Math.max(0, updatedSpots)
            : Math.max(0, availableSpots - 1);

          this.applyDisplayedSpots(selected, nextSpots);

          if (nextSpots <= 0) {
            this.reserveMessage = 'There are no spots anymore.';
          }
        },
        error: (err) => {
          this.reserveMessage = err?.status === 409
            ? 'There are no spots anymore.'
            : 'Could not reserve right now.';
          console.error('Failed to save reservation', err);
        }
      });
  }

  private applyDisplayedSpots(
    targetLocation: MobilityLocation,
    spots: number
  ): void {
    targetLocation.availableSpots = spots;

    // Force immediate repaint of the open info-window on first click.
    if (this.selectedLocation?.placeId === targetLocation.placeId) {
      this.selectedLocation = {
        ...this.selectedLocation,
        availableSpots: spots
      };
    }

    this.cdr.detectChanges();
  }
}

