// See https://aka.ms/new-console-template for more information
namespace TestAuditTrail;

public class DateRange : ValueObject, IRange<DateOnly>
{
    public DateOnly Start { get; private set; }
    public DateOnly End { get; private set; }
    private DateRange() { }
    public DateRange(DateTime start, DateTime end)
    {
        Start = DateOnly.FromDateTime(start);
        End = DateOnly.FromDateTime(end);
    }
    public bool Includes(DateOnly value)
    {
        return Start <= value && value <= End;
    }

    public bool Includes(IRange<DateOnly> range)
    {
        return Start <= range.Start && range.End <= End;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}
