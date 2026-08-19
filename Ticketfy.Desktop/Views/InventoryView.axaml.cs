using Avalonia.Controls;
using Ticketfy.Data.Dtos;
using Ticketfy.ViewModels;

namespace Ticketfy.Views;

public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
    }

    private void OnDataGridDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (sender is DataGrid dg && dg.SelectedItem is ProductDto product)
        {
            if (DataContext is InventoryViewModel vm)
            {
                vm.EditProductCommand.Execute(product);
            }
        }
    }
}
