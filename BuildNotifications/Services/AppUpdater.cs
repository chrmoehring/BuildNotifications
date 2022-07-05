﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog.Fluent;

namespace BuildNotifications.Services
{
    internal class AppUpdater : IAppUpdater
    {
        public AppUpdater(bool includePreReleases, INotifier notifier, IUpdateUrls updateUrls)
        {
            _includePreReleases = includePreReleases;
            _notifier = notifier;
            _updateExePath = FindUpdateExe();
            _packagesFolder = FindPackagesFolder();
            Log.Info().Message($"Update.exe should be located at {_updateExePath}").Write();
            _updateUrls = updateUrls;
        }

        private async Task<bool> DownloadFullNupkgFile(string targetFilePath, string version)
        {
            var fileName = Path.GetFileName(targetFilePath);
            var updateUrl = await GetUpdateUrl(version);
            if (updateUrl == null)
                return false;

            var url = _updateUrls.DownloadFileFromReleasePackage(updateUrl, fileName);

            if (File.Exists(targetFilePath))
                File.Delete(targetFilePath);

            using var client = new HttpClient {BaseAddress = _updateUrls.BaseAddressOf(url)};
            var stream = await client.GetStreamAsync(_updateUrls.RelativeFileDownloadUrl(url));

            await using var fileStream = File.OpenWrite(targetFilePath);
            await stream.CopyToAsync(fileStream);
            return true;
        }

        private async Task<bool> DownloadReleasesFile(string targetFilePath, string version)
        {
            var updateUrl = await GetUpdateUrl(version);
            if (updateUrl == null)
                return false;

            var url = _updateUrls.DownloadFileFromReleasePackage(updateUrl, "RELEASES");

            if (File.Exists(targetFilePath))
                File.Delete(targetFilePath);

            using var client = new HttpClient {BaseAddress = _updateUrls.BaseAddressOf(url)};
            var stream = await client.GetStreamAsync(_updateUrls.RelativeFileDownloadUrl(url));

            await using var fileStream = File.OpenWrite(targetFilePath);
            await stream.CopyToAsync(fileStream);
            return true;
        }

        private bool FilterRelease(Release x)
        {
            if (_includePreReleases)
                return true;

            if (!x.PreRelease)
                return true;

            Log.Debug().Message($"Ignoring pre-release at \"{x.HtmlUrl}\"").Write();
            return false;
        }

        private static string FindPackagesFolder()
        {
            var rootDirectory = FindRootDirectory();

            var fullPath = Path.Combine(rootDirectory, "packages");
            return fullPath;
        }

        private static string FindRootDirectory()
        {
            var assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation)
                                    ?? Directory.GetCurrentDirectory();

            return Path.Combine(assemblyDirectory, "..");
        }

        private static string FindUpdateExe()
        {
            var rootDirectory = FindRootDirectory();

            var fullPath = Path.Combine(rootDirectory, "update.exe");
            return fullPath;
        }

        private Task<string?> GetLatestUpdateUrl() => GetUpdateUrl(string.Empty);

        private async Task<string?> GetUpdateUrl(string version)
        {
            if (_updateUrlCache.TryGetValue(version, out var url))
                return url!;

            var currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);
            var userAgent = new ProductInfoHeaderValue(AppName, currentVersion.ToString(3));

            using var client = new HttpClient {BaseAddress = _updateUrls.BaseAddressForApiRequests()};
            client.DefaultRequestHeaders.UserAgent.Add(userAgent);
            var response = await client.GetAsync(_updateUrls.ListReleases());
            response.EnsureSuccessStatusCode();

            var releases = JsonConvert.DeserializeObject<List<Release>>(await response.Content.ReadAsStringAsync());
            if (releases == null)
                return null;

            var release = releases
                .Where(x => string.IsNullOrEmpty(version) || x.HtmlUrl.Contains(version, StringComparison.OrdinalIgnoreCase))
                .Where(FilterRelease)
                .OrderByDescending(x => x.PublishedAt)
                .First();

