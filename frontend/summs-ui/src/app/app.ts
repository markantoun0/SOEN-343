import { Component, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JsonPipe } from '@angular/common';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [JsonPipe, RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  private http = inject(HttpClient);

  result: any = null;

  pingApi() {
    this.http.get('/api/ping').subscribe({
      next: (r) => (this.result = r),
      error: (e) => (this.result = e),
    });
  }
}
