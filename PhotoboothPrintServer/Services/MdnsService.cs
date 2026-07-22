using Makaretu.Dns;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mengiklankan Print Server ke jaringan Wi-Fi lokal via mDNS/DNS-SD (RFC 6762/6763)
/// agar aplikasi Android dapat menemukannya otomatis tanpa input IP manual.
///
/// Service type selalu konsisten: "_photobooth._tcp" - inilah yang harus dicari
/// oleh Android (NsdManager) via DNS-SD, TIDAK berubah walau nama PC berbeda-beda.
/// Instance name membedakan Print Server jika ada lebih dari satu di jaringan yang sama.
/// </summary>
public class MdnsService : IDisposable
{
    /// <summary>Service type DNS-SD. Harus sama persis dengan yang dicari Android.</summary>
    public const string ServiceType = "_photobooth._tcp";

    private MulticastService? _mdns;
    private ServiceDiscovery? _discovery;

    public bool IsRunning { get; private set; }
    public string InstanceName { get; private set; } = string.Empty;

    /// <summary>Dipicu untuk pesan log (ditampilkan di WinForms log panel).</summary>
    public event Action<string>? LogMessage;

    /// <summary>
    /// Mulai mengiklankan Print Server. Dipanggil setiap kali HTTP API berhasil start,
    /// dihentikan setiap kali HTTP API berhenti - supaya Android tidak diarahkan
    /// ke server yang sebenarnya sedang mati.
    /// </summary>
    public void Start(int port, string instanceName)
    {
        if (IsRunning) return;

        try
        {
            InstanceName = SanitizeInstanceName(instanceName);

            _mdns = new MulticastService();
            _discovery = new ServiceDiscovery(_mdns);

            var profile = new ServiceProfile(InstanceName, ServiceType, (ushort)port);
            _discovery.Advertise(profile);

            _mdns.Start();

            IsRunning = true;
            LogMessage?.Invoke(
                $"mDNS aktif: '{InstanceName}.{ServiceType}.local' di port {port}.");
        }
        catch (Exception ex)
        {
            IsRunning = false;
            LogMessage?.Invoke($"Gagal memulai mDNS Auto Discovery: {ex.Message}");
            Cleanup();
        }
    }

    /// <summary>Hentikan iklan mDNS. Aman dipanggil berkali-kali / walau belum pernah Start.</summary>
    public void Stop()
    {
        if (!IsRunning)
        {
            Cleanup();
            return;
        }

        try
        {
            _discovery?.Unadvertise();
            LogMessage?.Invoke("mDNS Auto Discovery dihentikan.");
        }
        catch
        {
            // Kegagalan saat unadvertise tidak boleh membuat aplikasi crash.
        }
        finally
        {
            Cleanup();
        }
    }

    private void Cleanup()
    {
        try { _discovery?.Dispose(); } catch { /* diabaikan */ }
        try { _mdns?.Stop(); } catch { /* diabaikan */ }
        try { _mdns?.Dispose(); } catch { /* diabaikan */ }

        _discovery = null;
        _mdns = null;
        IsRunning = false;
    }

    private static string SanitizeInstanceName(string name)
    {
        return string.IsNullOrWhiteSpace(name) ? "PhotoboothPrintServer" : name;
    }

    public void Dispose() => Stop();
}
