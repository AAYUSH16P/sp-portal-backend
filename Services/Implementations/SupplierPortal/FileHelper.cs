using Microsoft.AspNetCore.Http;

namespace Services.Implementations.SupplierPortal;

public static class FileHelper
{
    public static async Task<byte[]> ToByteArrayAsync(IFormFile file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        return ms.ToArray();
    }
}