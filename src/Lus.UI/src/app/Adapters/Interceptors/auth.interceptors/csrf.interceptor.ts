import { Injectable } from '@angular/core';
import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, switchMap, take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { CsrfService } from 'src/app/Infrastructure/Services/Auth/csrf.service';

const METHODS_REQUIRING_CSRF = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);
const CSRF_SKIP = ['/csrf-token', '/connect/'];

@Injectable()
export class CsrfInterceptor implements HttpInterceptor {
  constructor(private csrf: CsrfService) { }

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    if (!this.shouldAddCsrf(req)) {
      return next.handle(req);
    }

    return this.csrf.getToken().pipe(
      take(1),
      switchMap(token => {
        if (!token) {
          return throwError(() => new Error('Missing CSRF token'));
        }

        return next.handle(req.clone({ setHeaders: { 'X-CSRF-Token': token } }));
      }),
      catchError(error => {
        if (error instanceof HttpErrorResponse && (error.status === 400 || error.status === 403)) {
          return this.csrf.refreshToken().pipe(
            take(1),
            switchMap(token => {
              if (!token) {
                return throwError(() => error);
              }

              return next.handle(req.clone({ setHeaders: { 'X-CSRF-Token': token } }));
            })
          );
        }

        return throwError(() => error);
      })
    );
  }

  private shouldAddCsrf(req: HttpRequest<any>): boolean {
    if (!METHODS_REQUIRING_CSRF.has(req.method.toUpperCase())) {
      return false;
    }

    if (CSRF_SKIP.some(url => req.url.includes(url))) {
      return false;
    }

    if (req.headers.has('Authorization')) {
      return false;
    }

    const apiBase = environment.target.replace(/\/+$/, '');
    return req.url.startsWith(apiBase) || req.url.startsWith('/api');
  }
}
