using System.Text.Json;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Configuration;

/// <summary>
/// Menyimpan Printer Profile untuk setiap printer secara lokal (satu file JSON
/// berisi dictionary "nama printer" -> PrinterProfile). Ganti printer aktif tidak
/// pernah menghapus profil printer lain; setiap profil berdiri sendiri per nama printer.
/// </summary>
public class PrinterProfileStore
{
    private readonly string _profilesPath;
    private readonly object _lock = new();

    public PrinterProfileStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhotoboothPrintServer");

        Directory.CreateDirectory(folder);
        _profilesPath = Path.Combine(folder, "printer-profiles.json");
    }

    public Dictionary<string, PrinterProfile> LoadAll()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_profilesPath))
                    return new Dictionary<string, PrinterProfile>();

                string json = File.ReadAllText(_profilesPath);
                return JsonSerializer.Deserialize<Dictionary<string, PrinterProfile>>(json)
                       ?? new Dictionary<string, PrinterProfile>();
            }
            catch
            {
                // File korup / tidak terbaca tidak boleh membuat aplikasi crash.
                return new Dictionary<string, PrinterProfile>();
            }
        }
    }

    public void SaveAll(Dictionary<string, PrinterProfile> profiles)
    {
        lock (_lock)
        {
            try
            {
                string json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_profilesPath, json);
            }
            catch
            {
                // Kegagalan simpan tidak boleh membuat aplikasi crash.
            }
        }
    }

    /// <summary>
    /// Mengambil profil printer tertentu. Jika belum ada, membuat profil default baru
    /// (tanpa langsung menyimpannya - caller yang memutuskan kapan menyimpan).
    /// </summary>
    public PrinterProfile GetOrCreate(string printerName)
    {
        if (string.IsNullOrWhiteSpace(printerName))
            return new PrinterProfile();

        var all = LoadAll();

        if (all.TryGetValue(printerName, out var existing))
            return existing;

        return new PrinterProfile { PrinterName = printerName };
    }

    /// <summary>Menyimpan/mengganti profil satu printer tanpa mengubah profil printer lain.</summary>
    public void Save(PrinterProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.PrinterName)) return;

        var all = LoadAll();
        all[profile.PrinterName] = profile;
        SaveAll(all);
    }
}
