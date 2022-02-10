// See https://aka.ms/new-console-template for more information
using System.Reflection.Metadata.Ecma335;

namespace TestAuditTrail;

public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public string CreatedBy { get; set; } = null!;
    public DateTime CreatedWhen { get; set; }

    public string ModifiedBy { get; set; } = null!;
    public DateTime ModifiedWhen { get; set; }

}
