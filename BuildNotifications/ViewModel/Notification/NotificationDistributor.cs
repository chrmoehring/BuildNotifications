﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using BuildNotifications.Core.Pipeline.Notification;
using BuildNotifications.Core.Pipeline.Notification.Distribution;
using BuildNotifications.PluginInterfaces.Builds;
using BuildNotifications.PluginInterfaces.Notification;
using BuildNotifications.Resources.BuildTree.Converter;
using BuildNotifications.ViewModel.Utils;
using BuildNotifications.Views.Notification;
using NLog.Fluent;

namespace BuildNotifications.ViewModel.Notification;

public class NotificationDistributor : BaseNotificationDistributor
{
    public NotificationDistributor()
    {
        var codeBase = Assembly.GetExecutingAssembly().Location;
        var uri = new UriBuilder(codeBase);
        var unescapedDataString = Uri.UnescapeDataString(uri.Path);
        _assemblyPath = Path.GetDirectoryName(unescapedDataString) ?? "";
    }

    private static string TemporaryPngStorageLocation => $"{Path.GetTempPath()}BuildNotifications";

    public static void DeleteAllTemporaryImageFiles()
    {
        var path = TemporaryPngStorageLocation;
        Log.Info().Message($"Removing old png files from location: \"{path}\".").Write();
        if (!Directory.Exists(path))
            return;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.png").ToList())
            {
                Log.Info().Message($"Deleting \"{file}\".").Write();
                File.Delete(file);
            }
        }
        catch (Exception e)
        {
            Log.Error().Message("Failed to delete temporary png files.").Exception(e).Write();
        }
    }

    protected override IDistributedNotification ToDistributedNotification(INotification notification)
    {
        var distributedNotification = new DistributedNotification
        {
            Title = notification.DisplayTitle,
            Content = notification.DisplayContent,
            AppIconUrl = AppIconPath(notification.Status),
            NotificationType = ToDistributedNotificationType(notification.Type),
            NotificationErrorType = ToDistributedErrorType(notification.Status),
            IssueSource = notification.IssueSource,
            Source = notification.Source,
            BasedOnNotification = notification.Guid
        };

        var statusToColorConverter = BuildStatusToBrushConverter.Instance;
        var brushFromStatus = statusToColorConverter.Convert(notification.Status) as SolidColorBrush ?? statusToColorConverter.DefaultBrush;
        distributedNotification.ColorCode = brushFromStatus.Color.ToUintColor();

        distributedNotification.ContentImageUrl = CreateNotificationImage(distributedNotification);
        distributedNotification.FeedbackArguments = distributedNotification.ToUriProtocol();

        Log.Debug().Message($"Created Feedback Argument for notification \"{distributedNotification.FeedbackArguments}\".").Write();
        return distributedNotification;
    }

    private string? AppIconPath(BuildStatus forBuildStatus)
    {
        return forBuildStatus switch
        {
            BuildStatus.Succeeded => ToAbsolute("/Resources/Icons/Green.ico"),
            BuildStatus.PartiallySucceeded => ToAbsolute("/Resources/Icons/Green.ico"),
            BuildStatus.Failed => ToAbsolute("/Resources/Icons/Red.ico"),
            _ => ToAbsolute("/Resources/Icons/Gray.ico")
        };
    }

    private string CreateNotificationImage(IDistributedNotification notification)
    {
        var view = new DistributedNotificationView();
        var viewModel = new DistributedNotificationViewModel(notification);

        view.DataContext = viewModel;

        var pngPath = CreateTempPngPath();
        view.ExportToPng(pngPath);

        return pngPath;
    }

    private string CreateTempPngPath()
    {
        var tmpPath = TemporaryPngStorageLocation;
        Directory.CreateDirectory(tmpPath);

        return Path.Combine(tmpPath, $"{Guid.NewGuid()}.png");
    }

    private string? ToAbsolute(string relativePath)
    {
        relativePath = relativePath.Replace('/', '\\');
        var absolutePath = $"{_assemblyPath}{relativePath}";
        return File.Exists(absolutePath) ? absolutePath : null;
    }

    private DistributedNotificationErrorType ToDistributedErrorType(BuildStatus notificationStatus)
    {
        return notificationStatus switch
        {
            BuildStatus.Failed => DistributedNotificationErrorType.Error,
            BuildStatus.Succeeded => DistributedNotificationErrorType.Success,
            BuildStatus.PartiallySucceeded => DistributedNotificationErrorType.Success,
            BuildStatus.Cancelled => DistributedNotificationErrorType.Cancel,
            _ => DistributedNotificationErrorType.None
        };
    }

    private DistributedNotificationType ToDistributedNotificationType(NotificationType notificationType)
    {
        return notificationType switch
        {
            NotificationType.Branch => DistributedNotificationType.Branch,
            NotificationType.Definition => DistributedNotificationType.Definition,
            NotificationType.DefinitionAndBranch => DistributedNotificationType.DefinitionAndBranch,
            NotificationType.Build => DistributedNotificationType.Build,
            NotificationType.Error => DistributedNotificationType.GeneralError,
            _ => DistributedNotificationType.General
        };
    }

    private readonly string _assemblyPath;
}