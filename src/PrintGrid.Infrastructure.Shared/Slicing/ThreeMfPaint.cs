using System.Numerics;

namespace PrintGrid.Infrastructure.Shared.Slicing;

/// <summary>Decodes the nibble tree used by Bambu/Orca/Prusa facet painting.</summary>
public static class ThreeMfPaint
{
    /// <summary>Remaps leaf slots without changing the subdivision tree or surface geometry.</summary>
    public static string Remap(string paint, int defaultSlot, Func<int, int> map)
    {
        var cursor = paint.Length - 1;
        var output = new List<char>();
        var nodes = 0;
        int Next()
        {
            if (cursor < 0) throw new InvalidDataException("Dữ liệu tô màu 3MF bị thiếu.");
            var c = paint[cursor--];
            return c switch { >= '0' and <= '9' => c - '0', >= 'A' and <= 'F' => c - 'A' + 10,
                >= 'a' and <= 'f' => c - 'a' + 10, _ => throw new InvalidDataException("Mã tô màu 3MF không hợp lệ.") };
        }
        void Nibble(int value) => output.Add("0123456789ABCDEF"[value]);
        void Leaf(int state)
        {
            state = map(state == 0 ? defaultSlot : state);
            if (state is < 1 or > 256) throw new InvalidDataException("Chỉ số filament 3MF không hợp lệ.");
            if (state < 3) Nibble(state << 2);
            else { Nibble(12); state -= 3; while (state >= 15) { Nibble(15); state -= 15; } Nibble(state); }
        }
        void Visit(int depth)
        {
            if (depth > 64 || ++nodes > 65536) throw new InvalidDataException("Phân vùng màu 3MF quá phức tạp.");
            var code = Next(); var split = code & 3;
            if (split == 0)
            {
                var state = code >> 2;
                if (state == 3) { int extra; do { extra = Next(); state += extra; } while (extra == 15 && state <= 256); }
                if (state > 256) throw new InvalidDataException("Chỉ số filament 3MF không hợp lệ.");
                Leaf(state); return;
            }
            if ((code >> 2) > 2 || (split == 3 && code != 3)) throw new InvalidDataException("Cạnh phân vùng màu 3MF không hợp lệ.");
            Nibble(code);
            for (var i = 0; i <= split; i++) Visit(depth + 1);
        }
        if (paint.Length == 0) Leaf(defaultSlot); else Visit(0);
        if (cursor != -1) throw new InvalidDataException("Dữ liệu tô màu 3MF có phần dư.");
        output.Reverse();
        return new string([.. output]);
    }

    public static void Decode(string paint, QuoteMesh.Triangle triangle, int defaultSlot,
        Action<QuoteMesh.Triangle, int> emit)
    {
        var cursor = paint.Length - 1;
        var nodes = 0;
        int Next()
        {
            if (cursor < 0) throw new InvalidDataException("Dữ liệu tô màu 3MF bị thiếu.");
            var c = paint[cursor--];
            return c switch
            {
                >= '0' and <= '9' => c - '0', >= 'A' and <= 'F' => c - 'A' + 10,
                >= 'a' and <= 'f' => c - 'a' + 10,
                _ => throw new InvalidDataException("Mã tô màu 3MF không hợp lệ.")
            };
        }
        void Visit(QuoteMesh.Triangle t, int depth)
        {
            if (depth > 64 || ++nodes > 65536) throw new InvalidDataException("Phân vùng màu 3MF quá phức tạp.");
            var code = Next();
            var split = code & 3;
            if (split == 0)
            {
                var state = code >> 2;
                if (state == 3)
                {
                    int extra;
                    do { extra = Next(); state += extra; } while (extra == 15 && state <= 256);
                }
                if (state > 256) throw new InvalidDataException("Chỉ số filament 3MF không hợp lệ.");
                emit(t, state == 0 ? defaultSlot : state);
                return;
            }
            var side = code >> 2;
            if (side > 2 || (split == 3 && side != 0)) throw new InvalidDataException("Cạnh phân vùng màu 3MF không hợp lệ.");
            var a = side == 0 ? t.A : side == 1 ? t.B : t.C;
            var b = side == 0 ? t.B : side == 1 ? t.C : t.A;
            var c = side == 0 ? t.C : side == 1 ? t.A : t.B;
            var ab = (a + b) / 2; var bc = (b + c) / 2; var ca = (c + a) / 2;
            // Serialized children are visited in reverse order; preserve winding and exact midpoints.
            QuoteMesh.Triangle[] children = split switch
            {
                1 => [new(a, b, bc), new(bc, c, a)],
                2 => [new(a, ab, ca), new(ab, b, ca), new(b, c, ca)],
                _ => [new(a, ab, ca), new(ab, b, bc), new(bc, c, ca), new(ab, bc, ca)]
            };
            for (var i = children.Length - 1; i >= 0; i--) Visit(children[i], depth + 1);
        }
        if (paint.Length == 0) { emit(triangle, defaultSlot); return; }
        Visit(triangle, 0);
        if (cursor != -1) throw new InvalidDataException("Dữ liệu tô màu 3MF có phần dư.");
    }
}
