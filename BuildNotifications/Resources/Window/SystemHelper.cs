﻿using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace BuildNotifications.Resources.Window;

internal static class SystemHelper
{
    public static int GetCurrentDpi()
    {
        var propertyInfo = typeof(SystemParameters).GetProperty("Dpi", BindingFlags.Static | BindingFlags.NonPublic);
        var value = propertyInfo?.GetValue(null, null);
        return (int?)value ?? 96;
    }

    public static double GetCurrentDpiScaleFactor() => (double)GetCurrentDpi() / 96;

    public static Point GetMouseScreenPosition()
    {
        var point = Control.MousePosition;
        return new Point(point.X, point.Y);
    }
}