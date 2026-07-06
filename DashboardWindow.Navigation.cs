using System;
using System.Windows;
using System.Windows.Controls;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private void SetActiveNav(Button active)
    {
        NavClientesButton.Tag = null;
        NavAgendaButton.Tag = null;
        NavProfissionaisButton.Tag = null;
        NavLaudosButton.Tag = null;
        NavConfiguracoesButton.Tag = null;
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

    private void MenuProfissionaisButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 2;
        SetActiveNav(NavProfissionaisButton);
    }

    private void MenuLaudosButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 3;
        SetActiveNav(NavLaudosButton);
    }

    private void MenuConfiguracoesButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 4;
        SetActiveNav(NavConfiguracoesButton);
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
