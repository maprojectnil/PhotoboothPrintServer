namespace PhotoboothPrintServer;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private Label lblTitle = null!;

    private GroupBox grpServer = null!;
    private Label lblServerStatusCaption = null!;
    private Label lblServerStatusValue = null!;
    private Label lblIpCaption = null!;
    private Label lblIpValue = null!;
    private Label lblPortCaption = null!;
    private Label lblPortValue = null!;
    private Label lblApiUrlCaption = null!;
    private Label lblApiUrlValue = null!;
    private Label lblMdnsCaption = null!;
    private Label lblMdnsValue = null!;
    private Button btnToggleServer = null!;

    private GroupBox grpPrinter = null!;
    private Label lblPrinterCaption = null!;
    private ComboBox cmbPrinters = null!;
    private Button btnRefresh = null!;
    private Label lblPrinterStatusCaption = null!;
    private Label lblPrinterStatusValue = null!;
    private Button btnTestPrint = null!;

    private GroupBox grpPrinterProfile = null!;
    private Label lblPaperSizeCaption = null!;
    private ComboBox cmbPaperSize = null!;
    private Label lblPrintQualityCaption = null!;
    private ComboBox cmbPrintQuality = null!;
    private Label lblColorModeCaption = null!;
    private ComboBox cmbColorMode = null!;
    private Label lblOrientationCaption = null!;
    private ComboBox cmbOrientation = null!;
    private CheckBox chkBorderless = null!;
    private Label lblPrintSizeCaption = null!;
    private ComboBox cmbPrintSize = null!;
    private Label lblScalingCaption = null!;
    private ComboBox cmbScaling = null!;
    private Label lblPositionCaption = null!;
    private ComboBox cmbPosition = null!;
    private Label lblCustomSizeCaption = null!;
    private TextBox txtCustomWidthMm = null!;
    private Label lblCustomSizeSeparator = null!;
    private TextBox txtCustomHeightMm = null!;
    private Label lblPaperTypeCaption = null!;
    private ComboBox cmbPaperType = null!;
    private Label lblProfileInfoCaption = null!;
    private Label lblProfileInfoValue = null!;

    private GroupBox grpQueue = null!;
    private Label lblQueueLengthCaption = null!;
    private Label lblQueueLengthValue = null!;
    private Label lblCurrentJobCaption = null!;
    private Label lblCurrentJobValue = null!;
    private Label lblTotalPrintedCaption = null!;
    private Label lblTotalPrintedValue = null!;
    private Label lblTotalFailedCaption = null!;
    private Label lblTotalFailedValue = null!;
    private ListView lvQueue = null!;

    private GroupBox grpHistory = null!;
    private Button btnClearHistory = null!;
    private ListView lvHistory = null!;

    private GroupBox grpLog = null!;
    private TextBox txtLog = null!;

    // Undock/float grip ("≡") - satu per panel, memungkinkan panel dilepas ke jendela terpisah.
    private ToolTip tooltipUndock = null!;
    private Button btnUndockServer = null!;
    private Button btnUndockPrinter = null!;
    private Button btnUndockPrinterProfile = null!;
    private Button btnUndockQueue = null!;
    private Button btnUndockHistory = null!;
    private Button btnUndockLog = null!;

    private NotifyIcon trayIcon = null!;
    private ContextMenuStrip trayMenu = null!;
    private ToolStripMenuItem trayMenuOpen = null!;
    private ToolStripMenuItem trayMenuStartServer = null!;
    private ToolStripMenuItem trayMenuStopServer = null!;
    private ToolStripMenuItem trayMenuRefreshPrinter = null!;
    private ToolStripMenuItem trayMenuExit = null!;

    private void InitializeComponent()
    {
        lblTitle = new Label();

        tooltipUndock = new ToolTip();

        grpServer = new GroupBox();
        btnUndockServer = new Button();
        lblServerStatusCaption = new Label();
        lblServerStatusValue = new Label();
        lblIpCaption = new Label();
        lblIpValue = new Label();
        lblPortCaption = new Label();
        lblPortValue = new Label();
        lblApiUrlCaption = new Label();
        lblApiUrlValue = new Label();
        lblMdnsCaption = new Label();
        lblMdnsValue = new Label();
        btnToggleServer = new Button();

        grpPrinter = new GroupBox();
        btnUndockPrinter = new Button();
        lblPrinterCaption = new Label();
        cmbPrinters = new ComboBox();
        btnRefresh = new Button();
        lblPrinterStatusCaption = new Label();
        lblPrinterStatusValue = new Label();
        btnTestPrint = new Button();

        grpPrinterProfile = new GroupBox();
        btnUndockPrinterProfile = new Button();
        lblPaperSizeCaption = new Label();
        cmbPaperSize = new ComboBox();
        lblPrintQualityCaption = new Label();
        cmbPrintQuality = new ComboBox();
        lblColorModeCaption = new Label();
        cmbColorMode = new ComboBox();
        lblOrientationCaption = new Label();
        cmbOrientation = new ComboBox();
        chkBorderless = new CheckBox();
        lblPrintSizeCaption = new Label();
        cmbPrintSize = new ComboBox();
        lblScalingCaption = new Label();
        cmbScaling = new ComboBox();
        lblPositionCaption = new Label();
        cmbPosition = new ComboBox();
        lblCustomSizeCaption = new Label();
        txtCustomWidthMm = new TextBox();
        lblCustomSizeSeparator = new Label();
        txtCustomHeightMm = new TextBox();
        lblPaperTypeCaption = new Label();
        cmbPaperType = new ComboBox();
        lblProfileInfoCaption = new Label();
        lblProfileInfoValue = new Label();

        grpQueue = new GroupBox();
        btnUndockQueue = new Button();
        lblQueueLengthCaption = new Label();
        lblQueueLengthValue = new Label();
        lblCurrentJobCaption = new Label();
        lblCurrentJobValue = new Label();
        lblTotalPrintedCaption = new Label();
        lblTotalPrintedValue = new Label();
        lblTotalFailedCaption = new Label();
        lblTotalFailedValue = new Label();
        lvQueue = new ListView();

        grpHistory = new GroupBox();
        btnUndockHistory = new Button();
        btnClearHistory = new Button();
        lvHistory = new ListView();

        grpLog = new GroupBox();
        btnUndockLog = new Button();
        txtLog = new TextBox();

        trayIcon = new NotifyIcon();
        trayMenu = new ContextMenuStrip();
        trayMenuOpen = new ToolStripMenuItem();
        trayMenuStartServer = new ToolStripMenuItem();
        trayMenuStopServer = new ToolStripMenuItem();
        trayMenuRefreshPrinter = new ToolStripMenuItem();
        trayMenuExit = new ToolStripMenuItem();

        // lblTitle
        lblTitle.Text = "Photobooth Print Server";
        lblTitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        lblTitle.Location = new Point(20, 15);
        lblTitle.AutoSize = true;

        // grpServer
        grpServer.Text = "Server Info";
        grpServer.Location = new Point(20, 55);
        grpServer.Size = new Size(680, 155);
        grpServer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnUndockServer.Text = "☰";
        btnUndockServer.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnUndockServer.Location = new Point(grpServer.Width - 34, 2);
        btnUndockServer.Size = new Size(28, 22);
        btnUndockServer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUndockServer.FlatStyle = FlatStyle.Flat;
        btnUndockServer.FlatAppearance.BorderSize = 0;
        btnUndockServer.Cursor = Cursors.Hand;
        btnUndockServer.TabStop = false;
        btnUndockServer.Tag = grpServer;
        btnUndockServer.Click += UndockButton_Click;
        tooltipUndock.SetToolTip(btnUndockServer, "Undock panel ini ke jendela terpisah");

        lblServerStatusCaption.Text = "Status Server:";
        lblServerStatusCaption.Location = new Point(15, 25);
        lblServerStatusCaption.AutoSize = true;

        lblServerStatusValue.Text = "-";
        lblServerStatusValue.Location = new Point(150, 25);
        lblServerStatusValue.AutoSize = true;
        lblServerStatusValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblIpCaption.Text = "IP Address:";
        lblIpCaption.Location = new Point(15, 50);
        lblIpCaption.AutoSize = true;

        lblIpValue.Text = "-";
        lblIpValue.Location = new Point(150, 50);
        lblIpValue.AutoSize = true;
        lblIpValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblPortCaption.Text = "API Port:";
        lblPortCaption.Location = new Point(15, 75);
        lblPortCaption.AutoSize = true;

        lblPortValue.Text = "8080";
        lblPortValue.Location = new Point(150, 75);
        lblPortValue.AutoSize = true;
        lblPortValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblApiUrlCaption.Text = "API URL:";
        lblApiUrlCaption.Location = new Point(15, 100);
        lblApiUrlCaption.AutoSize = true;

        lblApiUrlValue.Text = "-";
        lblApiUrlValue.Location = new Point(150, 100);
        lblApiUrlValue.AutoSize = true;
        lblApiUrlValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblMdnsCaption.Text = "mDNS Discovery:";
        lblMdnsCaption.Location = new Point(15, 125);
        lblMdnsCaption.AutoSize = true;

        lblMdnsValue.Text = "-";
        lblMdnsValue.Location = new Point(150, 125);
        lblMdnsValue.AutoSize = true;
        lblMdnsValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        btnToggleServer.Text = "Start Server";
        btnToggleServer.Location = new Point(515, 55);
        btnToggleServer.Size = new Size(140, 32);
        btnToggleServer.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnToggleServer.Click += btnToggleServer_Click;

        grpServer.Controls.Add(lblServerStatusCaption);
        grpServer.Controls.Add(lblServerStatusValue);
        grpServer.Controls.Add(lblIpCaption);
        grpServer.Controls.Add(lblIpValue);
        grpServer.Controls.Add(lblPortCaption);
        grpServer.Controls.Add(lblPortValue);
        grpServer.Controls.Add(lblApiUrlCaption);
        grpServer.Controls.Add(lblApiUrlValue);
        grpServer.Controls.Add(lblMdnsCaption);
        grpServer.Controls.Add(lblMdnsValue);
        grpServer.Controls.Add(btnToggleServer);
        grpServer.Controls.Add(btnUndockServer);

        // grpPrinter
        grpPrinter.Text = "Printer";
        grpPrinter.Location = new Point(20, 220);
        grpPrinter.Size = new Size(680, 150);
        grpPrinter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnUndockPrinter.Text = "☰";
        btnUndockPrinter.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnUndockPrinter.Location = new Point(grpPrinter.Width - 34, 2);
        btnUndockPrinter.Size = new Size(28, 22);
        btnUndockPrinter.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUndockPrinter.FlatStyle = FlatStyle.Flat;
        btnUndockPrinter.FlatAppearance.BorderSize = 0;
        btnUndockPrinter.Cursor = Cursors.Hand;
        btnUndockPrinter.TabStop = false;
        btnUndockPrinter.Tag = grpPrinter;
        btnUndockPrinter.Click += UndockButton_Click;
        tooltipUndock.SetToolTip(btnUndockPrinter, "Undock panel ini ke jendela terpisah");

        lblPrinterCaption.Text = "Printer Aktif:";
        lblPrinterCaption.Location = new Point(15, 28);
        lblPrinterCaption.AutoSize = true;

        cmbPrinters.Location = new Point(150, 25);
        cmbPrinters.Size = new Size(350, 25);
        cmbPrinters.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPrinters.SelectedIndexChanged += cmbPrinters_SelectedIndexChanged;

        btnRefresh.Text = "Refresh Printers";
        btnRefresh.Location = new Point(515, 24);
        btnRefresh.Size = new Size(140, 28);
        btnRefresh.Click += btnRefresh_Click;

        lblPrinterStatusCaption.Text = "Status:";
        lblPrinterStatusCaption.Location = new Point(15, 65);
        lblPrinterStatusCaption.AutoSize = true;

        lblPrinterStatusValue.Text = "No printer selected";
        lblPrinterStatusValue.Location = new Point(150, 65);
        lblPrinterStatusValue.AutoSize = true;
        lblPrinterStatusValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        btnTestPrint.Text = "Test Print";
        btnTestPrint.Location = new Point(150, 100);
        btnTestPrint.Size = new Size(140, 32);
        btnTestPrint.Enabled = false;
        btnTestPrint.Click += btnTestPrint_Click;

        grpPrinter.Controls.Add(lblPrinterCaption);
        grpPrinter.Controls.Add(cmbPrinters);
        grpPrinter.Controls.Add(btnRefresh);
        grpPrinter.Controls.Add(lblPrinterStatusCaption);
        grpPrinter.Controls.Add(lblPrinterStatusValue);
        grpPrinter.Controls.Add(btnTestPrint);
        grpPrinter.Controls.Add(btnUndockPrinter);

        // grpPrinterProfile
        grpPrinterProfile.Text = "Printer Profile";
        grpPrinterProfile.Location = new Point(20, 380);
        grpPrinterProfile.Size = new Size(680, 285);
        grpPrinterProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnUndockPrinterProfile.Text = "☰";
        btnUndockPrinterProfile.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnUndockPrinterProfile.Location = new Point(grpPrinterProfile.Width - 34, 2);
        btnUndockPrinterProfile.Size = new Size(28, 22);
        btnUndockPrinterProfile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUndockPrinterProfile.FlatStyle = FlatStyle.Flat;
        btnUndockPrinterProfile.FlatAppearance.BorderSize = 0;
        btnUndockPrinterProfile.Cursor = Cursors.Hand;
        btnUndockPrinterProfile.TabStop = false;
        btnUndockPrinterProfile.Tag = grpPrinterProfile;
        btnUndockPrinterProfile.Click += UndockButton_Click;
        tooltipUndock.SetToolTip(btnUndockPrinterProfile, "Undock panel ini ke jendela terpisah");

        lblPaperSizeCaption.Text = "Paper Size:";
        lblPaperSizeCaption.Location = new Point(15, 28);
        lblPaperSizeCaption.AutoSize = true;

        cmbPaperSize.Location = new Point(150, 25);
        cmbPaperSize.Size = new Size(200, 25);
        cmbPaperSize.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPaperSize.SelectedIndexChanged += ProfileControl_Changed;

        lblPrintQualityCaption.Text = "Print Quality:";
        lblPrintQualityCaption.Location = new Point(380, 28);
        lblPrintQualityCaption.AutoSize = true;

        cmbPrintQuality.Location = new Point(480, 25);
        cmbPrintQuality.Size = new Size(170, 25);
        cmbPrintQuality.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPrintQuality.SelectedIndexChanged += ProfileControl_Changed;

        lblColorModeCaption.Text = "Color Mode:";
        lblColorModeCaption.Location = new Point(15, 63);
        lblColorModeCaption.AutoSize = true;

        cmbColorMode.Location = new Point(150, 60);
        cmbColorMode.Size = new Size(200, 25);
        cmbColorMode.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbColorMode.SelectedIndexChanged += ProfileControl_Changed;

        lblOrientationCaption.Text = "Orientation:";
        lblOrientationCaption.Location = new Point(380, 63);
        lblOrientationCaption.AutoSize = true;

        cmbOrientation.Location = new Point(480, 60);
        cmbOrientation.Size = new Size(170, 25);
        cmbOrientation.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbOrientation.Items.Add("Portrait");
        cmbOrientation.Items.Add("Landscape");
        cmbOrientation.SelectedIndexChanged += ProfileControl_Changed;

        // --- Print Size (fisik, terpisah dari Paper Size) & Scaling ---
        lblPrintSizeCaption.Text = "Print Size:";
        lblPrintSizeCaption.Location = new Point(15, 98);
        lblPrintSizeCaption.AutoSize = true;

        cmbPrintSize.Location = new Point(150, 95);
        cmbPrintSize.Size = new Size(200, 25);
        cmbPrintSize.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPrintSize.SelectedIndexChanged += ProfileControl_Changed;

        lblScalingCaption.Text = "Scaling:";
        lblScalingCaption.Location = new Point(380, 98);
        lblScalingCaption.AutoSize = true;

        cmbScaling.Location = new Point(480, 95);
        cmbScaling.Size = new Size(170, 25);
        cmbScaling.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbScaling.SelectedIndexChanged += ProfileControl_Changed;

        // --- Position & Borderless ---
        lblPositionCaption.Text = "Position:";
        lblPositionCaption.Location = new Point(15, 133);
        lblPositionCaption.AutoSize = true;

        cmbPosition.Location = new Point(150, 130);
        cmbPosition.Size = new Size(200, 25);
        cmbPosition.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPosition.SelectedIndexChanged += ProfileControl_Changed;

        chkBorderless.Text = "Borderless";
        chkBorderless.Location = new Point(480, 133);
        chkBorderless.AutoSize = true;
        chkBorderless.CheckedChanged += ProfileControl_Changed;

        // --- Custom Print Size (mm) - hanya terlihat saat Print Size = "Custom" ---
        lblCustomSizeCaption.Text = "Custom Size (mm):";
        lblCustomSizeCaption.Location = new Point(15, 168);
        lblCustomSizeCaption.AutoSize = true;
        lblCustomSizeCaption.Visible = false;

        txtCustomWidthMm.Location = new Point(150, 165);
        txtCustomWidthMm.Size = new Size(70, 25);
        txtCustomWidthMm.Visible = false;
        txtCustomWidthMm.TextChanged += ProfileControl_Changed;

        lblCustomSizeSeparator.Text = "x";
        lblCustomSizeSeparator.Location = new Point(228, 168);
        lblCustomSizeSeparator.AutoSize = true;
        lblCustomSizeSeparator.Visible = false;

        txtCustomHeightMm.Location = new Point(245, 165);
        txtCustomHeightMm.Size = new Size(70, 25);
        txtCustomHeightMm.Visible = false;
        txtCustomHeightMm.TextChanged += ProfileControl_Changed;

        // --- Paper Type / Media Type (Glossy, Matte, Plain, dst.) - hanya opsi yang benar-benar
        // dilaporkan driver printer aktif via DeviceCapabilities, plus "Driver Default" untuk
        // tidak meng-override sama sekali (banyak printer non-foto tidak melaporkan apa pun).
        lblPaperTypeCaption.Text = "Paper Type:";
        lblPaperTypeCaption.Location = new Point(15, 203);
        lblPaperTypeCaption.AutoSize = true;

        cmbPaperType.Location = new Point(150, 200);
        cmbPaperType.Size = new Size(335, 25);
        cmbPaperType.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbPaperType.SelectedIndexChanged += ProfileControl_Changed;

        lblProfileInfoCaption.Text = "Info:";
        lblProfileInfoCaption.Location = new Point(15, 240);
        lblProfileInfoCaption.AutoSize = true;

        lblProfileInfoValue.Text = "-";
        lblProfileInfoValue.Location = new Point(150, 240);
        lblProfileInfoValue.AutoSize = true;
        lblProfileInfoValue.Font = new Font("Segoe UI", 8.5F, FontStyle.Italic);
        lblProfileInfoValue.ForeColor = Color.DimGray;

        grpPrinterProfile.Controls.Add(lblPaperSizeCaption);
        grpPrinterProfile.Controls.Add(cmbPaperSize);
        grpPrinterProfile.Controls.Add(lblPrintQualityCaption);
        grpPrinterProfile.Controls.Add(cmbPrintQuality);
        grpPrinterProfile.Controls.Add(lblColorModeCaption);
        grpPrinterProfile.Controls.Add(cmbColorMode);
        grpPrinterProfile.Controls.Add(lblOrientationCaption);
        grpPrinterProfile.Controls.Add(cmbOrientation);
        grpPrinterProfile.Controls.Add(lblPrintSizeCaption);
        grpPrinterProfile.Controls.Add(cmbPrintSize);
        grpPrinterProfile.Controls.Add(lblScalingCaption);
        grpPrinterProfile.Controls.Add(cmbScaling);
        grpPrinterProfile.Controls.Add(lblPositionCaption);
        grpPrinterProfile.Controls.Add(cmbPosition);
        grpPrinterProfile.Controls.Add(chkBorderless);
        grpPrinterProfile.Controls.Add(lblCustomSizeCaption);
        grpPrinterProfile.Controls.Add(txtCustomWidthMm);
        grpPrinterProfile.Controls.Add(lblCustomSizeSeparator);
        grpPrinterProfile.Controls.Add(txtCustomHeightMm);
        grpPrinterProfile.Controls.Add(lblPaperTypeCaption);
        grpPrinterProfile.Controls.Add(cmbPaperType);
        grpPrinterProfile.Controls.Add(lblProfileInfoCaption);
        grpPrinterProfile.Controls.Add(lblProfileInfoValue);
        grpPrinterProfile.Controls.Add(btnUndockPrinterProfile);

        // grpQueue
        grpQueue.Text = "HTTP API && Print Queue";
        grpQueue.Location = new Point(20, 675);
        grpQueue.Size = new Size(680, 240);
        grpQueue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnUndockQueue.Text = "☰";
        btnUndockQueue.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnUndockQueue.Location = new Point(grpQueue.Width - 34, 2);
        btnUndockQueue.Size = new Size(28, 22);
        btnUndockQueue.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUndockQueue.FlatStyle = FlatStyle.Flat;
        btnUndockQueue.FlatAppearance.BorderSize = 0;
        btnUndockQueue.Cursor = Cursors.Hand;
        btnUndockQueue.TabStop = false;
        btnUndockQueue.Tag = grpQueue;
        btnUndockQueue.Click += UndockButton_Click;
        tooltipUndock.SetToolTip(btnUndockQueue, "Undock panel ini ke jendela terpisah");

        lblQueueLengthCaption.Text = "Queue Length:";
        lblQueueLengthCaption.Location = new Point(15, 25);
        lblQueueLengthCaption.AutoSize = true;

        lblQueueLengthValue.Text = "0";
        lblQueueLengthValue.Location = new Point(150, 25);
        lblQueueLengthValue.AutoSize = true;
        lblQueueLengthValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblCurrentJobCaption.Text = "Current Job:";
        lblCurrentJobCaption.Location = new Point(15, 50);
        lblCurrentJobCaption.AutoSize = true;

        lblCurrentJobValue.Text = "-";
        lblCurrentJobValue.Location = new Point(150, 50);
        lblCurrentJobValue.AutoSize = true;
        lblCurrentJobValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblTotalPrintedCaption.Text = "Total Printed:";
        lblTotalPrintedCaption.Location = new Point(350, 25);
        lblTotalPrintedCaption.AutoSize = true;

        lblTotalPrintedValue.Text = "0";
        lblTotalPrintedValue.Location = new Point(470, 25);
        lblTotalPrintedValue.AutoSize = true;
        lblTotalPrintedValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lblTotalFailedCaption.Text = "Total Failed:";
        lblTotalFailedCaption.Location = new Point(350, 50);
        lblTotalFailedCaption.AutoSize = true;

        lblTotalFailedValue.Text = "0";
        lblTotalFailedValue.Location = new Point(470, 50);
        lblTotalFailedValue.AutoSize = true;
        lblTotalFailedValue.Font = new Font("Segoe UI", 9, FontStyle.Bold);

        lvQueue.Location = new Point(15, 80);
        lvQueue.Size = new Size(650, 145);
        lvQueue.View = View.Details;
        lvQueue.FullRowSelect = true;
        lvQueue.GridLines = true;
        lvQueue.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        lvQueue.Columns.Add("Job ID", 90);
        lvQueue.Columns.Add("File", 220);
        lvQueue.Columns.Add("Copies", 70);
        lvQueue.Columns.Add("Status", 100);
        lvQueue.Columns.Add("Error", 150);

        grpQueue.Controls.Add(lblQueueLengthCaption);
        grpQueue.Controls.Add(lblQueueLengthValue);
        grpQueue.Controls.Add(lblCurrentJobCaption);
        grpQueue.Controls.Add(lblCurrentJobValue);
        grpQueue.Controls.Add(lblTotalPrintedCaption);
        grpQueue.Controls.Add(lblTotalPrintedValue);
        grpQueue.Controls.Add(lblTotalFailedCaption);
        grpQueue.Controls.Add(lblTotalFailedValue);
        grpQueue.Controls.Add(lvQueue);
        grpQueue.Controls.Add(btnUndockQueue);

        // grpHistory
        grpHistory.Text = "Print History";
        grpHistory.Location = new Point(20, 925);
        grpHistory.Size = new Size(680, 230);
        grpHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnUndockHistory.Text = "☰";
        btnUndockHistory.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnUndockHistory.Location = new Point(grpHistory.Width - 34, 2);
        btnUndockHistory.Size = new Size(28, 22);
        btnUndockHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUndockHistory.FlatStyle = FlatStyle.Flat;
        btnUndockHistory.FlatAppearance.BorderSize = 0;
        btnUndockHistory.Cursor = Cursors.Hand;
        btnUndockHistory.TabStop = false;
        btnUndockHistory.Tag = grpHistory;
        btnUndockHistory.Click += UndockButton_Click;
        tooltipUndock.SetToolTip(btnUndockHistory, "Undock panel ini ke jendela terpisah");

        btnClearHistory.Text = "Clear History";
        btnClearHistory.Location = new Point(515, 24);
        btnClearHistory.Size = new Size(140, 28);
        btnClearHistory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnClearHistory.Click += btnClearHistory_Click;

        lvHistory.Location = new Point(15, 60);
        lvHistory.Size = new Size(650, 160);
        lvHistory.View = View.Details;
        lvHistory.FullRowSelect = true;
        lvHistory.GridLines = true;
        lvHistory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lvHistory.Columns.Add("Job ID", 80);
        lvHistory.Columns.Add("File", 150);
        lvHistory.Columns.Add("Copies", 60);
        lvHistory.Columns.Add("Printer", 120);
        lvHistory.Columns.Add("Profile", 170);
        lvHistory.Columns.Add("Status", 80);
        lvHistory.Columns.Add("Created At", 130);
        lvHistory.Columns.Add("Completed At", 130);
        lvHistory.Columns.Add("Error", 150);

        grpHistory.Controls.Add(btnClearHistory);
        grpHistory.Controls.Add(lvHistory);
        grpHistory.Controls.Add(btnUndockHistory);

        // grpLog
        grpLog.Text = "Log";
        grpLog.Location = new Point(20, 1165);
        grpLog.Size = new Size(680, 150);
        grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        btnUndockLog.Text = "☰";
        btnUndockLog.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnUndockLog.Location = new Point(grpLog.Width - 34, 2);
        btnUndockLog.Size = new Size(28, 22);
        btnUndockLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnUndockLog.FlatStyle = FlatStyle.Flat;
        btnUndockLog.FlatAppearance.BorderSize = 0;
        btnUndockLog.Cursor = Cursors.Hand;
        btnUndockLog.TabStop = false;
        btnUndockLog.Tag = grpLog;
        btnUndockLog.Click += UndockButton_Click;
        tooltipUndock.SetToolTip(btnUndockLog, "Undock panel ini ke jendela terpisah");

        txtLog.Location = new Point(15, 25);
        txtLog.Size = new Size(650, 110);
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Consolas", 9);

        grpLog.Controls.Add(txtLog);
        grpLog.Controls.Add(btnUndockLog);

        // trayMenu
        trayMenuOpen.Text = "Open";
        trayMenuOpen.Click += trayMenuOpen_Click;

        trayMenuStartServer.Text = "Start Server";
        trayMenuStartServer.Click += trayMenuStartServer_Click;

        trayMenuStopServer.Text = "Stop Server";
        trayMenuStopServer.Click += trayMenuStopServer_Click;

        trayMenuRefreshPrinter.Text = "Refresh Printer";
        trayMenuRefreshPrinter.Click += trayMenuRefreshPrinter_Click;

        trayMenuExit.Text = "Exit";
        trayMenuExit.Click += trayMenuExit_Click;

        trayMenu.Items.Add(trayMenuOpen);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(trayMenuStartServer);
        trayMenu.Items.Add(trayMenuStopServer);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(trayMenuRefreshPrinter);
        trayMenu.Items.Add(new ToolStripSeparator());
        trayMenu.Items.Add(trayMenuExit);

        // trayIcon
        trayIcon.Text = "Photobooth Print Server";
        trayIcon.Icon = SystemIcons.Application; // fallback awal, diganti UpdateTrayStatus() saat form load
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        trayIcon.DoubleClick += trayIcon_DoubleClick;

        // MainForm
        AutoScaleMode = AutoScaleMode.Font;
        AutoScroll = true;
        ClientSize = new Size(720, 1015);
        MinimumSize = new Size(680, 500);
        Text = "Photobooth Print Server";
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(lblTitle);
        Controls.Add(grpServer);
        Controls.Add(grpPrinter);
        Controls.Add(grpPrinterProfile);
        Controls.Add(grpQueue);
        Controls.Add(grpHistory);
        Controls.Add(grpLog);
    }
}