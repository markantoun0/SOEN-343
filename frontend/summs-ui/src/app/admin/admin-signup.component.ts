import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth/auth.service';

@Component({
  selector: 'app-admin-signup',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-signup.component.html',
  styleUrl: './admin-auth.component.scss'
})
export class AdminSignupComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected name = '';
  protected email = '';
  protected password = '';

  protected loading = signal(false);
  protected errorMsg = signal<string | null>(null);

  protected submit(): void {
    this.errorMsg.set(null);

    if (!this.name.trim() || !this.email.trim() || !this.password.trim()) {
      this.errorMsg.set('Name, email, and password are required.');
      return;
    }

    this.loading.set(true);

    this.auth.adminSignup(this.name.trim(), this.email.trim(), this.password).subscribe({
      next: (res) => {
        this.loading.set(false);
        const currentUser = this.auth.extractCurrentUser(res, 'admin');
        if (!currentUser) {
          this.errorMsg.set(res.message ?? 'Sign up failed. Please try again.');
          return;
        }

        this.auth.setUser(currentUser);
        this.router.navigate(['/admin/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMsg.set(err?.error?.message ?? 'Sign up failed. Please try again.');
      }
    });
  }
}
