// src/app/services/http-request.service.ts
import { Injectable } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';  // Import environment for the base URL

@Injectable({
  providedIn: 'root'
})
export class HttpRequestService {
  private baseUrl: string = environment.target;
  constructor(private http: HttpClient, private router: Router) {}

  // GET method
  get<T>(endpoint: string, params?: HttpParams): Observable<T> {
    return this.http.get<T>(`${this.baseUrl}/${endpoint}`, { params, withCredentials: true })
      .pipe(catchError((error) => this.handleError(error, () => this.get(endpoint, params))));
  }

  // POST method
  post<T>(endpoint: string, body: any): Observable<T> {
    return this.http.post<T>(`${this.baseUrl}/${endpoint}`, body, { withCredentials: true })
      .pipe(catchError((error) => this.handleError(error, () => this.post(endpoint, body))));
  }

  // PUT method
  put<T>(endpoint: string, body: any): Observable<T> {
    return this.http.put<T>(`${this.baseUrl}/${endpoint}`, body, { withCredentials: true })
      .pipe(catchError((error) => this.handleError(error, () => this.put(endpoint, body))));
  }

  // DELETE method
  delete<T>(endpoint: string): Observable<T> {
    return this.http.delete<T>(`${this.baseUrl}/${endpoint}`, { withCredentials: true })
      .pipe(catchError((error) => this.handleError(error, () => this.delete(endpoint))));
  }

  // Handle errors and retry logic
  private handleError(error: HttpErrorResponse, retryRequest: () => Observable<any>): Observable<any> {
    if (error.status === 401) {
      this.router.navigate(['/login']);
      return throwError(() => new Error('Unauthorized - Redirecting to login'));
    }

    return throwError(() => new Error(error.message || 'Server Error'));
  }
}
