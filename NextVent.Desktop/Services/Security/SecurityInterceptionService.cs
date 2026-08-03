using System;
using System.Threading.Tasks;
using NextVent.Core.Models;
using NextVent.Core.Repositories;

namespace NextVent.Services.Security;

public class SecurityInterceptionService : ISecurityInterceptionService
{
    private readonly IUserRepository _userRepository;
    public event Action<string, Action<bool, UserModel?>>? RequestSupervisorPinDialog;

    public SecurityInterceptionService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(bool IsAuthorized, string? SupervisorId, string SignatureName)> AuthorizeHighRiskActionAsync(
        string actionTitle,
        string reasonRequiredMessage)
    {
        if (RequestSupervisorPinDialog == null)
        {
            return (false, null, string.Empty);
        }

        var tcs = new TaskCompletionSource<(bool, string?, string)>();

        RequestSupervisorPinDialog.Invoke(actionTitle, (authorized, user) =>
        {
            if (authorized && user != null)
            {
                tcs.SetResult((true, user.Id.ToString(), user.FullName));
            }
            else
            {
                tcs.SetResult((false, null, string.Empty));
            }
        });

        return await tcs.Task;
    }
}
