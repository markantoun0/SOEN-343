import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface CurrentUser {
  id: number;
  name: string;
  email: string;
}

interface AuthResponse {
  success: boolean;
  user?: CurrentUser;
  message?: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly STORAGE_KEY = 'summs_user';

  /** Reactive signal — any component can read this directly */
  readonly currentUser = signal<CurrentUser | null>(this.loadFromStorage());
  /** Controls whether the auth dialog is visible */
  readonly showDialog = signal(false);

  constructor(private http: HttpClient) {}

  private loadFromStorage(): CurrentUser | null {
    try {
      const stored = localStorage.getItem(this.STORAGE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }

  openDialog(): void  { this.showDialog.set(true);  }
  closeDialog(): void { this.showDialog.set(false); }

  signup(name: string, email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/users/signup', { name, email, password });
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>('/api/users/login', { email, password });
  }

  setUser(user: CurrentUser): void {
    localStorage.setItem(this.STORAGE_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  logout(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    this.currentUser.set(null);
  }
}
