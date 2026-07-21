using PhotoboothPrintServer.Configuration;
using PhotoboothPrintServer.Models;
using PhotoboothPrintServer.Services;
using PhotoboothPrintServer.Utilities;

namespace PhotoboothPrintServer;

public partial class MainForm : Form
{
    private readonly PrinterService _printerService = new();
    private readonly TestPrintService _testPrintService = new();
    private readonly AppSettingsService _settingsService = new();

    private AppSettings _settings = new();
    private List<PrinterInfo> _printers = new();

    public MainForm()
    {
        InitializeComponent();
        Load += MainForm_Load;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        _settings = _settingsService.Load();

        lblIpValue.Text = NetworkUtils.GetLocalIPv4Address();
        lblPortValue.Text = _settings.ApiPort.ToString();
        lblServerStatusValue.Text = "Not Started (Fase 2)";
        lblServerStatusValue.ForeColor = Color.Gray;

        RefreshPrinterList();
    }

    private void btnRefresh_Click(object? sender, EventArgs e)
    {
        RefreshPrinterList();
    }

    private void RefreshPrinterList()
    {
        AppendLog("Scanning for installed printers...");
        btnRefresh.Enabled = false;

        try
        {
            _printers = _printerService.GetInstalledPrinters();

            cmbPrinters.Items.Clear();
            foreach (var printer in _printers)
            {
                string label = printer.Name;
                if (printer.IsVirtual) label += "  (virtual)";
                if (printer.IsDefault) label += "  [default]";
                cmbPrinters.Items.Add(label);
            }

            if (cmbPrinters.Items.Count == 0)
            {
                AppendLog("Tidak ada printer terdeteksi di sistem ini.");
                UpdatePrinterDetails(null);
                return;
            }

            int indexToSelect = 0;
            if (!string.IsNullOrEmpty(_settings.SelectedPrinter))
            {
                int savedIndex = _printers.FindIndex(p => p.Name == _settings.SelectedPrinter);
                if (savedIndex >= 0) indexToSelect = savedIndex;
            }
            else
            {
                int defaultIndex = _printers.FindIndex(p => p.IsDefault);
                if (defaultIndex >= 0) indexToSelect = defaultIndex;
            }

            cmbPrinters.SelectedIndex = indexToSelect;
            AppendLog($"Ditemukan {_printers.Count} printer.");
        }
        catch (Exception ex)
        {
            AppendLog($"Gagal mendeteksi printer: {ex.Message}");
        }
        finally
        {
            btnRefresh.Enabled = true;
        }
    }

    private void cmbPrinters_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (cmbPrinters.SelectedIndex < 0 || cmbPrinters.SelectedIndex >= _printers.Count)
        {
            UpdatePrinterDetails(null);
            return;
        }

        var selected = _printers[cmbPrinters.SelectedIndex];
        UpdatePrinterDetails(selected);

        _settings.SelectedPrinter = selected.Name;
        _settingsService.Save(_settings);
    }

    private void UpdatePrinterDetails(PrinterInfo? printer)
    {
        if (printer == null)
        {
            lblPrinterStatusValue.Text = "No printer selected";
            lblPrinterStatusValue.ForeColor = Color.Gray;
            btnTestPrint.Enabled = false;
            return;
        }

        string kind = printer.IsVirtual ? "Virtual" : "Physical";
        lblPrinterStatusValue.Text = $"{printer.StatusText}  |  {kind}  |  {(printer.IsOnline ? "Online" : "Offline")}";
        lblPrinterStatusValue.ForeColor = printer.IsReady ? Color.DarkGreen : Color.DarkOrange;

        btnTestPrint.Enabled = true;
    }

    private async void btnTestPrint_Click(object? sender, EventArgs e)
    {
        if (cmbPrinters.SelectedIndex < 0 || cmbPrinters.SelectedIndex >= _printers.Count)
        {
            AppendLog("Pilih printer terlebih dahulu.");
            return;
        }

        var selected = _printers[cmbPrinters.SelectedIndex];

        btnTestPrint.Enabled = false;
        AppendLog($"Mengirim test print ke '{selected.Name}'...");

        var result = await Task.Run(() => _testPrintService.PrintTestPage(selected.Name));

        AppendLog(result.Message);
        btnTestPrint.Enabled = true;
    }

    private void AppendLog(string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(new Action(() => AppendLogInternal(line)));
        }
        else
        {
            AppendLogInternal(line);
        }
    }

    private void AppendLogInternal(string line)
    {
        txtLog.AppendText(line + Environment.NewLine);
    }
}