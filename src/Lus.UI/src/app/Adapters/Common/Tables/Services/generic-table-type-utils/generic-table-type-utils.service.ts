import { Injectable } from '@angular/core';
import { SearchRequestFiltering } from '../../../../../Infrastructure/Classes & Models/Filtering/SearchEntities/SearchRequestFiltering/search-request-filtering';
import { SearchRequestFilteringParams } from '../../../../../Infrastructure/Classes & Models/Filtering/SearchEntities/SearchRequestFilteringParams/search-request-filtering-params';
import { GroupingOperations } from '../../../../../Infrastructure/Classes & Models/Filtering/SearchEntities/Types/grouping-operations.enums';
import { Operations } from '../../../../../Infrastructure/Classes & Models/Filtering/SearchEntities/Types/operations.enums';

export type FieldKind = 'number' | 'date' | 'boolean' | 'string';

export type FilterOpKey =
  | 'contains'      // Ct
  | 'equals'        // Eq
  | 'startsWith'    // Sw
  | 'gt'            // Gt
  | 'lt'            // Lt
  | 'in'            // In
  | 'isNull'        // IsNull
  | 'isNotNull';    // IsNotNull

export type FilterOverride = {
  op?: FilterOpKey;
  paramGroup?: GroupingOperations;
  blockGroup?: GroupingOperations;
  coerceNumberOnEq?: boolean;
  asString?: boolean;
  innerPath?: string | null;
  negate?: boolean;
  property?: string;
  valueMap?: Record<string, string | number>;
};

/**
 * Builds backend SearchRequest filtering blocks from free-text column terms.
 * Supports type detection, numeric/date ranges, operator overrides, value
 * mapping and server-side projection-friendly property targeting.
 */
@Injectable({ providedIn: 'root' })
export class GenericTableTypeUtilsService {
  constructor() { }

  // ---------- Basic type helpers ----------
  isDateLike(value: any): boolean {
    if (value instanceof Date) return true;
    if (typeof value !== 'string') return false;
    const parsed = Date.parse(value);
    return !isNaN(parsed) && value.includes('-');
  }

  detectFieldType(rows: any[], field: string): FieldKind {
    for (const r of rows ?? []) {
      const v = r?.[field];
      if (v === null || v === undefined) continue;
      if (typeof v === 'boolean') return 'boolean';
      if (typeof v === 'number') return 'number';
      if (this.isDateLike(v)) return 'date';
      if (!isNaN(+v) && v !== '' && isFinite(+v)) return 'number';
      break;
    }
    return 'string';
  }

  // ---------- Date term parser (supports two syntaxes) ----------
  private parseDateTerm(term: string): { from?: string; to?: string } | null {
    if (!term) return null;
    const m1 = /^date:(?:from=(\d{4}-\d{2}-\d{2}))?;?(?:to=(\d{4}-\d{2}-\d{2}))?$/i.exec(term);
    if (m1) return { from: m1[1] || undefined, to: m1[2] || undefined };
    const m2 = /^(\d{4}-\d{2}-\d{2})?\.\.(\d{4}-\d{2}-\d{2})?$/.exec(term);
    if (m2) return { from: m2[1] || undefined, to: m2[2] || undefined };
    return null;
  }

