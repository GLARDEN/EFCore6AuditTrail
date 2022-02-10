using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.Text.Json;

using TinyHelpers.EntityFrameworkCore.Extensions;

namespace TestAuditTrail;
public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditTrail").HasKey(p => p.Id);        
        builder.Property(p => p.ActionType).HasConversion<string>();        
        builder.Property(ae => ae.PropertyChanges).HasJsonConversion();       
        builder.Ignore(p => p.TempProperties);

    }
}
                                                                   