import { Injectable, NgZone } from '@angular/core';
import {
  HttpClient,
  HttpHandler,
  HttpHeaders,
  HttpResponse,
} from '@angular/common/http';
import { from, Observable, of } from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from 'src/app/Infrastructure/Services/Auth/auth.service';
import { TokenResponse } from './token-response';
import { HttpSendType } from '../Interfaces/http-send-type';
// ** Remove import { ReCaptchaV3Service } from 'ng-recaptcha'; **

@Injectable({
  providedIn: 'root',
})
export class ExtendedHttpClient extends HttpClient {
  private httpSendType: HttpSendType = new HttpSendType();
  private alreadySendRefresh: boolean = false;

  constructor(
    private ngZone: NgZone,
    private router: Router,
    // ** Remove private recaptchaV3Service: ReCaptchaV3Service, **
    private http: HttpClient,
    private httpHandler: HttpHandler,
    private authService: AuthService
  ) {
    super(httpHandler);
  }

  public Get<T>(url: string, options: any | null = null): Observable<any> {
    while (this.alreadySendRefresh) {}
    const promise = new Promise((_resolve, _reject) => {
      this._send<T>(this.httpSendType.GET, url, null, options, null).subscribe(
        (observedData: any) => {
          observedData?.subscribe(
            (data: any) => _resolve(data.body),
            (error: any) => {
              if (error.status === 401) {
                this.tryToRefreshToken().subscribe(() => {
                  this._send<T>(
                    this.httpSendType.GET,
                    url,
                    null,
                    options,
                    null
                  ).subscribe((observedData: any) => {
                    observedData.subscribe(
                      (data: any) => _resolve(data.body),
                      (error: any) => _reject(error.error)
                    );
                  });
                });
              } else {
                return _reject(error.error);
              }
            }
          );
        }
      );
    });

    return from(promise);
  }

  public Delete<T>(url: string, options: any | null = null): Observable<any> {
    while (this.alreadySendRefresh) {}
    const promise = new Promise((_resolve, _reject) => {
      this._send<T>(
        this.httpSendType.DELETE,
        url,
        null,
        options,
        null
      ).subscribe((observedData: any) => {
        observedData.subscribe(
          (data: any) => _resolve(data.body),
          (error: any) => {
            if (error.status === 401) {
              this.tryToRefreshToken().subscribe(() => {
                this._send<T>(
                  this.httpSendType.GET,
                  url,
                  null,
                  options,
                  null
                ).subscribe((observedData: any) => {
                  observedData.subscribe(
                    (data: any) => _resolve(data.body),
                    (error: any) => _reject(error.error)
                  );
                });
              });
            } else {
              return _reject(error.error);
            }
          }
        );
      });
    });

    return from(promise);
  }

  public Post<T>(
    url: string,
    body: any | null,
    options: any | null = null,
    contentType: string | null = null
  ): Observable<any> {
    while (this.alreadySendRefresh) {}
    const promise = new Promise((_resolve, _reject) => {
      const response = this._send<T>(
        this.httpSendType.POST,
        url,
        body,
        options,
        contentType
      );
      response.subscribe((observedData: any) => {
        observedData.subscribe(
          // If the response is a token object, resolve that; else resolve data.body
          (data: any) => _resolve('access_token' in data ? data : data.body),
          (error: any) => {
            if (error.status === 401) {
              this.tryToRefreshToken().subscribe(() => {
                this._send<T>(
                  this.httpSendType.POST,
                  url,
                  body,
                  options,
                  null
                ).subscribe((observedData: any) => {
                  observedData.subscribe(
                    (data: any) => _resolve(data.body),
                    (error: any) => _reject(error.error)
                  );
                });
              });
            } else {
              return _reject(error.error);
            }
          }
        );
      });
    });

    return from(promise);
  }

