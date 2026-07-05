using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.ViewModels;

internal sealed class ProfissionaisViewModel : ViewModelBase
{
    private readonly Action _selectionChanged;
    private IEnumerable? _items;
    private ProfissionalSala? _selectedItem;
    private string _countText = "";
    private string _nome = "";
    private string _tipo = "Profissional";
    private string _especialidadeFuncao = "";
    private string _telefone = "";
    private string _email = "";
    private string _observacoes = "";
    private bool _ativo = true;
    private string _statusMessage = "";
    private Brush _statusBrush = SparkPalette.Success;

    public ProfissionaisViewModel(
        Action selectionChanged,
        Action saveRequested,
        Action newRequested,
        Action deleteRequested)
    {
        _selectionChanged = selectionChanged;
        SaveCommand = new RelayCommand(saveRequested);
        NewCommand = new RelayCommand(newRequested);
        DeleteCommand = new RelayCommand(deleteRequested);
    }

    public IReadOnlyList<string> Tipos { get; } = new[] { "Profissional", "Sala" };

    public IEnumerable? Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public ProfissionalSala? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
                _selectionChanged();
        }
    }

    public string CountText
    {
        get => _countText;
        private set => SetProperty(ref _countText, value);
    }

    public string Nome
    {
        get => _nome;
        set => SetProperty(ref _nome, value);
    }

    public string Tipo
    {
        get => _tipo;
        set => SetProperty(ref _tipo, string.IsNullOrWhiteSpace(value) ? "Profissional" : value);
    }

    public string EspecialidadeFuncao
    {
        get => _especialidadeFuncao;
        set => SetProperty(ref _especialidadeFuncao, value);
    }

    public string Telefone
    {
        get => _telefone;
        set => SetProperty(ref _telefone, value);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Observacoes
    {
        get => _observacoes;
        set => SetProperty(ref _observacoes, value);
    }

    public bool Ativo
    {
        get => _ativo;
        set => SetProperty(ref _ativo, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public Brush StatusBrush
    {
        get => _statusBrush;
        private set => SetProperty(ref _statusBrush, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public ProfissionalSala BuildFromForm(ProfissionalSala? current)
    {
        var item = current ?? new ProfissionalSala();
        item.Nome = Nome.Trim();
        item.Tipo = Tipo;
        item.EspecialidadeFuncao = EspecialidadeFuncao.Trim();
        item.Telefone = InputNormalizer.NormalizeTelefone(Telefone);
        item.Email = InputNormalizer.NormalizeEmail(Email);
        item.Observacoes = Observacoes.Trim();
        item.Ativo = Ativo;
        return item;
    }

    public void LoadForm(ProfissionalSala? item)
    {
        if (item == null)
        {
            ClearForm();
            return;
        }

        Nome = item.Nome;
        Tipo = string.IsNullOrWhiteSpace(item.Tipo) ? "Profissional" : item.Tipo;
        EspecialidadeFuncao = item.EspecialidadeFuncao;
        Telefone = InputNormalizer.FormatTelefone(item.Telefone);
        Email = item.Email;
        Observacoes = item.Observacoes;
        Ativo = item.Ativo;
        SetStatus("", false);
    }

    public void ClearForm()
    {
        Nome = "";
        Tipo = "Profissional";
        EspecialidadeFuncao = "";
        Telefone = "";
        Email = "";
        Observacoes = "";
        Ativo = true;
        SetStatus("", false);
    }

    public void SetCount(int count)
    {
        CountText = $"{count} profissional(is)/sala(s) cadastrado(s)";
    }

    public void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusBrush = isError ? SparkPalette.Error : SparkPalette.Success;
    }
}
