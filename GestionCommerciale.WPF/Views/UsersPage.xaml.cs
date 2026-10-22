using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GestionCommerciale.WPF.Services;

namespace GestionCommerciale.WPF.Views;

public partial class UsersPage : Page
{
    private readonly ApiService _api = new();

    public UsersPage()
    {
        InitializeComponent();
    }

    private async void BtnCreate_Click(object sender, RoutedEventArgs e)
    {
        var username = TxtUsername.Text.Trim();
        var password = TxtPassword.Password;
        var role = (CbRole.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Seller";

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowMessage("Le nom d'utilisateur et le mot de passe sont requis.", error: true);
            return;
        }

        BtnCreate.IsEnabled = false;

        try
        {
            var (success, message) = await _api.CreateUserAsync(username, password, role);

            if (success)
            {
                ShowMessage($"Compte {role} créé avec succès.", error: false);
                TxtUsername.Clear();
                TxtPassword.Clear();
                CbRole.SelectedIndex = 0;
            }
            else
            {
                ShowMessage($"Erreur : {message}", error: true);
            }
        }
        catch (System.Exception ex)
        {
            ShowMessage($"Erreur : {ex.Message}", error: true);
        }
        finally
        {
            BtnCreate.IsEnabled = true;
        }
    }

    private void ShowMessage(string msg, bool error)
    {
        TxtMessage.Text = msg;
        TxtMessage.Foreground = error
            ? new SolidColorBrush(Color.FromRgb(220, 80, 80))
            : new SolidColorBrush(Color.FromRgb(60, 150, 60));
        TxtMessage.Visibility = Visibility.Visible;
    }
}