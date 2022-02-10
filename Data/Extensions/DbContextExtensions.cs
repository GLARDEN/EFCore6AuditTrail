using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TestAuditTrail;

public static class DbContextExtensions
{
    public static bool IsAdded(this EntityEntry entry) =>
       entry.State == EntityState.Added;

    public static bool IsDeleted(this EntityEntry entry) =>
        entry.State == EntityState.Deleted;

    public static bool IsModified(this EntityEntry entry) =>
        entry.State != EntityState.Added &&
        (entry.State == EntityState.Modified ||
            entry.References.Any(r => r.TargetEntry is not null &&
                                   r.TargetEntry.Metadata.IsOwned() &&
                                   r.TargetEntry.Metadata.ClrType.BaseType == typeof(ValueObject) &&
                                   (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified)));

    public static List<AuditEntry> ApplyAuditing(this DbContext dbContext, string currentUser)
    {
        var addedEntries = dbContext.ChangeTracker.Entries<IAuditable>().Where(e => e.IsAdded());
        var modifiedEntries = dbContext.ChangeTracker.Entries<IAuditable>().Where(e => e.IsModified());
        var deletedEntries = dbContext.ChangeTracker.Entries<IAuditable>().Where(e => e.IsDeleted());

        var now = DateTime.Now;

        foreach (var entry in addedEntries)
        {
            entry.CurrentValues[nameof(BaseEntity.CreatedBy)]=currentUser;
            entry.CurrentValues[nameof(BaseEntity.CreatedWhen)]=now;
            entry.CurrentValues[nameof(BaseEntity.ModifiedBy)]=currentUser;
            entry.CurrentValues[nameof(BaseEntity.ModifiedWhen)]=now;
        }

        foreach (var entry in modifiedEntries)
        {
            entry.CurrentValues[nameof(BaseEntity.ModifiedBy)]=currentUser;
            entry.CurrentValues[nameof(BaseEntity.ModifiedWhen)]=now;
        }

        foreach (var entry in deletedEntries)
        {
            entry.CurrentValues[nameof(BaseEntity.ModifiedBy)]=currentUser;
            entry.CurrentValues[nameof(BaseEntity.ModifiedWhen)]=now;
        }


        var entriesToAudit = addedEntries.Concat(modifiedEntries).Concat(deletedEntries);

        var auditTrail = AddAuditTrail(dbContext, entriesToAudit);

        return auditTrail;
    }


