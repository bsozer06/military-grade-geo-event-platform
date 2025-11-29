import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser, CommonModule } from '@angular/common';
import { EventStoreService } from '../services/event-store.service';
import { GeoEvent, isUnitPosition } from '../models/geo-event';
import { SymbolService } from '../services/symbol.service';
import { Subscription } from 'rxjs';
import { Viewer, Cartesian3, EntityCollection, Color, ConstantPositionProperty, ConstantProperty, VerticalOrigin, Rectangle, Camera, HeadingPitchRange, PolylineDashMaterialProperty, PolylineArrowMaterialProperty } from 'cesium';
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
  private followPauseUntil?: number;

  private lastEventId?: string;
  private hasZoomed = false;
  private unitPositions = new Map<string, { lat: number; lon: number }>();
  showArrows = true;
  private arrowIds = new Set<string>();
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
      sceneModePicker: true,
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
      console.log('MapComponent: received zones for rendering:', zs);
      zs.forEach((z: any) => {
        const id = z.zoneId || z.id || `zone-${Math.random().toString(36).slice(2)}`;
        const lat = z.centerLat;
        const lon = z.centerLon;
        const radius = z.radiusMeters;
        if (!Number.isFinite(lat) || !Number.isFinite(lon) || !Number.isFinite(radius)) {
          console.warn('MapComponent: skipping zone with invalid coords/radius:', z);
          return;
        }
        const positions = this.buildCirclePositions(lat, lon, radius);
        if (!this.entities?.getById(id)) {
          console.log('MapComponent: adding persistent zone:', id, 'at', lat, lon, 'radius', radius);
          this.entities?.add({
            id,
            polygon: {
              hierarchy: positions,
              material: Color.CYAN.withAlpha(0.2),
              outline: true,
              outlineColor: Color.CYAN,
              outlineWidth: 2
            } as any,
            label: { text: new ConstantProperty(z.name || id), font: '14px sans-serif', fillColor: Color.CYAN, outlineColor: Color.BLACK, outlineWidth: 2 }
          });
        }
      });
    });

    // Subscribe to individual events, not the full list
    this.sub = this.store.filtered$.subscribe(list => {
      // Process newest event (first in array)
      const e = list[0];
      if (this.entities && list.length === 0) {
        try { this.entities.removeAll(); this.viewer?.scene?.requestRender?.(); } catch {}
      }
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
      const prev = this.unitPositions.get(id);
      const existing = this.entities?.getById(id);
      const img = this.symbols.buildSymbol(this.SIDC.unit, { size: 28, uniqueDesignation: id });
      // Draw movement arrow from previous to current, if available
      if (prev) {
        const metersPerDegLat = 111320;
        const metersPerDegLon = 111320 * Math.cos((lat * Math.PI) / 180);
        const dLat = (lat - prev.lat) * metersPerDegLat;
        const dLon = (lon - prev.lon) * metersPerDegLon;
        const dist = Math.hypot(dLat, dLon);
        if (dist >= 3) {
          const arrowId = `move-${id}-arrow`;
          const arrow = this.entities?.getById(arrowId);
          const positions = [
            Cartesian3.fromDegrees(prev.lon, prev.lat),
            Cartesian3.fromDegrees(lon, lat)
          ];
          if (!arrow) {
            this.entities?.add({
              id: arrowId,
              polyline: {
                positions,
                width: 4,
                material: new PolylineArrowMaterialProperty(Color.YELLOW.withAlpha(0.9))
              },
              show: this.showArrows
            });
            this.arrowIds.add(arrowId);
          } else if (arrow.polyline) {
            arrow.polyline.positions = positions as any;
            arrow.polyline.width = 4 as any;
            arrow.polyline.material = new PolylineArrowMaterialProperty(Color.YELLOW.withAlpha(0.9)) as any;
            arrow.show = this.showArrows;
          }
        }
      }

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
          // Closer initial fit for better live visibility
          this.viewer.flyTo(this.entities, { offset: new HeadingPitchRange(0, -0.85, 120000), duration: 0.8 });
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

      const now = Date.now();
      const followAllowed = this.followLatest && (!this.followPauseUntil || now > this.followPauseUntil);
      if (followAllowed && this.viewer) {
        const ent = this.entities?.getById(id);
        if (ent) {
          // Tighter follow camera for close-up tracking
          this.viewer.flyTo(ent, { offset: new HeadingPitchRange(0, -0.9, 60000), duration: 0.6 });
        }
      }
      // Track last known unit position for proximity geometry and movement arrows
      this.unitPositions.set(id, { lat, lon });
      return;
    }

    // Zone violation: draw polygon (circle approximation) with optional label (no milsymbol to avoid unknown icon)
    if ((e as any).type === 'ZONE_VIOLATION') {
      const zidRaw = (e as any).zoneIdentifier ?? (e as any).zoneId ?? (e as any).zoneID;
      const zid = `zone-${e.source}-${zidRaw ?? 'unknown'}`;
      const existing = this.entities?.getById(zid);
      const labelText = `${zidRaw ?? ''} sev:${(e as any).severity ?? ''}`.trim();
      let radius = (e as any).radiusMeters as number | undefined;
      if (!existing) {
        const zoneEntity: any = { id: zid };
        if (Number.isFinite(radius)) {
          const r = radius as number;
          const positions = this.buildCirclePositions(lat, lon, r);
          zoneEntity.polygon = {
            hierarchy: positions,
            material: Color.ORANGE.withAlpha(0.15),
            outline: true,
            outlineColor: Color.ORANGE
          } as any;
        } else {
          // Fallback: mark the violation location with a small point
          zoneEntity.position = new ConstantPositionProperty(Cartesian3.fromDegrees(lon, lat));
          zoneEntity.point = { pixelSize: 6, color: Color.ORANGE.withAlpha(0.9), outlineColor: Color.WHITE, outlineWidth: 1 };
        }
        zoneEntity.label = { text: new ConstantProperty(labelText), font: '12px sans-serif', fillColor: Color.WHITE };
        this.entities?.add(zoneEntity);
      } else {
        if (existing.label && existing.label.text instanceof ConstantProperty) {
          (existing.label.text as ConstantProperty).setValue(labelText);
        }
        if (Number.isFinite(radius)) {
          const r = radius as number;
          const positions = this.buildCirclePositions(lat, lon, r);
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
        } else {
          // Ensure we at least place a point at violation location
          (existing as any).polygon = undefined;
          if (existing.position instanceof ConstantPositionProperty) {
            (existing.position as ConstantPositionProperty).setValue(Cartesian3.fromDegrees(lon, lat));
          } else {
            existing.position = new ConstantPositionProperty(Cartesian3.fromDegrees(lon, lat));
          }
          (existing as any).point = { pixelSize: 6, color: Color.ORANGE.withAlpha(0.9), outlineColor: Color.WHITE, outlineWidth: 1 } as any;
        }
      }
      return;
    }

    // Proximity alert: dashed ring centered on unit position, point at target, and label
    if ((e as any).type === 'PROXIMITY_ALERT') {
      const targetId = (e as any).targetIdentifier ?? (e as any).otherUnit ?? (e as any).targetId;
      const pid = `prox-${e.source}-${targetId}`;
      const existing = this.entities?.getById(pid);
      const labelText = `${targetId} d:${(e as any).distanceMeters}m`;
      const proxRadius = (e as any).distanceMeters as number | undefined;
      const unitCenter = this.unitPositions.get(e.source) || { lat, lon };
      const otherCenter = targetId ? this.unitPositions.get(targetId) : undefined;
      if (!existing) {
        // If proximity event lacks coordinates, fall back to unit positions
        const basePos = Number.isFinite(lat) && Number.isFinite(lon)
          ? Cartesian3.fromDegrees(lon, lat)
          : Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat);
        this.entities?.add({
          id: pid,
          position: new ConstantPositionProperty(basePos),
          point: { pixelSize: 6, color: Color.RED.withAlpha(0.9), outlineColor: Color.WHITE, outlineWidth: 1 },
          label: { text: new ConstantProperty(labelText), font: '12px sans-serif', fillColor: Color.WHITE }
        });
        // Add dashed ring polyline for proximity
        if (Number.isFinite(proxRadius)) {
          const r = proxRadius as number;
          const ringId = `${pid}-ring`;
          const positions = this.buildCirclePositions(unitCenter.lat, unitCenter.lon, r);
          this.entities?.add({
            id: ringId,
            polyline: {
              positions,
              width: 2,
              material: new PolylineDashMaterialProperty({ color: Color.RED, dashLength: 16 })
            }
          });
          const lineId = `${pid}-line`;
          const linePositions = otherCenter
            ? [Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat), Cartesian3.fromDegrees(otherCenter.lon, otherCenter.lat)]
            : (Number.isFinite(lat) && Number.isFinite(lon)
              ? [Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat), Cartesian3.fromDegrees(lon, lat)]
              : undefined);
          if (linePositions) {
            this.entities?.add({
              id: lineId,
              polyline: { positions: linePositions, width: 2, material: Color.RED.withAlpha(0.6) }
            });
          }
        }
      } else {
        const basePos = Number.isFinite(lat) && Number.isFinite(lon)
          ? Cartesian3.fromDegrees(lon, lat)
          : Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat);
        if (existing.position instanceof ConstantPositionProperty) {
          (existing.position as ConstantPositionProperty).setValue(basePos);
        } else {
          existing.position = new ConstantPositionProperty(basePos);
        }
        if (existing.label && existing.label.text instanceof ConstantProperty) {
          (existing.label.text as ConstantProperty).setValue(labelText);
        }
        // Update or create ring entity
        const ringId = `${pid}-ring`;
        const ring = this.entities?.getById(ringId);
        if (Number.isFinite(proxRadius)) {
          const r = proxRadius as number;
          const positions = this.buildCirclePositions(unitCenter.lat, unitCenter.lon, r);
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
          const updatedLinePositions = otherCenter
            ? [Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat), Cartesian3.fromDegrees(otherCenter.lon, otherCenter.lat)]
            : (Number.isFinite(lat) && Number.isFinite(lon)
              ? [Cartesian3.fromDegrees(unitCenter.lon, unitCenter.lat), Cartesian3.fromDegrees(lon, lat)]
              : undefined);
          if (updatedLinePositions) {
            if (!line) {
              this.entities?.add({ id: lineId, polyline: { positions: updatedLinePositions, width: 2, material: Color.RED.withAlpha(0.6) } });
            } else if (line.polyline) {
              line.polyline.positions = updatedLinePositions as any;
              line.polyline.width = 2 as any;
              line.polyline.material = Color.RED.withAlpha(0.6) as any;
            }
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
      try { this.viewer.camera.cancelFlight(); } catch {}
      this.viewer.flyTo(this.entities, { offset: new HeadingPitchRange(0, -0.6, 50000) });
    }
  }

  toggleFollow(on: boolean) {
    this.followLatest = on;
  }

  toggleArrows(on: boolean) {
    this.showArrows = on;
    this.arrowIds.forEach(id => {
      const ent = this.entities?.getById(id);
      if (ent) ent.show = on;
    });
  }

  // Using Cesium's built-in Scene Mode Picker for 2D/3D toggle

  zoomIn() {
    const camera = this.viewer?.camera;
    if (!camera) return;
    try { camera.cancelFlight(); } catch {}
    const height = camera.positionCartographic.height;
    const amount = Math.max(height * 0.5, camera.defaultZoomAmount);
    camera.zoomIn(amount);
    this.pauseFollow(3000);
  }

  zoomOut() {
    const camera = this.viewer?.camera;
    if (!camera) return;
    try { camera.cancelFlight(); } catch {}
    const height = camera.positionCartographic.height;
    const amount = Math.max(height * 0.5, camera.defaultZoomAmount);
    camera.zoomOut(amount);
    this.pauseFollow(3000);
  }

  private pauseFollow(ms: number) {
    this.followPauseUntil = Date.now() + ms;
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    if (this.isBrowser) window.removeEventListener('resize', this.onResize);
    try { this.resizeObserver?.disconnect(); } catch {}
    this.viewer?.destroy();
  }
}
