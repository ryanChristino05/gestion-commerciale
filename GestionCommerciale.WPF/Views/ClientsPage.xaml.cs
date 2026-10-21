using System.Windows;
using System.Windows.Controls;
using GestionCommerciale.WPF.Services;

namespace GestionCommerciale.WPF.Views;

public partial class ClientsPage : Page
{
    private readonly ApiService _api = new();

    public ClientsPage()
    {
        InitializeComponent();
        Loaded += async (s, e) => await LoadClientsAsync();
    }

    private async Task LoadClientsAsync(string search = "")
    {
        try
        {
            var clients = await _api.GetClientsAsync(search);

            if (clients == null)
            {
                MessageBox.Show("Impossible de charger les clients.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ClientsGrid.ItemsSource = clients;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des clients :\n{ex.Message}", "Erreur",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        => await LoadClientsAsync(TxtSearch.Text.Trim());

    private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtSearch.Text))
            await LoadClientsAsync();
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        var name = TxtName.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("Le nom est requis.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        BtnAdd.IsEnabled = false;

        try
        {
            var success = await _api.CreateClientAsync(new
            {
                name,
                phone = TxtPhone.Text.Trim(),
                email = TxtEmail.Text.Trim(),
                address = TxtAddress.Text.Trim()
            });

            if (!success)
            {
                MessageBox.Show("Erreur lors de la création du client.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            TxtName.Clear();
            TxtPhone.Clear();
            TxtEmail.Clear();
            TxtAddress.Clear();

            await LoadClientsAsync();
        }
        finally
        {
            BtnAdd.IsEnabled = true;
        }
    }
}