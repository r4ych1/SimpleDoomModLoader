using System;

namespace ModLoader.App;

public static class WindowSizingPolicy
{
    public const double DefaultExpandedWindowWidth = 1180d;
    public const double ProfileOnlyWindowWidth = 420d;

    public static double GetExpandedWindowWidth(double? rememberedExpandedWindowWidth)
    {
        return NormalizeRememberedExpandedWindowWidth(rememberedExpandedWindowWidth) ?? DefaultExpandedWindowWidth;
    }

    public static double? NormalizeRememberedExpandedWindowWidth(double? width)
    {
        if (!width.HasValue || double.IsNaN(width.Value) || double.IsInfinity(width.Value) || width.Value <= 0d)
        {
            return null;
        }

        return width.Value;
    }
}
