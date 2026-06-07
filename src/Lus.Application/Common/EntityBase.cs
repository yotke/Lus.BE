namespace Lus.Application.Common
{
    public abstract class EntityBase<TEntityKey> : IEntityWithKey<TEntityKey>, ICreationAuditable, IModificationAuditable, IDeletionAuditable, IConcurrentEntity
    {
        public TEntityKey? Id { get; set; }

        public DateTime CreatedOn { get; set; }

        public int? CreatedById { get; set; }

        public DateTime UpdatedOn { get; set; }

        public int? UpdatedById { get; set; }

        public DateTime? DeletedOn { get; set; }

        public int? DeletedById { get; set; }

        public bool IsDeleted { get; set; }

        public bool? Active { get; set; }

        public byte[]? RowVersion { get; set; }
    }
}
