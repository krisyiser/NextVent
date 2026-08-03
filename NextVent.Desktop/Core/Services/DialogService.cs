using System;
using System.Threading.Tasks;
using NextVent.ViewModels.Dialogs;

namespace NextVent.Core.Services;

public class DialogService : IDialogService
{
    private readonly Func<object?, Task<object?>> _showModalHandler;
    private readonly Action _closeModalHandler;

    public DialogService(Func<object?, Task<object?>> showModalHandler, Action closeModalHandler)
    {
        _showModalHandler = showModalHandler;
        _closeModalHandler = closeModalHandler;
    }

    public async Task<TResult?> ShowModalAsync<TViewModel, TResult>(TViewModel viewModel) where TViewModel : class
    {
        var rawResult = await _showModalHandler(viewModel);
        if (rawResult is TResult result)
        {
            return result;
        }
        return default;
    }

    public async Task ShowLockScreenAsync()
    {
        await _showModalHandler("LOCK_SCREEN");
    }

    public async Task<bool> ShowConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();
        var confirmVm = new ConfirmDialogViewModel(title, message, (result) =>
        {
            _closeModalHandler();
            tcs.TrySetResult(result);
        });

        await _showModalHandler(confirmVm);
        return await tcs.Task;
    }

    public void CloseModal()
    {
        _closeModalHandler();
    }
}
