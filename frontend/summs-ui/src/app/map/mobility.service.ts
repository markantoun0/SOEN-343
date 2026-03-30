import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface MobilityLocation {
  placeId: string;
  name: string;
  type: 'bixi' | 'parking';
  latitude: number;
  longitude: number;
  vicinity?: string;
  city?: string;
  availableSpots?: number;
  capacity?: number;
}

export interface MobilityResponse {
  success: boolean;
  count: number;
  locations: MobilityLocation[];
}

export interface RouteRequest {
  origin: string;
  destination: string;
  travelMode: 'car' | 'bike';
}

export interface RouteResponse {
  success: boolean;
  distanceMeters: number;
  duration: string;
  encodedPolyline: string;
}

@Injectable({ providedIn: 'root' })
export class MobilityService {
  private http = inject(HttpClient);
  private base = environment.apiBaseUrl;

  getMontrealAndLaval(): Observable<MobilityResponse> {
    return this.http.get<MobilityResponse>(`${this.base}/api/mobility/montreal-laval`);
  }

  getNearby(lat: number, lng: number, radius = 8000): Observable<MobilityResponse> {
    return this.http.get<MobilityResponse>(
      `${this.base}/api/mobility/nearby?lat=${lat}&lng=${lng}&radius=${radius}`
    );
  }

  getRoute(req: RouteRequest): Observable<RouteResponse> {
    return this.http.post<RouteResponse>(`${this.base}/api/mobility/route`, req);
  }
}
