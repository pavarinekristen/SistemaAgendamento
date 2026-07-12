using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _isSidebarCollapsed = !_isSidebarCollapsed;
        ApplySidebarLayout();
    }

    private void ApplySidebarLayout()
    {
        SidebarColumn.Width = new GridLength(_isSidebarCollapsed ? 76 : 236);
        FooterSidebarColumn.Width = SidebarColumn.Width;
        SidebarContentGrid.Margin = _isSidebarCollapsed
            ? new Thickness(10, 18, 10, 18)
            : new Thickness(16, 22, 16, 22);
        MainTabControl.Margin = _isSidebarCollapsed
            ? new Thickness(20, 24, 20, 18)
            : new Thickness(24, 24, 24, 18);

        SidebarToggleButton.ToolTip = _isSidebarCollapsed ? "Abrir menu" : "Fechar menu";

        var sidebarTextVisibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        foreach (var textBlock in FindVisualChildren<TextBlock>(SidebarBorder))
            textBlock.Visibility = sidebarTextVisibility;

        NavServicosBadge.Visibility = sidebarTextVisibility;
        NavServicosButton.Visibility = sidebarTextVisibility;

        var buttonAlignment = _isSidebarCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Left;
        var buttonPadding = _isSidebarCollapsed ? new Thickness(0) : new Thickness(13, 0, 13, 0);
        NavClientesButton.HorizontalContentAlignment = buttonAlignment;
        NavClientesButton.Padding = buttonPadding;
        NavAgendaButton.HorizontalContentAlignment = buttonAlignment;
        NavAgendaButton.Padding = buttonPadding;
        NavPesquisaButton.HorizontalContentAlignment = buttonAlignment;
        NavPesquisaButton.Padding = buttonPadding;
        NavProfissionaisButton.HorizontalContentAlignment = buttonAlignment;
        NavProfissionaisButton.Padding = buttonPadding;
        NavLaudosButton.HorizontalContentAlignment = buttonAlignment;
        NavLaudosButton.Padding = buttonPadding;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent)
        where T : DependencyObject
    {
        if (parent == null)
            yield break;

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
                yield return typedChild;

            foreach (var descendant in FindVisualChildren<T>(child))
                yield return descendant;
        }
    }

    private void SetActiveNav(Button? active)
    {
        NavClientesButton.Tag = null;
        NavAgendaButton.Tag = null;
        NavPesquisaButton.Tag = null;
        NavProfissionaisButton.Tag = null;
        NavLaudosButton.Tag = null;
        if (active != null)
            active.Tag = "Active";
    }

    private void MenuClientesButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 0;
        SetActiveNav(NavClientesButton);
    }

    private void MenuAgendaButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 1;
        SetActiveNav(NavAgendaButton);
    }

    private void MenuPesquisaButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 2;
        SetActiveNav(NavPesquisaButton);
    }

    private void MenuProfissionaisButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 3;
        SetActiveNav(NavProfissionaisButton);
    }

    private void MenuLaudosButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 4;
        SetActiveNav(NavLaudosButton);
    }

    private void MenuConfiguracoesButton_Click(object sender, RoutedEventArgs e)
    {
        // Configuracoes agora e acessada pelo menu do perfil; nenhum item da lateral fica ativo.
        MainTabControl.SelectedIndex = 5;
        SetActiveNav(null);
    }

    private void ConfigureTopBarProfile()
    {
        var usuario = string.IsNullOrWhiteSpace(SessionState.UsuarioNome) ? "Usuário" : SessionState.UsuarioNome.Trim();
        var empresa = string.IsNullOrWhiteSpace(SessionState.EmpresaNome) ? "SparkCore" : SessionState.EmpresaNome.Trim();

        ProfileNameTextBlock.Text = $"{usuario} - {empresa}";
        ProfilePopupNameTextBlock.Text = usuario;
        ProfilePopupCompanyTextBlock.Text = empresa;
        ProfileInitialsTextBlock.Text = GetInitials(usuario);
        ProfilePopupInitialsTextBlock.Text = GetInitials(usuario);

        ProfileMenuPopup.CustomPopupPlacementCallback = PlaceBelowRightAligned;
        NotificationsPopup.CustomPopupPlacementCallback = PlaceBelowRightAligned;
    }

    private static string GetInitials(string nome)
    {
        var partes = nome.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length == 0)
            return "U";

        return partes.Length == 1
            ? char.ToUpperInvariant(partes[0][0]).ToString()
            : string.Concat(char.ToUpperInvariant(partes[0][0]), char.ToUpperInvariant(partes[^1][0]));
    }

    private static CustomPopupPlacement[] PlaceBelowRightAligned(Size popupSize, Size targetSize, Point offset)
    {
        // Margem de 12 no card compensa a sombra; alinha a borda direita do popup com a do alvo.
        return new[]
        {
            new CustomPopupPlacement(new Point(targetSize.Width - popupSize.Width + 12, targetSize.Height - 4), PopupPrimaryAxis.Horizontal)
        };
    }

    private void NotificationsButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationsPopup.IsOpen = !NotificationsPopup.IsOpen;
    }

    private void ProfileMenuConfiguracoes_Click(object sender, RoutedEventArgs e)
    {
        ProfileMenuToggle.IsChecked = false;
        MenuConfiguracoesButton_Click(sender, e);
    }

    private void ProfileMenuSair_Click(object sender, RoutedEventArgs e)
    {
        ProfileMenuToggle.IsChecked = false;
        LogoutButton_Click(sender, e);
    }

    private void ConfiguracoesView_LogoutRequested(object? sender, EventArgs e)
    {
        LogoutButton_Click(sender ?? this, new RoutedEventArgs());
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        SessionState.Clear();
        Close();
    }
}
