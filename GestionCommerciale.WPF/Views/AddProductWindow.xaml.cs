
using System.Globalization;
using System.Windows;
using GestionCommerciale.WPF.Services;

namespace GestionCommerciale.WPF.Views;

public partial class AddProductWindow : Window
{
    private readonly ApiService _api = new();

    public AddProductWindow()
    {
        InitializeComponent();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = "";

        string reference = TxtReference.Text.Trim();
        string name = TxtName.Text.Trim();
        string unit = TxtUnit.Text.Trim();

        if (string.IsNullOrWhiteSpace(reference) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(unit))
        {
            TxtError.Text = "Veuillez remplir la référence, le nom et l'unité.";
            return;
        }

        if (!decimal.TryParse(
                TxtUnitPrice.Text.Trim(),
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal unitPrice) || unitPrice < 0)
        {
            TxtError.Text = "Veuillez saisir un prix valide et positif ou nul.";
            return;
        }

        if (!int.TryParse(TxtStockQuantity.Text.Trim(), out int stockQuantity)
            || stockQuantity < 0)
        {
            TxtError.Text = "Le stock doit être un entier positif ou nul.";
            return;
        }

        if (!int.TryParse(TxtAlertThreshold.Text.Trim(), out int alertThreshold)
            || alertThreshold < 0)
        {
            TxtError.Text = "Le seuil d'alerte doit être un entier positif ou nul.";
            return;
        }

        BtnSave.IsEnabled = false;
        BtnSave.Content = "Enregistrement...";

        try
        {
            var dto = new
            {
                reference,
                name,
                unitPrice,
                unit,
                initialStock = stockQuantity,
                alertThreshold
            };

            bool success = await _api.CreateProductAsync(dto);

            if (success)
            {
                DialogResult = true;
            }
            else
            {
                TxtError.Text = "L'API a refusé la création du produit.";
            }
        }
        catch (Exception ex)
        {
            TxtError.Text = ex.Message;
        }
        finally
        {
            BtnSave.IsEnabled = true;
            BtnSave.Content = "Enregistrer";
        }
    }
}
