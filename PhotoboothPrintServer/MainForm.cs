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
    private readonly PrinterProfileStore _profileStore = new();
    private readonly PrintHistoryStore _historyStore = new();

    private readonly PrintQueueService _printQueue = new();
    private readonly PrintManager _printManager;
    private readonly WebServerService _webServer;
    private readonly MdnsService _mdnsService = new();
    private readonly WebSocketBroadcastService _wsBroadcast = new();
    private readonly PrinterWatcherService _printerWatcher;
    private readonly NetworkWatcherService _networkWatcher = new();

    private AppSettings _settings = new();
    private List<PrinterInfo> _printers = new();
    private PrinterCapabilities _currentCapabilities = new();
    private bool _suppressProfileEvents;
    private bool _isExiting;
    private Color? _lastTrayColor;

    public MainForm()
    {
        InitializeComponent();

        _printManager = new PrintManager(_printQueue, _settingsService, _profileStore, _historyStore, _printerService);
        _webServer = new WebServerService(_printQueue, _settingsService, _wsBroadcast);
        _printerWatcher = new PrinterWatcherService(_printerService, _settingsService);

        Load += MainForm_Load;
        Resize += MainForm_Resize;
    }

    private async void MainForm_Load(object? sender, EventArgs e)
    {
        _settings = _settingsService.Load();

        lblIpValue.Text = NetworkUtils.GetLocalIPv4Address();
        lblPortValue.Text = _settings.ApiPort.ToString();
        lblApiUrlValue.Text = $"http://{lblIpValue.Text}:{_settings.ApiPort}";
        lblServerStatusValue.Text = "Starting...";
        lblServerStatusValue.ForeColor = Color.Gray;
        lblMdnsValue.Text = "-";
        lblMdnsValue.ForeColor = Color.Gray;

        _printQueue.LogMessage += AppendLog;
        _printManager.LogMessage += AppendLog;
        _printManager.StateChanged += PrintManager_StateChanged;
        _mdnsService.LogMessage += AppendLog;
        _wsBroadcast.LogMessage += AppendLog;
        _printQueue.JobStatusChanged += job => _ = _wsBroadcast.BroadcastJobStatusAsync(job);
        _historyStore.EntryAdded += HistoryStore_EntryAdded;
        _historyStore.HistoryCleared += HistoryStore_HistoryCleared;
        _printerWatcher.StatusPolled += PrinterWatcher_StatusPolled;
        _printerWatcher.StatusChanged += PrinterWatcher_StatusChanged;
        _networkWatcher.NetworkChanged += NetworkWatcher_NetworkChanged;

        _printManager.Start();
        _printerWatcher.Start();
        _networkWatcher.Start();

        RefreshPrinterList();
        RefreshQueueUi();
        LoadHistoryUi();
        UpdateTrayStatus();

        await StartServerAsync();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Klik tombol X (atau Alt+F4) hanya menyembunyikan ke System Tray - Print Server,
        // HTTP API, dan Print Queue tetap berjalan di background. Shutdown sungguhan hanya
        // terjadi lewat menu "Exit" di tray (yang set _isExiting = true sebelum Close()),
        // atau alasan lain di luar kendali user (Windows shutdown/log off, Task Manager, dst).
        if (!_isExiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            trayIcon.ShowBalloonTip(
                1500,
                "Photobooth Print Server",
                "Aplikasi tetap berjalan di background. Klik kanan ikon tray untuk Exit.",
                ToolTipIcon.Info);
            return;
        }

        try
        {
            _printManager.Stop();
            _printerWatcher.Stop();
            _networkWatcher.Stop();
            _mdnsService.Stop();
            _webServer.StopAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Diabaikan saat aplikasi ditutup.
        }

        trayIcon.Visible = false;
        trayIcon.Dispose();

        base.OnFormClosing(e);
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        // Minimize juga disembunyikan ke tray (bukan cuma di-minimize di taskbar),
        // supaya konsisten dengan perilaku "tetap berjalan di background" saat X ditekan.
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
            trayIcon.ShowBalloonTip(
                1500,
                "Photobooth Print Server",
                "Aplikasi tetap berjalan di background.",
                ToolTipIcon.Info);
        }
    }

    // ===================== System Tray (Fase 3 - STEP 5) =====================

    private void ShowMainWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void trayIcon_DoubleClick(object? sender, EventArgs e) => ShowMainWindow();

    private void trayMenuOpen_Click(object? sender, EventArgs e) => ShowMainWindow();

    private async void trayMenuStartServer_Click(object? sender, EventArgs e)
    {
        if (!_webServer.IsRunning) await StartServerAsync();
    }

    private async void trayMenuStopServer_Click(object? sender, EventArgs e)
    {
        if (_webServer.IsRunning) await StopServerAsync();
    }

    private void trayMenuRefreshPrinter_Click(object? sender, EventArgs e) => RefreshPrinterList();

    private void trayMenuExit_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "Keluar dari Photobooth Print Server? HTTP API dan Print Queue akan berhenti.",
            "Konfirmasi Exit",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        _isExiting = true;
        Close();
    }

    /// <summary>
    /// Update tooltip dan warna ikon tray sesuai status server & printer saat ini.
    /// Dipanggil setiap ada perubahan status server, printer, atau antrean.
    /// </summary>
    private void UpdateTrayStatus()
    {
        bool serverRunning = _webServer.IsRunning;

        PrinterInfo? selectedPrinter =
            cmbPrinters.SelectedIndex >= 0 && cmbPrinters.SelectedIndex < _printers.Count
                ? _printers[cmbPrinters.SelectedIndex]
                : null;

        Color statusColor;
        string statusText;

        if (!serverRunning)
        {
            statusColor = Color.Gray;
            statusText = "Server: Stopped";
        }
        else if (selectedPrinter == null)
        {
            statusColor = Color.Orange;
            statusText = "Server: Running | Printer: (belum dipilih)";
        }
        else if (!selectedPrinter.IsReady)
        {
            statusColor = Color.Orange;
            statusText = "Server: Running | Printer: Offline";
        }
        else
        {
            statusColor = Color.Green;
            statusText = "Server: Running | Printer: Ready";
        }

        string tooltip = $"{statusText} | Queue: {_printQueue.PendingCount}";
        if (tooltip.Length > 63) tooltip = tooltip[..60] + "..."; // batas Windows untuk NotifyIcon.Text

        trayIcon.Text = tooltip;

        if (_lastTrayColor != statusColor)
        {
            trayIcon.Icon = TrayIconFactory.CreateStatusIcon(statusColor);
            _lastTrayColor = statusColor;
        }

        trayMenuStartServer.Enabled = !serverRunning;
        trayMenuStopServer.Enabled = serverRunning;
    }

    // ===================== Auto Reconnect & Auto Recovery (Fase 3 - STEP 6) =====================

    /// <summary>
    /// Dipanggil setiap siklus poll PrinterWatcherService (~5 detik sekali). Update label status
    /// printer + tray HANYA untuk printer yang sedang aktif dipilih, tanpa mengubah pilihan combo
    /// user maupun me-reset daftar printer (supaya tidak mengganggu interaksi user).
    /// </summary>
    private void PrinterWatcher_StatusPolled(PrinterInfo? info)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => PrinterWatcher_StatusPolled(info)));
            return;
        }

        if (info == null)
        {
            UpdateTrayStatus();
            return;
        }

        // Update entri di _printers secara in-place supaya index/selection combo tidak berubah.
        var existing = _printers.FirstOrDefault(p => p.Name == info.Name);
        if (existing != null)
        {
            existing.IsOnline = info.IsOnline;
            existing.IsReady = info.IsReady;
            existing.StatusText = info.StatusText;

            bool isCurrentlySelected =
                cmbPrinters.SelectedIndex >= 0 &&
                cmbPrinters.SelectedIndex < _printers.Count &&
                _printers[cmbPrinters.SelectedIndex].Name == info.Name;

            if (isCurrentlySelected)
            {
                string kind = existing.IsVirtual ? "Virtual" : "Physical";
                lblPrinterStatusValue.Text =
                    $"{existing.StatusText}  |  {kind}  |  {(existing.IsOnline ? "Online" : "Offline")}";
                lblPrinterStatusValue.ForeColor = existing.IsReady ? Color.DarkGreen : Color.DarkOrange;
            }
        }

        UpdateTrayStatus();
    }

    /// <summary>Dipicu hanya saat status online/offline printer benar-benar berubah - dicatat ke log.</summary>
    private void PrinterWatcher_StatusChanged(PrinterInfo info)
    {
        string message = info.IsOnline
            ? $"Printer '{info.Name}' tersambung kembali (online)."
            : $"Printer '{info.Name}' terputus / offline. Print Job baru akan menunggu di antrean.";

        AppendLog(message);
    }

    /// <summary>
    /// Dipicu saat IP lokal berubah (mis. Wi-Fi sempat putus lalu reconnect dengan IP baru dari DHCP).
    /// mDNS di-restart dengan info terbaru supaya Android tetap bisa auto-discover, dan label IP
    /// di UI diperbarui - semua tanpa perlu restart aplikasi maupun HTTP API.
    /// </summary>
    private void NetworkWatcher_NetworkChanged(string newIp)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => NetworkWatcher_NetworkChanged(newIp)));
            return;
        }

        AppendLog($"Alamat IP jaringan berubah menjadi {newIp}. Memperbarui mDNS...");

        lblIpValue.Text = newIp;
        lblApiUrlValue.Text = $"http://{newIp}:{_settings.ApiPort}";

        if (_webServer.IsRunning)
        {
            _mdnsService.Stop();
            _mdnsService.Start(_settings.ApiPort, $"PhotoboothPrintServer-{Environment.MachineName}");

            if (_mdnsService.IsRunning)
            {
                lblMdnsValue.Text = $"Advertising ({_mdnsService.InstanceName})";
                lblMdnsValue.ForeColor = Color.DarkGreen;
            }
            else
            {
                lblMdnsValue.Text = "Gagal - gunakan input IP manual";
                lblMdnsValue.ForeColor = Color.DarkOrange;
            }
        }

        UpdateTrayStatus();
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

            string instanceName = $"PhotoboothPrintServer-{Environment.MachineName}";
            _mdnsService.Start(_settings.ApiPort, instanceName);

            if (_mdnsService.IsRunning)
            {
                lblMdnsValue.Text = $"Advertising ({_mdnsService.InstanceName})";
                lblMdnsValue.ForeColor = Color.DarkGreen;
            }
            else
            {
                lblMdnsValue.Text = "Gagal - gunakan input IP manual";
                lblMdnsValue.ForeColor = Color.DarkOrange;
            }
        }
        else
        {
            lblServerStatusValue.Text = "Failed to start";
            lblServerStatusValue.ForeColor = Color.Red;
            btnToggleServer.Text = "Start Server";
            AppendLog($"Gagal menjalankan HTTP API: {_webServer.LastError}");
        }

        btnToggleServer.Enabled = true;
        UpdateTrayStatus();
    }

    private async Task StopServerAsync()
    {
        btnToggleServer.Enabled = false;
        AppendLog("Menghentikan HTTP API...");

        _mdnsService.Stop();
        lblMdnsValue.Text = "Stopped";
        lblMdnsValue.ForeColor = Color.Gray;

        await _webServer.StopAsync();

        lblServerStatusValue.Text = "Stopped";
        lblServerStatusValue.ForeColor = Color.Gray;
        btnToggleServer.Text = "Start Server";
        AppendLog("HTTP API dihentikan.");

        btnToggleServer.Enabled = true;
        UpdateTrayStatus();
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

            ClearProfileUi();
            UpdateTrayStatus();
            return;
        }

        string kind = printer.IsVirtual ? "Virtual" : "Physical";
        lblPrinterStatusValue.Text = $"{printer.StatusText}  |  {kind}  |  {(printer.IsOnline ? "Online" : "Offline")}";
        lblPrinterStatusValue.ForeColor = printer.IsReady ? Color.DarkGreen : Color.DarkOrange;

        btnTestPrint.Enabled = true;

        LoadPrinterProfileUi(printer.Name);
        UpdateTrayStatus();
    }

    // ===================== Printer Profile (Fase 3 - STEP 1) =====================

    private void ClearProfileUi()
    {
        _suppressProfileEvents = true;
        try
        {
            cmbPaperSize.Items.Clear();
            cmbPaperSize.Enabled = false;

            cmbPrintQuality.Items.Clear();
            cmbPrintQuality.Enabled = false;

            cmbColorMode.Items.Clear();
            cmbColorMode.Enabled = false;

            cmbOrientation.SelectedIndex = -1;
            cmbOrientation.Enabled = false;

            chkBorderless.Checked = false;
            chkBorderless.Enabled = false;

            cmbMediaType.Items.Clear();
            cmbMediaType.Enabled = false;

            lblProfileInfoValue.Text = "-";
        }
        finally
        {
            _suppressProfileEvents = false;
        }
    }

    /// <summary>
    /// Memuat kapabilitas asli printer (dari driver Windows) dan Printer Profile
    /// tersimpan untuk printer tersebut, lalu menampilkannya di panel Printer Profile.
    /// Dipanggil setiap kali printer aktif berganti - konfigurasi printer sebelumnya
    /// tidak hilang, hanya tidak sedang ditampilkan.
    /// </summary>
    private void LoadPrinterProfileUi(string printerName)
    {
        _suppressProfileEvents = true;
        try
        {
            _currentCapabilities = _printerService.GetCapabilities(printerName);
            var profile = _profileStore.GetOrCreate(printerName);
            profile.PrinterName = printerName;

            // --- Paper Size: hanya opsi yang benar-benar dilaporkan driver ---
            cmbPaperSize.Items.Clear();
            foreach (var size in _currentCapabilities.PaperSizes)
                cmbPaperSize.Items.Add(size);

            cmbPaperSize.Enabled = cmbPaperSize.Items.Count > 0;
            if (cmbPaperSize.Items.Count > 0)
            {
                int idx = string.IsNullOrEmpty(profile.PaperSizeName)
                    ? -1
                    : cmbPaperSize.Items.IndexOf(profile.PaperSizeName);
                cmbPaperSize.SelectedIndex = idx >= 0 ? idx : 0;
                profile.PaperSizeName = cmbPaperSize.SelectedItem?.ToString() ?? string.Empty;
            }

            // --- Print Quality: hanya level yang benar-benar tersedia di driver ---
            cmbPrintQuality.Items.Clear();
            var availableLevels = _currentCapabilities.QualityOptions
                .Select(q => q.Level)
                .Distinct()
                .ToList();

            if (availableLevels.Count == 0)
                availableLevels.Add(PrintQualityLevel.High); // fallback minimal, driver tidak melaporkan apa pun

            foreach (var level in availableLevels)
                cmbPrintQuality.Items.Add(level);

            cmbPrintQuality.Enabled = true;
            int qIdx = cmbPrintQuality.Items.IndexOf(profile.PrintQuality);
            cmbPrintQuality.SelectedIndex = qIdx >= 0 ? qIdx : 0;
            profile.PrintQuality = (PrintQualityLevel)cmbPrintQuality.SelectedItem!;

            // --- Color Mode: opsi Monochrome hanya muncul jika printer mendukung warna ---
            // (printer color bisa tetap dipakai mode Monochrome; printer B/W murni cuma punya 1 opsi)
            cmbColorMode.Items.Clear();
            cmbColorMode.Items.Add("Color");
            if (_currentCapabilities.SupportsColor)
                cmbColorMode.Items.Add("Monochrome");

            cmbColorMode.Enabled = _currentCapabilities.SupportsColor;
            int cIdx = profile.ColorMode ? 0 : cmbColorMode.Items.IndexOf("Monochrome");
            cmbColorMode.SelectedIndex = cIdx >= 0 ? cIdx : 0;
            profile.ColorMode = cmbColorMode.SelectedIndex == 0;

            // --- Orientation ---
            cmbOrientation.Enabled = true;
            cmbOrientation.SelectedIndex = profile.Landscape ? 1 : 0;

            // --- Borderless ---
            chkBorderless.Enabled = true;
            chkBorderless.Checked = profile.Borderless;

            // --- Media Type/tipe kertas: opsional - banyak printer non-foto tidak
            // mengekspos kapabilitas ini sama sekali, jadi selalu ada opsi "Default
            // driver" di posisi pertama supaya operator tidak wajib memilih.
            const string defaultMediaTypeLabel = "(Default driver)";
            cmbMediaType.Items.Clear();
            cmbMediaType.Items.Add(defaultMediaTypeLabel);
            foreach (var mediaType in _currentCapabilities.MediaTypes)
                cmbMediaType.Items.Add(mediaType.Name);

            cmbMediaType.Enabled = _currentCapabilities.MediaTypes.Count > 0;
            int mIdx = string.IsNullOrEmpty(profile.MediaTypeName)
                ? 0
                : cmbMediaType.Items.IndexOf(profile.MediaTypeName);
            cmbMediaType.SelectedIndex = mIdx >= 0 ? mIdx : 0;
            profile.MediaTypeName = cmbMediaType.SelectedIndex == 0
                ? string.Empty
                : cmbMediaType.SelectedItem?.ToString() ?? string.Empty;

            lblProfileInfoValue.Text =
                $"{_currentCapabilities.PaperSizes.Count} paper size terdeteksi  |  " +
                (_currentCapabilities.SupportsColor ? "Color printer" : "Monochrome printer") +
                (_currentCapabilities.MediaTypes.Count > 0
                    ? $"  |  {_currentCapabilities.MediaTypes.Count} media type terdeteksi"
                    : string.Empty);

            UpdateBorderlessHint(printerName, profile);

            _profileStore.Save(profile);
        }
        catch (Exception ex)
        {
            // Lapis pertahanan kedua (selain global handler di Program.cs): kegagalan
            // query kapabilitas driver printer (Paper Size/Media Type/borderless hint) di
            // sini tidak boleh membuat aplikasi force-close - cukup dicatat di log.
            AppendLog($"Gagal memuat kapabilitas printer '{printerName}': {ex.Message}");
        }
        finally
        {
            _suppressProfileEvents = false;
        }
    }

    /// <summary>
    /// Menambahkan info kapabilitas borderless printer aktif (untuk Paper Size yang
    /// sedang dipilih) ke lblProfileInfoValue - dicek LANGSUNG dari driver, generik untuk
    /// printer apa pun. Tujuannya operator tahu SEBELUM sesi photobooth berjalan kalau
    /// printer yang sedang aktif ternyata tidak benar-benar mendukung true borderless,
    /// bukan baru ketahuan dari log setelah hasil cetak sudah terlanjur ada tepi putih.
    /// </summary>
    private void UpdateBorderlessHint(string printerName, PrinterProfile profile)
    {
        if (!profile.Borderless || string.IsNullOrWhiteSpace(profile.PaperSizeName))
            return;

        var capability = _printerService.CheckBorderlessCapability(printerName, profile.PaperSizeName);
        if (!capability.PaperSizeFound) return;

        string hint = capability.LikelyTrueBorderless
            ? "Borderless: printer mendukung true full-bleed untuk Paper Size ini."
            : $"Borderless: printer punya area tak-tercetak ~{capability.HardMarginXMm:0.#}x" +
              $"{capability.HardMarginYMm:0.#}mm di tepi untuk Paper Size ini - kemungkinan " +
              "tidak 100% full-bleed (batas hardware printer, bukan bug aplikasi).";

        lblProfileInfoValue.Text += "\n" + hint;
    }

    private void ProfileControl_Changed(object? sender, EventArgs e)
    {
        if (_suppressProfileEvents) return;
        if (cmbPrinters.SelectedIndex < 0 || cmbPrinters.SelectedIndex >= _printers.Count) return;

        try
        {
            var printerName = _printers[cmbPrinters.SelectedIndex].Name;

            var profile = _profileStore.GetOrCreate(printerName);
            profile.PrinterName = printerName;
            profile.PaperSizeName = cmbPaperSize.SelectedItem?.ToString() ?? string.Empty;
            profile.PrintQuality = cmbPrintQuality.SelectedItem is PrintQualityLevel level ? level : PrintQualityLevel.High;
            profile.ColorMode = cmbColorMode.SelectedIndex <= 0; // index 0 = "Color"
            profile.Landscape = cmbOrientation.SelectedIndex == 1; // index 1 = "Landscape"
            profile.Borderless = chkBorderless.Checked;
            profile.MediaTypeName = cmbMediaType.SelectedIndex <= 0
                ? string.Empty
                : cmbMediaType.SelectedItem?.ToString() ?? string.Empty;

            lblProfileInfoValue.Text =
                $"{_currentCapabilities.PaperSizes.Count} paper size terdeteksi  |  " +
                (_currentCapabilities.SupportsColor ? "Color printer" : "Monochrome printer") +
                (_currentCapabilities.MediaTypes.Count > 0
                    ? $"  |  {_currentCapabilities.MediaTypes.Count} media type terdeteksi"
                    : string.Empty);

            // Query kapabilitas borderless memanggil driver printer secara sinkron (bisa
            // lambat, terutama printer jaringan) - hanya jalankan kalau memang Paper Size
            // atau Borderless yang berubah, bukan di SETIAP perubahan dropdown (Print
            // Quality/Color/Orientation tidak mempengaruhi hasil hint ini sama sekali).
            if (ReferenceEquals(sender, cmbPaperSize) || ReferenceEquals(sender, chkBorderless))
            {
                UpdateBorderlessHint(printerName, profile);
            }

            _profileStore.Save(profile);

            AppendLog($"Printer Profile '{printerName}' disimpan " +
                       $"(Paper: {profile.PaperSizeName}, Quality: {profile.PrintQuality}, " +
                       $"Color: {(profile.ColorMode ? "Color" : "Monochrome")}, " +
                       $"Orientation: {(profile.Landscape ? "Landscape" : "Portrait")}, " +
                       $"Borderless: {profile.Borderless}, " +
                       $"Media Type: {(string.IsNullOrEmpty(profile.MediaTypeName) ? "Default driver" : profile.MediaTypeName)}).");
        }
        catch (Exception ex)
        {
            // Lapis pertahanan kedua (selain global handler di Program.cs): kegagalan
            // query driver printer di sini tidak boleh membuat aplikasi force-close -
            // cukup dicatat di log, operator tetap bisa lanjut pakai aplikasi.
            AppendLog($"Gagal menerapkan perubahan Printer Profile: {ex.Message}");
        }
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

        UpdateTrayStatus();
    }

    // ===================== Print History (Fase 3 - STEP 4) =====================

    private void LoadHistoryUi()
    {
        var entries = _historyStore.LoadAll();

        lvHistory.BeginUpdate();
        lvHistory.Items.Clear();

        // Terbaru di atas supaya operator langsung melihat hasil print terakhir.
        foreach (var entry in entries.OrderByDescending(e => e.CreatedAt))
        {
            lvHistory.Items.Add(BuildHistoryRow(entry));
        }

        lvHistory.EndUpdate();
    }

    private void HistoryStore_EntryAdded(PrintHistoryEntry entry)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => HistoryStore_EntryAdded(entry)));
            return;
        }

        // Entri baru selalu ditaruh paling atas (paling baru).
        lvHistory.Items.Insert(0, BuildHistoryRow(entry));
    }

    private void HistoryStore_HistoryCleared()
    {
        if (InvokeRequired)
        {
            Invoke(new Action(HistoryStore_HistoryCleared));
            return;
        }

        lvHistory.Items.Clear();
    }

    private static ListViewItem BuildHistoryRow(PrintHistoryEntry entry)
    {
        var item = new ListViewItem(entry.JobId);
        item.SubItems.Add(entry.FileName);
        item.SubItems.Add(entry.Copies.ToString());
        item.SubItems.Add(entry.PrinterName);
        item.SubItems.Add(entry.ProfileSummary);
        item.SubItems.Add(entry.Status.ToString());
        item.SubItems.Add(entry.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        item.SubItems.Add(entry.CompletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "-");
        item.SubItems.Add(entry.ErrorMessage ?? string.Empty);

        item.ForeColor = entry.Status == PrintJobStatus.Failed ? Color.DarkRed : Color.Black;

        return item;
    }

    private void btnClearHistory_Click(object? sender, EventArgs e)
    {
        var confirm = MessageBox.Show(
            "Hapus semua riwayat print? Tindakan ini tidak bisa dibatalkan.",
            "Konfirmasi Clear History",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        _historyStore.Clear();
        AppendLog("Print History dibersihkan oleh operator.");
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