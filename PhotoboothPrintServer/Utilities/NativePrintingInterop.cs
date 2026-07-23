using System.Runtime.InteropServices;
using PhotoboothPrintServer.Models;

namespace PhotoboothPrintServer.Utilities;

/// <summary>
/// System.Drawing.Printing TIDAK punya abstraksi untuk Media Type/tipe kertas (Glossy,
/// Matte, Plain, dst.) sama sekali - tidak seperti PaperSize/PrinterResolution yang sudah
/// dibungkus .NET. Satu-satunya cara mengakses ini adalah lewat Win32 winspool API
/// (DeviceCapabilities) untuk membaca daftar tipe media driver, dan menulis langsung ke
/// field dmMediaType pada struktur DEVMODE mentah untuk menerapkannya.
///
/// Semua operasi di sini defensif: kegagalan (driver tidak mendukung, versi DEVMODE lebih
/// lama, dll.) selalu mengembalikan hasil kosong/false, TIDAK PERNAH melempar exception ke
/// caller dan tidak pernah menulis di luar batas memori yang sudah divalidasi - supaya
/// fitur Paper Size/Borderless yang sudah berjalan tidak ikut rusak kalau Media Type gagal
/// diterapkan pada driver tertentu.
/// </summary>
internal static class NativePrintingInterop
{
    // PENTING: nilai ini SEBELUMNYA TERTUKAR (34/35 tertukar) di versi sebelumnya - itu
    // penyebab pasti crash STATUS_HEAP_CORRUPTION (0xc0000374). DC_MEDIATYPES dipakai untuk
    // membaca array ID (4 byte/entri), DC_MEDIATYPENAMES untuk array nama (128 byte/entri
    // Unicode) - kalau kodenya tertukar, driver menulis data 128 byte/entri ke buffer yang
    // cuma dialokasikan 4 byte/entri -> buffer overflow ke heap. Nilai berikut sudah
    // diverifikasi ulang terhadap wingdi.h resmi (Microsoft Docs + Wine headers):
    private const short DC_MEDIATYPENAMES = 34;
    private const short DC_MEDIATYPES = 35;
    private const int MEDIA_TYPE_NAME_LEN = 64; // panjang tetap (wchar) per entri, ditentukan Win32 API

    private const int DM_MEDIATYPE = 0x02000000;

    [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern int DeviceCapabilities(
        string pDevice, string? pPort, short fwCapability, IntPtr pOutput, IntPtr pDevMode);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);

