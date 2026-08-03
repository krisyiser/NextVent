using System.Threading.Tasks;

namespace NextVent.Core.Services;

public interface IDialogService
{
    Task<TResult?> ShowModalAsync<TViewModel, TResult>(TViewModel viewModel) where TViewModel : class;
    Task ShowLockScreenAsync();
    Task<bool> ShowConfirmAsync(string title, string message);
    void CloseModal();
}
