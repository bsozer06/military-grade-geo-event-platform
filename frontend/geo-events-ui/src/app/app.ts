import { Component, signal } from '@angular/core';
import { MapComponent } from './map/map.component';
import { EventStreamComponent } from './event-stream/event-stream.component';
import { HeaderComponent } from './header/header.component';

@Component({
  selector: 'app-root',
  imports: [MapComponent, EventStreamComponent, HeaderComponent],
  template: '<div class="app-root"><app-header></app-header><div class="app-shell"><div class="map-pane"><app-map></app-map></div><div class="stream-pane"><app-event-stream></app-event-stream></div></div></div>',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('geo-events-ui');
}