            var updateUrl = release.HtmlUrl.Replace("/tag/", "/download/", StringComparison.OrdinalIgnoreCase);
            _updateUrlCache[version] = updateUrl;
            return updateUrl;
        }

        private async Task SanitizePackages()
        {
            Log.Info().Message($"Sanitizing packages folder at {_packagesFolder}").Write();

            if (!Directory.Exists(_packagesFolder))
            {
                Log.Debug().Message("Folder missing. Creating.").Write();
                Directory.CreateDirectory(_packagesFolder);
            }

            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
            if (version == null)
            {
                Log.Debug().Message("Unable to determine current version").Write();
                return;
            }

            var releasesFilePath = Path.Combine(_packagesFolder, "RELEASES");
            if (!File.Exists(releasesFilePath))
            {
                Log.Debug().Message("RELEASES file does not exist. Downloading.").Write();
                var success = await DownloadReleasesFile(releasesFilePath, version);
                if (!success)
                {
                    Log.Debug().Message("Failed to download RELEASES file").Write();
                    return;
                }
            }

            var currentNupkgName = $"{AppName}-{version}-full.nupkg";
            var currentNupkgFilePath = Path.Combine(_packagesFolder, currentNupkgName);
            if (!File.Exists(currentNupkgFilePath))
            {
                Log.Debug().Message("Current full nupkg does not exist. Downloading").Write();
                var success = await DownloadFullNupkgFile(currentNupkgFilePath, version);
                if (!success)
                {
                    Log.Debug().Message("Failed to download nupkg file").Write();
                    return;
                }
            }

            Log.Info().Message("Packages folder is sanitized").Write();
        }

        public async Task<UpdateCheckResult?> CheckForUpdates(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_updateExePath))
            {
                Log.Warn().Message($"Update.exe not found. Expected it to be located at {_updateExePath}").Write();
                return null;
            }

            Log.Info().Message($"Checking for updates (include pre-releases: {_includePreReleases})").Write();

            await SanitizePackages();

            var latestUpdateUrl = await GetLatestUpdateUrl();

            return await Task.Run(() =>
            {
                var pi = new ProcessStartInfo
                {
                    FileName = _updateExePath,
                    Arguments = $"--checkForUpdate={latestUpdateUrl}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };

                var p = new Process
                {
                    StartInfo = pi
                };

                string? textResult = null;
                p.OutputDataReceived += (_, e) =>
                {
                    Log.Debug().Message($"Checking: {e.Data}").Write();
                    if (e.Data?.StartsWith("{", StringComparison.OrdinalIgnoreCase) ?? false)
                        textResult = e.Data;
                };

                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();

                if (!string.IsNullOrWhiteSpace(textResult))
                {
                    Log.Debug().Message($"Updater response is: {textResult}").Write();
                    return JsonConvert.DeserializeObject<UpdateCheckResult>(textResult);
                }

                Log.Info().Message("Got no meaningful response from updater").Write();
                return null;
            }, cancellationToken);
        }

        public async Task PerformUpdate(CancellationToken cancellationToken = default)
        {
            if (!File.Exists(_updateExePath))
            {
                Log.Warn().Message($"Update.exe not found. Expected it to be located at {_updateExePath}").Write();
                return;
            }

            var latestUpdateUrl = await GetLatestUpdateUrl();

            await Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _updateExePath,
                    RedirectStandardOutput = true,
                    Arguments = $"--update={latestUpdateUrl}",
                    UseShellExecute = false
                };

                var p = new Process
                {
                    StartInfo = startInfo
                };
                p.OutputDataReceived += (_, e) => { Debug.WriteLine($"Updating: {e.Data}"); };

                p.Start();
                p.BeginOutputReadLine();
                p.WaitForExit();
            }, cancellationToken);

            _notifier.ShowNotifications(new[] {new UpdateNotification()});
        }

        private readonly Dictionary<string, string> _updateUrlCache = new Dictionary<string, string>();
        private readonly bool _includePreReleases;
        private readonly INotifier _notifier;
        private readonly string _packagesFolder;
        private readonly string _updateExePath;
        private readonly IUpdateUrls _updateUrls;
        private const string AppName = "BuildNotifications";

        [DataContract]
        private sealed class Release
        {
            public Release()
            {
                HtmlUrl = string.Empty;
            }

            [DataMember(Name = "html_url")]
            public string HtmlUrl { get; set; }

            // ReSharper disable once StringLiteralTypo
            [DataMember(Name = "prerelease")]
            public bool PreRelease { get; set; }

            [DataMember(Name = "published_at")]
            public DateTime PublishedAt { get; set; }
        }
    }
}