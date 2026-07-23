namespace PhotoboothPrintServer;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        // PENTING: tanpa handler ini, exception apa pun yang tidak tertangkap di mana pun
        // (termasuk di event handler dropdown UI) akan membuat SELURUH aplikasi mati
        // seketika tanpa pesan apa pun ("force close") - perilaku default .NET modern,
        // berbeda dari .NET Framework lama yang menampilkan dialog error dulu.
        // Dengan handler ini, exception yang tidak terduga ditampilkan sebagai pesan dan
        // (kalau memungkinkan) aplikasi tetap berjalan, bukan langsung mati.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (sender, e) =>
        {
            MessageBox.Show(
                $"Terjadi error yang tidak terduga:\n\n{e.Exception.Message}\n\n" +
                "Aplikasi akan mencoba tetap berjalan. Kalau error ini terus muncul, " +
                "laporkan pesan ini beserta langkah yang dilakukan sebelumnya.",
                "Error Tidak Terduga",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };
        AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show(
                $"Terjadi error fatal:\n\n{ex?.Message}\n\n" +
                (e.IsTerminating
                    ? "Aplikasi harus ditutup."
                    : "Aplikasi akan mencoba tetap berjalan."),
                "Error Fatal",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}