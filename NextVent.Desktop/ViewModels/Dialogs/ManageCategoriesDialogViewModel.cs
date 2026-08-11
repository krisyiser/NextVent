using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NextVent.Data;
using NextVent.Data.Entities;

namespace NextVent.ViewModels.Dialogs;

public partial class ManageCategoriesDialogViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<string> Categories { get; } = [];

    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public event Action? RequestClose;
    public event Action? CategoriesUpdated;

    public ManageCategoriesDialogViewModel(AppDbContext db)
    {
        _db = db;
        _ = LoadCategoriesAsync();
    }

    public async Task LoadCategoriesAsync()
    {
        try
        {
            var list = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync();

            Categories.Clear();
            foreach (var item in list)
            {
                Categories.Add(item);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar categorías: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            ErrorMessage = "El nombre de la categoría no puede estar vacío.";
            return;
        }

        string trimmedName = NewCategoryName.Trim();

        try
        {
            bool exists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                ErrorMessage = "Esta categoría ya existe.";
                return;
            }

            var entity = new CategoryEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = trimmedName
            };

            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();

            NewCategoryName = string.Empty;
            ErrorMessage = string.Empty;
            await LoadCategoriesAsync();
            CategoriesUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al guardar categoría: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}
