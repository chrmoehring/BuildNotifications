﻿namespace BuildNotifications.PluginInterfaces.Configuration.Options;

/// <summary>
/// Result of a asynchronous value calculation.
/// </summary>
/// <typeparam name="T">Type of value that is contained in this result.</typeparam>
public interface IValueCalculationResult<out T>
{
    /// <summary>
    /// Flag indicating whether the calculation was successful.
    /// </summary>
    bool Success { get; }

    /// <summary>
    /// Result of the calculation. Only valid if <see cref="Success" /> is <c>true</c>.
    /// </summary>
    T Value { get; }
}