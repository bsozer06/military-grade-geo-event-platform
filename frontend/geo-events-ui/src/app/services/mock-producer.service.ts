import { Injectable, OnDestroy, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BehaviorSubject, Subscription, interval } from 'rxjs';
import { EventStoreService } from './event-store.service';
import { GeoEvent } from '../models/geo-event';

@Injectable({ providedIn: 'root' })
export class MockProducerService implements OnDestroy {
  private sub?: Subscription;
  private running$ = new BehaviorSubject<boolean>(false);
  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  constructor(private store: EventStoreService) {}

  isRunning() { return this.running$.asObservable(); }

  start(periodMs = 5000) {
    if (!this.isBrowser) return;
    if (this.sub) return;
    this.sub = interval(periodMs).subscribe(() => this.pushRandom());
    this.running$.next(true);
  }

  stop() {
    this.sub?.unsubscribe();
    this.sub = undefined;
    this.running$.next(false);
  }

  toggle() { this.sub ? this.stop() : this.start(); }

  private pushRandom() {
    const now = new Date().toISOString();
    const id = (globalThis.crypto?.randomUUID && globalThis.crypto.randomUUID()) || Math.random().toString(36).slice(2);
    const kinds: Array<GeoEvent['type']> = ['UNIT_POSITION', 'ZONE_VIOLATION', 'PROXIMITY_ALERT'];
    const type = kinds[Math.floor(Math.random() * kinds.length)];
    const base: any = {
      eventId: id,
      type,
      timestamp: now,
      source: 'mock-unit-' + Math.floor(Math.random()*5 + 1),
      location: { lat: 41 + Math.random(), lon: 29 + Math.random() }
    };
    if (type === 'UNIT_POSITION') base.metadata = { speed: 40 + Math.random() * 20, heading: Math.floor(Math.random() * 360) };
    if (type === 'ZONE_VIOLATION') base.metadata = { zoneId: 'Z-0' + Math.floor(Math.random()*3+1), zoneName: 'Demo Zone', distanceMeters: Math.round(Math.random() * 200) };
    if (type === 'PROXIMITY_ALERT') base.metadata = { otherUnitId: 'mock-'+Math.floor(Math.random()*5+1), separationMeters: Math.round(50 + Math.random() * 500) };
    this.store.push(base as GeoEvent);
  }

  ngOnDestroy(): void {
    this.stop();
    this.running$.complete();
  }
}
