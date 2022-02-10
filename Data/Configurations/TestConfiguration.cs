// See https://aka.ms/new-console-template for more information
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using System.Text.Json;

using TinyHelpers.EntityFrameworkCore.Extensions;

namespace TestAuditTrail;
public class TestConfiguration : IEntityTypeConfiguration<Test>
{
    public TestConfiguration()
    {
       
    }
    public void Configure(EntityTypeBuilder<Test> builder)
    {
        

        builder.ToTable("Tests").HasKey(k => k.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Date).HasDateOnlyConversion();
        builder.Property(p => p.Time).HasTimeOnlyConversion();

        builder.OwnsOne(p => p.DateRange, p =>
        {
            p.Property(p => p.Start)
                .HasColumnName("Start")
                .HasDateOnlyConversion();
            p.Property(p => p.End)
            .HasColumnName("End")
                .HasDateOnlyConversion();
        });

        builder.OwnsOne(p => p.MainAddress, p =>
        {
            p.Property(p => p.Address1)
                .HasColumnName("MainAddress_Address1");
            p.Property(p => p.Address2)
            .HasColumnName("MainAddress_Address2");
        });

        builder.OwnsOne(p => p.SecondaryAddress, p =>
        {
            p.Property(p => p.Address1)
                .HasColumnName("SecondaryAddress_Address1");
            p.Property(p => p.Address2)
            .HasColumnName("SecondaryAddress_Address2");
        });
    }
}
