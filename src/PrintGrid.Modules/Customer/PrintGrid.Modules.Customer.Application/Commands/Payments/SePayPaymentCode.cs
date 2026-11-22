using System.Buffers.Binary;

namespace PrintGrid.Modules.Customer.Application.Commands.Payments;

/// <summary>Stable payment code that encodes the order id for an O(1) webhook lookup.</summary>
public static class SePayPaymentCode
{
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
    private const int EncodedLength = 26;

    public static string ForOrder(Guid orderId)
    {
        Span<byte> bytes = stackalloc byte[16];
        orderId.TryWriteBytes(bytes);
        var value = BinaryPrimitives.ReadUInt128BigEndian(bytes);
        Span<char> encoded = stackalloc char[EncodedLength];
        for (var index = EncodedLength - 1; index >= 0; index--)
        {
            encoded[index] = Alphabet[(int)(value & 31)];
            value >>= 5;
        }
        return $"PG{new string(encoded)}";
    }

    public static bool TryGetOrderId(string? value, out Guid orderId)
    {
        orderId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var compact = new string(value.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        var start = compact.IndexOf("PG", StringComparison.Ordinal);
        if (start < 0 || compact.Length < start + 2 + EncodedLength) return false;

        UInt128 decoded = 0;
        foreach (var character in compact.AsSpan(start + 2, EncodedLength))
        {
            var digit = Alphabet.IndexOf(character);
            if (digit < 0 || (decoded > (UInt128.MaxValue >> 5))) return false;
            decoded = (decoded << 5) | (uint)digit;
        }

        Span<byte> bytes = stackalloc byte[16];
        BinaryPrimitives.WriteUInt128BigEndian(bytes, decoded);
        orderId = new Guid(bytes);
        return true;
    }
}
