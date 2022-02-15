namespace TestAuditTrail;

public class PropertyChange
{       
    public string PropertyName { get; set; } = null!;
    public object OriginalValue { get; set; } = null!;
    public object CurrentValue { get; set; } = null!;
}


