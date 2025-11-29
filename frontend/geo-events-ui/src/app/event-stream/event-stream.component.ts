import { Component, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { EventStoreService } from '../services/event-store.service';
import { GeoEvent } from '../models/geo-event';

@Component({
  selector: 'app-event-stream',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './event-stream.component.html',
  styleUrls: ['./event-stream.component.scss']
})
export class EventStreamComponent {
  private store = inject(EventStoreService);

  // Filter toggles
  showUnit = signal(true);
  showZone = signal(true);
  showProx = signal(true);

  events = signal<GeoEvent[]>([]);

  constructor() {
    effect(() => {
      this.store.setFilter('UNIT_POSITION', this.showUnit());
      this.store.setFilter('ZONE_VIOLATION', this.showZone());
      this.store.setFilter('PROXIMITY_ALERT', this.showProx());
    });

    this.store.filtered$.subscribe(list => this.events.set(list));
  }

  clear() { this.store.clear(); }

  addMock() {
    const now = new Date().toISOString();
    const id = (crypto?.randomUUID && crypto.randomUUID()) || Math.random().toString(36).slice(2);
    const kinds: Array<GeoEvent['type']> = ['UNIT_POSITION', 'ZONE_VIOLATION', 'PROXIMITY_ALERT'];
    const type = kinds[Math.floor(Math.random() * kinds.length)];
    const base = {
      eventId: id,
      type,
      timestamp: now,
      source: 'mock-unit',
      location: { lat: 41 + Math.random(), lon: 29 + Math.random() }
    } as any;
    if (type === 'UNIT_POSITION') base.metadata = { speed: 40 + Math.random() * 20, heading: Math.floor(Math.random() * 360) };
    if (type === 'ZONE_VIOLATION') base.metadata = { zoneId: 'Z-01', zoneName: 'Demo Zone', distanceMeters: Math.round(Math.random() * 200) };
    if (type === 'PROXIMITY_ALERT') base.metadata = { otherUnitId: 'mock-2', separationMeters: Math.round(50 + Math.random() * 500) };
    this.store.push(base as GeoEvent);
  }

  latOf(e: any): number | null {
    return Number.isFinite(e?.latitude) ? e.latitude : (Number.isFinite(e?.lat) ? e.lat : (Number.isFinite(e?.location?.lat) ? e.location.lat : null));
  }
  lonOf(e: any): number | null {
    return Number.isFinite(e?.longitude) ? e.longitude : (Number.isFinite(e?.lon) ? e.lon : (Number.isFinite(e?.location?.lon) ? e.location.lon : null));
  }
  detailsOf(e: any): string {
    if (e?.type === 'UNIT_POSITION') {
      const h = e.headingDegrees ?? e.metadata?.heading;
      const s = e.speedMps ?? e.metadata?.speed;
      return `heading:${h ?? '-'} speed:${s ?? '-'}`;
    }
    if (e?.type === 'ZONE_VIOLATION') {
      const z = e.zoneIdentifier || e.metadata?.zoneId || e.metadata?.zoneName;
      const sev = e.severity ?? '-';
      const r = e.radiusMeters ?? e.metadata?.distanceMeters;
      return `zone:${z ?? '-'} sev:${sev} r:${r ?? '-'}m`;
    }
    if (e?.type === 'PROXIMITY_ALERT') {
      const t = e.targetIdentifier ?? e.metadata?.otherUnitId;
      const d = e.distanceMeters ?? e.metadata?.separationMeters;
      const sev = e.severity ?? '-';
      return `target:${t ?? '-'} d:${d ?? '-'}m sev:${sev}`;
    }
    return '';
  }
}
