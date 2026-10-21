using System.Windows;
using System.Windows.Controls;
using GestionCommerciale.WPF.Services;

namespace GestionCommerciale.WPF.Views;

public partial class ProductsPage : Page
{
    private readonly ApiService _api = new();

    public ProductsPage()
    {
        InitializeComponent();

        if (ApiService.Role != "Admin")
            BtnAdd.Visibility = Visibility.Collapsed;

        Loaded += async (s, e) => await LoadProductsAsync();
    }


    private async Task LoadProductsAsync(string search = "")
    {
        try
        {
            var result = await _api.GetProductsAsync(search);

            if (result == null)
            {
                MessageBox.Show(
                    "Impossible de charger les produits.",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            ProductsGrid.ItemsSource = result.Products;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Une erreur est survenue lors du chargement des produits :\n{ex.Message}",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }


    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        => await LoadProductsAsync(TxtSearch.Text.Trim());

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            await LoadProductsAsync();
    }

    private void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddProductWindow();
        if (dialog.ShowDialog() == true)
            _ = LoadProductsAsync();
    }
}