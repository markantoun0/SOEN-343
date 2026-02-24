import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { GoogleMap, MapMarker, MapInfoWindow } from '@angular/google-maps';
import { catchError, map, of } from 'rxjs';

export interface MobilityLocation {
  placeId: string;
  name: string;
  type: 'bixi' | 'parking';
  latitude: number;
  longitude: number;
  vicinity?: string;
  availableSpots?: number;
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

  protected center = { lat: 45.5451, lng: -73.6395 };
  protected zoom = 11;
  protected mapOptions = {
    mapTypeId: 'roadmap',
    mapTypeControl: false,
    streetViewControl: false,
    fullscreenControl: true,
  };

  protected selectedLocation: MobilityLocation | null = null;

  // Single observable — Angular async pipe handles subscribe/unsubscribe & change detection
  protected readonly locations$ = this.http
    .get<{ locations: MobilityLocation[] }>('/api/mobility/montreal-laval')
    .pipe(
      map((res) => ({ data: res.locations, error: null })),
      catchError((err) => of({ data: null, error: err?.message ?? 'Failed to load locations.' }))
    );

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
    infoWindow.open(marker);
  }
}

