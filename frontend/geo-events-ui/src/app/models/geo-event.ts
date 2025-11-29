export interface GeoLocation {
  lat: number;
  lon: number;
}

export interface BaseGeoEvent {
  eventId: string; // Guid string
  type: string;
  timestamp: string; // ISO8601
  source: string;
  latitude: number;
  longitude: number;
  altitude?: number | null;
}

export interface UnitPositionEvent extends BaseGeoEvent {
  type: 'UNIT_POSITION';
  headingDegrees?: number | null;
  speedMps?: number | null; // meters per second from backend
}

export interface ZoneViolationEvent extends BaseGeoEvent {
  type: 'ZONE_VIOLATION';
  zoneIdentifier: string;
  severity: number;
  metadata?: string | null;
}

export interface ProximityAlertEvent extends BaseGeoEvent {
  type: 'PROXIMITY_ALERT';
  targetIdentifier: string;
  distanceMeters: number;
  severity: number;
}

export type GeoEvent = UnitPositionEvent | ZoneViolationEvent | ProximityAlertEvent | BaseGeoEvent;

export function isUnitPosition(e: GeoEvent): e is UnitPositionEvent {
  return e.type === 'UNIT_POSITION';
}
export function isZoneViolation(e: GeoEvent): e is ZoneViolationEvent {
  return e.type === 'ZONE_VIOLATION';
}
export function isProximityAlert(e: GeoEvent): e is ProximityAlertEvent {
  return e.type === 'PROXIMITY_ALERT';
}
