import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CurrentUser {
  id: number;
  name: string;
  email: string;
  role: 'user' | 'admin';
  preferredCity?: 'montreal' | 'laval';
  preferredMobilityType?: 'bixi' | 'parking';
}

interface AuthResponse {
  success: boolean;
  user?: Omit<CurrentUser, 'role'>;
  admin?: Omit<CurrentUser, 'role'>;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly STORAGE_KEY = 'summs_user';
  private readonly RECOMMENDATION_FLAG_KEY = 'summs_login_recommendation_shown';

  /** Reactive signal — any component can read this directly */
  readonly currentUser = signal<CurrentUser | null>(this.loadFromStorage());
  /** Controls whether the auth dialog is visible */
  readonly showDialog = signal(false);
  /** Login recommendation popup content */
  readonly recommendationMessage = signal<string | null>(null);

  constructor(private http: HttpClient) {}

  private loadFromStorage(): CurrentUser | null {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      if (!stored) return null;

      const parsed = JSON.parse(stored) as Partial<CurrentUser>;
      if (!parsed.id || !parsed.name || !parsed.email) return null;

      return {
        id: parsed.id,
        name: parsed.name,
        email: parsed.email,
        role: parsed.role === 'admin' ? 'admin' : 'user',
        preferredCity: parsed.preferredCity === 'laval' ? 'laval' : (parsed.preferredCity === 'montreal' ? 'montreal' : undefined),
        preferredMobilityType:
          parsed.preferredMobilityType === 'bixi'
            ? 'bixi'
            : (parsed.preferredMobilityType === 'parking' ? 'parking' : undefined),
      };
    } catch {
      return null;
    }
  }

  openDialog(): void  { this.showDialog.set(true);  }
  closeDialog(): void { this.showDialog.set(false); }

  signup(name: string, email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/users/signup', { name, email, password });
  }

  adminSignup(name: string, email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/admins/signup', { name, email, password });
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/users/login', { email, password });
  }

  adminLogin(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/admins/login', { email, password });
  }

  extractCurrentUser(response: AuthResponse, role: 'user' | 'admin'): CurrentUser | null {
    if (!response.success) return null;

    if (role === 'user' && response.user) {
      return { ...response.user, role: 'user' };
    }

    if (role === 'admin' && response.admin) {
      return { ...response.admin, role: 'admin' };
    }

    return null;
  }

  setUser(user: CurrentUser): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  updateCurrentUserPreferences(
    preferredCity: 'montreal' | 'laval',
    preferredMobilityType: 'bixi' | 'parking'
  ): void {
    const user = this.currentUser();
    if (!user) return;

    this.setUser({
      ...user,
      preferredCity,
      preferredMobilityType
    });
  }

  canShowLoginRecommendation(): boolean {
    return !sessionStorage.getItem(this.RECOMMENDATION_FLAG_KEY);
  }

  markLoginRecommendationShown(): void {
    sessionStorage.setItem(this.RECOMMENDATION_FLAG_KEY, '1');
  }

  showRecommendation(message: string): void {
    this.recommendationMessage.set(message);
  }

  closeRecommendation(): void {
    this.recommendationMessage.set(null);
  }

  logout(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    sessionStorage.removeItem(this.RECOMMENDATION_FLAG_KEY);
    this.recommendationMessage.set(null);
    this.currentUser.set(null);
  }
}
