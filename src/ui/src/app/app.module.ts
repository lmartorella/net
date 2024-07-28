import { ErrorHandler, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AdminComponent } from './components/admin';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { SolarComponent } from './components/solar';
import { GardenComponent } from './components/garden';
import { LoginComponent } from './components/login';
import { LoginInterceptor } from './services/interceptor';
import { AlertErrorHandler } from './services/errorHandler';
import { XhrService } from './services/xhr';
import { CommonModule } from '@angular/common';

@NgModule({
  declarations: [
    AppComponent,
    AdminComponent,
    SolarComponent,
    GardenComponent,
    LoginComponent
  ],
  imports: [
    CommonModule,
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
  ],
  providers: [
    XhrService,
    { provide: HTTP_INTERCEPTORS, useClass: LoginInterceptor, multi: true },
    { provide: ErrorHandler, useClass: AlertErrorHandler }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