  // ---------- Multi-field builder with overrides + date-range support ----------
  buildFilterBlocks(
    rows: any[],
    columnTerms: Record<string, string>,
    overrides?: Record<string, FilterOverride | FilterOpKey>
  ): SearchRequestFiltering[] {
    const blocks: SearchRequestFiltering[] = [];

    const toOperation = (key?: FilterOpKey): Operations | null => {
      switch (key) {
        case 'contains': return Operations.Ct;
        case 'startsWith': return Operations.Sw;
        case 'equals': return Operations.Eq;
        case 'gt': return Operations.Gt;
        case 'lt': return Operations.Lt;
        case 'in': return Operations.In;
        case 'isNull': return Operations.IsNull;
        case 'isNotNull': return Operations.IsNotNull;
        default: return null;
      }
    };

    Object.entries(columnTerms ?? {}).forEach(([field, raw]) => {
      let term = (raw ?? '').toString().trim();

      const ov = overrides?.[field];
      const ovObj: FilterOverride | undefined =
        typeof ov === 'string' ? { op: ov } : ov;

      // Target backend property (enables server-side projection on a renamed path)
      const propertyName = (ovObj?.property?.trim() || field) as any;

      // ----- DATE RANGE BRANCH -----
      const dateRange = this.parseDateTerm(term);
      if (dateRange) {
        const blockGroup = ovObj?.blockGroup ?? GroupingOperations.And;
        const paramGroup = ovObj?.paramGroup ?? GroupingOperations.And;

        if (dateRange.from) {
          const pFrom: SearchRequestFilteringParams = {
            Operation: Operations.Gt,
            Values: [dateRange.from],
            GroupingOperation: paramGroup,
            IsNegated: !!ovObj?.negate,
            InnerPath: ovObj?.innerPath ?? null
          };
          blocks.push({
            PropertyName: propertyName,
            GroupingOperation: blockGroup,
            FilterParameters: [pFrom]
          });
        }

        if (dateRange.to) {
          const pTo: SearchRequestFilteringParams = {
            Operation: Operations.Lt,
            Values: [dateRange.to],
            GroupingOperation: paramGroup,
            IsNegated: !!ovObj?.negate,
            InnerPath: ovObj?.innerPath ?? null
          };
          blocks.push({
            PropertyName: propertyName,
            GroupingOperation: blockGroup,
            FilterParameters: [pTo]
          });
        }

        return; // handled as date-range; skip generic logic
      }

      // ----- NON-DATE FLOW -----
      const kind = this.detectFieldType(rows, field);

      const chosenOp = toOperation(ovObj?.op);
      let op = chosenOp ?? ((!isNaN(Number(term)) && term !== '') ? Operations.Eq : Operations.Ct);

      // value mapping (case-insensitive key support)
      if (ovObj?.valueMap) {
        const lc = term.toLowerCase();
        const mapped = (ovObj.valueMap as any)[lc] ?? (ovObj.valueMap as any)[term];
        if (mapped !== undefined) term = String(mapped);
      }

      // values by op
      let values: any[] = [];
      if (op === Operations.IsNull || op === Operations.IsNotNull) {
        values = [];
      } else if (op === Operations.In) {
        values = term.split(',')
          .map(s => s.trim())
          .filter(Boolean)
          .map(p => {
            const n = Number(p);
            return (!isNaN(n) && p !== '') ? n : p;
          });
      } else if (op === Operations.Eq) {
        const coerce = ovObj?.coerceNumberOnEq ?? true;
        const n = Number(term);
        values = (coerce && !isNaN(n) && term !== '') ? [n] : [term];
      } else {
        values = term ? [term] : [];
      }

      // groupings
      const paramGroup = ovObj?.paramGroup ?? GroupingOperations.Or;
      const blockGroup = ovObj?.blockGroup ?? GroupingOperations.And;

      // AsString heuristic for text ops on non-string fields
      const isTextOp = (op === Operations.Ct || op === Operations.Sw);
      const heuristicAsString = isTextOp && kind !== 'string' ? true : undefined;
      const asString = ovObj?.asString !== undefined ? ovObj.asString : heuristicAsString;

      const param: SearchRequestFilteringParams = {
        Operation: op,
        Values: values as any,
        GroupingOperation: paramGroup,
        IsNegated: !!ovObj?.negate,
        InnerPath: ovObj?.innerPath ?? null,
        AsString: asString,
      };

      blocks.push({
        PropertyName: propertyName,
        GroupingOperation: blockGroup,
        FilterParameters: [param]
      });
    });

    return blocks;
  }
}
