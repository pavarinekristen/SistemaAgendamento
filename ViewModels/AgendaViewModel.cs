using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using AgendamentoWpfApp.Models;
using AgendamentoWpfApp.Services.Validation;

namespace AgendamentoWpfApp.ViewModels;

internal sealed class AgendaViewModel : ViewModelBase
{
    private readonly Action _dateChanged;
    private readonly Action _selectionChanged;
    private bool _suppressDateChanged;
    private IEnumerable? _clientesSource;
    private IEnumerable? _profissionaisSource;
    private IEnumerable? _consultasSource;
    private Consulta? _selectedConsulta;
    private DateTime? _selectedDate;
    private DateTime? _formDate;
    private string _headerDateText = "";
    private string _countText = "";
    private string _selectedClienteId = "";
    private string _horario = "";
    private string _local = "";
    private string _selectedProfissionalId = "";
    private string _status = "Agendada";
    private string _observacoes = "";
    private string _statusMessage = "";
    private Brush _statusBrush = SparkPalette.Success;

    public AgendaViewModel(
        Action dateChanged,
        Action selectionChanged,
        Action saveRequested,
        Action newRequested,
        Action deleteRequested)
    {
        _dateChanged = dateChanged;
        _selectionChanged = selectionChanged;
        SaveCommand = new RelayCommand(saveRequested);
        NewCommand = new RelayCommand(newRequested);
        DeleteCommand = new RelayCommand(deleteRequested);
    }

    public IReadOnlyList<string> StatusOptions { get; } = new[] { "Agendada", "Aguardando", "Concluida", "Cancelada" };

    public IEnumerable? ClientesSource
    {
        get => _clientesSource;
        set => SetProperty(ref _clientesSource, value);
    }

    public IEnumerable? ProfissionaisSource
    {
        get => _profissionaisSource;
        set => SetProperty(ref _profissionaisSource, value);
    }

    public IEnumerable? ConsultasSource
    {
        get => _consultasSource;
        set => SetProperty(ref _consultasSource, value);
    }

    public Consulta? SelectedConsulta
    {
        get => _selectedConsulta;
        set
        {
            if (SetProperty(ref _selectedConsulta, value))
                _selectionChanged();
        }
    }

    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            var date = value?.Date;
            if (!SetProperty(ref _selectedDate, date))
                return;

            FormDate = date ?? DateTime.Today;
            if (!_suppressDateChanged)
                _dateChanged();
        }
    }

    public DateTime? FormDate
    {
        get => _formDate;
        set => SetProperty(ref _formDate, value?.Date);
    }

    public string HeaderDateText
    {
        get => _headerDateText;
        private set => SetProperty(ref _headerDateText, value);
    }

    public string CountText
    {
        get => _countText;
        private set => SetProperty(ref _countText, value);
    }

    public string SelectedClienteId
    {
        get => _selectedClienteId;
        set => SetProperty(ref _selectedClienteId, value ?? "");
    }

    public string Horario
    {
        get => _horario;
        set => SetProperty(ref _horario, value);
    }

    public string Local
    {
        get => _local;
        set => SetProperty(ref _local, value);
    }

    public string SelectedProfissionalId
    {
        get => _selectedProfissionalId;
        set => SetProperty(ref _selectedProfissionalId, value ?? "");
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, string.IsNullOrWhiteSpace(value) ? "Agendada" : value);
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

    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand DeleteCommand { get; }

    public void RefreshClientes()
    {
        OnPropertyChanged(nameof(ClientesSource));
    }

    public void RefreshProfissionais()
    {
        OnPropertyChanged(nameof(ProfissionaisSource));
    }

    public void SetSelectedDate(DateTime date)
    {
        _suppressDateChanged = true;
        try
        {
            SelectedDate = date.Date;
            FormDate = date.Date;
        }
        finally
        {
            _suppressDateChanged = false;
        }
    }

    public void SetHeader(DateTime date, int count)
    {
        HeaderDateText = $"Dia {date:dd/MM/yyyy}";
        CountText = count == 0
            ? "Nenhuma consulta marcada para este dia."
            : $"{count} consulta(s) marcada(s) para este dia.";
    }

    public bool TryBuildFromForm(Consulta? current, out Consulta consulta, out string errorMessage)
    {
        consulta = current ?? new Consulta();
        errorMessage = "";

        var cliente = FindCliente(SelectedClienteId);
        if (cliente == null)
        {
            errorMessage = "Selecione um cliente cadastrado.";
            return false;
        }

        if (FormDate == null)
        {
            errorMessage = "Informe o dia da consulta.";
            return false;
        }

        if (!InputNormalizer.TryNormalizeHorario(Horario, out var horario))
        {
            errorMessage = "Informe um horario valido, como 14:00.";
            return false;
        }

        consulta.ClienteIdLocal = cliente.IdLocal;
        consulta.ClienteNome = cliente.Nome;
        consulta.DataConsulta = FormDate.Value.Date;
        consulta.Horario = horario;
        consulta.Local = Local.Trim();

        var profissionalSala = FindProfissionalSala(SelectedProfissionalId);
        if (profissionalSala == null)
        {
            consulta.ProfissionalSalaIdLocal = "";
            consulta.ProfissionalSala = "";
        }
        else
        {
            consulta.ProfissionalSalaIdLocal = profissionalSala.IdLocal;
            consulta.ProfissionalSala = profissionalSala.Nome;
        }

        consulta.Status = Status;
        consulta.Observacoes = Observacoes.Trim();
        return true;
    }

    public void LoadForm(Consulta? consulta)
    {
        if (consulta == null)
        {
            ClearForm(SelectedDate ?? DateTime.Today);
            return;
        }

        SelectedClienteId = consulta.ClienteIdLocal;
        FormDate = consulta.DataConsulta;
        Horario = consulta.Horario;
        Local = consulta.Local;
        SelectedProfissionalId = consulta.ProfissionalSalaIdLocal;
        Status = string.IsNullOrWhiteSpace(consulta.Status) ? "Agendada" : consulta.Status;
        Observacoes = consulta.Observacoes;
        SetStatus("", false);
    }

    public void ClearForm(DateTime date)
    {
        SelectedClienteId = "";
        FormDate = date.Date;
        Horario = "";
        Local = "";
        SelectedProfissionalId = "";
        Status = "Agendada";
        Observacoes = "";
        SetStatus("", false);
    }

    public void NormalizeHorario()
    {
        if (InputNormalizer.TryNormalizeHorario(Horario, out var horario))
            Horario = horario;
    }

    public void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        StatusBrush = isError ? SparkPalette.Error : SparkPalette.Success;
    }

    private Cliente? FindCliente(string idLocal)
    {
        return (_clientesSource ?? Array.Empty<object>())
            .OfType<Cliente>()
            .FirstOrDefault(c => c.IdLocal == idLocal);
    }

    private ProfissionalSala? FindProfissionalSala(string idLocal)
    {
        return (_profissionaisSource ?? Array.Empty<object>())
            .OfType<ProfissionalSala>()
            .FirstOrDefault(p => p.IdLocal == idLocal);
    }
}
