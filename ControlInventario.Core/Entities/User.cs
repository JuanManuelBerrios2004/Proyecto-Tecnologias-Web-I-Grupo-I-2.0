using System;
using System.Collections.Generic;

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

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
