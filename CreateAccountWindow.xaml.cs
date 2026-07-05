using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using AgendamentoWpfApp.Services;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp;

public partial class CreateAccountWindow : Window
{
    private readonly AuthApiService _authApiService;

    public string? ConfirmedEmail { get; private set; }
    private bool _formattingCnpj;
    private bool _formattingCodigo;

    internal CreateAccountWindow(AuthApiService authApiService)
    {
        _authApiService = authApiService;
        InitializeComponent();
    }

    private async void CreateButton_Click(object sender, RoutedEventArgs e)
    {
        await CreateAccountAsync();
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        await ConfirmEmailAsync();
    }

    private async Task CreateAccountAsync()
    {
        var email = EmailTextBox.Text.Trim();
        var password = SenhaBox.Password;

        var cnpjDigits = InputNormalizer.OnlyDigits(CnpjTextBox.Text);
        if (string.IsNullOrWhiteSpace(cnpjDigits) ||
            string.IsNullOrWhiteSpace(RazaoSocialTextBox.Text) ||
            string.IsNullOrWhiteSpace(NomeFantasiaTextBox.Text) ||
            string.IsNullOrWhiteSpace(UsuarioNomeTextBox.Text) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            ShowStatus("Preencha os dados da empresa, e-mail e senha.", StatusKind.Warning);
            return;
        }

        if (cnpjDigits.Length != 14)
        {
            ShowStatus("CNPJ deve ter 14 digitos.", StatusKind.Warning);
            CnpjTextBox.Focus();
            return;
        }

        if (!InputNormalizer.IsValidEmail(email))
        {
            ShowStatus("Informe um e-mail valido.", StatusKind.Warning);
            EmailTextBox.Focus();
            return;
        }

        if (password.Length < 8)
        {
            ShowStatus("A senha deve ter pelo menos 8 caracteres.", StatusKind.Warning);
            SenhaBox.Focus();
            return;
        }

        SetBusy(true);
        ShowStatus("Criando conta e enviando codigo...", StatusKind.Info);

        var request = new CreateAccountRequest
        {
            Cnpj = cnpjDigits,
            RazaoSocial = RazaoSocialTextBox.Text.Trim(),
            NomeFantasia = NomeFantasiaTextBox.Text.Trim(),
            Email = email,
            UsuarioNome = UsuarioNomeTextBox.Text.Trim(),
            Login = string.Empty,
            Senha = password,
            Perfil = "Administrador"
        };

        var result = await _authApiService.CreateAccountAsync(request);
        if (!result.Success)
        {
            ShowStatus(result.Message, StatusKind.Error);
            SetBusy(false);
            return;
        }

        ShowStatus("Conta criada. Digite o codigo recebido no e-mail e confirme.", StatusKind.Info);
        CodigoTextBox.Focus();
        SetBusy(false);
    }

    private async Task ConfirmEmailAsync()
    {
        var email = EmailTextBox.Text.Trim();
        var code = InputNormalizer.OnlyDigits(CodigoTextBox.Text);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(code))
        {
            ShowStatus("Informe o e-mail e o codigo recebido.", StatusKind.Warning);
            return;
        }

        if (!InputNormalizer.IsValidEmail(email))
        {
            ShowStatus("Informe um e-mail valido.", StatusKind.Warning);
            EmailTextBox.Focus();
            return;
        }

        if (code.Length != 6)
        {
            ShowStatus("Codigo deve ter 6 digitos.", StatusKind.Warning);
            CodigoTextBox.Focus();
            return;
        }

        SetBusy(true);
        ShowStatus("Confirmando e-mail...", StatusKind.Info);

        var result = await _authApiService.ConfirmEmailAsync(email, code);
        if (!result.Success)
        {
            ShowStatus(result.Message, StatusKind.Error);
            SetBusy(false);
            return;
        }

        ConfirmedEmail = email;
        ShowStatus("E-mail confirmado. A conta ja pode fazer login.", StatusKind.Info);
        DialogResult = true;
        SetBusy(false);
    }

    private void SetBusy(bool busy)
    {
        CreateButton.IsEnabled = !busy;
        ConfirmButton.IsEnabled = !busy;
        CnpjTextBox.IsEnabled = !busy;
        RazaoSocialTextBox.IsEnabled = !busy;
        NomeFantasiaTextBox.IsEnabled = !busy;
        UsuarioNomeTextBox.IsEnabled = !busy;
        EmailTextBox.IsEnabled = !busy;
        SenhaBox.IsEnabled = !busy;
        CodigoTextBox.IsEnabled = !busy;
    }

    private void CnpjTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_formattingCnpj)
            return;

        _formattingCnpj = true;
        CnpjTextBox.Text = FormatCnpj(InputNormalizer.OnlyDigits(CnpjTextBox.Text));
        CnpjTextBox.CaretIndex = CnpjTextBox.Text.Length;
        _formattingCnpj = false;
    }

    private void CodigoTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_formattingCodigo)
            return;

        _formattingCodigo = true;
        var digits = InputNormalizer.OnlyDigits(CodigoTextBox.Text);
        CodigoTextBox.Text = digits.Length > 6 ? digits[..6] : digits;
        CodigoTextBox.CaretIndex = CodigoTextBox.Text.Length;
        _formattingCodigo = false;
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

    private static string FormatCnpj(string digits)
    {
        digits = InputNormalizer.OnlyDigits(digits);
        if (digits.Length > 14)
            digits = digits[..14];

        if (digits.Length <= 2)
            return digits;
        if (digits.Length <= 5)
            return $"{digits[..2]}.{digits[2..]}";
        if (digits.Length <= 8)
            return $"{digits[..2]}.{digits[2..5]}.{digits[5..]}";
        if (digits.Length <= 12)
            return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..]}";

        return $"{digits[..2]}.{digits[2..5]}.{digits[5..8]}/{digits[8..12]}-{digits[12..]}";
    }
}
