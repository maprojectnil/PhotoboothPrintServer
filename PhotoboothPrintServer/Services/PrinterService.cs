using System.Drawing.Printing;
using System.Management;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Services;

public class PrinterService
{
    private static readonly string[] VirtualPrinterKeywords =
    {
        "PDF", "XPS", "OneNote", "Fax", "OneDrive", "Send To", "Microsoft Print"
    };

    public List<PrinterInfo> GetInstalledPrinters()
    {
        var result = new List<PrinterInfo>();

        // Daftar dasar dari .NET - selalu berhasil walau WMI gagal
        var installedNames = new List<string>();
        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            installedNames.Add(name);
        }

        Dictionary<string, PrinterWmiData>? wmiData;
        try
        {
            wmiData = QueryWmiPrinters();
        }
        catch
        {
            wmiData = null;
        }

        foreach (var name in installedNames)
        {
            var info = new PrinterInfo
            {
                Name = name,
                IsVirtual = IsLikelyVirtualPrinter(name),
                StatusText = "Unknown",
                IsOnline = true,
                IsReady = true
            };

            if (wmiData != null && wmiData.TryGetValue(name, out var wmi))
            {
                info.IsDefault = wmi.IsDefault;
                info.IsOnline = !wmi.WorkOffline;
                info.IsReady = wmi.PrinterStatus == 3 || wmi.PrinterStatus == 4; // 3=Idle, 4=Printing
                info.StatusText = DescribeStatus(wmi.PrinterStatus, wmi.WorkOffline);

                if (!info.IsVirtual)
                {
                    info.IsVirtual = IsLikelyVirtualPort(wmi.PortName);
                }
            }
            else
            {
                // Fallback tanpa WMI: cek validitas dasar saja
                using var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = name;
                info.IsOnline = pd.PrinterSettings.IsValid;
                info.IsReady = pd.PrinterSettings.IsValid;
                info.StatusText = pd.PrinterSettings.IsValid ? "Ready (basic check)" : "Unavailable";
            }

            result.Add(info);
        }