    /// <summary>
    /// Layout DEVMODE (bagian tetap, tanpa data ekstra privat driver di belakangnya) sesuai
    /// definisi resmi Win32 wingdi.h. Dipakai HANYA untuk menghitung offset field
    /// dmFields/dmMediaType/dmSize secara aman lewat Marshal.OffsetOf - tidak pernah dipakai
    /// untuk overlay/replace seluruh struktur, supaya data driver privat di luar field ini
    /// (termasuk dmDriverExtra di belakangnya) tidak tersentuh sama sekali.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public short dmOrientation;
        public short dmPaperSize;
        public short dmPaperLength;
        public short dmPaperWidth;
        public short dmScale;
        public short dmCopies;
        public short dmDefaultSource;
        public short dmPrintQuality;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    /// <summary>
    /// Enumerasi tipe kertas/media yang benar-benar dilaporkan driver printer via
    /// DeviceCapabilities. List kosong kalau driver tidak mendukung kapabilitas ini
    /// (normal untuk banyak printer non-foto) atau query gagal - tidak pernah throw.
    /// </summary>
    public static List<PrinterMediaType> EnumerateMediaTypes(string printerName)
    {
        var result = new List<PrinterMediaType>();
        if (string.IsNullOrWhiteSpace(printerName)) return result;

        IntPtr idsPtr = IntPtr.Zero;
        IntPtr namesPtr = IntPtr.Zero;

        try
        {
            int count = DeviceCapabilities(printerName, null, DC_MEDIATYPES, IntPtr.Zero, IntPtr.Zero);

            // Pagar keamanan: driver yang salah/tidak mendukung kapabilitas ini kadang
            // mengembalikan nilai negatif (ditangani count<=0 di bawah) atau, pada driver
            // yang benar-benar bermasalah, angka yang tidak masuk akal. Batasi supaya tidak
            // pernah mengalokasikan buffer raksasa/aneh berdasarkan nilai yang tidak wajar -
            // tidak ada printer nyata yang punya ratusan tipe media.
            const int MaxReasonableMediaTypeCount = 200;
            if (count <= 0 || count > MaxReasonableMediaTypeCount) return result;

            idsPtr = Marshal.AllocHGlobal(count * sizeof(int));
            namesPtr = Marshal.AllocHGlobal(count * MEDIA_TYPE_NAME_LEN * sizeof(char));

            int idsResult = DeviceCapabilities(printerName, null, DC_MEDIATYPES, idsPtr, IntPtr.Zero);
            int namesResult = DeviceCapabilities(printerName, null, DC_MEDIATYPENAMES, namesPtr, IntPtr.Zero);

            if (idsResult <= 0 || namesResult <= 0) return result;

            // Clamp ke ukuran buffer yang benar-benar dialokasikan (dari query count di
            // atas) - kalau karena alasan apa pun driver melaporkan angka berbeda di
            // panggilan kedua, jangan pernah membaca melebihi buffer yang sudah dialokasikan.
            int n = Math.Min(Math.Min(idsResult, namesResult), count);
            for (int i = 0; i < n; i++)
            {
                int id = Marshal.ReadInt32(idsPtr, i * sizeof(int));

                IntPtr namePtr = namesPtr + i * MEDIA_TYPE_NAME_LEN * sizeof(char);
                string name = (Marshal.PtrToStringAuto(namePtr, MEDIA_TYPE_NAME_LEN) ?? string.Empty)
                    .TrimEnd('\0')
                    .Trim();

                if (!string.IsNullOrWhiteSpace(name))
                    result.Add(new PrinterMediaType { Id = id, Name = name });
            }
        }
        catch
        {
            // Driver tidak mendukung query ini / gagal di tengah jalan - kembalikan apa
            // yang berhasil didapat (bisa kosong), jangan crash.
        }
        finally
        {
            if (idsPtr != IntPtr.Zero) Marshal.FreeHGlobal(idsPtr);
            if (namesPtr != IntPtr.Zero) Marshal.FreeHGlobal(namesPtr);
        }

        return result;
    }

    /// <summary>
    /// Menulis mediaTypeId ke field dmMediaType pada DEVMODE mentah (hasil
    /// PrinterSettings.GetHdevmode) dan menyalakan flag DM_MEDIATYPE di dmFields, supaya
    /// driver benar-benar menerapkan tipe kertas ini (bukan cuma properti .NET di memori).
    /// Mengembalikan false (dengan pesan di error) tanpa mengubah apa pun kalau struktur
    /// DEVMODE driver ternyata lebih kecil dari yang diharapkan (driver sangat lama/tidak
    /// lazim) - tidak pernah menulis di luar batas yang sudah divalidasi lewat dmSize.
    /// </summary>
    public static bool TryPatchMediaType(IntPtr hDevMode, int mediaTypeId, out string? error)
    {
        error = null;
        if (hDevMode == IntPtr.Zero)
        {
            error = "Handle DEVMODE kosong.";
            return false;
        }

        IntPtr ptr = GlobalLock(hDevMode);
        if (ptr == IntPtr.Zero)
        {
            error = "GlobalLock atas DEVMODE gagal.";
            return false;
        }

        try
        {
            int dmSizeOffset = Marshal.OffsetOf<DEVMODE>(nameof(DEVMODE.dmSize)).ToInt32();
            short dmSize = Marshal.ReadInt16(ptr, dmSizeOffset);

            int neededSize = Marshal.SizeOf<DEVMODE>();
            if (dmSize < neededSize)
            {
                error = $"Struktur DEVMODE driver ini hanya {dmSize} byte (field dmMediaType " +
                        $"butuh minimal {neededSize} byte) - driver kemungkinan terlalu lama/tidak " +
                        "mendukung Media Type lewat DEVMODE standar. Dilewati dengan aman.";
                return false;
            }

            int fieldsOffset = Marshal.OffsetOf<DEVMODE>(nameof(DEVMODE.dmFields)).ToInt32();
            int mediaTypeOffset = Marshal.OffsetOf<DEVMODE>(nameof(DEVMODE.dmMediaType)).ToInt32();

            int currentFields = Marshal.ReadInt32(ptr, fieldsOffset);
            Marshal.WriteInt32(ptr, fieldsOffset, currentFields | DM_MEDIATYPE);
            Marshal.WriteInt32(ptr, mediaTypeOffset, mediaTypeId);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
        finally
        {
            GlobalUnlock(hDevMode);
        }
    }
}