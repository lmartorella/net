import { ErrorHandler, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { AdminComponent } from './components/admin';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { GardenComponent } from './components/garden';
import { LoginComponent } from './components/login';
import { LoginInterceptor } from './services/interceptor';
import { AlertErrorHandler } from './services/errorHandler';

import { SolarModule } from "@lucky-home/solar-lib";

@NgModule({
  declarations: [
    AppComponent,
    AdminComponent,
    GardenComponent,
    LoginComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    FormsModule,
    SolarModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: LoginInterceptor, multi: true },
    { provide: ErrorHandler, useClass: AlertErrorHandler }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
