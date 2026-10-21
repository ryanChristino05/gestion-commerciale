using System.Windows;
using GestionCommerciale.WPF.Services;
using GestionCommerciale.WPF.Views;

namespace GestionCommerciale.WPF;

public partial class DashboardWindow : Window
{
    public DashboardWindow()
    {
        InitializeComponent();

        TxtUser.Text = "Bienvenue, " + (ApiService.Token != null ? "utilisateur" : "?");
        TxtRole.Text = ApiService.Role == "Admin" ? "Administrateur" : "Vendeur";

        // Cacher le bouton Stock si Seller
        if (ApiService.Role != "Admin")
            BtnStockMenu.Visibility = Visibility.Collapsed;

        // Page par défaut
        MainFrame.Navigate(new ProductsPage());
    }

    private void BtnProduits_Click(object sender, RoutedEventArgs e)
        => MainFrame.Navigate(new ProductsPage());

    private void BtnStock_Click(object sender, RoutedEventArgs e)
        => MainFrame.Navigate(new StockPage());

    private void BtnClients_Click(object sender, RoutedEventArgs e)
        => MainFrame.Navigate(new ClientsPage());

    private void BtnVentes_Click(object sender, RoutedEventArgs e)
        => MainFrame.Navigate(new SalesPage());

    private void BtnDeconnexion_Click(object sender, RoutedEventArgs e)
    {
        var api = new ApiService();
        api.Logout();

        var login = new MainWindow();
        login.Show();

        this.Close();
    }
}