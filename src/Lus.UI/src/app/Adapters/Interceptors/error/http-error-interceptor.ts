import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from 'src/app/Infrastructure/Services/Auth/auth.service';

@Injectable()
export class HttpErrorInterceptor implements HttpInterceptor {

  constructor(private router: Router, private authService: AuthService) { }

  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    return next.handle(request).pipe(
      // retry(0),
      catchError((err) => {
        return this.handleErrorResponse(err);
      })
    );
  }

  private handleErrorResponse(ErrorResponse: HttpErrorResponse): Observable<never> {
    switch (ErrorResponse.status) {
      case 500:
        this.router.navigate(['/Home']);
        return throwError('Error 500');
      case 2:
        this.authService.doLogout().then(() => {
          this.router.navigate(['/Home']);
        });
        return throwError('Unauthorized');

    }

    return throwError(ErrorResponse);
  }
}
