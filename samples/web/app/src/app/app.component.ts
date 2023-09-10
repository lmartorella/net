import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { InitialPage } from './app-routing.module';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html'
})
export class AppComponent {
  constructor(router: Router) {
    router.navigate([InitialPage]);
  }
}
