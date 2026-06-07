import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { finalize } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Emitters } from 'src/app/Infrastructure/Emitters/Emitters';
import { CustomHttpContextTokens } from 'src/app/Infrastructure/Classes & Models/customClasses/custom-http-context-tokens';
import { LoaderService } from 'src/app/Adapters/loader/service/loader.service';

@Injectable()
export class HttpRequestInterceptor implements HttpInterceptor {

  constructor(private loaderSvc: LoaderService) { }
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const BYPASS_SPINNER = request.context.get(CustomHttpContextTokens.BYPASS_SPINNER);
    const spinnerSubscription: Subscription | null = !BYPASS_SPINNER ? this.loaderSvc.spinner$.subscribe() : null;
    Emitters.isLoadingEmitter.emit(true);
    if (!request.url.endsWith('.json') && !/^https?:\/\//i.test(request.url))
      request = request.clone({
        url: `${environment.target}/${request.url}`
      });

    return next.handle(request.clone({ withCredentials: true }))
      .pipe(
        finalize(() => {
          spinnerSubscription?.unsubscribe()
        })
      )
  }
}
