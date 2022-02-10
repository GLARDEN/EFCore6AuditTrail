using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

using System.Text.Json;

namespace TestAuditTrail;

public abstract class AuditableContext : DbContext
{
    private JsonSerializerOptions jsonSerializerOptions { get; set; }
    protected AuditableContext() : base()    {    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AuditableContext).Assembly);

        //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);

    }

    public DbSet<AuditEntry> AuditTrails { get; set; }

    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new())
    {
       // ChangeTracker.DetectChanges();
       

        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChangesAsync(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();

        var entries = this.ApplyAuditing("TestUser");
        AuditTrails.AddRange(entries);

        return entries;
    }
    private Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        var entries = this.FixTemporyPropertyValues(auditEntries);
        return SaveChangesAsync();
    }
}

       