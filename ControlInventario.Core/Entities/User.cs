
using System;

namespace ControlInventario.Core.Entities;

public partial class User
{
    public int Id { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateOnly DateOfBirth { get; set; }

    public string? Telephone { get; set; }

    public ulong IsActive { get; set; }
}
