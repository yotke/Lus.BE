import { Injectable } from '@angular/core';
import { JwtHelperService } from '@auth0/angular-jwt';
import { HttpClient } from '@angular/common/http';
import { Emitters } from 'src/app/Infrastructure/Emitters/Emitters';
import { environment } from 'src/environments/environment';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  expToken: string = '';
  currOrgId: number;
  private authStateSubject = new BehaviorSubject<any>({
    isAuthenticated: false,
    user: null,
    currentOrganization: null,
    organizations: [],
    roles: [],
    permissions: []
  });

  authState$ = this.authStateSubject.asObservable();
  isLoggedIn$ = this.authState$.pipe(map(state => !!state.isAuthenticated));
  currentUser$ = this.authState$.pipe(map(state => state.user));
  currentOrg$ = this.authState$.pipe(map(state => state.currentOrganization));

  constructor(private jwtHelper: JwtHelperService, private http: HttpClient) {
    this.clearLegacyTokenStorage();
    this.checkAuthStatus().subscribe();
  }

  ngOnDestroy() { }

  //  LOGIN LOGOUT SECTION
  doLogIn(tokenResult: any) {
    this.clearLegacyTokenStorage();
    const organizations = tokenResult?.organizations ?? tokenResult?.Organizations ?? [];
    const currentOrganization = organizations[0] ?? null;

    const state = {
      isAuthenticated: true,
      user: {
        firstName: tokenResult?.first_name ?? tokenResult?.FirstName ?? tokenResult?.firstName ?? '',
        lastName: tokenResult?.last_name ?? tokenResult?.LastName ?? tokenResult?.lastName ?? '',
        email: tokenResult?.email ?? tokenResult?.Email ?? '',
        phone: tokenResult?.phone ?? tokenResult?.Phone ?? null,
        idNumber: tokenResult?.id_number ?? tokenResult?.IdNumber ?? null,
        organizationId: currentOrganization?.id ?? currentOrganization?.Id ?? null,
        currOrganization: currentOrganization,
        organizations
      },
      currentOrganization,
      organizations,
      roles: tokenResult?.roles ?? tokenResult?.Roles ?? [],
      permissions: tokenResult?.permissions ?? tokenResult?.Permissions ?? []
    };

    this.authStateSubject.next(state);
    return this.fetchCsrfToken();
  }

  doLogin(tokenResult: any): Observable<void> {
    return this.doLogIn(tokenResult);
  }

  doLogout() {
    return this.logout().toPromise();
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${environment.target}/api/auth/logout`, {}, { withCredentials: true }).pipe(
      catchError(() => of(void 0)),
      tap(() => {
        this.cleanToken();
        this.authStateSubject.next({
          isAuthenticated: false,
          user: null,
          currentOrganization: null,
          organizations: [],
          roles: [],
          permissions: []
        });
      })
    );
  }

  checkAuthStatus(): Observable<boolean> {
    return this.http.get<any>(`${environment.target}/api/auth/state`, { withCredentials: true }).pipe(
      map(response => this.mapAuthState(response)),
      tap(state => this.authStateSubject.next(state)),
      map(state => !!state.isAuthenticated),
      catchError(() => {
        this.authStateSubject.next({
          isAuthenticated: false,
          user: null,
          currentOrganization: null,
          organizations: [],
          roles: [],
          permissions: []
        });
        return of(false);
      })
    );
  }

  // CHECK FUNCTIONS TO CREATE CONVINIENT AUTH USE
  IsTokenExpired(): boolean {
    return !this.authStateSubject.value.isAuthenticated;
  }

  // PRIVATE FUNCTIONS, HELPER FUNCTIONS
  private cleanToken(): Promise<void> {
    return new Promise<void>((resolve) => {
      sessionStorage.removeItem(environment.jwt_token);
      sessionStorage.removeItem(environment.user_data);
      sessionStorage.removeItem(environment.lastPing);
      sessionStorage.removeItem(environment.refresh_token);
      sessionStorage.removeItem(environment.current_org);
      this.clearLegacyTokenStorage();
      setTimeout(resolve, 250);
    });
  }

  private clearLegacyTokenStorage(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    sessionStorage.removeItem(environment.jwt_token);
    sessionStorage.removeItem(environment.refresh_token);
  }

  private fetchCsrfToken(): Observable<void> {
    return this.http.get(`${environment.target}/csrf-token`, { withCredentials: true }).pipe(
      map(() => void 0),
      catchError(() => of(void 0))
    );
  }

  private mapAuthState(response: any): any {
    if (!response?.isAuthenticated && !response?.IsAuthenticated) {
      return {
        isAuthenticated: false,
        user: null,
        currentOrganization: null,
        organizations: [],
        roles: [],
        permissions: []
      };
    }

    const user = response.user ?? response.User ?? {};
    const organizations = response.organizations ?? response.Organizations ?? [];
    const currentOrganization = response.currentOrganization ?? response.CurrentOrganization ?? organizations[0] ?? null;
    return {
      isAuthenticated: true,
      user: {
        ...user,
        currOrganization: currentOrganization,
        organizations
      },
      currentOrganization,
      organizations,
      roles: response.roles ?? response.Roles ?? [],
      permissions: response.permissions ?? response.Permissions ?? []
    };
  }

  // GET FUNCTIONS TO USE OUTSIDE THIS COMPONENT
  get getToken(): string | null {
    return null;
  }

  get getRefreshToken(): string | null {
    return null;
  }

}
