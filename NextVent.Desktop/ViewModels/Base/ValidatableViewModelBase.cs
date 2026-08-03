using CommunityToolkit.Mvvm.ComponentModel;
using FluentValidation;
using FluentValidation.Results;
using System.Collections;
using System.ComponentModel;
using System.Linq;

namespace NextVent.ViewModels.Base;

public abstract class ValidatableViewModelBase : ObservableObject, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public bool HasErrors => _errors.Any();

    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            return Enumerable.Empty<string>();

        return _errors[propertyName];
    }

    protected async Task<bool> ValidateDtoAsync<TDto>(TDto dto, IValidator<TDto> validator)
    {
        if (validator == null) return true;

        var result = await validator.ValidateAsync(dto);

        var oldErrorProperties = _errors.Keys.ToList();
        _errors.Clear();

        foreach (var error in result.Errors)
        {
            if (!_errors.ContainsKey(error.PropertyName))
            {
                _errors[error.PropertyName] = new List<string>();
            }
            _errors[error.PropertyName].Add(error.ErrorMessage);
        }

        var propertiesToNotify = oldErrorProperties.Union(_errors.Keys).Distinct();

        foreach (var prop in propertiesToNotify)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(prop));
        }

        OnPropertyChanged(nameof(HasErrors));

        return result.IsValid;
    }
}
