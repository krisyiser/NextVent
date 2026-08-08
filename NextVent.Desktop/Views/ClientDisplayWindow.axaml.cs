using Avalonia.Controls;
using NextVent.ViewModels;

namespace NextVent.Views;

public partial class ClientDisplayWindow : Window
{
    public ClientDisplayWindow()
    {
        InitializeComponent();
        var vm = new ClientDisplayViewModel();
        DataContext = vm;
        Closed += (s, e) => vm.Dispose();
    }
}
