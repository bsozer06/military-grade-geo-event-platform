export interface GeoLocation {
  lat: number;
  lon: number;
}

export interface BaseGeoEvent<TMeta = Record<string, unknown>> {
  eventId: string; // Guid string
  type: string;
  timestamp: string; // ISO8601
  source: string;
  location?: GeoLocation;
  metadata?: TMeta;
}

export interface UnitPositionMetadata {
  speed?: number; // km/h
  heading?: number; // degrees
  altitude?: number; // meters
}

export interface UnitPositionEvent extends BaseGeoEvent<UnitPositionMetadata> {
  type: 'UNIT_POSITION';
  location: GeoLocation;
}

export interface ZoneViolationMetadata {
  zoneId: string;
  zoneName?: string;
  distanceMeters?: number; // penetration depth or proximity
}

export interface ZoneViolationEvent extends BaseGeoEvent<ZoneViolationMetadata> {
  type: 'ZONE_VIOLATION';
  location: GeoLocation;
}

export interface ProximityAlertMetadata {
  otherUnitId: string;
  separationMeters: number;
}

export interface ProximityAlertEvent extends BaseGeoEvent<ProximityAlertMetadata> {
  type: 'PROXIMITY_ALERT';
  location: GeoLocation;
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
