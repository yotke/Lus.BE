import { NgModule, InjectionToken } from '@angular/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { MaterialModule } from './Infrastructure/material/material.module';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NgSelectModule } from '@ng-select/ng-select';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatInputModule } from '@angular/material/input';
import { HTTP_INTERCEPTORS, HttpClientModule, provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { BrowserModule } from '@angular/platform-browser';
import { CommonModule, DatePipe, HashLocationStrategy, LocationStrategy } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { DataLoaderComponent } from './Activities/data-loader/data-loader.component';
import { ProjectsManagerComponent } from './Activities/projects-manager/projects-manager.component';
import { CreateProjectComponent } from './Activities/create-project/create-project.component';
import { AuthService } from './Infrastructure/Services/Auth/auth.service';
import { JWT_OPTIONS, JwtHelperService, JwtModule } from '@auth0/angular-jwt';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { environment } from 'src/environments/environment';
import { HttpRequestInterceptor } from './Adapters/Interceptors/blockUI-request/http-request-interceptor';
import { AppConsts } from './Infrastructure/Classes & Models/customClasses/app-consts';
import { HomeComponent } from './Adapters/HomePageComponents/home/home.component';
import { MatStepperModule } from '@angular/material/stepper';
import { HeaderComponent } from './Adapters/HomePageComponents/header/header.component';
import { ExcelExportComponent } from './Activities/excel-export/excel-export.component';
import { ProjectValidatorComponent } from './Activities/project-validator/project-validator.component';
import { HistoryProjectsComponent } from './Activities/history-projects/history-projects.component';
import { ProjectRowComponent } from './Activities/history-projects/project-row/project-row.component';
import { ProjectTextRowComponent } from './Activities/history-projects/project-text-row/project-text-row.component';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { CommonHelpersModule } from './Adapters/Common/common-helpers.module';
import { AuthInterceptor } from './Adapters/Interceptors/auth.interceptors/auth.interceptor';
import { CsrfInterceptor } from './Adapters/Interceptors/auth.interceptors/csrf.interceptor';
// import { RECAPTCHA_V3_SITE_KEY, ReCaptchaV3Service, RecaptchaModule } from 'ng-recaptcha';


export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL');
export function getBaseUrl(): string {
  return AppConsts.baseUrl;
}
declare var $: any;


@NgModule({
  declarations: [
    AppComponent,
    DataLoaderComponent,
    ProjectsManagerComponent,
    CreateProjectComponent,
    HomeComponent,
    HeaderComponent,
    ExcelExportComponent,
    ProjectValidatorComponent,
    HistoryProjectsComponent,
    ProjectRowComponent,
    ProjectTextRowComponent,

  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    MaterialModule,
    FormsModule,
    NgSelectModule,
    MatPaginatorModule,
    CommonModule,
    BrowserAnimationsModule,
    MatInputModule,
    ReactiveFormsModule,
    HttpClientModule,
    MatIconModule,
    MatButtonModule,
    MatToolbarModule,
    // NgxMatTimepickerModule.setLocale('en-GB'),
    // NgxMatTimepickerModule.setLocale('he-IL'),
    JwtModule,
    MatStepperModule,
    // RecaptchaModule,
    CommonHelpersModule

  ],
  providers: [

    { provide: API_BASE_URL, useFactory: getBaseUrl },
    { provide: LocationStrategy, useClass: HashLocationStrategy },
    { provide: JWT_OPTIONS, useValue: JWT_OPTIONS }, JwtHelperService,
    // { provide: RECAPTCHA_V3_SITE_KEY, useValue: environment.recaptcha.siteKey },
    { provide: HTTP_INTERCEPTORS, useClass: HttpRequestInterceptor, multi: true },
    provideHttpClient(
      withInterceptorsFromDi()  // Enable DI-based interceptors
  ),
  { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
  { provide: HTTP_INTERCEPTORS, useClass: CsrfInterceptor, multi: true },
    { provide: MAT_DATE_LOCALE, useValue: 'he-IL' },
    AuthService,
    { provide: DatePipe },
    // { provide: ReCaptchaV3Service },
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
