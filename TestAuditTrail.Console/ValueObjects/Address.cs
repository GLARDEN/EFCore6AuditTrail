// See https://aka.ms/new-console-template for more information
namespace TestAuditTrail;

public class Address : ValueObject
{
    public string Address1 { get; private set; }
    public string  Address2 { get; private set; }
    private Address() { }
    public Address(string address1, string address2)
    {
        Address1=address1;
        Address2=address2;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Address1;
        yield return Address2;
    }
}