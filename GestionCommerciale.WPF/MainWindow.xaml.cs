using System.Windows;
using GestionCommerciale.WPF.Services;

namespace GestionCommerciale.WPF;

public partial class MainWindow : Window
{
    private readonly ApiService _api = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = "";
        BtnLogin.IsEnabled = false;
        BtnLogin.Content = "Connexion...";

        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            TxtError.Text = "Veuillez remplir tous les champs.";
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Se connecter";
            return;
        }

        var success = await _api.LoginAsync(username, password);

        if (success)
        {
            var dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }
        else
        {
            TxtError.Text = "Identifiants incorrects.";
            BtnLogin.IsEnabled = true;
            BtnLogin.Content = "Se connecter";
        }
    }
}