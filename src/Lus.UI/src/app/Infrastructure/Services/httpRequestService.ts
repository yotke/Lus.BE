// src/app/services/http-request.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';  // Import environment for the base URL

@Injectable({
  providedIn: 'root'
})
export class HttpRequestService {
  private baseUrl: string = environment.target;
  private refreshTokenInProgress: boolean = false;

  constructor(private http: HttpClient, private router: Router) {}

  // Helper function to get access and refresh token from localStorage
  private getAuthTokens() {
    const accessToken = localStorage.getItem('access_token');
    const refreshToken = localStorage.getItem('refresh_token');
    return { accessToken, refreshToken };
  }

  // Helper function to set tokens to localStorage after refreshing
  private setAuthTokens(accessToken: string, refreshToken: string) {
    localStorage.setItem('access_token', accessToken);
    localStorage.setItem('refresh_token', refreshToken);
  }

  // Attach token to request headers
  private getHeaders(): HttpHeaders {
    const { accessToken } = this.getAuthTokens();
    return new HttpHeaders({
      'Authorization': `Bearer ${accessToken}`,
      'Content-Type': 'application/json'
    });
  }

  // GET method
  get<T>(endpoint: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { headers: this.getHeaders(), params })
      .pipe(catchError((error) => this.handleError(error, () => this.get(endpoint, params))));
  }

  // POST method
  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, { headers: this.getHeaders() })
      .pipe(catchError((error) => this.handleError(error, () => this.post(endpoint, body))));
  }

  // PUT method
  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, { headers: this.getHeaders() })
      .pipe(catchError((error) => this.handleError(error, () => this.put(endpoint, body))));
  }

  // DELETE method
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${endpoint}`, { headers: this.getHeaders() })
      .pipe(catchError((error) => this.handleError(error, () => this.delete(endpoint))));
  }

  // Handle errors and retry logic
  private handleError(error: HttpErrorResponse, retryRequest: () => Observable<any>): Observable<any> {
    if (error.status === 401 && !this.refreshTokenInProgress) {
      // Unauthorized, attempt to refresh token
      return this.refreshToken().pipe(
        switchMap(() => retryRequest())
      );
    } else if (error.status === 401 && this.refreshTokenInProgress) {
      // If already refreshing token, reject and handle elsewhere
      this.router.navigate(['/login']);  // Redirect to login page if refresh fails
      return throwError(() => new Error('Unauthorized - Redirecting to login'));
    } else {
      // Handle other errors
      return throwError(() => new Error(error.message || 'Server Error'));
    }
  }

  // Refresh token method
  private refreshToken(): Observable<any> {
    const { refreshToken } = this.getAuthTokens();
    this.refreshTokenInProgress = true;

    return this.http.post<any>(`${this.baseUrl}/connect/token`, { refreshToken })
      .pipe(
        switchMap((response: any) => {
          this.setAuthTokens(response.accessToken, response.refreshToken);
          this.refreshTokenInProgress = false;
          return response;
        }),
        catchError(error => {
          this.refreshTokenInProgress = false;
          this.router.navigate(['/login']);  // Redirect to login if refresh fails
          return throwError(() => new Error('Refresh Token Failed'));
        })
      );
  }
}
