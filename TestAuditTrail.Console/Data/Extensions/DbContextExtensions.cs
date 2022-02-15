using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

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
        var propertiesToIgnore = typeof(BaseEntity).GetProperties().Select(p => p.Name);

        var addedOwnedTypes = dbContext.ChangeTracker.Entries()
                        .Where(
                   e => e.Metadata.IsOwned() &&
                       e.Metadata.ClrType.BaseType == typeof(ValueObject) &&
                       e.State == EntityState.Added).ToList();

        var deletedOwnedTypes = dbContext.ChangeTracker.Entries()
                                     .Where(
                                e => e.Metadata.IsOwned() &&
                                    e.Metadata.ClrType.BaseType == typeof(ValueObject) &&
                                    e.State == EntityState.Deleted).ToList();

        foreach (var entry in entities)
        {
            if (entry != null)
            {
                var auditEntryId = Guid.NewGuid();
                var auditEntry = new AuditEntry
                {
                    Id = auditEntryId,
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

                List<PropertyChange> ownedTypeChanges = ProcessOwnedTypes(dbContext,entry,addedOwnedTypes,deletedOwnedTypes);
                auditEntry.PropertyChanges.AddRange(ownedTypeChanges);
                                                                                                          
                entries.Add(auditEntry);
            }
        }
        return entries;
    }

    /// <summary>
    /// Calls the appropriate method to build the list of property change objects for owned types based on the parent entity state
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="entry"></param>
    /// <param name="addedOwnedTypes"></param>
    /// <param name="deletedOwnedTypes"></param>
    /// <returns></returns>
    internal static List<PropertyChange> ProcessOwnedTypes(DbContext dbContext, EntityEntry<IAuditable> entry, List<EntityEntry> addedOwnedTypes, List<EntityEntry> deletedOwnedTypes) 
    {
        List<PropertyChange> ownedTypeChanges = new();
        switch (entry.State)
        {
            case EntityState.Added:
                ownedTypeChanges = BuildAuditTrailForAddedOrDeletedEntry(dbContext, addedOwnedTypes, entry);
                return ownedTypeChanges;
            case EntityState.Modified:
                ownedTypeChanges = BuildAuditTrailForModifiedOwnedTypes(dbContext, addedOwnedTypes, deletedOwnedTypes, entry);
                return ownedTypeChanges;
            case EntityState.Deleted:
                ownedTypeChanges = BuildAuditTrailForAddedOrDeletedEntry(dbContext, deletedOwnedTypes, entry);
                return ownedTypeChanges;
            default:
                return ownedTypeChanges;
        }
    }

    /// <summary>
    /// Builds a list of property change objects for owned types when the parent object is either Added or Deleted
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="ownedTypes"></param>
    /// <param name="entry"></param>
    /// <returns></returns>
    internal static List<PropertyChange> BuildAuditTrailForAddedOrDeletedEntry(DbContext dbContext,List<EntityEntry> ownedTypes, EntityEntry<IAuditable> entry)
    {
        //if entry is not modified just return from the method
        if (!entry.IsAdded() && !entry.IsDeleted())
        {
            return new();
        }

        var ownedTypeChanges = new List<PropertyChange>();

        foreach (var ownedType in ownedTypes)
        {
            var parent = GetOwnedTypeParentEntity(dbContext, ownedType);

            if (parent != null && parent.Equals(entry.Entity))
            {
                string? ownedTypePropertyPath = null;

                //Reiterate over added ownedTypes to create list of the changed values
                ownedType.Properties.Where(p => !p.Metadata.IsKey()).ToList().ForEach(p =>
                {
                    ownedTypePropertyPath = GetOwnedTypeParentPropertyName(ownedType, p);
                    
                    object? originalValue = null;
                    object? currentValue = null;

                    if (entry.State == EntityState.Deleted)
                    {
                        originalValue = p.OriginalValue;
                        currentValue = null;
                    }

                    if( entry.State == EntityState.Added)
                    {
                        originalValue =null;
                        currentValue = p.CurrentValue;
                    }                      

                    ownedTypeChanges.Add(new PropertyChange()
                    {
                        PropertyName  = ownedTypePropertyPath ?? "",
                        OriginalValue = originalValue ?? "",
                        CurrentValue  = currentValue ?? ""
                    });
                });
            }
        }
        return ownedTypeChanges;
    }

    /// <summary>
    /// Builds a list of property change objects for owned types that are modified
    /// </summary>
    /// <param name="dbContext"></param>
    /// <param name="addedOwnedTypes"></param>
    /// <param name="deletedOwnedTypes"></param>
    /// <param name="entry"></param>
    /// <returns></returns>
    internal static List<PropertyChange> BuildAuditTrailForModifiedOwnedTypes(DbContext dbContext, List<EntityEntry> addedOwnedTypes, List<EntityEntry> deletedOwnedTypes, EntityEntry<IAuditable> entry)
    {
        //if entry is not modified just return from the method
        if (!entry.IsModified())
        {
            return new();
        }

        var ownedTypeChanges = new List<PropertyChange>();

        foreach (var deletedOwnedType in deletedOwnedTypes)
        {
            var parent = GetOwnedTypeParentEntity(dbContext, deletedOwnedType);
           
            if (parent != null && parent.Equals(entry.Entity))
            {
                //Get the added value object that matches the deleted value object
                var addedOwnedType = addedOwnedTypes.FirstOrDefault(a =>
                {
                    var addedFks = a.Metadata.GetForeignKeys();
                    var deletedFks = deletedOwnedType.Metadata.GetForeignKeys();
                    var nameMatch = a.Metadata.Name.Equals(deletedOwnedType.Metadata.Name);
                    return deletedFks.Equals(addedFks) && nameMatch;
                });

                //Reiterate over added ownedTypes to create list of the changed values
                addedOwnedType.Properties.Where(p => !p.Metadata.IsKey()).ToList().ForEach(p =>
                {
                    var deletedProperty = deletedOwnedType.Properties.First(dp => dp.Metadata.Name == p.Metadata.Name);

                    if (entry.IsModified() && !deletedProperty.OriginalValue.Equals(p.CurrentValue))
                    {
                        var ownedTypePropertyPath = GetOwnedTypeParentPropertyName(addedOwnedType, deletedProperty);                        
                        ownedTypeChanges.Add(new PropertyChange()
                        {
                            PropertyName  = ownedTypePropertyPath ?? "",
                            OriginalValue =  deletedProperty.OriginalValue ?? "",
                            CurrentValue  =  p.CurrentValue  ?? ""
                        });
                    }
                });                           
            }
        }
        return ownedTypeChanges;
    }


    internal static object? GetOwnedTypeParentEntity(DbContext dbContext, EntityEntry ownedType)
    {
        var ownership = ownedType.Metadata.FindOwnership();
        var parentKey = ownership.Properties.Select(p => ownedType.Property(p.Name).CurrentValue).ToArray();
        var parent = dbContext.Find(ownership.PrincipalEntityType.ClrType, parentKey);

        return parent;
    }
    /// <summary>
    /// Appends parent entity property name to value objects property name incase there are
    /// multiple properties with the same value object
    /// </summary>
    /// <param name="ownedType"></param>
    /// <param name="ownedTypeProperty"></param>
    /// <returns></returns>
    internal static string? GetOwnedTypeParentPropertyName(EntityEntry? ownedType, PropertyEntry? ownedTypeProperty)
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

public static class IEnumerableExtensions
{
    public static IEnumerable<T> CastEnumerable<T>(this IEnumerable<object> sourceEnum)
    {
        if (sourceEnum == null)
            return new List<T>();

        try
        {
            // Covert the objects in the list to the target type (T) 
            // (this allows to receive other types and then convert in the desired type)
            var convertedEnum = sourceEnum.Select(x => Convert.ChangeType(x, typeof(T)));
            // Cast the IEnumerable<object> to IEnumerable<T>
            return convertedEnum.Cast<T>();
        }
        catch (Exception e)
        {
            throw new InvalidCastException($"There was a problem converting {sourceEnum.GetType()} to {typeof(IEnumerable<T>)}", e);
        }
    }
}