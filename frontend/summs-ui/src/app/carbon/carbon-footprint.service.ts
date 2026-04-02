import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CarbonFootprintData {
  userId: number;
  userName: string;
  totalCarbonKg: number;
  tripsCompleted: number;
  lastUpdated: string;
}

export interface UserLeaderboardEntry {
  rank: number;
  userId: number;
  userName: string;
  totalCarbonKg: number;
  tripsCompleted: number;
}

export interface TripCarbonFootprint {
  reservationId: number;
  mobilityType: string;
  distanceKm: number;
  durationMinutes: number;
  carbonKg: number;
}

export interface UserRankResponse {
  userId: number;
  rank: number;
}

@Injectable({
  providedIn: 'root'
})
export class CarbonFootprintService {
  private http = inject(HttpClient);
  private apiUrl = '/api/carbonfootprint';

  getUserCarbonFootprint(userId: number): Observable<{ success: boolean; data: CarbonFootprintData }> {
    return this.http.get<{ success: boolean; data: CarbonFootprintData }>(
      `${this.apiUrl}/user/${userId}`
    );
  }

  calculateTripCarbonFootprint(reservationId: number, distanceKm: number): Observable<{ success: boolean; data: TripCarbonFootprint }> {
    return this.http.post<{ success: boolean; data: TripCarbonFootprint }>(
      `${this.apiUrl}/calculate-trip`,
      { reservationId, distanceKm }
    );
  }

  getLeaderboard(topN: number = 10): Observable<{ success: boolean; data: UserLeaderboardEntry[] }> {
    return this.http.get<{ success: boolean; data: UserLeaderboardEntry[] }>(
      `${this.apiUrl}/leaderboard`,
      { params: { topN } }
    );
  }

  getUserRank(userId: number): Observable<{ success: boolean; data: UserRankResponse }> {
    return this.http.get<{ success: boolean; data: UserRankResponse }>(
      `${this.apiUrl}/rank/${userId}`
    );
  }
}

