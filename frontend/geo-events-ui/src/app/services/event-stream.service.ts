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

    this.hub.on('geo-event', (payload: any) => {
      // Normalize and forward; production should validate schema strictly
      try {
        console.log('[EventStreamService] received from hub:', payload?.type, payload);
      } catch {}
      const e = this.normalizeEvent(payload);
      try {
        console.log('[EventStreamService] normalized event:', e?.type, e);
      } catch {}
      this.events$.next(e as any);
    });

    this.hub.start()
      .then(() => {
        this.connected$.next(true);
        this.error$.next(null);
        try { console.log('[EventStreamService] SignalR connected'); } catch {}
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

  private normalizeEvent(e: any): any {
    if (!e || typeof e !== 'object') return e;
    // Normalize type to uppercase
    if (e.type && typeof e.type === 'string') e.type = e.type.toUpperCase();
    // Prefer top-level lat/lon when location is provided
    if ((e.location && e.location.lat !== undefined) && e.latitude === undefined) e.latitude = e.location.lat;
    if ((e.location && e.location.lon !== undefined) && e.longitude === undefined) e.longitude = e.location.lon;
    // ZoneViolation: ensure zoneIdentifier present
    if (e.type === 'ZONE_VIOLATION') {
      if (!e.zoneIdentifier && e.zoneId) e.zoneIdentifier = e.zoneId;
    }
    // ProximityAlert: ensure targetIdentifier present
    if (e.type === 'PROXIMITY_ALERT') {
      if (!e.targetIdentifier && e.otherUnit) e.targetIdentifier = e.otherUnit;
    }
    return e;
  }

  ngOnDestroy(): void {
    this.hub?.stop();
    this.events$.complete();
    this.connected$.complete();
    this.error$.complete();
  }
}
