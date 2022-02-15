using TestAuditTrail;

using var dbContext = new AppDbContext();

Console.WriteLine("Deleting Previous Database... ");
dbContext.Database.EnsureDeleted();

Console.WriteLine("Creating new copy of database...\n\n\n");
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
        var address1 = new Address("Original Address 1_1", "Original Address 1_2");
        var address2 = new Address("Original Address 2_1", "Original Address 2_2");
        var dateRange = new DateRange(DateTime.Now, DateTime.Now.AddDays(new Random().Next(1, 10)));
        var entity1 = new Test(Guid.NewGuid(), "Test 1", dateRange, DateOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now), address1, address2);
        dbContext.Tests.Add(entity1);

        address1 =  new Address("Original Address 1_1", "Original Address 1_2"); ;
        address2 = new Address("Original Address 2_1", "Original Address 2_2");

        dateRange = new DateRange(DateTime.Now, DateTime.Now.AddDays(new Random().Next(1, 10)));
        var entity2 = new Test(Guid.NewGuid(), "Test 2", dateRange, DateOnly.FromDateTime(DateTime.Now), TimeOnly.FromDateTime(DateTime.Now), address1, address2);
        dbContext.Tests.Add(entity2);       
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

    
    //Update the address value objects
    updatedEntity.MainAddress = new Address("UPDATED Original Address 1_1", "UPDATED  Original Address 1_2");      
    updatedEntity.SecondaryAddress = new Address("UPDATED Original Address 2_1", "UPDATED  Original Address 2_2");


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
        Console.WriteLine($"\n\nId: {auditEntry.Id} Name: {auditEntry.EntityName}\nActionType: {auditEntry.ActionType}");
        auditEntry.PropertyChanges.ToList().ForEach(p => {
            Console.WriteLine($"\n\tProperty Name: {p.PropertyName}");
            Console.WriteLine($"\tOriginal Value: {p.OriginalValue}");
            Console.WriteLine($"\tCurrent Value: {p.CurrentValue}");
        });
    });
}