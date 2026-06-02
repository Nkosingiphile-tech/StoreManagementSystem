using System;
using System.Collections.Generic;

namespace StoreManagementSystem.Models;

public partial class Customer
{
    public int CustomerId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? StreetAddress { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
