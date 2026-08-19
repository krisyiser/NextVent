using System.Threading.Tasks;

namespace Ticketfy.Core.Services;

public interface IDialogService
{
    Task<TResult?> ShowModalAsync<TViewModel, TResult>(TViewModel viewModel) where TViewModel : class;
    Task ShowLockScreenAsync();
    Task<bool> ShowConfirmAsync(string title, string message);
    void CloseModal();
}
