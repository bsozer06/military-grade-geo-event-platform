import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { EventStoreService } from '../services/event-store.service';
import { GeoEvent, isUnitPosition } from '../models/geo-event';
import { SymbolService } from '../services/symbol.service';
import { Subscription } from 'rxjs';
import { Viewer, Cartesian3, EntityCollection, Color, ConstantPositionProperty, ConstantProperty, VerticalOrigin, Rectangle, Camera, HeadingPitchRange } from 'cesium';

@Component({
  selector: 'app-map',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.scss']
})
export class MapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer', { static: true }) mapContainer!: ElementRef<HTMLDivElement>;
  private viewer?: Viewer;
  private sub?: Subscription;
  private entities?: EntityCollection;
  private isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  followLatest = false;

  private lastEventId?: string;
  private hasZoomed = false;

  constructor(private store: EventStoreService, private symbols: SymbolService) {}

  ngAfterViewInit(): void {
    if (!this.isBrowser) {
      return; // SSR guard
    }
    Camera.DEFAULT_VIEW_RECTANGLE = Rectangle.fromDegrees(25, 35, 45, 43); // Turkey region
    Camera.DEFAULT_VIEW_FACTOR = 0.9;
    this.viewer = new Viewer(this.mapContainer.nativeElement, {
      animation: false,
      timeline: false,
      sceneModePicker: false,
      baseLayerPicker: true, // OK once CESIUM_BASE_URL is set
      geocoder: false,
      navigationHelpButton: true
    });
    this.entities = this.viewer.entities;
    this.sub = this.store.events$.subscribe(list => {
      const e = list[0];
      if (!e || e.eventId === this.lastEventId) return;
      this.lastEventId = e.eventId;
      this.onEvent(e);
    });
  }

  private onEvent(e: GeoEvent) {
    if (isUnitPosition(e)) {
      const id = e.source;
      const lat = e.location.lat;
      const lon = e.location.lon;
      const speed = e.metadata?.speed;
      const heading = e.metadata?.heading;
      const existing = this.entities?.getById(id);
      const img = this.symbols.buildSymbol('SFGPUCI----K', { size: 42, uniqueDesignation: id }); // Example SIDC
      const position = Cartesian3.fromDegrees(lon, lat);
      if (!existing) {
        this.entities?.add({
          id,
          position: new ConstantPositionProperty(position),
          billboard: {
            image: img,
            verticalOrigin: VerticalOrigin.BOTTOM,
            scale: 1.0
          },
          label: {
            text: new ConstantProperty(heading !== undefined ? `${id} h:${heading}` : id),
            font: '12px sans-serif',
            fillColor: Color.WHITE
          }
        });
        if (!this.hasZoomed && this.viewer && this.entities && this.entities.values.length > 0) {
          this.hasZoomed = true;
          this.viewer.flyTo(this.entities, { offset: new HeadingPitchRange(0, -0.6, 300000) });
        }
      } else {
        existing.position = new ConstantPositionProperty(position);
        if (existing.label) {
          existing.label.text = new ConstantProperty(heading !== undefined ? `${id} h:${heading}` : id);
        }
      }

      if (this.followLatest && this.viewer) {
        const ent = this.entities?.getById(id);
        if (ent) {
          this.viewer.flyTo(ent, { offset: new HeadingPitchRange(0, -0.6, 150000) });
        }
      }
    }
  }

  fitToData() {
    if (this.viewer && this.entities && this.entities.values.length > 0) {
      this.viewer.flyTo(this.entities, { offset: new HeadingPitchRange(0, -0.6, 300000) });
    }
  }

  toggleFollow(on: boolean) {
    this.followLatest = on;
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.viewer?.destroy();
  }
}
