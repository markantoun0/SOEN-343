import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MobilityLocation {
  placeId: string;
  name: string;
  type: 'bike' | 'parking';
  latitude: number;
  longitude: number;
  vicinity?: string;
}

export interface MobilityResponse {
  success: boolean;
  count: number;
  locations: MobilityLocation[];
}

@Injectable({ providedIn: 'root' })
export class MobilityService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  /** Fetch bike + parking locations for Montréal and Laval */
  getMontrealAndLaval(): Observable<MobilityResponse> {
    return this.http.get<MobilityResponse>(`${this.base}/api/mobility/montreal-laval`);
  }

  /** Fetch locations near a custom coordinate */
  getNearby(lat: number, lng: number, radius = 8000): Observable<MobilityResponse> {
    return this.http.get<MobilityResponse>(
      `${this.base}/api/mobility/nearby?lat=${lat}&lng=${lng}&radius=${radius}`
    );
  }
}

