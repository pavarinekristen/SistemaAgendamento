using System;
using System.Linq;
using System.Windows;
using AgendamentoWpfApp.Models;

namespace AgendamentoWpfApp;

public partial class DashboardWindow
{
    private void ProfissionaisView_SelectionChanged(object? sender, EventArgs e)
    {
        _selectedProfissionalSala = ProfissionaisView.SelectedProfissionalSala;
        ProfissionaisView.LoadForm(_selectedProfissionalSala);
    }

    private async void ProfissionaisView_SaveRequested(object? sender, EventArgs e)
    {
        var item = ProfissionaisView.BuildFromForm(_selectedProfissionalSala);
        try
        {
            await _profissionalWorkflow.SaveAsync(item);
        }
        catch (InvalidOperationException ex)
        {
            SetProfissionalStatus(ex.Message, true);
            return;
        }

        await LoadProfissionaisAsync();
        ProfissionaisView.SelectItem(_profissionaisSalas.FirstOrDefault(p => p.IdLocal == item.IdLocal));
        SetProfissionalStatus("Cadastro salvo e disponivel na agenda.", false);
    }

    private void ProfissionaisView_NewRequested(object? sender, EventArgs e)
    {
        ClearProfissionalForm();
        ProfissionaisView.SelectItem(null);
    }

    private async void ProfissionaisView_DeleteRequested(object? sender, EventArgs e)
    {
        if (_selectedProfissionalSala == null)
        {
            SetProfissionalStatus("Selecione um cadastro para excluir.", true);
            return;
        }

        var result = MessageBox.Show(
            $"Excluir {_selectedProfissionalSala.Nome}?",
            "Excluir profissional/sala",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        await _profissionalWorkflow.DeleteAsync(_selectedProfissionalSala);
        await LoadProfissionaisAsync();
        ClearProfissionalForm();
        SetProfissionalStatus("Cadastro excluido.", false);
    }

    private void LoadProfissionalForm(ProfissionalSala? item)
    {
        ProfissionaisView.LoadForm(item);
    }

    private void ClearProfissionalForm()
    {
        _selectedProfissionalSala = null;
        ProfissionaisView.ClearForm();
    }
}