        return result
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.IsVirtual)
            .ThenBy(p => p.Name)
            .ToList();
    }

    /// <summary>
    /// Query kemampuan asli sebuah printer (Paper Size, dukungan warna, resolusi/kualitas)
    /// langsung dari driver Windows. Dipakai Fase 3 agar Printer Profile hanya menawarkan
    /// opsi yang benar-benar didukung printer tersebut, bukan opsi yang diasumsikan.
    /// </summary>
    public PrinterCapabilities GetCapabilities(string printerName)
    {
        var caps = new PrinterCapabilities();

        if (string.IsNullOrWhiteSpace(printerName)) return caps;

        try
        {
            var settings = new PrinterSettings { PrinterName = printerName };
            if (!settings.IsValid) return caps;

            caps.SupportsColor = settings.SupportsColor;

            foreach (PaperSize size in settings.PaperSizes)
            {
                if (!string.IsNullOrWhiteSpace(size.PaperName))
                    caps.PaperSizes.Add(size.PaperName);
            }

            var resolutions = settings.PrinterResolutions.Cast<PrinterResolution>().ToList();

            var seenLevels = new HashSet<PrintQualityLevel>();
            foreach (var res in resolutions)
            {
                PrintQualityLevel? level = res.Kind switch
                {
                    PrinterResolutionKind.High => PrintQualityLevel.High,
                    PrinterResolutionKind.Medium => PrintQualityLevel.Normal,
                    PrinterResolutionKind.Draft => PrintQualityLevel.Draft,
                    PrinterResolutionKind.Low => PrintQualityLevel.Draft,
                    _ => null
                };

                if (level == null || !seenLevels.Add(level.Value)) continue;

                caps.QualityOptions.Add(new PrinterQualityOption
                {
                    Level = level.Value,
                    ResolutionName = res.Kind == PrinterResolutionKind.Custom
                        ? $"{res.X}x{res.Y} dpi"
                        : res.Kind.ToString()
                });
            }

            // Sebagian driver (terutama printer foto seperti DNP) tidak melaporkan Kind
            // standar dan hanya mengekspos daftar DPI custom. Fallback: petakan berdasarkan
            // urutan DPI tertinggi -> High, tengah -> Normal, terendah -> Draft.
            if (caps.QualityOptions.Count == 0 && resolutions.Count > 0)
            {
                var byDpi = resolutions.OrderByDescending(r => r.X).ToList();

                caps.QualityOptions.Add(new PrinterQualityOption
                {
                    Level = PrintQualityLevel.High,
                    ResolutionName = $"{byDpi.First().X}x{byDpi.First().Y} dpi"
                });

                if (byDpi.Count >= 3)
                {
                    var mid = byDpi[byDpi.Count / 2];
                    caps.QualityOptions.Add(new PrinterQualityOption
                    {
                        Level = PrintQualityLevel.Normal,
                        ResolutionName = $"{mid.X}x{mid.Y} dpi"
                    });
                }

                if (byDpi.Count >= 2)
                {
                    caps.QualityOptions.Add(new PrinterQualityOption
                    {
                        Level = PrintQualityLevel.Draft,
                        ResolutionName = $"{byDpi.Last().X}x{byDpi.Last().Y} dpi"
                    });
                }
            }
        }
        catch
        {
            // Query kapabilitas gagal (mis. driver bermasalah) - kembalikan apa adanya
            // agar UI tetap bisa jalan tanpa crash, dengan opsi seadanya.
        }

        return caps;
    }

    /// <summary>
    /// Cek status satu printer tertentu (dipakai polling Auto Reconnect - Fase 3 STEP 6).
    /// Reuse GetInstalledPrinters() supaya sumber data konsisten dengan daftar printer di UI.
    /// </summary>
    public PrinterInfo? GetPrinterStatus(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName)) return null;

        try
        {
            return GetInstalledPrinters().FirstOrDefault(p => p.Name == printerName);
        }
        catch
        {
            // Kegagalan cek status (mis. WMI sementara tidak bisa diakses) tidak boleh crash -
            // caller akan mencoba lagi di siklus polling berikutnya.
            return null;
        }
    }

    private Dictionary<string, PrinterWmiData> QueryWmiPrinters()
    {
        var data = new Dictionary<string, PrinterWmiData>();

        using var searcher = new ManagementObjectSearcher(
            "SELECT Name, Default, WorkOffline, PrinterStatus, PortName FROM Win32_Printer");

        foreach (ManagementBaseObject item in searcher.Get())
        {
            var printer = (ManagementObject)item;

            string name = printer["Name"]?.ToString() ?? string.Empty;
            if (string.IsNullOrEmpty(name)) continue;

            ushort status = 0;
            try
            {
                if (printer["PrinterStatus"] != null)
                    status = Convert.ToUInt16(printer["PrinterStatus"]);
            }
            catch { /* biarkan default 0 jika gagal parse */ }

            data[name] = new PrinterWmiData
            {
                IsDefault = printer["Default"] is bool b && b,
                WorkOffline = printer["WorkOffline"] is bool wo && wo,
                PrinterStatus = status,
                PortName = printer["PortName"]?.ToString() ?? string.Empty
            };

            printer.Dispose();
        }

        return data;
    }

    private static bool IsLikelyVirtualPrinter(string name)
    {
        return VirtualPrinterKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsLikelyVirtualPort(string portName)
    {
        if (string.IsNullOrEmpty(portName)) return false;

        return portName.Contains("PORTPROMPT", StringComparison.OrdinalIgnoreCase)
            || portName.Contains("nul", StringComparison.OrdinalIgnoreCase)
            || portName.Contains("FILE:", StringComparison.OrdinalIgnoreCase)
            || portName.Contains("XPSPort", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeStatus(ushort status, bool workOffline)
    {
        if (workOffline) return "Offline";

        return status switch
        {
            1 => "Other",
            2 => "Unknown",
            3 => "Idle / Ready",
            4 => "Printing",
            5 => "Warming Up",
            6 => "Stopped Printing",
            7 => "Offline",
            _ => "Ready"
        };
    }

    private class PrinterWmiData
    {
        public bool IsDefault { get; set; }
        public bool WorkOffline { get; set; }
        public ushort PrinterStatus { get; set; }
        public string PortName { get; set; } = string.Empty;
    }
}