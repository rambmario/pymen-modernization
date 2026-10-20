import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { authGuard, roleGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
  });

  afterEach(() => sessionStorage.clear());

  it('authGuard redirects anonymous users to login', () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(String(result)).toContain('login');
  });

  it('authGuard allows authenticated users', () => {
    sessionStorage.setItem(
      'pymen.auth.session',
      JSON.stringify({
        accessToken: 'token',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        tenantId: '11111111-1111-1111-1111-111111111111',
        email: 'admin.t1@demo.local',
        roles: ['Admin'],
      }),
    );

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const result = TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
    expect(result).toBe(true);
  });

  it('roleGuard blocks Operator from Planner routes', () => {
    sessionStorage.setItem(
      'pymen.auth.session',
      JSON.stringify({
        accessToken: 'token',
        expiresAt: new Date(Date.now() + 60_000).toISOString(),
        tenantId: '11111111-1111-1111-1111-111111111111',
        email: 'operator.t1@demo.local',
        roles: ['Operator'],
      }),
    );

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });

    const guard = roleGuard('Admin', 'Planner');
    const result = TestBed.runInInjectionContext(() => guard({} as never, {} as never));
    expect(String(result)).toContain('work-orders');
    expect(TestBed.inject(AuthService).hasAnyRole('Planner')).toBe(false);
  });
});
