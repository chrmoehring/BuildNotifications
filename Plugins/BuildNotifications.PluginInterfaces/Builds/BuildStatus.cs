﻿using JetBrains.Annotations;

namespace BuildNotifications.PluginInterfaces.Builds;

/// <summary>
/// Possible statuses of a build. Sorted by importance. Higher = more important
/// </summary>
[PublicAPI]
public enum BuildStatus
{
    /// <summary>
    /// Unknown status.
    /// </summary>
    None = 0,

    /// <summary>
    /// Build has been cancelled.
    /// </summary>
    Cancelled = 1,

    /// <summary>
    /// Build has not yet been finished.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Build is currently running
    /// </summary>
    Running = 3,

    /// <summary>
    /// Build succeeded.
    /// </summary>
    Succeeded = 4,

    /// <summary>
    /// Build completed with warnings.
    /// </summary>
    PartiallySucceeded = 5,

    /// <summary>
    /// Build failed with errors.
    /// </summary>
    Failed = 6
}