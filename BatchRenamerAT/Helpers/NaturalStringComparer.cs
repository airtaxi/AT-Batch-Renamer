using Windows.Win32;

namespace BatchRenamerAT.Helpers;

// Compares names the way File Explorer orders them ("file2" before "file10").
public sealed class NaturalStringComparer : IComparer<string>
{
    public static NaturalStringComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        return PInvoke.StrCmpLogical(x, y);
    }
}
