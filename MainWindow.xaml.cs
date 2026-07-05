using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AgendamentoWpfApp.Services;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace AgendamentoWpfApp;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthApiService _authApiService;

    internal MainWindow(IServiceProvider serviceProvider, AuthApiService authApiService)
    {
        _serviceProvider = serviceProvider;
        _authApiService = authApiService;
        InitializeComponent();
        ApiInfoTextBlock.Text = $"API: {AppSettings.ApiBaseUrl}";
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        var baseUrl = AppSettings.ApiBaseUrl;
        var email = EmailTextBox.Text.Trim();
        var senha = PasswordBox.Password;

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(senha))
        {
            ShowStatus("Informe e-mail e senha.", StatusKind.Warning);
            return;
        }

        if (!InputNormalizer.IsValidEmail(email))
        {
            ShowStatus("Informe um e-mail valido.", StatusKind.Warning);
            EmailTextBox.Focus();
            return;
        }

        SetBusy(true);
        ShowStatus("Conectando...", StatusKind.Info);

        var result = await _authApiService.LoginAsync(email, senha);
        try
        {
            if (!result.Success || result.Value == null)
            {
                ShowStatus(result.Message, StatusKind.Error);
                return;
            }

            SessionState.BaseUrl = baseUrl;
            SessionState.Token = result.Value.Token ?? "";
            SessionState.UsuarioNome = result.Value.Usuario?.Nome ?? email;
            SessionState.EmpresaNome = result.Value.Empresa?.NomeFantasia ?? result.Value.Empresa?.RazaoSocial ?? "Empresa";

            var dashboard = _serviceProvider.GetRequiredService<DashboardWindow>();
            dashboard.Owner = this;
            Hide();
            dashboard.ShowDialog();
            Show();
        }
        catch (Exception ex)
        {
            ShowStatus($"Erro inesperado: {ex.Message}", StatusKind.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        LoginButton.Content = busy ? "Entrando..." : "Entrar";
        EmailTextBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
    }

    private void CreateAccountButton_Click(object sender, RoutedEventArgs e)
    {
        var window = _serviceProvider.GetRequiredService<CreateAccountWindow>();
        window.Owner = this;

        if (window.ShowDialog() == true && !string.IsNullOrWhiteSpace(window.ConfirmedEmail))
        {
            EmailTextBox.Text = window.ConfirmedEmail;
            ShowStatus("Conta confirmada. Informe a senha e entre no sistema.", StatusKind.Info);
        }
    }

    private void ShowStatus(string message, StatusKind kind)
    {
        StatusBorder.Visibility = Visibility.Visible;
        StatusTextBlock.Text = message;

        switch (kind)
        {
            case StatusKind.Error:
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(253, 242, 242));
                StatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(233, 196, 192));
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(139, 46, 38));
                break;
            case StatusKind.Warning:
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(255, 248, 235));
                StatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(232, 207, 160));
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(113, 78, 28));
                break;
            default:
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(252, 235, 217));
                StatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(242, 217, 184));
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(201, 94, 12));
                break;
        }
    }
}

internal enum StatusKind
{
    Info,
    Warning,
    Error
}
