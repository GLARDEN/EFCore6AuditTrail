// See https://aka.ms/new-console-template for more information
namespace TestAuditTrail;
public class Test : BaseEntity, IAuditable
{
    public string Name { get;  set; }
    public DateOnly Date { get;  set; }
    public TimeOnly Time { get; set; }
    public DateRange DateRange { get;  set; }
    public Address MainAddress { get; set; }
    public Address SecondaryAddress { get; set; }
    private Test()  {    }
    public Test(Guid id, string name, DateRange dateRange, DateOnly date, TimeOnly time, Address mainAddress, Address secondaryAddress)
    {
        Id = id;
        Name = name;
        DateRange = dateRange;
        Date=date;
        Time=time;
        MainAddress=mainAddress;
        SecondaryAddress=secondaryAddress;
    }
}
