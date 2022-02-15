using Microsoft.EntityFrameworkCore.ChangeTracking;

using Newtonsoft.Json;
namespace TestAuditTrail;

public class AuditEntry
{
    public Guid Id { get; set; }
    public string EntityName { get; set; } = null!;
    public AuditType ActionType { get; set; }
    public string Username { get; set; } = null!;
    public DateTime TimeStamp { get; set; }
    public string EntityId { get; set; } = null!;

    public List<PropertyChange> PropertyChanges { get; set; } = null!;

    //public List<PropertyChange> ChangeList { get; set; } = new();

    // TempProperties are used for properties that are only generated on save, e.g. ID's
    public List<PropertyEntry> TempProperties { get; set; } = null!;
}


