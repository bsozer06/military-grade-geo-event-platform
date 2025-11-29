import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface MockZone {
  id: string;
  name?: string;
  centerLat: number;
  centerLon: number;
  radiusMeters: number;
}

@Injectable({ providedIn: 'root' })
export class ZonesService {
  constructor(private http: HttpClient) {}

  getZones(): Observable<MockZone[]> {
    return this.http.get<MockZone[]>('/mock-zones.json');
  }
}
