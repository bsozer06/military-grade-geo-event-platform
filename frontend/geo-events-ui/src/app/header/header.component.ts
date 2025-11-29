import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventStreamService } from '../services/event-stream.service';
import { EventStoreService } from '../services/event-store.service';
import { MockProducerService } from '../services/mock-producer.service';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.scss']
})
export class HeaderComponent {
  private stream = inject(EventStreamService);
  private store = inject(EventStoreService);
  private mock = inject(MockProducerService);

  connected = signal<boolean>(false);
  running = signal<boolean>(false);
  count = signal<number>(0);
  lastError = signal<string | null>(null);

  constructor() {
    this.stream.connectionState().subscribe(v => this.connected.set(!!v));
    this.stream.connectionError().subscribe(e => this.lastError.set(e));
    this.mock.isRunning().subscribe(v => this.running.set(!!v));
    this.store.events$.subscribe(list => this.count.set(list.length));
  }

  toggleMock() { this.mock.toggle(); }
  clear() { this.store.clear(); }
}
