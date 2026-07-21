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