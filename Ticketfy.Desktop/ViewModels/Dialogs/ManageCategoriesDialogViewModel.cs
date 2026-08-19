using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Ticketfy.Data;
using Ticketfy.Data.Entities;

namespace Ticketfy.ViewModels.Dialogs;

public class CategoryItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public string ProductCountText => ProductCount == 1 ? "(1 producto)" : $"({ProductCount} productos)";
    public bool IsNotGeneral => Name.ToLower() != "general";
}

public partial class ManageCategoriesDialogViewModel : ObservableObject
{
    private readonly AppDbContext _db;

    public ObservableCollection<CategoryItemViewModel> Categories { get; } = [];

    [ObservableProperty] private string _newCategoryName = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _inputHeader = "Nueva Categoría";
    [ObservableProperty] private string _saveButtonText = "Agregar";
    [ObservableProperty] private bool _isEditing = false;

    private CategoryItemViewModel? _editingCategory = null;

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
            // Load categories and count products in each category
            var categoriesList = await _db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            var productsList = await _db.Products
                .AsNoTracking()
                .Select(p => p.Category)
                .ToListAsync();

            Categories.Clear();
            foreach (var cat in categoriesList)
            {
                int count = productsList.Count(c => c != null && c.ToLower() == cat.Name.ToLower());
                Categories.Add(new CategoryItemViewModel
                {
                    Id = cat.Id,
                    Name = cat.Name,
                    ProductCount = count
                });
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al cargar categorías: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCategoryName))
        {
            ErrorMessage = "El nombre de la categoría no puede estar vacío.";
            return;
        }

        string trimmedName = NewCategoryName.Trim();

        try
        {
            if (IsEditing && _editingCategory != null)
            {
                // Renaming
                if (_editingCategory.Name.ToLower() == trimmedName.ToLower())
                {
                    CancelEdit();
                    return;
                }

                bool exists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == trimmedName.ToLower() && c.Id != _editingCategory.Id);
                if (exists)
                {
                    ErrorMessage = "Esta categoría ya existe.";
                    return;
                }

                var entity = await _db.Categories.FindAsync(_editingCategory.Id);
                if (entity != null)
                {
                    string oldName = entity.Name;
                    entity.Name = trimmedName;

                    // Migrate products in database to the new name
                    var productsToUpdate = await _db.Products
                        .Where(p => p.Category.ToLower() == oldName.ToLower())
                        .ToListAsync();
                    foreach (var p in productsToUpdate)
                    {
                        p.Category = trimmedName;
                    }

                    await _db.SaveChangesAsync();
                }

                CancelEdit();
            }
            else
            {
                // Adding new category
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
            }

            await LoadCategoriesAsync();
            CategoriesUpdated?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al guardar categoría: " + ex.Message;
        }
    }

    [RelayCommand]
    public void StartEdit(CategoryItemViewModel item)
    {
        if (item == null || !item.IsNotGeneral) return;

        _editingCategory = item;
        NewCategoryName = item.Name;
        InputHeader = "Editar Categoría";
        SaveButtonText = "Guardar";
        IsEditing = true;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    public void CancelEdit()
    {
        _editingCategory = null;
        NewCategoryName = string.Empty;
        InputHeader = "Nueva Categoría";
        SaveButtonText = "Agregar";
        IsEditing = false;
        ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(CategoryItemViewModel item)
    {
        if (item == null || !item.IsNotGeneral) return;

        try
        {
            var entity = await _db.Categories.FindAsync(item.Id);
            if (entity != null)
            {
                // Migrate all products in this category to "General"
                var productsToUpdate = await _db.Products
                    .Where(p => p.Category.ToLower() == item.Name.ToLower())
                    .ToListAsync();
                foreach (var p in productsToUpdate)
                {
                    p.Category = "General";
                }

                _db.Categories.Remove(entity);
                await _db.SaveChangesAsync();

                // If currently editing the deleted category, cancel the edit
                if (_editingCategory?.Id == item.Id)
                {
                    CancelEdit();
                }

                ErrorMessage = string.Empty;
                await LoadCategoriesAsync();
                CategoriesUpdated?.Invoke();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Error al eliminar categoría: " + ex.Message;
        }
    }

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
    }
}
