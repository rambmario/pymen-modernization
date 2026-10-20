import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse } from '../api/models';

const SESSION_KEY = 'pymen.auth.session';

export interface AuthSession {
  accessToken: string;
  expiresAt: string;
  tenantId: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly sessionSignal = signal<AuthSession | null>(this.readStoredSession());

  readonly session = this.sessionSignal.asReadonly();
  readonly isAuthenticated = computed(() => {
    const session = this.sessionSignal();
    return !!session && !this.isExpired(session);
  });
  readonly roles = computed(() => this.sessionSignal()?.roles ?? []);
  readonly email = computed(() => this.sessionSignal()?.email ?? null);

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', request).pipe(
      tap((response) => {
        const session: AuthSession = {
          accessToken: response.accessToken,
          expiresAt: response.expiresAt,
          tenantId: response.tenantId,
          email: response.email,
          roles: response.roles,
        };
        this.persist(session);
      }),
    );
  }

  logout(navigateToLogin = true): void {
    this.sessionSignal.set(null);
    sessionStorage.removeItem(SESSION_KEY);
    if (navigateToLogin) {
      void this.router.navigate(['/login']);
    }
  }

  getAccessToken(): string | null {
    const session = this.sessionSignal();
    if (!session || this.isExpired(session)) {
      return null;
    }
    return session.accessToken;
  }

  hasAnyRole(...required: string[]): boolean {
    const roles = this.roles();
    return required.some((role) => roles.includes(role));
  }

  isOperatorOnly(): boolean {
    const roles = this.roles();
    return roles.includes('Operator') && !roles.includes('Admin') && !roles.includes('Planner');
  }

  private persist(session: AuthSession): void {
    this.sessionSignal.set(session);
    sessionStorage.setItem(SESSION_KEY, JSON.stringify(session));
  }

  private readStoredSession(): AuthSession | null {
    const raw = sessionStorage.getItem(SESSION_KEY);
    if (!raw) {
      return null;
    }

    try {
      const session = JSON.parse(raw) as AuthSession;
      if (!session.accessToken || this.isExpired(session)) {
        sessionStorage.removeItem(SESSION_KEY);
        return null;
      }
      return session;
    } catch {
      sessionStorage.removeItem(SESSION_KEY);
      return null;
    }
  }

  private isExpired(session: AuthSession): boolean {
    const expiresAt = Date.parse(session.expiresAt);
    return Number.isNaN(expiresAt) || expiresAt <= Date.now();
  }
}
