namespace PhotoboothPrintServer.Models;

/// <summary>
/// Satu opsi tipe kertas/media (mis. Glossy Photo Paper, Matte, Plain Paper) yang
/// benar-benar dilaporkan driver printer via winspool DeviceCapabilities
/// (DC_MEDIATYPES/DC_MEDIATYPENAMES). Id adalah nilai numerik mentah driver yang
/// dipakai untuk mengisi field dmMediaType pada DEVMODE - HARUS dipakai apa adanya,
/// tidak boleh ditebak/di-hardcode, karena nilainya spesifik per driver.
/// </summary>
public class PrinterMediaType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}