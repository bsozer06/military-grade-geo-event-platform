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

  start(periodMs = 3000) {
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
    const lat = 39 + Math.random() * 3; // Turkey region
    const lon = 27 + Math.random() * 3;
    const base: any = {
      eventId: id,
      type: 'UNIT_POSITION',
      timestamp: now,
      source: 'mock-unit-' + Math.floor(Math.random()*5 + 1),
      latitude: lat,
      longitude: lon,
      altitude: null
    };
    // Emit unit position
    base.headingDegrees = Math.floor(Math.random() * 360);
    base.speedMps = 10 + Math.random() * 20; // 10-30 m/s
    this.store.push(base as GeoEvent);

    // Emit a zone violation ensuring the unit is likely inside the zone
    const zoneRadius = 150 + Math.floor(Math.random() * 200); // 150-350 m
    const zoneBearing = Math.random() * 360; // degrees
    const metersPerDegLat = 111320;
    const metersPerDegLon = 111320 * Math.cos((lat * Math.PI) / 180);
    const zoneOffsetMeters = Math.random() * zoneRadius * 0.7; // keep center within 70% of radius from unit
    const zoneRad = (zoneBearing * Math.PI) / 180;
    const zLat = lat + (Math.sin(zoneRad) * zoneOffsetMeters) / metersPerDegLat;
    const zLon = lon + (Math.cos(zoneRad) * zoneOffsetMeters) / metersPerDegLon;
    const zoneEvent: any = {
      eventId: (globalThis.crypto?.randomUUID && globalThis.crypto.randomUUID()) || Math.random().toString(36).slice(2),
      type: 'ZONE_VIOLATION',
      timestamp: now,
      source: base.source,
      latitude: zLat,
      longitude: zLon,
      altitude: null,
      zoneIdentifier: 'Z-0' + Math.floor(Math.random()*3+1),
      severity: Math.floor(Math.random() * 3) + 1,
      metadata: 'Mock zone violation',
      radiusMeters: zoneRadius
    };
    this.store.push(zoneEvent as GeoEvent);

    // Emit a proximity alert: target at a distance and bearing from the unit
    const proxDistance = Math.round(50 + Math.random() * 200); // 50-250 m
    const proxBearing = Math.random() * 360; // degrees
    const proxRad = (proxBearing * Math.PI) / 180;
    const pLat = lat + (Math.sin(proxRad) * proxDistance) / metersPerDegLat;
    const pLon = lon + (Math.cos(proxRad) * proxDistance) / metersPerDegLon;
    const proxEvent: any = {
      eventId: (globalThis.crypto?.randomUUID && globalThis.crypto.randomUUID()) || Math.random().toString(36).slice(2),
      type: 'PROXIMITY_ALERT',
      timestamp: now,
      source: base.source,
      latitude: pLat,
      longitude: pLon,
      altitude: null,
      targetIdentifier: 'mock-' + Math.floor(Math.random()*5+1),
      distanceMeters: proxDistance,
      severity: Math.floor(Math.random() * 3) + 1
    };
    this.store.push(proxEvent as GeoEvent);
  }

  ngOnDestroy(): void {
    this.stop();
    this.running$.complete();
  }
}
