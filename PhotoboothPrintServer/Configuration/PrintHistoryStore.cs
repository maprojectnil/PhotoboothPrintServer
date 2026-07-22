using System.Text.Json;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Configuration;

/// <summary>
/// Menyimpan Print History secara lokal (satu file JSON berisi list PrintHistoryEntry).
/// Sederhana dan stabil - tidak ada database, tidak ada cloud, sesuai kebutuhan operasional
/// photobooth (baca cepat, tulis sekali per job selesai).
///
/// Jumlah entri dibatasi (MaxEntries) agar file tidak tumbuh tanpa batas selama
/// event/operasional berjalan lama - entri terlama otomatis dibuang saat melebihi batas.
/// </summary>
public class PrintHistoryStore
{
    public const int MaxEntries = 1000;

    private readonly string _historyPath;
    private readonly object _lock = new();

    /// <summary>Dipicu setiap ada entri baru ditambahkan - dipakai UI untuk update langsung tanpa reload penuh.</summary>
    public event Action<PrintHistoryEntry>? EntryAdded;

    /// <summary>Dipicu setiap history dibersihkan total.</summary>
    public event Action? HistoryCleared;

    public PrintHistoryStore()
    {
        string folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhotoboothPrintServer");

        Directory.CreateDirectory(folder);
        _historyPath = Path.Combine(folder, "print-history.json");
    }

    public List<PrintHistoryEntry> LoadAll()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_historyPath))
                    return new List<PrintHistoryEntry>();

                string json = File.ReadAllText(_historyPath);
                return JsonSerializer.Deserialize<List<PrintHistoryEntry>>(json)
                       ?? new List<PrintHistoryEntry>();
            }
            catch
            {
                // File korup / tidak terbaca tidak boleh membuat aplikasi crash.
                return new List<PrintHistoryEntry>();
            }
        }
    }

    /// <summary>Menambahkan satu entri riwayat baru, memangkas entri terlama jika melebihi MaxEntries.</summary>
    public void Add(PrintHistoryEntry entry)
    {
        lock (_lock)
        {
            var all = LoadAll();
            all.Add(entry);

            if (all.Count > MaxEntries)
            {
                all = all
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(MaxEntries)
                    .OrderBy(e => e.CreatedAt)
                    .ToList();
            }

            SaveAll(all);
        }

        EntryAdded?.Invoke(entry);
    }

    public void Clear()
    {
        lock (_lock)
        {
            SaveAll(new List<PrintHistoryEntry>());
        }

        HistoryCleared?.Invoke();
    }

    private void SaveAll(List<PrintHistoryEntry> entries)
    {
        try
        {
            string json = JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_historyPath, json);
        }
        catch
        {
            // Kegagalan simpan tidak boleh membuat aplikasi crash.
        }
    }
}
