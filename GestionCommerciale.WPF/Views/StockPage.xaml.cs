using GestionCommerciale.WPF.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestionCommerciale.WPF.Views;

public partial class StockPage : Page
{
    private readonly ApiService _api = new();

    public StockPage()
    {
        InitializeComponent();

        if (ApiService.Role != "Admin")
        {
            BtnValider.IsEnabled = false;
            ShowMessage("Accès réservé à l'administrateur.", error: true);
        }

        Loaded += async (_, _) => await ChargerProduitsAsync();
    }

    private async Task ChargerProduitsAsync()
    {
        var result = await _api.GetProductsAsync(page: 1);
        if (result is not null)
            CbProduit.ItemsSource = result.Products;
    }

    private async void BtnValider_Click(object sender, RoutedEventArgs e)
    {
        // Validation
        if (CbProduit.SelectedValue is not int productId)
        {
            ShowMessage("Sélectionne un produit.", error: true);
            return;
        }

        if (!int.TryParse(TxtQuantite.Text.Trim(), out int qty) || qty <= 0)
        {
            ShowMessage("Quantité invalide.", error: true);
            return;
        }

        var motif = TxtMotif.Text.Trim();
        if (string.IsNullOrEmpty(motif))
        {
            ShowMessage("Le motif est obligatoire.", error: true);
            return;
        }

        BtnValider.IsEnabled = false;
        var ok = await _api.StockInAsync(productId, qty, motif);
        BtnValider.IsEnabled = true;

        if (ok)
        {
            ShowMessage("Entrée de stock enregistrée ✓", error: false);
            TxtQuantite.Text = "";
            TxtMotif.Text = "";
            CbProduit.SelectedIndex = -1;
            await ChargerProduitsAsync(); // rafraîchit le stock affiché dans le ComboBox
        }
        else
        {
            ShowMessage("Erreur lors de l'enregistrement.", error: true);
        }
    }

    private void ShowMessage(string msg, bool error)
    {
        TxtMessage.Text = msg;
        TxtMessage.Foreground = error
            ? new SolidColorBrush(Color.FromRgb(220, 80, 80))
            : new SolidColorBrush(Color.FromRgb(100, 200, 100));
        TxtMessage.Visibility = Visibility.Visible;
    }
}