using PhotoboothPrintServer.Utilities;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Memantau alamat IP lokal secara berkala. Wi-Fi yang sempat putus lalu tersambung ulang
/// sering mendapat IP baru dari DHCP - saat itu terjadi, event NetworkChanged dipicu supaya
/// mDNS bisa di-restart dengan info terbaru dan label IP di UI diperbarui, TANPA perlu
/// merestart aplikasi maupun HTTP API (Kestrel sudah listen di 0.0.0.0 sehingga tidak
/// terikat ke satu IP tertentu dan tetap berjalan meski adapter jaringan berganti).
/// </summary>
public class NetworkWatcherService : IDisposable
{
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private string? _lastIp;

    /// <summary>Dipicu saat IP lokal berubah (bukan saat poll pertama kali). Membawa IP baru.</summary>
    public event Action<string>? NetworkChanged;

    public NetworkWatcherService(TimeSpan? interval = null)
    {
        _interval = interval ?? TimeSpan.FromSeconds(10);
    }

    public void Start()
    {
        if (_cts != null) return;

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => PollLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                string currentIp = NetworkUtils.GetLocalIPv4Address();

                bool isValidIp = !string.IsNullOrWhiteSpace(currentIp) && currentIp != "Not Connected";

                if (_lastIp != null && isValidIp && _lastIp != currentIp)
                {
                    NetworkChanged?.Invoke(currentIp);
                }

                if (isValidIp) _lastIp = currentIp;
            }
            catch
            {
                // Diabaikan - dicoba lagi di siklus poll berikutnya.
            }

            try
            {
                await Task.Delay(_interval, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose() => Stop();
}
