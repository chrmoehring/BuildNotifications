﻿using System;
using JetBrains.Annotations;

namespace BuildNotifications.PluginInterfaces.Configuration.Options;

/// <summary>
/// A configuration option that contains a value.
/// </summary>
[PublicAPI]
public interface IValueOption : IOption
{
    /// <summary>
    /// Gets or sets the current value of this option.
    /// </summary>
    object? Value { get; set; }

    /// <summary>
    /// Raised when the value of this option has changed.
    /// </summary>
    event EventHandler<EventArgs>? ValueChanged;
}