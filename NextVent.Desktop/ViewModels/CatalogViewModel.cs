using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using NextVent.Core.Messages;
using NextVent.Data.Dtos;
using NextVent.Services.Interfaces;
using NextVent.Core.State;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace NextVent.ViewModels;

public partial class CatalogViewModel : ObservableObject
{
    private readonly IProductService _productService;
    private readonly IExternalCatalogService _externalCatalogService;
    private readonly IItemKitService? _kitService;
    private readonly CartStateStore _cartStateStore;
    public event Action<NextVent.ViewModels.Dialogs.ProductDialogParameters>? OpenProductDialogWithParamsRequested;

    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<ProductDto> FilteredProducts { get; } = [];
    public ObservableCollection<CategoryChipDto> CategoryChips { get; } = [];

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _selectedCategory = "⭐ Top Ventas";
    [ObservableProperty] private string _feedbackMessage = string.Empty;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FeedbackColor))]
    private bool _feedbackIsError;

    public string FeedbackColor => FeedbackIsError ? "#EF4444" : "#10B981";

    public CatalogViewModel(IProductService productService, IExternalCatalogService externalCatalogService, IItemKitService? kitService, CartStateStore cartStateStore)
    {
        _productService = productService;
        _externalCatalogService = externalCatalogService;
        _kitService = kitService;
        _cartStateStore = cartStateStore;
    }

    public async Task LoadProductsAsync()
    {
        try
        {
            var list = await _productService.GetAllAsync();
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Products.Clear();
                foreach (var p in list) Products.Add(p);
                BuildCategoryChips();
                FilterProducts();
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading products");
        }
    }

    private void BuildCategoryChips()
    {
        CategoryChips.Clear();
        CategoryChips.Add(new CategoryChipDto("⭐ Top Ventas", Products.Count, $"⭐ TOP VENTAS ({Products.Count})"));

        var groups = Products.GroupBy(p => p.Category ?? "General").OrderBy(g => g.Key);
        foreach (var g in groups)
        {
            CategoryChips.Add(new CategoryChipDto(g.Key, g.Count(), $"{g.Key.ToUpper()} ({g.Count()})"));
        }
    }

    [RelayCommand]
    private void SelectCategoryChip(CategoryChipDto chip)
    {
        if (chip == null) return;
        SelectedCategory = chip.Name;
        FilterProducts();
    }

    private void FilterProducts()
    {
        FilteredProducts.Clear();
        var query = SearchQuery.Trim().ToLower();

        var matches = Products.Where(p =>
            (SelectedCategory == "⭐ Top Ventas" || SelectedCategory == "Todos" || p.Category == SelectedCategory) &&
            (string.IsNullOrWhiteSpace(query) ||
             p.Name.ToLower().Contains(query) ||
             (p.Barcode != null && p.Barcode.ToLower().Contains(query))) &&
            p.Stock > 0.0
        );

        foreach (var m in matches) FilteredProducts.Add(m);
    }

    partial void OnSearchQueryChanged(string value) => FilterProducts();

    [RelayCommand]
    private void FocusSearch() => WeakReferenceMessenger.Default.Send(new FocusSearchMessage());

    [RelayCommand]
    private async Task ProcessScanOrSearchSubmit()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(SearchQuery)) return;

            var input = SearchQuery.Trim();
            double quantityMultiplier = 1.0;
            string productQuery = input;

            var parts = input.Split('*', 2);
            if (parts.Length == 2 && double.TryParse(parts[0], out double parsedQty))
            {
                quantityMultiplier = parsedQty;
                productQuery = parts[1].Trim();
            }

            var p = Products.FirstOrDefault(x =>
                (x.Barcode != null && x.Barcode.Equals(productQuery, StringComparison.OrdinalIgnoreCase)) ||
                x.Name.Equals(productQuery, StringComparison.OrdinalIgnoreCase));

            if (p == null) p = Products.FirstOrDefault(x => x.Name.Contains(productQuery, StringComparison.OrdinalIgnoreCase));

            if (p != null)
            {
                AddToCartWithQuantity(p, quantityMultiplier);
                SearchQuery = string.Empty;
            }
            else
            {
                await TryAddKitBarcodeAsync(productQuery, quantityMultiplier);
            }
        }
        finally
        {
            WeakReferenceMessenger.Default.Send(new FocusSearchMessage());
        }
    }

    private async Task TryAddKitBarcodeAsync(string barcode, double multiplier)
    {
        bool kitFound = false;
        if (_kitService != null)
        {
            var kit = await _kitService.GetByBarcodeAsync(barcode);
            if (kit != null)
            {
                kitFound = true;
                foreach (var item in kit.Items)
                {
                    var prod = Products.FirstOrDefault(p => p.Id == item.ProductId);
                    if (prod != null)
                    {
                        AddToCartWithQuantity(prod, item.Quantity * multiplier);
                    }
                }
                SearchQuery = string.Empty;
                FeedbackMessage = $"¡Combo / Paquete '{kit.Name}' agregado al ticket!";
            }
        }

        if (!kitFound)
        {
            FeedbackMessage = $"Buscando '{barcode}' en OpenFoodFacts...";
            var externalProduct = await _externalCatalogService.FetchProductByBarcodeAsync(barcode);
            
            if (externalProduct != null)
            {
                OpenProductDialogWithParamsRequested?.Invoke(new NextVent.ViewModels.Dialogs.ProductDialogParameters 
                { 
                    IsEditMode = false,
                    PreFilledData = externalProduct,
                    ShowAutoFillBanner = true
                });
            }
            else
            {
                OpenProductDialogWithParamsRequested?.Invoke(new NextVent.ViewModels.Dialogs.ProductDialogParameters 
                { 
                    IsEditMode = false,
                    PreFilledBarcode = barcode
                });
            }
            SearchQuery = string.Empty;
        }
    }

    [RelayCommand]
    private void AddToCart(ProductDto product) => AddToCartWithQuantity(product, 1.0);

    private void AddToCartWithQuantity(ProductDto product, double qty)
    {
        if (product == null) return;

        var cartItem = new CartItemDto(product.Id, product.Name, product.Price, qty, product.Unit)
        {
            Category = product.Category ?? "General",
            Cost = product.Cost,
            OriginalUnitPrice = product.Price
        };

        _cartStateStore.AddItem(cartItem, product.Stock);

        FeedbackIsError = false;
        FeedbackMessage = $"Agregado: {product.Name}";
    }
}
