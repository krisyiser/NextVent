using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Ticketfy.Core.Models;

public enum SystemRole
{
    CAJERO,
    ADMIN
}

public partial class UserModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string FullName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public SystemRole Role { get; init; } = SystemRole.CAJERO;
    public string Pin4Digits { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}
