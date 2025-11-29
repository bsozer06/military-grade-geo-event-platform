import { Injectable, OnDestroy, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr';
import { Subject, Observable, BehaviorSubject } from 'rxjs';
import { GeoEvent } from '../models/geo-event';

@Injectable({ providedIn: 'root' })
export class EventStreamService implements OnDestroy {
  private hub?: HubConnection;
  private events$ = new Subject<GeoEvent>();
  private connected$ = new BehaviorSubject<boolean>(false);
  private error$ = new BehaviorSubject<string | null>(null);

  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  constructor() {
    if (this.isBrowser) {
      this.init();
    }
  }

  private init() {
    const url = (window as any).__GEO_EVENTS_SIGNALR_URL__ as string | undefined;
    if (!url) {
      console.info('EventStreamService: SignalR URL not configured, skipping connect');
      return;
    }
    this.hub = new HubConnectionBuilder()
      .withUrl(url)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Error)
      .build();

    this.hub.on('geo-event', (payload: GeoEvent) => {
      // Basic shape trust; production should validate schema
      this.events$.next(payload);
    });

    this.hub.start()
      .then(() => {
        this.connected$.next(true);
        this.error$.next(null);
      })
      .catch(err => {
        const message = err?.message || 'SignalR negotiation failed';
        console.warn('SignalR connect failed:', message);
        this.connected$.next(false);
        this.error$.next(message);
        // Keep app running; no throw.
      });
  }

  stream(): Observable<GeoEvent> { return this.events$.asObservable(); }
  connectionState(): Observable<boolean> { return this.connected$.asObservable(); }
  connectionError(): Observable<string | null> { return this.error$.asObservable(); }

  ngOnDestroy(): void {
    this.hub?.stop();
    this.events$.complete();
    this.connected$.complete();
    this.error$.complete();
  }
}
