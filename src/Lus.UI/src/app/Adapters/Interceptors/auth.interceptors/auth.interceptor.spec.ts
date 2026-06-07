import { HttpHandler, HttpRequest } from '@angular/common/http';
import { of } from 'rxjs';
import { AuthInterceptor } from './auth.interceptor';

describe('AuthInterceptor', () => {
  it('sends requests with credentials and does not attach bearer token', (done) => {
    localStorage.setItem('access_token', 'legacy-token');
    const interceptor = new AuthInterceptor();
    const next = jasmine.createSpyObj<HttpHandler>('HttpHandler', ['handle']);
    next.handle.and.returnValue(of({} as any));

    interceptor.intercept(new HttpRequest('GET', '/api/auth/state'), next).subscribe(() => {
      const request = next.handle.calls.mostRecent().args[0] as HttpRequest<any>;
      expect(request.withCredentials).toBeTrue();
      expect(request.headers.has('Authorization')).toBeFalse();
      localStorage.removeItem('access_token');
      done();
    });
  });
});
