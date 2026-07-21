using System.Drawing;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PhotoboothPrintServer.Configuration;

namespace PhotoboothPrintServer.Services;

/// <summary>
/// Menjalankan HTTP API (Kestrel + Minimal API) di dalam proses WinForms yang sama.
/// Endpoint: POST /print, GET /status, GET /jobs/{jobId}
/// </summary>
public class WebServerService
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".bmp" };
    private const long MaxFileSizeBytes = 30L * 1024 * 1024; // 30 MB

    private readonly PrintQueueService _queue;
    private readonly AppSettingsService _settingsService;
    private readonly string _incomingFolder;

    private WebApplication? _app;

    public bool IsRunning { get; private set; }
    public string? LastError { get; private set; }

    public WebServerService(PrintQueueService queue, AppSettingsService settingsService)
    {
        _queue = queue;
        _settingsService = settingsService;

        _incomingFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhotoboothPrintServer", "Incoming");

        Directory.CreateDirectory(_incomingFolder);
    }

    public async Task<bool> StartAsync(int port)
    {
        if (IsRunning) return true;

        try
        {
            var builder = WebApplication.CreateBuilder();

            // Matikan logging console bawaan ASP.NET Core agar tidak bentrok
            // dengan console/output WinForms.
            builder.Logging.ClearProviders();

            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = MaxFileSizeBytes;
            });

            _app = builder.Build();

            MapEndpoints(_app);

            await _app.StartAsync();

            IsRunning = true;
            LastError = null;
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            IsRunning = false;
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (_app != null)
        {
            try
            {
                await _app.StopAsync();
                await _app.DisposeAsync();
            }
            catch
            {
                // Diabaikan saat shutdown.
            }

            _app = null;
        }

        IsRunning = false;
    }

    private void MapEndpoints(WebApplication app)
    {
        app.MapGet("/status", () =>
        {
            AppSettings settings = _settingsService.Load();
            bool hasPrinter = !string.IsNullOrWhiteSpace(settings.SelectedPrinter);

            return Results.Json(new
            {
                server = "running",
                printer = hasPrinter ? settings.SelectedPrinter : "(belum dipilih)",
                printerStatus = hasPrinter ? "ready" : "not-configured",
                queueLength = _queue.PendingCount
            });
        });

        app.MapGet("/jobs/{jobId}", (string jobId) =>
        {
            var job = _queue.GetJob(jobId);
            if (job == null)
                return Results.NotFound(new { success = false, message = "Job tidak ditemukan." });

            return Results.Json(new
            {
                jobId = job.JobId,
                fileName = job.FileName,
                copies = job.Copies,
                status = job.Status.ToString().ToLowerInvariant(),
                createdAt = job.CreatedAt,
                startedAt = job.StartedAt,
                completedAt = job.CompletedAt,
                errorMessage = job.ErrorMessage
            });
        });

        app.MapPost("/print", async (HttpRequest request) =>
        {
            if (!request.HasFormContentType)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Request harus berupa multipart/form-data."
                });
            }

            IFormCollection form;
            try
            {
                form = await request.ReadFormAsync();
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"Gagal membaca form: {ex.Message}"
                });
            }

            var file = form.Files["file"];
            if (file == null || file.Length == 0)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Field 'file' wajib diisi."
                });
            }

            string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"Format file '{ext}' tidak didukung. Gunakan JPG, JPEG, PNG, atau BMP."
                });
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return Results.BadRequest(new
                {
                    success = false,
                    message = "Ukuran file melebihi batas maksimum (30 MB)."
                });
            }

            int copies = 1;
            if (form.TryGetValue("copies", out var copiesValue))
            {
                if (!int.TryParse(copiesValue, out copies) || copies < 1)
                    copies = 1;
            }
            copies = Math.Min(copies, 20); // batas wajar agar tidak disalahgunakan

            string safeFileName = $"{Guid.NewGuid():N}{ext}";
            string savedPath = Path.Combine(_incomingFolder, safeFileName);

            try
            {
                await using (var stream = new FileStream(savedPath, FileMode.Create, FileAccess.Write))
                {
                    await file.CopyToAsync(stream);
                }

                // Validasi cepat: pastikan file benar-benar bisa dibaca sebagai gambar.
                using (var validationImage = Image.FromFile(savedPath))
                {
                    _ = validationImage.Width; // memaksa decode
                }
            }
            catch (Exception ex)
            {
                TryDelete(savedPath);
                return Results.BadRequest(new
                {
                    success = false,
                    message = $"File tidak valid atau gagal disimpan: {ex.Message}"
                });
            }

            var job = _queue.Enqueue(file.FileName, savedPath, copies);

            return Results.Json(new
            {
                success = true,
                jobId = job.JobId,
                status = job.Status.ToString().ToLowerInvariant()
            });
        });
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Diabaikan.
        }
    }
}