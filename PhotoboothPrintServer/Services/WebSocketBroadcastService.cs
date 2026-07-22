using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Mengelola koneksi WebSocket yang aktif dan mem-broadcast update status Print Job
/// secara real-time ke semua Android yang terhubung.
///
/// Alur:
/// Android -> HTTP POST /print -> job dibuat (status Queued)
/// Print Server -> WebSocket -> semua client: "JOB-001: Queued"
/// Print Server -> WebSocket -> semua client: "JOB-001: Printing"
/// Print Server -> WebSocket -> semua client: "JOB-001: Completed"
///
/// HTTP API (/print, /status, /jobs/{jobId}) tetap berjalan seperti biasa dan
/// TIDAK digantikan oleh WebSocket ini - WebSocket hanya kanal tambahan untuk status real-time.
/// </summary>
public class WebSocketBroadcastService
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

    /// <summary>Dipicu untuk pesan log (client connect/disconnect, dsb).</summary>
    public event Action<string>? LogMessage;

    public int ConnectedClients => _clients.Count;

    /// <summary>
    /// Menahan koneksi WebSocket selama client tersambung (dipanggil dari endpoint /ws/status).
    /// Kanal ini read-only dari sisi server: pesan yang dikirim Android (jika ada) diabaikan,
    /// hanya dipakai untuk mendeteksi ping/close dari client.
    /// </summary>
    public async Task HandleClientAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        var clientId = Guid.NewGuid();
        _clients[clientId] = socket;
        LogMessage?.Invoke($"WebSocket client terhubung. Total client aktif: {_clients.Count}.");

        var buffer = new byte[1024];

        try
        {
            while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                // Pesan masuk dari Android (jika ada) sengaja diabaikan - kanal ini hanya untuk
                // mendorong status job dari server ke client, bukan dua arah.
            }
        }
        catch (OperationCanceledException)
        {
            // Server sedang shutdown / client di-cancel - normal, tidak perlu di-log sebagai error.
        }
        catch (WebSocketException)
        {
            // Client terputus tiba-tiba (mis. Wi-Fi hilang) - tidak boleh membuat HTTP API crash.
        }
        finally
        {
            _clients.TryRemove(clientId, out _);
            LogMessage?.Invoke($"WebSocket client terputus. Total client aktif: {_clients.Count}.");
        }
    }

    /// <summary>Mengirim status job terbaru ke semua client yang sedang terhubung.</summary>
    public async Task BroadcastJobStatusAsync(PrintJob job)
    {
        if (_clients.IsEmpty) return;

        string payload = JsonSerializer.Serialize(new
        {
            type = "job_status",
            jobId = job.JobId,
            fileName = job.FileName,
            copies = job.Copies,
            status = job.Status.ToString().ToLowerInvariant(),
            errorMessage = job.ErrorMessage,
            updatedAt = DateTime.Now
        });

        byte[] bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (clientId, socket) in _clients)
        {
            if (socket.State != WebSocketState.Open)
            {
                _clients.TryRemove(clientId, out _);
                continue;
            }

            try
            {
                await socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            }
            catch
            {
                // Kirim gagal (client sudah terputus) - buang dari daftar, jangan hentikan broadcast ke client lain.
                _clients.TryRemove(clientId, out _);
            }
        }
    }
}
