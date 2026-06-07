import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

@Injectable({ providedIn: 'root' })
export class CsrfService {
  constructor(private http: HttpClient) { }

  getToken(): Observable<string | null> {
    return this.http.get<{ token: string }>(`${environment.target}/csrf-token`, { withCredentials: true }).pipe(
      map(response => {
        const token = (response?.token ?? '').trim();
        return token || null;
      }),
      catchError(() => of(null))
    );
  }

  refreshToken(): Observable<string | null> {
    return this.getToken();
  }
}
