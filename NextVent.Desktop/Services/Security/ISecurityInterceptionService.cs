using System;
using System.Threading.Tasks;

namespace NextVent.Services.Security;

public interface ISecurityInterceptionService
{
    Task<(bool IsAuthorized, string? SupervisorId, string SignatureName)> AuthorizeHighRiskActionAsync(
        string actionTitle,
        string reasonRequiredMessage);
}
