using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AgendamentoWpfApp.Services;
using AgendamentoWpfApp.Services.Validation;
using Microsoft.Web.WebView2.Core;
using Microsoft.Extensions.DependencyInjection;

namespace AgendamentoWpfApp;

public partial class MainWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AuthApiService _authApiService;
    private bool _passwordVisible;

    internal MainWindow(IServiceProvider serviceProvider, AuthApiService authApiService)
    {
        _serviceProvider = serviceProvider;
        _authApiService = authApiService;
        InitializeComponent();
        ApiInfoTextBlock.Text = $"API: {AppSettings.ApiBaseUrl}";
        Loaded += async (_, _) =>
        {
            await InitializeHologramAsync();
            await RefreshConnectionStatusAsync();
        };
    }

    private async Task InitializeHologramAsync()
    {
        var assetsPath = Path.Combine(AppContext.BaseDirectory, "Assets");
        var hologramPath = Path.Combine(assetsPath, "login-hologram.html");
        if (!Directory.Exists(assetsPath) || !File.Exists(hologramPath))
            return;

        HologramWebView.DefaultBackgroundColor = global::System.Drawing.Color.Transparent;
        await HologramWebView.EnsureCoreWebView2Async();
        HologramWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
        HologramWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        HologramWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "sparkcore.local",
            assetsPath,
            CoreWebView2HostResourceAccessKind.Allow);
        HologramWebView.Source = new Uri("https://sparkcore.local/login-hologram.html");
    }

    private string GetPassword()
    {
        return _passwordVisible ? PasswordRevealTextBox.Text : PasswordBox.Password;
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await LoginAsync();
    }

    private async Task LoginAsync()
    {
        var baseUrl = AppSettings.ApiBaseUrl;
        var email = EmailTextBox.Text.Trim();
        var senha = GetPassword();

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

    private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;

        if (_passwordVisible)
        {
            PasswordRevealTextBox.Text = PasswordBox.Password;
            PasswordRevealTextBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            EyeIcon.Visibility = Visibility.Collapsed;
            EyeOffIcon.Visibility = Visibility.Visible;
            PasswordRevealTextBox.Focus();
            PasswordRevealTextBox.CaretIndex = PasswordRevealTextBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = PasswordRevealTextBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordRevealTextBox.Visibility = Visibility.Collapsed;
            EyeIcon.Visibility = Visibility.Visible;
            EyeOffIcon.Visibility = Visibility.Collapsed;
            PasswordBox.Focus();
        }
    }

    private async void ForgotPasswordButton_Click(object sender, RoutedEventArgs e)
    {
        var email = EmailTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(email) || !InputNormalizer.IsValidEmail(email))
        {
            ShowStatus("Digite seu e-mail no campo acima e clique em Esqueceu? novamente.", StatusKind.Warning);
            EmailTextBox.Focus();
            return;
        }

        SetBusy(true);
        ShowStatus("Solicitando recuperacao de senha...", StatusKind.Info);

        try
        {
            var result = await _authApiService.RecuperarSenhaAsync(email);
            if (!result.Success || result.Value == null)
            {
                ShowStatus(result.Message, StatusKind.Error);
                return;
            }

            var mensagem = result.Value.Mensagem ?? "Solicitacao registrada.";
            if (!string.IsNullOrWhiteSpace(result.Value.SenhaTemporaria))
                mensagem += $" Senha temporaria: {result.Value.SenhaTemporaria}";

            ShowStatus(mensagem, StatusKind.Info);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task RefreshConnectionStatusAsync()
    {
        var online = await _authApiService.CheckConnectionAsync();

        if (online)
        {
            var green = Color.FromRgb(34, 197, 94);
            ConnectionDot.Fill = new SolidColorBrush(green);
            ConnectionDotGlow.Color = green;
            ConnectionStatusTextBlock.Text = "Conectado";
        }
        else
        {
            var red = Color.FromRgb(217, 83, 60);
            ConnectionDot.Fill = new SolidColorBrush(red);
            ConnectionDotGlow.Color = red;
            ConnectionStatusTextBlock.Text = "Offline";
        }
    }

    private void SetBusy(bool busy)
    {
        LoginButton.IsEnabled = !busy;
        LoginButton.Content = busy ? "Entrando..." : "Entrar";
        EmailTextBox.IsEnabled = !busy;
        PasswordBox.IsEnabled = !busy;
        PasswordRevealTextBox.IsEnabled = !busy;
        ForgotPasswordButton.IsEnabled = !busy;
        TogglePasswordButton.IsEnabled = !busy;
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
                StatusBorder.Background = new SolidColorBrush(Color.FromRgb(253, 243, 234));
                StatusBorder.BorderBrush = new SolidColorBrush(Color.FromRgb(247, 224, 204));
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(194, 90, 8));
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
