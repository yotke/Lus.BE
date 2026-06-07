import { v4 as uuidv4 } from 'uuid';

/**
 * Base class for all repository entities. Mirrors the backend EntityBase audit
 * shape (soft-delete + audit columns) so search/projection field names line up.
 */
export class EntityBase {
  Id?: number | null | undefined;
  StringId: string;
  CreatedOn?: Date;
  CreatedById?: number;
  UpdatedOn?: Date;
  UpdatedById?: number;
  DeletedOn?: Date | null;
  DeletedById?: number | null;
  IsDeleted?: boolean;
  Active?: boolean;

  constructor(
    Id: number | null = 0,
    CreatedOn: Date = new Date(),
    CreatedById: number = 0,
    UpdatedOn: Date = new Date(),
    UpdatedById: number = 0,
    DeletedOn: Date | null = null,
    DeletedById: number | null = null,
    IsDeleted: boolean = false,
    Active: boolean = true
  ) {
    this.Id = Id;
    this.StringId = uuidv4();
    this.CreatedOn = CreatedOn;
    this.CreatedById = CreatedById;
    this.UpdatedOn = UpdatedOn;
    this.UpdatedById = UpdatedById;
    this.DeletedOn = DeletedOn;
    this.DeletedById = DeletedById;
    this.IsDeleted = IsDeleted;
    this.Active = Active;
  }
}
