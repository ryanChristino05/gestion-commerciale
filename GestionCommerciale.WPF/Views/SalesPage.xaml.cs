
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using GestionCommerciale.WPF.Services;

namespace GestionCommerciale.WPF.Views;

public partial class SalesPage : Page
{
    private readonly ApiService _apiService = new();

    private SaleDto? _selectedSale;

    private readonly ObservableCollection<SaleLine> _saleItems = new();

    public SalesPage()
    {
        InitializeComponent();

        SaleItemsGrid.ItemsSource = _saleItems;

        CancelSaleButton.Visibility =
            string.Equals(ApiService.Role, "Admin",
                StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await ChargerClientsEtProduitsAsync();
        await ChargerVentesAsync();
    }

    private async Task ChargerClientsEtProduitsAsync()
    {
        try
        {
            ClientComboBox.IsEnabled = false;
            ProductComboBox.IsEnabled = false;

            var clients = await _apiService.GetClientsAsync();
            var resultatProduits = await _apiService.GetProductsAsync();

            ClientComboBox.ItemsSource = clients;
            ProductComboBox.ItemsSource = resultatProduits?.Products;

            if (ClientComboBox.Items.Count > 0)
                ClientComboBox.SelectedIndex = 0;

            if (ProductComboBox.Items.Count > 0)
                ProductComboBox.SelectedIndex = 0;

            if (clients == null || resultatProduits?.Products == null)
            {
                MessageTextBlock.Text =
                    "Attention : les clients ou les produits n'ont pas pu être chargés.";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Impossible de charger les clients et produits.\n{ex.Message}",
                "Erreur",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            ClientComboBox.IsEnabled = true;
            ProductComboBox.IsEnabled = true;
        }
    }

    private async void Actualiser_Click(object sender, RoutedEventArgs e)
    {
        await ChargerVentesAsync();
        await ChargerClientsEtProduitsAsync();
    }

    private async Task ChargerVentesAsync()
    {
        SalesDataGrid.IsEnabled = false;
        MessageTextBlock.Text = "Chargement des ventes...";

        try
        {
            var ventes = await _apiService.GetSalesAsync();

            if (ventes == null)
            {
                SalesDataGrid.ItemsSource = null;
                MessageTextBlock.Text =
                    "Impossible de récupérer les ventes. Vérifiez la connexion et votre session.";
                return;
            }

            var ventesTriees = ventes
                .OrderByDescending(v => v.Date)
                .ToList();

            SalesDataGrid.ItemsSource = ventesTriees;

            MessageTextBlock.Text =
                $"{ventesTriees.Count} vente(s) chargée(s).";

            _selectedSale = null;
            SalesDataGrid.SelectedItem = null;

            DetailsTextBlock.Text =
                "Sélectionnez une vente dans le tableau.";

            CancelReasonTextBox.Clear();
            CancelSaleButton.IsEnabled = false;
        }
        catch (Exception ex)
        {
            MessageTextBlock.Text =
                $"Erreur lors du chargement : {ex.Message}";
        }
        finally
        {
            SalesDataGrid.IsEnabled = true;
        }
    }

    private void AjouterProduit_Click(object sender, RoutedEventArgs e)
    {
        if (ProductComboBox.SelectedItem is not ProductDto produit)
        {
            MessageBox.Show(
                "Sélectionnez un produit.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(QuantityTextBox.Text, out int quantite) ||
            quantite <= 0)
        {
            MessageBox.Show(
                "La quantité doit être un nombre entier supérieur à zéro.",
                "Quantité invalide",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            QuantityTextBox.Focus();
            return;
        }

        var ligneExistante = _saleItems
            .FirstOrDefault(x => x.ProductId == produit.Id);

        int quantiteDejaAjoutee = ligneExistante?.Quantity ?? 0;

        if (quantite + quantiteDejaAjoutee > produit.StockQuantity)
        {
            MessageBox.Show(
                $"Stock insuffisant pour {produit.Name}.\n" +
                $"Stock disponible : {produit.StockQuantity}.",
                "Stock insuffisant",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (ligneExistante != null)
        {
            ligneExistante.Quantity += quantite;

            // Rafraîchir l'affichage après modification de la quantité.
            SaleItemsGrid.Items.Refresh();
        }
        else
        {
            _saleItems.Add(new SaleLine
            {
                ProductId = produit.Id,
                ProductName = produit.Name,
                UnitPrice = produit.UnitPrice,
                Quantity = quantite
            });
        }

        MettreAJourTotal();
        QuantityTextBox.Text = "1";
        MessageTextBlock.Text = $"{produit.Name} ajouté au panier.";
    }

    private void RetirerProduit_Click(object sender, RoutedEventArgs e)
    {
        if (SaleItemsGrid.SelectedItem is not SaleLine ligne)
        {
            MessageBox.Show(
                "Sélectionnez un article du panier à retirer.",
                "Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        _saleItems.Remove(ligne);
        MettreAJourTotal();
    }

    private void MettreAJourTotal()
    {
        decimal total = _saleItems.Sum(x => x.Subtotal);

        TotalTextBlock.Text = $"{total:N2} Ar";
    }

    private async void EnregistrerVente_Click(
        object sender, RoutedEventArgs e)
    {
        if (ClientComboBox.SelectedItem is not ClientDto client)
        {
            MessageBox.Show(
                "Sélectionnez un client.",
                "Client obligatoire",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (_saleItems.Count == 0)
        {
            MessageBox.Show(
                "Ajoutez au moins un produit au panier.",
                "Panier vide",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var confirmation = MessageBox.Show(
            $"Confirmer la vente pour {client.Name} ?\n\n" +
            $"Nombre de lignes : {_saleItems.Count}\n" +
            $"Total estimé : {_saleItems.Sum(x => x.Subtotal):N2} Ar",
            "Confirmer la vente",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
            return;

        CreateSaleButton.IsEnabled = false;
        MessageTextBlock.Text = "Enregistrement de la vente...";

        try
        {
            // L'API calcule elle-même les prix et le total.
            var dto = new
            {
                clientId = client.Id,
                items = _saleItems.Select(x => new
                {
                    productId = x.ProductId,
                    quantity = x.Quantity
                }).ToList()
            };

            var (success, message) =
                await _apiService.CreateSaleAsync(dto);

            if (!success)
            {
                MessageTextBlock.Text =
                    $"La vente n'a pas pu être enregistrée : {message}";

                MessageBox.Show(
                    MessageTextBlock.Text,
                    "Échec de la vente",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Le panier reste présent pour permettre une correction.
                await ChargerClientsEtProduitsAsync();
                return;
            }

            _saleItems.Clear();
            MettreAJourTotal();

            await ChargerClientsEtProduitsAsync();
            await ChargerVentesAsync();

            MessageBox.Show(
                "La vente a été enregistrée avec succès.",
                "Succès",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageTextBlock.Text =
                $"Erreur lors de l'enregistrement : {ex.Message}";
        }
        finally
        {
            CreateSaleButton.IsEnabled = true;
        }
    }

    private void SalesDataGrid_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        _selectedSale = SalesDataGrid.SelectedItem as SaleDto;

        if (_selectedSale == null)
        {
            DetailsTextBlock.Text =
                "Sélectionnez une vente dans le tableau.";

            CancelSaleButton.IsEnabled = false;
            return;
        }

        DetailsTextBlock.Text =
            $"Vente : {_selectedSale.Number}\n" +
            $"Client : {_selectedSale.Client}\n" +
            $"Montant : {_selectedSale.TotalAmount:N2} Ar\n" +
            $"Statut : {_selectedSale.Status}";

        bool isAdmin = string.Equals(
            ApiService.Role, "Admin",
            StringComparison.OrdinalIgnoreCase);

        0bool isValidated = _selectedSale.Status == 0;

        CancelSaleButton.IsEnabled = isAdmin && isValidated;
    }

    private async void AnnulerVente_Click(
        object sender, RoutedEventArgs e)
    {
        if (_selectedSale == null)
            return;

        if (!string.Equals(
            ApiService.Role, "Admin",
            StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Seul un administrateur peut annuler une vente.",
                "Accès refusé",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        string reason = CancelReasonTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(reason))
        {
            MessageBox.Show(
                "Veuillez saisir le motif d'annulation.",
                "Motif obligatoire",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            CancelReasonTextBox.Focus();
            return;
        }

        var confirmation = MessageBox.Show(
            $"Confirmer l'annulation de la vente {_selectedSale.Number} ?\n\n" +
            "Le stock sera remis à jour par l'API.",
            "Confirmer l'annulation",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmation != MessageBoxResult.Yes)
            return;

        CancelSaleButton.IsEnabled = false;
        MessageTextBlock.Text = "Annulation en cours...";

        try
        {
            bool success = await _apiService.CancelSaleAsync(
                _selectedSale.Id, reason);

            if (success)
            {
                MessageBox.Show(
                    "La vente a été annulée. Le stock a été remis à jour.",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                await ChargerVentesAsync();
                await ChargerClientsEtProduitsAsync();
            }
            else
            {
                MessageTextBlock.Text =
                    "L'annulation a échoué. Vérifiez le statut de la vente et les droits.";
            }
        }
        catch (Exception ex)
        {
            MessageTextBlock.Text =
                $"Erreur lors de l'annulation : {ex.Message}";
        }
        finally
        {
            if (_selectedSale != null)
            {
                bool isAdmin = string.Equals(
                    ApiService.Role, "Admin",
                    StringComparison.OrdinalIgnoreCase);
                
                bool isValidated = _selectedSale.Status == 0;

                CancelSaleButton.IsEnabled = isAdmin && isValidated;
            }
        }
    }

    private sealed class SaleLine
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }

        public decimal Subtotal => UnitPrice * Quantity;
    }
}
