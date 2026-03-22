﻿﻿import { Component, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../auth.service';

type DialogMode = 'login' | 'signup';

@Component({
  selector: 'app-auth-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-dialog.component.html',
  styleUrl: './auth-dialog.component.scss',
})
export class AuthDialogComponent {
  protected auth = inject(AuthService);

  protected mode = signal<DialogMode>('login');

  // Form fields
  protected name     = '';
  protected email    = '';
  protected password = '';

  // UI state
  protected loading  = signal(false);
  protected errorMsg = signal<string | null>(null);

  protected switchMode(m: DialogMode): void {
    this.mode.set(m);
    this.errorMsg.set(null);
    this.name = this.email = this.password = '';
  }

  protected submit(): void {
    this.errorMsg.set(null);

    if (!this.email.trim() || !this.password.trim()) {
      this.errorMsg.set('Email and password are required.');
      return;
    }

    if (this.mode() === 'signup' && !this.name.trim()) {
      this.errorMsg.set('Name is required.');
      return;
    }

    this.loading.set(true);

    const request$ = this.mode() === 'signup'
      ? this.auth.signup(this.name.trim(), this.email.trim(), this.password)
      : this.auth.login(this.email.trim(), this.password);

    request$.subscribe({
      next: (res) => {
        this.loading.set(false);
        const currentUser = this.auth.extractCurrentUser(res, 'user');
        if (currentUser) {
          this.auth.setUser(currentUser);
          this.auth.closeDialog();
          return;
        }

        this.errorMsg.set(
          this.mode() === 'signup'
            ? 'Sign up failed. Please try again.'
            : 'Login failed. Please try again.'
        );
      },
      error: (err) => {
        this.loading.set(false);
        const fallback = this.mode() === 'signup'
          ? 'Sign up failed. Please try again.'
          : 'Login failed. Please try again.';
        const msg = err?.error?.message ?? fallback;
        this.errorMsg.set(msg);
      },
    });
  }

  protected closeOnBackdrop(event: MouseEvent): void {
    if ((event.target as HTMLElement).classList.contains('dialog-backdrop')) {
      this.auth.closeDialog();
    }
  }
}
