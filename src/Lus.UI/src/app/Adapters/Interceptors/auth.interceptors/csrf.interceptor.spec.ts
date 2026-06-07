import { HttpErrorResponse, HttpHandler, HttpRequest } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { CsrfInterceptor } from './csrf.interceptor';
import { CsrfService } from 'src/app/Infrastructure/Services/Auth/csrf.service';

describe('CsrfInterceptor', () => {
  it('adds CSRF header to unsafe API requests', (done) => {
    const csrf = jasmine.createSpyObj<CsrfService>('CsrfService', ['getToken', 'refreshToken']);
    csrf.getToken.and.returnValue(of('token-1'));
    const interceptor = new CsrfInterceptor(csrf);
    const next = jasmine.createSpyObj<HttpHandler>('HttpHandler', ['handle']);
    next.handle.and.returnValue(of({} as any));

    interceptor.intercept(new HttpRequest('POST', '/api/auth/logout', {}), next).subscribe(() => {
      const request = next.handle.calls.mostRecent().args[0] as HttpRequest<any>;
      expect(request.headers.get('X-CSRF-Token')).toBe('token-1');
      done();
    });
  });

  it('refreshes CSRF token and retries once on forbidden response', (done) => {
    const csrf = jasmine.createSpyObj<CsrfService>('CsrfService', ['getToken', 'refreshToken']);
    csrf.getToken.and.returnValue(of('token-1'));
    csrf.refreshToken.and.returnValue(of('token-2'));
    const interceptor = new CsrfInterceptor(csrf);
    const next = jasmine.createSpyObj<HttpHandler>('HttpHandler', ['handle']);
    next.handle.and.returnValues(
      throwError(() => new HttpErrorResponse({ status: 403 })),
      of({} as any)
    );

    interceptor.intercept(new HttpRequest('DELETE', '/api/users/1'), next).subscribe(() => {
      const retry = next.handle.calls.mostRecent().args[0] as HttpRequest<any>;
      expect(retry.headers.get('X-CSRF-Token')).toBe('token-2');
      expect(next.handle).toHaveBeenCalledTimes(2);
      done();
    });
  });
});
