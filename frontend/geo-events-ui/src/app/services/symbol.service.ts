import { Injectable } from '@angular/core';
import ms from 'milsymbol';

@Injectable({ providedIn: 'root' })
export class SymbolService {
  private cache = new Map<string, string>();

  buildSymbol(sidc: string, options?: { size?: number; uniqueDesignation?: string }): string {
    const key = sidc + JSON.stringify(options || {});
    if (this.cache.has(key)) return this.cache.get(key)!;

    const size = options?.size ?? 32;
    const lib: any = (ms as any)?.default ?? (ms as any);
    const SymbolCtor: any = lib.Symbol || lib; // some builds export the class directly, others under .Symbol
    const symbol = new SymbolCtor(sidc, { size, uniqueDesignation: options?.uniqueDesignation });
    const canvas = symbol.asCanvas();
    const dataUrl = canvas.toDataURL('image/png');
    this.cache.set(key, dataUrl);
    return dataUrl;
  }
}
