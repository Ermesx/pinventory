using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace Pinventory.Pins.Domain;

public static class HashExtensions
{
    public static string GetThumbprint(Action<IncrementalHash> action)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

        action(hash);

        Span<byte> hashBytes = stackalloc byte[32];
        hash.GetHashAndReset(hashBytes);

        return Convert.ToHexString(hashBytes);
    }

    public static void AddString(this IncrementalHash hash, string value)
    {
        var maxByteCount = Encoding.UTF8.GetMaxByteCount(value.Length);
        byte[]? rentedBuffer = null;

        try
        {
            Span<byte> buffer = maxByteCount <= 256
                ? stackalloc byte[maxByteCount]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(maxByteCount));

            var bytesWritten = Encoding.UTF8.GetBytes(value, buffer);
            hash.AppendData(buffer[..bytesWritten]);
        }
        finally
        {
            if (rentedBuffer is not null)
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
    }
}