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

    private GroupBox grpLog = null!;
    private TextBox txtLog = null!;

    private void InitializeComponent()
    {
        lblTitle = new Label();

        grpServer = new GroupBox();
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
        lblPrinterCaption = new Label();
        cmbPrinters = new ComboBox();
        btnRefresh = new Button();
        lblPrinterStatusCaption = new Label();
        lblPrinterStatusValue = new Label();
        btnTestPrint = new Button();

        grpPrinterProfile = new GroupBox();
        lblPaperSizeCaption = new Label();
        cmbPaperSize = new ComboBox();
        lblPrintQualityCaption = new Label();
        cmbPrintQuality = new ComboBox();
        lblColorModeCaption = new Label();
        cmbColorMode = new ComboBox();
        lblOrientationCaption = new Label();
        cmbOrientation = new ComboBox();
        chkBorderless = new CheckBox();
        lblProfileInfoCaption = new Label();
        lblProfileInfoValue = new Label();

        grpQueue = new GroupBox();
        lblQueueLengthCaption = new Label();
        lblQueueLengthValue = new Label();
        lblCurrentJobCaption = new Label();
        lblCurrentJobValue = new Label();
        lblTotalPrintedCaption = new Label();
        lblTotalPrintedValue = new Label();
        lblTotalFailedCaption = new Label();
        lblTotalFailedValue = new Label();
        lvQueue = new ListView();

        grpLog = new GroupBox();
        txtLog = new TextBox();

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

        // grpPrinter
        grpPrinter.Text = "Printer";
        grpPrinter.Location = new Point(20, 220);
        grpPrinter.Size = new Size(680, 150);
        grpPrinter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

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

        // grpPrinterProfile
        grpPrinterProfile.Text = "Printer Profile";
        grpPrinterProfile.Location = new Point(20, 380);
        grpPrinterProfile.Size = new Size(680, 170);
        grpPrinterProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

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

        chkBorderless.Text = "Borderless";
        chkBorderless.Location = new Point(150, 98);
        chkBorderless.AutoSize = true;
        chkBorderless.CheckedChanged += ProfileControl_Changed;

        lblProfileInfoCaption.Text = "Info:";
        lblProfileInfoCaption.Location = new Point(15, 135);
        lblProfileInfoCaption.AutoSize = true;

        lblProfileInfoValue.Text = "-";
        lblProfileInfoValue.Location = new Point(150, 135);
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
        grpPrinterProfile.Controls.Add(chkBorderless);
        grpPrinterProfile.Controls.Add(lblProfileInfoCaption);
        grpPrinterProfile.Controls.Add(lblProfileInfoValue);

        // grpQueue
        grpQueue.Text = "HTTP API && Print Queue";
        grpQueue.Location = new Point(20, 560);
        grpQueue.Size = new Size(680, 240);
        grpQueue.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

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

        // grpLog
        grpLog.Text = "Log";
        grpLog.Location = new Point(20, 810);
        grpLog.Size = new Size(680, 150);
        grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        txtLog.Location = new Point(15, 25);
        txtLog.Size = new Size(650, 110);
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Consolas", 9);

        grpLog.Controls.Add(txtLog);

        // MainForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(720, 985);
        MinimumSize = new Size(680, 855);
        Text = "Photobooth Print Server";
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(lblTitle);
        Controls.Add(grpServer);
        Controls.Add(grpPrinter);
        Controls.Add(grpPrinterProfile);
        Controls.Add(grpQueue);
        Controls.Add(grpLog);
    }
}