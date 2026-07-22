using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PhotoboothPrintServer.Utilities;

/// <summary>
/// Membuat Icon System Tray secara runtime (lingkaran warna solid) sesuai status server.
/// Menghindari kebutuhan file .ico eksternal - warna mencerminkan status:
/// Gray = Stopped, Orange = Running tapi printer belum siap, Green = Running & printer siap.
/// </summary>
public static class TrayIconFactory
{
    public static Icon CreateStatusIcon(Color color)
    {
        using var bitmap = new Bitmap(32, 32);

        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);

            using var brush = new SolidBrush(color);
            g.FillEllipse(brush, 3, 3, 26, 26);

            using var pen = new Pen(Color.White, 2);
            g.DrawEllipse(pen, 3, 3, 26, 26);
        }

        nint hIcon = bitmap.GetHicon();
        try
        {
            using var handleIcon = Icon.FromHandle(hIcon);
            // Clone supaya Icon hasil tidak bergantung pada HICON sementara yang akan di-destroy.
            return (Icon)handleIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint handle);
}
