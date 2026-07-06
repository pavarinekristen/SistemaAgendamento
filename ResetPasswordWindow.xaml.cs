using System.Windows;
using System.Windows.Media;
using AgendamentoWpfApp.Services;

namespace AgendamentoWpfApp;

/// <summary>
/// Segunda etapa do "Esqueceu?": recebe o código enviado por e-mail e a nova senha,
/// chamando POST /auth/redefinir-senha. A senha atual permanece válida até a confirmação.
/// </summary>
public partial class ResetPasswordWindow : Window
{
    private readonly AuthApiService _authApiService;
    private readonly string _email;

    internal ResetPasswordWindow(AuthApiService authApiService, string email)
    {
        InitializeComponent();
        _authApiService = authApiService;
        _email = email;
        SubtitleTextBlock.Text = $"Enviamos um código para {email}. Digite-o abaixo e escolha a nova senha.";
        Loaded += (_, _) => CodeTextBox.Focus();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        var codigo = CodeTextBox.Text.Trim();
        var novaSenha = NewPasswordBox.Password;
        var confirmacao = ConfirmPasswordBox.Password;

        if (string.IsNullOrWhiteSpace(codigo))
        {
            ShowStatus("Digite o código recebido por e-mail.", isError: true);
            CodeTextBox.Focus();
            return;
        }

        if (novaSenha.Trim().Length < 8)
        {
            ShowStatus("A nova senha deve possuir pelo menos 8 caracteres.", isError: true);
            NewPasswordBox.Focus();
            return;
        }

        if (novaSenha != confirmacao)
        {
            ShowStatus("A confirmação não confere com a nova senha.", isError: true);
            ConfirmPasswordBox.Focus();
            return;
        }

        SetBusy(true);
        ShowStatus("Redefinindo senha...", isError: false);

        try
        {
            var result = await _authApiService.RedefinirSenhaAsync(_email, codigo, novaSenha);
            if (!result.Success || result.Value == null)
            {
                ShowStatus(result.Message, isError: true);
                return;
            }

            DialogResult = true;
            Close();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        ConfirmButton.IsEnabled = !busy;
        CancelButton.IsEnabled = !busy;
        CodeTextBox.IsEnabled = !busy;
        NewPasswordBox.IsEnabled = !busy;
        ConfirmPasswordBox.IsEnabled = !busy;
    }

    private void ShowStatus(string message, bool isError)
    {
        StatusTextBlock.Text = message;
        StatusTextBlock.Foreground = isError
            ? (Brush)FindResource("SparkDangerBrush")
            : (Brush)FindResource("SparkMutedBrush");
        StatusTextBlock.Visibility = Visibility.Visible;
    }
}
