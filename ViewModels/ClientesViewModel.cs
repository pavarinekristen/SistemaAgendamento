using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.ViewModels;

internal sealed class ClientesViewModel : ViewModelBase
{
    private readonly Action _searchChanged;
    private readonly Action _selectionChanged;
    private IEnumerable? _items;
    private Cliente? _selectedItem;
    private string _countText = "";
    private string _searchTerm = "";
    private string _nome = "";
    private string _cpf = "";
    private string _email = "";
    private string _telefone = "";
    private DateTime? _dataNascimento;
    private string _endereco = "";
    private string _bairro = "";
    private string _cidade = "";
    private string _status = "Ativo";
    private string _observacoes = "";
    private string _statusMessage = "";
    private Brush _statusBrush = SparkPalette.Success;
    private string _detailName = "Nenhum cliente selecionado";
    private string _detailContact = "";
    private string _detailAddress = "";
    private string _detailNotes = "";

    public ClientesViewModel(
        Action searchChanged,
        Action selectionChanged,
        Action saveRequested,
        Action newRequested,
        Action deleteRequested)
    {
        _searchChanged = searchChanged;
        _selectionChanged = selectionChanged;
        SaveCommand = new RelayCommand(saveRequested);
        NewCommand = new RelayCommand(newRequested);
        DeleteCommand = new RelayCommand(deleteRequested);
    }

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "Ativo", "Inativo" };

    public IEnumerable? Items
    {
        get => _items;
        set => SetProperty(ref _items, value);
    }

    public Cliente? SelectedItem
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

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (SetProperty(ref _searchTerm, value))
                _searchChanged();
        }
    }

    public string Nome
    {
        get => _nome;
        set => SetProperty(ref _nome, value);
    }

    public string Cpf
    {
        get => _cpf;
        set => SetProperty(ref _cpf, InputNormalizer.FormatCpf(value));
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Telefone
    {
        get => _telefone;
        set => SetProperty(ref _telefone, InputNormalizer.FormatTelefone(value));
    }

    public DateTime? DataNascimento
    {
        get => _dataNascimento;
        set => SetProperty(ref _dataNascimento, value);
    }

    public string Endereco
    {
        get => _endereco;
        set => SetProperty(ref _endereco, value);
    }

    public string Bairro
    {
        get => _bairro;
        set => SetProperty(ref _bairro, value);
    }

    public string Cidade
    {
        get => _cidade;
        set => SetProperty(ref _cidade, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, string.IsNullOrWhiteSpace(value) ? "Ativo" : value);
    }

    public string Observacoes
    {
        get => _observacoes;
        set => SetProperty(ref _observacoes, value);
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

    public string DetailName
    {
        get => _detailName;
        private set => SetProperty(ref _detailName, value);
    }

    public string DetailContact
    {
        get => _detailContact;
        private set => SetProperty(ref _detailContact, value);
    }

    public string DetailAddress
    {
        get => _detailAddress;
        private set => SetProperty(ref _detailAddress, value);
    }

    public string DetailNotes
    {
        get => _detailNotes;
        private set => SetProperty(ref _detailNotes, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public Cliente BuildFromForm(Cliente? current)
    {
        var cliente = current ?? new Cliente();
        cliente.Nome = Nome.Trim();
        cliente.Cpf = InputNormalizer.NormalizeCpf(Cpf);
        cliente.Email = InputNormalizer.NormalizeEmail(Email);
        cliente.Telefone = InputNormalizer.NormalizeTelefone(Telefone);
        cliente.DataNascimento = DataNascimento;
        cliente.Endereco = Endereco.Trim();
        cliente.Bairro = Bairro.Trim();
        cliente.Cidade = Cidade.Trim();
        cliente.LocalAtendimento = "";
        cliente.DataAgendamento = null;
        cliente.HorarioAgendamento = "";
        cliente.Status = Status;
        cliente.Observacoes = Observacoes.Trim();
        cliente.AtualizadoEm = DateTime.Now;
        return cliente;
    }

    public void LoadForm(Cliente? cliente)
    {
        if (cliente == null)
        {
            ClearForm();
            return;
        }

        Nome = cliente.Nome;
        Cpf = cliente.Cpf;
        Email = cliente.Email;
        Telefone = cliente.Telefone;
        DataNascimento = cliente.DataNascimento;
        Endereco = cliente.Endereco;
        Bairro = cliente.Bairro;
        Cidade = cliente.Cidade;
        Status = string.IsNullOrWhiteSpace(cliente.Status) ? "Ativo" : cliente.Status;
        Observacoes = cliente.Observacoes;
        SetStatus("", false);
    }

    public void ClearForm()
    {
        Nome = "";
        Cpf = "";
        Email = "";
        Telefone = "";
        DataNascimento = null;
        Endereco = "";
        Bairro = "";
        Cidade = "";
        Status = "Ativo";
        Observacoes = "";
        SetStatus("", false);
    }

    public void SetCount(int total, int visible, bool hasSearch)
    {
        CountText = hasSearch
            ? $"{visible} de {total} cliente(s) encontrado(s)"
            : $"{total} cliente(s) cadastrado(s)";
    }

    public void SetDetails(Cliente? cliente)
    {
        if (cliente == null)
        {
            DetailName = "Nenhum cliente selecionado";
            DetailContact = "";
            DetailAddress = "";
            DetailNotes = "";
            return;
        }

        DetailName = cliente.Nome;
        DetailContact = $"CPF: {ValueOrDash(cliente.CpfFormatado)} | Tel: {ValueOrDash(cliente.TelefoneFormatado)} | E-mail: {ValueOrDash(cliente.Email)}";
        DetailAddress = $"Endereco: {ValueOrDash(cliente.Endereco)} {ValueOrDash(cliente.Bairro)} {ValueOrDash(cliente.Cidade)}".Trim();
        DetailNotes = $"Status: {cliente.Status}. {cliente.Observacoes}";
    }

    public void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusBrush = isError ? SparkPalette.Error : SparkPalette.Success;
    }

    private static string ValueOrDash(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }
}
