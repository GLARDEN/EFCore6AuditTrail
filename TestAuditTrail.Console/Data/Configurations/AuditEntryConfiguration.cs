using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using TestAuditTrail.EntityFrameworkCore.Shared.Extensions;

namespace TestAuditTrail;
public class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> builder)
    {
        builder.ToTable("AuditTrail");
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.ActionType).HasConversion<string>();        
        builder.Property(ae => ae.PropertyChanges).HasJsonConversion();       
        builder.Ignore(p => p.TempProperties);

    }
}
                                                                   