    public static List<AuditEntry> AddAuditTrail(this DbContext dbContext, IEnumerable<EntityEntry<IAuditable>> entities)
    {
        var entries = new List<AuditEntry>();

        //Get list of properties we don't need to audit
        var propertiesToIgnore  = typeof(BaseEntity).GetProperties().Select(p => p.Name);

        foreach (var entry in entities)
        {
            if (entry != null)
            {
                 var auditEntry = new AuditEntry
                {
                    ActionType = entry.State == EntityState.Added ? AuditType.Create : entry.State == EntityState.Deleted ? AuditType.Delete : AuditType.Update,
                    EntityId = entry?.Properties?.Single(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? "INVALID_ID",
                    EntityName = entry?.Metadata?.ClrType?.Name ?? "",
                    Username = "test_user",
                    TimeStamp = DateTime.UtcNow,
                    PropertyChanges = entry.Properties.Where(p => (p.IsModified || (entry.IsAdded() || entry.IsDeleted()))
                                                               && !propertiesToIgnore.Contains(p.Metadata.Name))
                                                      .Select(p =>
                                                            new PropertyChange()
                                                            {
                                                                PropertyName = p.Metadata.Name ?? "",
                                                                OriginalValue =    p.OriginalValue ?? "",
                                                                CurrentValue= p.CurrentValue ?? ""
                                                            }).ToList(),

                    // TempProperties are properties that are only generated on save, e.g. ID's
                    // These properties will be set correctly after the audited entity has been saved
                    TempProperties = entry.Properties.Where(p => p.IsTemporary).ToList()
                };

                //Get value object marked as deleted
                var deleted = dbContext.ChangeTracker.Entries().Where(e => e.Metadata.IsOwned() &&
                                                                    e.Metadata.ClrType.BaseType == typeof(ValueObject) &&
                                                                    e.State == EntityState.Deleted);
                //Get value object marked as added
                var added = dbContext.ChangeTracker.Entries()
                                             .Where(e => e.Metadata.IsOwned() &&                                             
                                            e.Metadata.ClrType.BaseType == typeof(ValueObject) &&
                                            e.State == EntityState.Added);

                foreach (var deletedOwnedType in deleted)
                {

                    var ownership = deletedOwnedType.Metadata.FindOwnership();
                    var primaryKey = entry.Properties.Where(p => p.Metadata.IsPrimaryKey());
                    var parentKey = ownership.PrincipalKey.Properties.Select(p => entry.Property(p.Name).CurrentValue).ToArray();
                    var parent = dbContext.Find(ownership.PrincipalEntityType.ClrType, parentKey);

                    if (parent != null)
                    {
                        //Get the added value object that matches the deleted value object
                        var addedOwnedType = added.FirstOrDefault(a =>
                        {
                            var addedFks = a.Metadata.GetForeignKeys();
                            var deletedFks = deletedOwnedType.Metadata.GetForeignKeys();
                            var nameMatch = a.Metadata.Name.Equals(deletedOwnedType.Metadata.Name);
                            return deletedFks.Equals(addedFks) && nameMatch;
                        });

                        //if entry is added or updated check there will be a added value object
                        if (entry.State != EntityState.Deleted)
                        {
                            addedOwnedType.Properties.Where(p => !p.Metadata.IsKey()).ToList().ForEach(p =>
                            {
                                var propertyName = p.Metadata.Name;
                                var deletedProperty = deletedOwnedType.Properties.First(dp => dp.Metadata.Name == propertyName);

                                if (!deletedProperty.OriginalValue.Equals(p.CurrentValue))
                                {
                                    var ownedTypePropertyPath = GetOwnedTypeParentPropertyName(addedOwnedType, deletedProperty);
                                    auditEntry.PropertyChanges.Add(new PropertyChange()
                                    {
                                        AuditEntryId  = auditEntry.Id,
                                        PropertyName  = ownedTypePropertyPath ?? "",
                                        OriginalValue = deletedProperty.OriginalValue,
                                        CurrentValue  = p.CurrentValue
                                    });
                                }
                            });
                        }

                        //If entity is deleted there will be no value object with a state of Added so just record the original value
                        if (entry.State == EntityState.Deleted)
                        {
                            var deletedProperty = deletedOwnedType.Properties.Where(p => !p.Metadata.IsKey());

                            deletedProperty.ToList().ForEach(p =>
                            {
                                var ownedTypePropertyPath = GetOwnedTypeParentPropertyName(deletedOwnedType, p);
                                auditEntry.PropertyChanges.Add(new PropertyChange()
                                {
                                    AuditEntryId = auditEntry.Id,
                                    PropertyName =ownedTypePropertyPath ?? "",
                                    OriginalValue = p.OriginalValue,
                                    CurrentValue=""
                                });
                            });
                        }
                    }
                }
                entries.Add(auditEntry);
            }
        }
        return entries;
    }

    /// <summary>
    /// Appends parent entity property name to value objects property name incase there are
    /// multiple properties with the same value object
    /// </summary>
    /// <param name="ownedType"></param>
    /// <param name="ownedTypeProperty"></param>
    /// <returns></returns>
    public static string? GetOwnedTypeParentPropertyName(EntityEntry? ownedType, PropertyEntry? ownedTypeProperty)
    {
        if (ownedType == null)
        {
            return null;
        }

        var nameSpaceDotIndex = ownedType.Metadata.Name.IndexOf('.');

        string? entityPropertyName = null;

        if (nameSpaceDotIndex != -1)
        {
            entityPropertyName = ownedType.Metadata.Name.Substring(nameSpaceDotIndex + 1, ownedType.Metadata.Name.Length-nameSpaceDotIndex-1);

            var typeDotIndex = entityPropertyName.IndexOf('.');
            if (typeDotIndex != -1)
            {
                entityPropertyName = entityPropertyName.Substring(typeDotIndex + 1, entityPropertyName.Length-typeDotIndex-1);
            }
            var poundIndex = entityPropertyName.IndexOf('#');
            if (poundIndex  != -1)
            {
                entityPropertyName = entityPropertyName.Substring(0, poundIndex);
            }
        }

        var valueObjectPropertyName = entityPropertyName == null ? ownedTypeProperty.Metadata.Name : $"{entityPropertyName}_{ownedTypeProperty.Metadata.Name}";
        return valueObjectPropertyName;
    }

    public static List<AuditEntry> FixTemporyPropertyValues(this DbContext dbContext, List<AuditEntry> entries)
    {

        // For each temporary property in each audit entry - update the value in the audit entry to the actual (generated) value
        foreach (var entry in entries)
        {
            foreach (var prop in entry.TempProperties)
            {
                if (prop !=null && prop.Metadata.IsPrimaryKey())
                {
                    entry.EntityId = prop.CurrentValue?.ToString() ?? "INVALID ID";
                    entry.PropertyChanges.FirstOrDefault(p => p.PropertyName == prop.Metadata.Name).CurrentValue = prop.CurrentValue ?? "";
                }
                else
                {
                    entry.PropertyChanges.FirstOrDefault(p => p.PropertyName == prop.Metadata.Name).CurrentValue = prop.CurrentValue ?? "";
                }
            }
        }

        return entries;
    }
}
