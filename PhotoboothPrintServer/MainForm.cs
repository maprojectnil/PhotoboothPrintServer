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

    private readonly PrintQueueService _printQueue = new();
    private readonly PrintManager _printManager;
    private readonly WebServerService _webServer;

    private AppSettings _settings = new();
    private List<PrinterInfo> _printers = new();

    public MainForm()
    {
        InitializeComponent();

        _printManager = new PrintManager(_printQueue, _settingsService);
        _webServer = new WebServerService(_printQueue, _settingsService);

        Load += MainForm_Load;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        _settings = _settingsService.Load();

        lblIpValue.Text = NetworkUtils.GetLocalIPv4Address();
        lblPortValue.Text = _settings.ApiPort.ToString();
        lblApiUrlValue.Text = $"http://{lblIpValue.Text}:{_settings.ApiPort}";
        lblServerStatusValue.Text = "Starting...";
        lblServerStatusValue.ForeColor = Color.Gray;

        _printQueue.LogMessage += AppendLog;
        _printManager.LogMessage += AppendLog;
        _printManager.StateChanged += PrintManager_StateChanged;

        _printManager.Start();

        RefreshPrinterList();
        RefreshQueueUi();

        await StartServerAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try
        {
            _printManager.Stop();
            _webServer.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Diabaikan saat aplikasi ditutup.
        }

        base.OnFormClosing(e);
    }

    // ===================== Server (HTTP API) =====================

    private async Task StartServerAsync()
    {
        btnToggleServer.Enabled = false;
        AppendLog($"Menjalankan HTTP API di port {_settings.ApiPort}...");

        bool ok = await _webServer.StartAsync(_settings.ApiPort);

        if (ok)
        {
            lblServerStatusValue.Text = "Running";
            lblServerStatusValue.ForeColor = Color.DarkGreen;
            btnToggleServer.Text = "Stop Server";
            AppendLog($"HTTP API berjalan di {lblApiUrlValue.Text}");
        }
        else
        {
            lblServerStatusValue.Text = "Failed to start";
            lblServerStatusValue.ForeColor = Color.Red;
            btnToggleServer.Text = "Start Server";
            AppendLog($"Gagal menjalankan HTTP API: {_webServer.LastError}");
        }

        btnToggleServer.Enabled = true;
    }

    private async Task StopServerAsync()
    {
        btnToggleServer.Enabled = false;
        AppendLog("Menghentikan HTTP API...");

        await _webServer.StopAsync();

        lblServerStatusValue.Text = "Stopped";
        lblServerStatusValue.ForeColor = Color.Gray;
        btnToggleServer.Text = "Start Server";
        AppendLog("HTTP API dihentikan.");

        btnToggleServer.Enabled = true;
    }

    private async void btnToggleServer_Click(object? sender, EventArgs e)
    {
        if (_webServer.IsRunning)
            await StopServerAsync();
        else
            await StartServerAsync();
    }

    // ===================== Printer (Fase 1) =====================

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

    // ===================== Print Queue (Fase 2) =====================

    private void PrintManager_StateChanged()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(RefreshQueueUi));
        }
        else
        {
            RefreshQueueUi();
        }
    }

    private void RefreshQueueUi()
    {
        lblQueueLengthValue.Text = _printQueue.PendingCount.ToString();
        lblCurrentJobValue.Text = _printManager.CurrentJob != null
            ? $"{_printManager.CurrentJob.JobId} ({_printManager.CurrentJob.FileName})"
            : "-";
        lblTotalPrintedValue.Text = _printManager.TotalPrinted.ToString();
        lblTotalFailedValue.Text = _printManager.TotalFailed.ToString();

        lvQueue.BeginUpdate();
        lvQueue.Items.Clear();

        foreach (var job in _printQueue.GetAllJobs())
        {
            var item = new ListViewItem(job.JobId);
            item.SubItems.Add(job.FileName);
            item.SubItems.Add(job.Copies.ToString());
            item.SubItems.Add(job.Status.ToString());
            item.SubItems.Add(job.ErrorMessage ?? string.Empty);
            lvQueue.Items.Add(item);
        }

        lvQueue.EndUpdate();
    }

    // ===================== Logging =====================

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

        // Update queue UI juga saat ada log terkait job (mis. job baru diterima).
        if (InvokeRequired)
        {
            Invoke(new Action(RefreshQueueUi));
        }
        else
        {
            RefreshQueueUi();
        }
    }

    private void AppendLogInternal(string line)
    {
        txtLog.AppendText(line + Environment.NewLine);
    }
}