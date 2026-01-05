public class CompanyContactDto
{
    public string ContactName { get; set; }
    public string RoleDesignation { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
}

public class CompanyAddressDto
{
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string PostalCode { get; set; }
    public string Country { get; set; }
}