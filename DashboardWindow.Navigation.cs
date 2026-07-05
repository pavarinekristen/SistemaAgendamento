using System;
using System.Windows;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private void MenuClientesButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 0;
    }

    private void MenuAgendaButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 1;
    }

    private void MenuSyncButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 4;
    }

    private void MenuProfissionaisButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 2;
    }

    private void MenuLaudosButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 3;
    }

    private void MenuConfiguracoesButton_Click(object sender, RoutedEventArgs e)
    {
        MainTabControl.SelectedIndex = 4;
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
