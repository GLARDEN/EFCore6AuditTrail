using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Reflection;

namespace TestAuditTrail;
public class AppDbContext : AuditableContext
{
    public DbSet<Test> Tests => Set<Test>();

    public string DbPath { get; set; }

    public AppDbContext() : base() { }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        var DbPath = System.IO.Path.Join(path, "AuditTrail.db");

        optionsBuilder.UseSqlite($"Data Source={DbPath}");
        //   .LogTo(Console.WriteLine, new[] { DbLoggerCategory.Database.Command.Name }, LogLevel.Information);
        base.OnConfiguring(optionsBuilder);
    }
}
