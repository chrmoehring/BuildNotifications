﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NLog.Fluent;

namespace BuildNotifications.ViewModel.Utils;

internal static class ViewToPngExtension
{
    public static void ExportToPng(this FrameworkElement element, string toPath)
    {
        Log.Info().Message($"Exporting FrameWorkElement {element.Name} to png. Target path: {toPath}.").Write();
        var size = new Size(0, 0);

        CallMeasureMethods(element, size);
        size = new Size(element.ActualWidth, element.ActualHeight);
        CallMeasureMethods(element, size);
        var pngEncoder = CreateBitmap(element, size);
        SavePngToFile(pngEncoder, toPath);
    }

    private static void CallMeasureMethods(FrameworkElement element, Size forSize)
    {
        element.Measure(forSize);
        element.Arrange(new Rect(forSize));
        element.UpdateLayout();
    }

    private static PngBitmapEncoder CreateBitmap(FrameworkElement element, Size size)
    {
        var renderTarget = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(element);
        element.InvalidateMeasure();

        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        return encoder;
    }

    private static void SavePngToFile(PngBitmapEncoder encoder, string toPath)
    {
        Log.Debug().Message("Storing file").Write();
        try
        {
            using var outputStream = new MemoryStream();
            encoder.Save(outputStream);
            using var file = new FileStream(toPath, FileMode.Create, FileAccess.Write);
            outputStream.Seek(0, SeekOrigin.Begin);
            outputStream.CopyTo(file);
        }
        catch (Exception e)
        {
            Log.Error().Message("Failed to create output image for notification").Exception(e).Write();
        }
    }
}