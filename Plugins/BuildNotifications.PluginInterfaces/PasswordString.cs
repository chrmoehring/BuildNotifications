﻿using JetBrains.Annotations;

namespace BuildNotifications.PluginInterfaces;

/// <summary>
/// A string that can be encrypted and decrypted using Windows' DP API.
/// </summary>
[PublicAPI]
public sealed class PasswordString
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public PasswordString(string encrypted = "")
    {
        _plainText = !string.IsNullOrEmpty(encrypted)
            ? DpApi.Decrypt(encrypted)
            : string.Empty;
    }

    /// <summary>
    /// Encrypts the password.
    /// </summary>
    /// <returns>The encrypted and base64 encoded password</returns>
    public string Encrypted() => DpApi.Encrypt(_plainText);

    /// <summary>
    /// Construct an instance from a plain text.
    /// </summary>
    /// <param name="plainText">The PasswordString.</param>
    public static PasswordString FromPlainText(string plainText)
    {
        var encrypted = DpApi.Encrypt(plainText);
        return new PasswordString(encrypted);
    }

    /// <summary>
    /// Decrypts the password.
    /// </summary>
    /// <returns>The plain text value of the password.</returns>
    public string PlainText() => _plainText;

    private readonly string _plainText;
}