  public Put<T>(
    url: string,
    body: any | null,
    options: any | null = null
  ): Observable<any> {
    while (this.alreadySendRefresh) {}
    const promise = new Promise((_resolve, _reject) => {
      this._send<T>(this.httpSendType.PUT, url, body, options, null).subscribe(
        (observedData: any) => {
          observedData.subscribe(
            (data: any) => _resolve(data.body),
            (error: any) => {
              if (error.status === 401) {
                this.tryToRefreshToken().subscribe(() => {
                  this._send<T>(
                    this.httpSendType.PUT,
                    url,
                    body,
                    options,
                    null
                  ).subscribe((observedData: any) => {
                    observedData.subscribe(
                      (data: any) => _resolve(data.body),
                      (error: any) => _reject(error.error)
                    );
                  });
                });
              } else {
                return _reject(error.error);
              }
            }
          );
        }
      );
    });

    return from(promise);
  }

  public Patch<T>(
    url: string,
    body: any | null,
    options: any | null = null
  ): Observable<any> {
    while (this.alreadySendRefresh) {}
    const promise = new Promise((_resolve, _reject) => {
      this._send<T>(
        this.httpSendType.PATCH,
        url,
        body,
        options,
        null
      ).subscribe((observedData: any) => {
        observedData.subscribe(
          (data: any) => _resolve(data.body),
          (error: any) => {
            if (error.status === 401) {
              this.tryToRefreshToken().subscribe(() => {
                this._send<T>(
                  this.httpSendType.PATCH,
                  url,
                  body,
                  options,
                  null
                ).subscribe((observedData: any) => {
                  observedData.subscribe(
                    (data: any) => _resolve(data.body),
                    (error: any) => _reject(error.error)
                  );
                });
              });
            } else {
              return _reject(error.error);
            }
          }
        );
      });
    });

    return from(promise);
  }

  /**
   * Core method that creates and returns an Observable of an
   * Observable<HttpResponse<T>>. We used to get a reCAPTCHA token here;
   * now we simply create the request directly.
   */
  public _send<T>(
    type: number,
    url: string,
    body: any | null,
    options: any | null,
    contentType: string | null
  ): Observable<HttpResponse<T>> {
    
    // Ensure options is initialized
    options = options || {};
  
    // Set 'observe' to 'response' to return an HttpResponse<T> instead of an HttpEvent<T>
    options = {
      ...options,
      observe: 'response' as 'response' // Ensures HttpResponse<T> is returned
    };
  
    let headers = new HttpHeaders({
      'Content-Type': contentType ? contentType : 'application/json',
    });
  
    options = { ...options, headers, withCredentials: true };
  
    let request: Observable<HttpResponse<T>>;
  
    switch (type) {
      case this.httpSendType.GET:
        request = this.http.get<T>(url, options) as Observable<HttpResponse<T>>;
        break;
      case this.httpSendType.POST:
        request = this.http.post<T>(url, body, options) as Observable<HttpResponse<T>>;
        break;
      case this.httpSendType.PUT:
        request = this.http.put<T>(url, body, options) as Observable<HttpResponse<T>>;
        break;
      case this.httpSendType.PATCH:
        request = this.http.patch<T>(url, body, options) as Observable<HttpResponse<T>>;
        break;
      case this.httpSendType.DELETE:
        request = this.http.delete<T>(url, options) as Observable<HttpResponse<T>>;
        break;
      default:
        request = this.http.post<T>(url, body, options) as Observable<HttpResponse<T>>;
    }
  
    return request;
  }
  
  
  

  // Remove the old `send(action: string)` that called reCaptcha:
  // public send(action: string): Observable<string> {
  //   return this.recaptchaV3Service.execute(action);
  // }

  private getAutorizationToken(options: any | null): string {
    return '';
  }

  public tryToRefreshToken(isRedirectNeed: boolean = true): Observable<any> {
    if (isRedirectNeed) {
      this.authService.doLogout().then(() => this.router.navigate(['/Home']));
    }

    return of(null);
  }
}
