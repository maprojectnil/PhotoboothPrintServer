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

    private GroupBox grpPrinter = null!;
    private Label lblPrinterCaption = null!;
    private ComboBox cmbPrinters = null!;
    private Button btnRefresh = null!;
    private Label lblPrinterStatusCaption = null!;
    private Label lblPrinterStatusValue = null!;
    private Button btnTestPrint = null!;

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

        grpPrinter = new GroupBox();
        lblPrinterCaption = new Label();
        cmbPrinters = new ComboBox();
        btnRefresh = new Button();
        lblPrinterStatusCaption = new Label();
        lblPrinterStatusValue = new Label();
        btnTestPrint = new Button();

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
        grpServer.Size = new Size(680, 100);
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

        grpServer.Controls.Add(lblServerStatusCaption);
        grpServer.Controls.Add(lblServerStatusValue);
        grpServer.Controls.Add(lblIpCaption);
        grpServer.Controls.Add(lblIpValue);
        grpServer.Controls.Add(lblPortCaption);
        grpServer.Controls.Add(lblPortValue);

        // grpPrinter
        grpPrinter.Text = "Printer";
        grpPrinter.Location = new Point(20, 165);
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

        // grpLog
        grpLog.Text = "Log";
        grpLog.Location = new Point(20, 325);
        grpLog.Size = new Size(680, 200);
        grpLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        txtLog.Location = new Point(15, 25);
        txtLog.Size = new Size(650, 160);
        txtLog.Multiline = true;
        txtLog.ScrollBars = ScrollBars.Vertical;
        txtLog.ReadOnly = true;
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Consolas", 9);

        grpLog.Controls.Add(txtLog);

        // MainForm
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(720, 550);
        MinimumSize = new Size(600, 450);
        Text = "Photobooth Print Server";
        StartPosition = FormStartPosition.CenterScreen;

        Controls.Add(lblTitle);
        Controls.Add(grpServer);
        Controls.Add(grpPrinter);
        Controls.Add(grpLog);
    }
}