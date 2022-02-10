
using System.Text.Json;

using TestAuditTrail;



using var dbContext = new AppDbContext();

dbContext.Database.EnsureDeleted();
dbContext.Database.EnsureCreated();


CreateEntities(dbContext);
await dbContext.SaveChangesAsync();


UpdateEntity(dbContext);
await dbContext.SaveChangesAsync();

DeleteEntity(dbContext);
await dbContext.SaveChangesAsync();

OutputAuditTrail(dbContext);



Console.ReadLine();

/// <summary>
/// Create some test data 
/// </summary>
void CreateEntities(AppDbContext dbContext)
{
    if (!dbContext.Tests.Any())
    {
        var address1 = new Address("Address 1 1", "Address 1 2");
        var address2 = new Address("Test Address 2 1", "Test Address 2 2");
        var dateRange = new DateRange(DateTime.Now, DateTime.Now.AddDays(6));
        var entity = new Test(Guid.NewGuid(), "Test 1", dateRange, DateOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now), address1, address2);
        dbContext.Tests.Add(entity);

        address1 = new Address("Address 1 1", "Address 1 2");
        address2 = new Address("Test Address 2 1", "Test Address 2 2");
        dateRange = new DateRange(DateTime.Now, DateTime.Now.AddDays(6));
        entity = new Test(Guid.NewGuid(), "Test 2", dateRange, DateOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now), address1, address2);
        dbContext.Tests.Add(entity);       
    }
}

/// <summary>
/// Update test object with random values to generate a update audit trail
/// </summary>
void UpdateEntity(AppDbContext dbContext)
{
    //Update entry to get update audit log
    var updatedEntity = dbContext.Tests.OrderBy(p => p.CreatedWhen).FirstOrDefault();


    #region Set some random values to insure audit log is created
    //Random Name value
    updatedEntity.Name = $"TEST {new Random().Next(10)}";

    //Random values for DateRange Value Object
    updatedEntity.Date =  new DateOnly(2022, 03, new Random().Next(1, 15));

    //Random values for Address Value Object
    var randomStreetNumber = new Random().Next(1, 50);
    var randomUnitNumber = new Random().Next(1, 20);
    updatedEntity.MainAddress = new Address($"{randomStreetNumber} Anywhere", $"Unit #{randomUnitNumber}");

    randomStreetNumber = new Random().Next(1, 50);
    randomUnitNumber = new Random().Next(1, 20);
    updatedEntity.SecondaryAddress = new Address($"{randomStreetNumber} Anywhere", $"Unit #{randomUnitNumber}");


    //Random values for DateRange Value Object
    var startDate = new DateTime(2022, 03, new Random().Next(1, 15));
    var endDate = new DateTime(2022, 03, new Random().Next(16, 31));
    updatedEntity.DateRange = new DateRange(startDate, endDate);
    #endregion
}

/// <summary>
/// Delete test object with random values to generate a delete audit trail
/// </summary>
void DeleteEntity(AppDbContext dbContext)
{

    var deletedEntity = dbContext.Tests.OrderBy(p => p.CreatedWhen).LastOrDefault();
    if (deletedEntity != null)
    {
        dbContext.Remove(deletedEntity);
    }
}

void OutputAuditTrail(AppDbContext dbContext)
{
    Console.WriteLine("********** Audit Trail **********");

    var auditTrail = dbContext.AuditTrails.OrderBy(a => a.TimeStamp);

    auditTrail.ToList().ForEach(auditEntry =>
    {
        Console.WriteLine($"\n\nName: {auditEntry.EntityName} \t ActionType: {auditEntry.ActionType}");
        auditEntry.PropertyChanges.ToList().ForEach(p => {
            Console.WriteLine($"\n\tProperty Name: {p.PropertyName}");
            Console.WriteLine($"\tOriginal Value: {p.OriginalValue}");
            Console.WriteLine($"\tCurrent Value: {p.CurrentValue}");
        });
    });
}