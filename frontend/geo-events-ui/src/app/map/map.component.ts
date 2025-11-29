import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { EventStoreService } from '../services/event-store.service';
import { GeoEvent, isUnitPosition } from '../models/geo-event';
import { SymbolService } from '../services/symbol.service';
import { Subscription } from 'rxjs';
import { Viewer, Cartesian3, EntityCollection, Color, ConstantPositionProperty, ConstantProperty, VerticalOrigin, Rectangle, Camera, HeadingPitchRange, PolylineDashMaterialProperty } from 'cesium';
import { ZonesService } from '../services/zones.service';

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
  private unitPositions = new Map<string, { lat: number; lon: number }>();
  private onResize = () => {
    try { this.viewer?.resize?.(); this.viewer?.scene?.requestRender?.(); } catch { /* noop */ }
  };
  private resizeObserver?: ResizeObserver;

  constructor(private store: EventStoreService, private symbols: SymbolService, private zones: ZonesService) {}
  // APP-6A SIDC mappings (can be refined later)
  private readonly SIDC = {
    unit: 'SFGPUCI----K', // Friendly Ground Unit, Infantry
    zoneViolation: 'GFGPAX-------', // General point: activity/incident
    proximityAlert: 'GFGPATC------' // General point: target/Contact
  } as const;

  ngAfterViewInit(): void { this.initViewer(); }

  private initViewer(retries = 12): void {
    if (!this.isBrowser) return; // SSR guard
    if (this.viewer) return; // already set up
    const el = this.mapContainer?.nativeElement;
    const w = el?.clientWidth ?? 0;
    const h = el?.clientHeight ?? 0;
    if ((w === 0 || h === 0) && retries > 0) {
      requestAnimationFrame(() => this.initViewer(retries - 1));
      return;
    }
    Camera.DEFAULT_VIEW_RECTANGLE = Rectangle.fromDegrees(25, 35, 45, 43); // Turkey region
    Camera.DEFAULT_VIEW_FACTOR = 0.9;
    this.viewer = new Viewer(el, {
      animation: false,
      timeline: false,
      sceneModePicker: false,
      baseLayerPicker: true, // OK once CESIUM_BASE_URL is set
      geocoder: false,
      navigationHelpButton: true
    });
    this.entities = this.viewer.entities;
    window.addEventListener('resize', this.onResize);
    // Observe container size changes for dynamic layouts
    if ('ResizeObserver' in window) {
      this.resizeObserver = new ResizeObserver(() => this.onResize());
      this.resizeObserver.observe(el);
    }
    // Nudge a resize after initial paint to avoid blank canvas
    setTimeout(this.onResize, 0);
    setTimeout(this.onResize, 200);

    // Load persistent zones from mock data and render as polygons (circle approximation)
    this.zones.getZones().subscribe(zs => {
      zs.forEach((z: any) => {
        const id = z.id || z.zoneId || `zone-${Math.random().toString(36).slice(2)}`;
        const lat = z.centerLat;
        const lon = z.centerLon;
        const radius = z.radiusMeters;
        if (!Number.isFinite(lat) || !Number.isFinite(lon) || !Number.isFinite(radius)) return;
        const positions = this.buildCirclePositions(lat, lon, radius);
        if (!this.entities?.getById(id)) {
          this.entities?.add({
            id,
            polygon: {
              hierarchy: positions,
              material: Color.CYAN.withAlpha(0.08),
              outline: true,
              outlineColor: Color.CYAN
            } as any,
            label: { text: new ConstantProperty(z.name || id), font: '12px sans-serif', fillColor: Color.WHITE }
          });
        }
      });
    });

    // Subscribe to individual events, not the full list
    this.sub = this.store.filtered$.subscribe(list => {
      // Process newest event (first in array)
      const e = list[0];
      if (!e || e.eventId === this.lastEventId) return;
      this.lastEventId = e.eventId;
      this.onEvent(e);
    });
  }

  private onEvent(e: GeoEvent) {
    const lat = (e as any).latitude ?? (e as any).lat ?? (e as any).location?.lat;
    const lon = (e as any).longitude ?? (e as any).lon ?? (e as any).location?.lon;
    if (!Number.isFinite(lat) || !Number.isFinite(lon)) {
      // No coordinates: cannot place on map, but don't block future events.
      console.warn('MapComponent: missing coordinates, event not plotted', e);
      return;
    }
    const position = Cartesian3.fromDegrees(lon, lat);

    if (isUnitPosition(e)) {
      const id = e.source;
      const heading = e.headingDegrees;
      const existing = this.entities?.getById(id);
      const img = this.symbols.buildSymbol(this.SIDC.unit, { size: 42, uniqueDesignation: id });
      // Track last known unit position for proximity geometry
      this.unitPositions.set(id, { lat, lon });

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
        if (existing.position instanceof ConstantPositionProperty) {
          (existing.position as ConstantPositionProperty).setValue(position);
        } else {
          existing.position = new ConstantPositionProperty(position);
        }
        if (existing.label && existing.label.text instanceof ConstantProperty) {
          (existing.label.text as ConstantProperty).setValue(heading !== undefined ? `${id} h:${heading}` : id);
        }
      }

      if (this.followLatest && this.viewer) {
        const ent = this.entities?.getById(id);
        if (ent) {
          this.viewer.flyTo(ent, { offset: new HeadingPitchRange(0, -0.6, 150000) });
        }
      }
      return;
    }

    // Zone violation: draw polygon (circle approximation) with optional label (no milsymbol to avoid unknown icon)
    if ((e as any).type === 'ZONE_VIOLATION') {
      const zid = `zone-${e.source}-${(e as any).zoneIdentifier}`;
      const existing = this.entities?.getById(zid);
      const labelText = `${(e as any).zoneIdentifier} sev:${(e as any).severity}`;
      const radius = (e as any).radiusMeters as number | undefined;
      if (!existing) {
        const zoneEntity: any = { id: zid };
        if (radius) {
          const positions = this.buildCirclePositions(lat, lon, radius);
          zoneEntity.polygon = {
            hierarchy: positions,
            material: Color.ORANGE.withAlpha(0.15),
            outline: true,
            outlineColor: Color.ORANGE
          } as any;
        }
        zoneEntity.label = { text: new ConstantProperty(labelText), font: '12px sans-serif', fillColor: Color.WHITE };
        this.entities?.add(zoneEntity);
      } else {
        if (existing.label && existing.label.text instanceof ConstantProperty) {
          (existing.label.text as ConstantProperty).setValue(labelText);
        }
        if (radius) {
          const positions = this.buildCirclePositions(lat, lon, radius);
          if ((existing as any).polygon) {
            (existing as any).polygon.hierarchy = positions as any;
            (existing as any).polygon.material = Color.ORANGE.withAlpha(0.15) as any;
            (existing as any).polygon.outline = true as any;
            (existing as any).polygon.outlineColor = Color.ORANGE as any;
          } else {
            (existing as any).polygon = {
              hierarchy: positions,
              material: Color.ORANGE.withAlpha(0.15),
              outline: true,
              outlineColor: Color.ORANGE
            } as any;
          }
        }
      }
      return;
    }

    // Proximity alert: dashed ring centered on unit position, point at target, and label
    if ((e as any).type === 'PROXIMITY_ALERT') {
      const pid = `prox-${e.source}-${(e as any).targetIdentifier}`;
      const existing = this.entities?.getById(pid);
      const labelText = `${(e as any).targetIdentifier} d:${(e as any).distanceMeters}m`;
      const proxRadius = (e as any).distanceMeters as number | undefined;
      const unitCenter = this.unitPositions.get(e.source) || { lat, lon };
      if (!existing) {
        this.entities?.add({
          id: pid,
          position: new ConstantPositionProperty(Cartesian3.fromDegrees(lon, lat)),
          point: { pixelSize: 6, color: Color.RED.withAlpha(0.9), outlineColor: Color.WHITE, outlineWidth: 1 },
          label: { text: new ConstantProperty(labelText), font: '12px sans-serif', fillColor: Color.WHITE }
        });
        // Add dashed ring polyline for proximity
        if (proxRadius) {
          const ringId = `${pid}-ring`;
          const positions = this.buildCirclePositions(unitCenter.lat, unitCenter.lon, proxRadius);
          this.entities?.add({
            id: ringId,
            polyline: {
              positions,
              width: 2,
              material: new PolylineDashMaterialProperty({ color: Color.RED, dashLength: 16 })
            }
          });
          const lineId = `${pid}-line`;
          this.entities?.add({
            id: lineId,
            polyline: {
              positions: [
                Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat),
                Cartesian3.fromDegrees(lon, lat)
              ],
              width: 2,
              material: Color.RED.withAlpha(0.6)
            }
          });
        }
      } else {
        if (existing.position instanceof ConstantPositionProperty) {
          (existing.position as ConstantPositionProperty).setValue(Cartesian3.fromDegrees(lon, lat));
        } else {
          existing.position = new ConstantPositionProperty(Cartesian3.fromDegrees(lon, lat));
        }
        if (existing.label && existing.label.text instanceof ConstantProperty) {
          (existing.label.text as ConstantProperty).setValue(labelText);
        }
        // Update or create ring entity
        const ringId = `${pid}-ring`;
        const ring = this.entities?.getById(ringId);
        if (proxRadius) {
          const positions = this.buildCirclePositions(unitCenter.lat, unitCenter.lon, proxRadius);
          if (!ring) {
            this.entities?.add({
              id: ringId,
              polyline: {
                positions,
                width: 2,
                material: new PolylineDashMaterialProperty({ color: Color.RED, dashLength: 16 })
              }
            });
          } else if (ring.polyline) {
            ring.polyline.positions = positions as any;
            ring.polyline.width = 2 as any;
            ring.polyline.material = new PolylineDashMaterialProperty({ color: Color.RED, dashLength: 16 }) as any;
          }
          const lineId = `${pid}-line`;
          const line = this.entities?.getById(lineId);
          const linePositions = [
            Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat),
            Cartesian3.fromDegrees(lon, lat)
          ];
          if (!line) {
            this.entities?.add({ id: lineId, polyline: { positions: linePositions, width: 2, material: Color.RED.withAlpha(0.6) } });
          } else if (line.polyline) {
            line.polyline.positions = linePositions as any;
            line.polyline.width = 2 as any;
            line.polyline.material = Color.RED.withAlpha(0.6) as any;
          }
        }
      }
      return;
    }
  }

  private buildCirclePositions(lat: number, lon: number, radiusMeters: number): Cartesian3[] {
    const result: Cartesian3[] = [];
    const metersPerDegLat = 111320;
    const metersPerDegLon = 111320 * Math.cos((lat * Math.PI) / 180);
    const step = 10; // degrees
    for (let deg = 0; deg <= 360; deg += step) {
      const rad = (deg * Math.PI) / 180;
      const dLat = (Math.sin(rad) * radiusMeters) / metersPerDegLat;
      const dLon = (Math.cos(rad) * radiusMeters) / metersPerDegLon;
      result.push(Cartesian3.fromDegrees(lon + dLon, lat + dLat));
    }
    return result;
  }

  fitToData() {
    if (this.viewer && this.entities && this.entities.values.length > 0) {
      this.viewer.flyTo(this.entities, { offset: new HeadingPitchRange(0, -0.6, 300000) });
    }
  }

  toggleFollow(on: boolean) {
    this.followLatest = on;
  }

  zoomIn() {
    const camera = this.viewer?.camera;
    if (!camera) return;
    const height = camera.positionCartographic.height;
    const amount = Math.max(height * 0.2, camera.defaultZoomAmount);
    camera.zoomIn(amount);
  }

  zoomOut() {
    const camera = this.viewer?.camera;
    if (!camera) return;
    const height = camera.positionCartographic.height;
    const amount = Math.max(height * 0.2, camera.defaultZoomAmount);
    camera.zoomOut(amount);
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    if (this.isBrowser) window.removeEventListener('resize', this.onResize);
    try { this.resizeObserver?.disconnect(); } catch {}
    this.viewer?.destroy();
  }
}
