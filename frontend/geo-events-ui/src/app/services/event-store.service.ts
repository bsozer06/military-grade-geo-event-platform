import { Injectable, OnDestroy } from '@angular/core';
import { BehaviorSubject, Subscription, combineLatest, map } from 'rxjs';
import { EventStreamService } from './event-stream.service';
import { GeoEvent } from '../models/geo-event';

@Injectable({ providedIn: 'root' })
export class EventStoreService implements OnDestroy {
  private readonly maxBuffer = 200;
  private sub?: Subscription;

  private eventsSubject = new BehaviorSubject<GeoEvent[]>([]);
  private filterSubject = new BehaviorSubject<Set<string>>(new Set(['UNIT_POSITION', 'ZONE_VIOLATION', 'PROXIMITY_ALERT']));

  readonly events$ = this.eventsSubject.asObservable();
  readonly filters$ = this.filterSubject.asObservable();
  readonly filtered$ = combineLatest([this.events$, this.filters$]).pipe(
    map(([events, filters]) => events.filter(e => filters.has(e.type)))
  );

  constructor(stream: EventStreamService) {
    this.sub = stream.stream().subscribe(e => this.push(e));
  }

  setFilter(type: string, enabled: boolean) {
    const next = new Set(this.filterSubject.value);
    if (enabled) next.add(type); else next.delete(type);
    this.filterSubject.next(next);
    try { console.log('[EventStoreService] filter set:', type, enabled); } catch {}
  }

  push(e: GeoEvent) {
    try { console.log('[EventStoreService] push event:', e.type, e.eventId, e); } catch {}
    const current = this.eventsSubject.value;
    const next = [e, ...current];
    if (next.length > this.maxBuffer) next.length = this.maxBuffer;
    this.eventsSubject.next(next);
    try { console.log('[EventStoreService] buffer size:', next.length); } catch {}
  }

  clear() { try { console.log('[EventStoreService] clear called'); } catch {} this.eventsSubject.next([]); }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.eventsSubject.complete();
    this.filterSubject.complete();
  }
}
