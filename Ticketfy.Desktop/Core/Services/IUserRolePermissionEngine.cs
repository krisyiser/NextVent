using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ticketfy.Core.Models;
using Ticketfy.Data.Entities;
using Ticketfy.Data.Dtos;

namespace Ticketfy.Core.Services;

/// <summary>
/// Specialized Engine contract for managing User accounts, System & Custom Roles,
/// RBAC Permission Evaluation, and Session Authorization.
/// </summary>
public interface IUserRolePermissionEngine
{
    /// <summary>
    /// Normalizes raw role strings into clean system display names.
    /// (e.g., "admin" -> "ADMINISTRADOR", "cajero" -> "CAJERO").
    /// </summary>
    string NormalizeRoleName(string? rawRole);

    /// <summary>
    /// Maps a database UserEntity into a domain UserModel maintaining full RoleString metadata.
    /// </summary>
    UserModel MapToModel(UserEntity entity);

    /// <summary>
    /// Maps a UserDto into a domain UserModel.
    /// </summary>
    UserModel MapDtoToModel(UserDto dto, string? pinCode = null);

    /// <summary>
    /// Evaluates if a given role possesses a specific granular permission key (e.g., "pos.cancel_sale").
    /// </summary>
    Task<bool> HasPermissionAsync(string roleName, string permissionKey);

    /// <summary>
    /// Gets all permission states for a given role name.
    /// </summary>
    Task<Dictionary<string, bool>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Determines whether a given role string corresponds to an administrative / supervisory tier.
    /// </summary>
    bool IsAdminOrManager(string roleName);
}
