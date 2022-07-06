﻿using System;
using System.Linq;
using System.Windows.Input;
using BuildNotifications.Core.Config;
using BuildNotifications.Core.Plugin;
using BuildNotifications.Resources.Icons;
using BuildNotifications.Services;
using BuildNotifications.ViewModel.Settings.Setup;
using BuildNotifications.ViewModel.Utils;
using Newtonsoft.Json;
using TweenSharp.Animation;
using TweenSharp.Factory;

namespace BuildNotifications.ViewModel.Overlays;

internal class InitialSetupOverlayViewModel : BaseViewModel
{
// properties *are* initialized within the constructor. However by a method call, which is not correctly recognized by the code analyzer yet.
#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    public InitialSetupOverlayViewModel(IConfiguration configuration, IPluginRepository pluginRepository, IConfigurationBuilder configurationBuilder, Action saveAction, IPopupService popupService)
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
    {
        _configuration = configuration;

        SetupViewModel = new SetupViewModel(configuration, pluginRepository, saveAction, configurationBuilder, popupService);
        SetupViewModel.Projects.Changed += UpdateText;
        SetupViewModel.Connections.Changed += UpdateText;
        SetupViewModel.Connections.TestFinished += UpdateText;
        RequestCloseCommand = new DelegateCommand(RequestClose);
        App.GlobalTweenHandler.Add(this.Tween(x => x.Opacity).To(1.0).In(0.5).Ease(Easing.ExpoEaseOut));
        UpdateText(this, EventArgs.Empty);

        StoreCurrentState();
    }

    public bool AnimateDisplay
    {
        get => _animateDisplay;
        set
        {
            _animateDisplay = value;
            OnPropertyChanged();
        }
    }

    public IconType DisplayedIconType
    {
        get => _displayedIconType;
        set
        {
            _displayedIconType = value;
            OnPropertyChanged();
        }
    }

    public string DisplayedTextId
    {
        get => _displayedTextId;
        set
        {
            if (_displayedTextId == value)
                return;

            _displayedTextId = value;
            AnimateDisplay = true;
            AnimateDisplay = false;
            OnPropertyChanged();
        }
    }

    public double Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            OnPropertyChanged();
        }
    }

    public ICommand RequestCloseCommand { get; set; }

    public SetupViewModel SetupViewModel { get; set; }

    public event EventHandler<InitialSetupEventArgs>? CloseRequested;

    private void RequestClose()
    {
        var currentlyConfiguredConnections = JsonConvert.SerializeObject(_configuration.Connections);
        var currentlyConfiguredProjects = JsonConvert.SerializeObject(_configuration.Projects);

        var anyChanges = !currentlyConfiguredConnections.Equals(_previouslyConfiguredConnections, StringComparison.OrdinalIgnoreCase)
                         || !currentlyConfiguredProjects.Equals(_previouslyConfiguredProjects, StringComparison.OrdinalIgnoreCase);

        CloseRequested?.Invoke(this, new InitialSetupEventArgs(anyChanges));
    }

    private void StoreCurrentState()
    {
        _previouslyConfiguredConnections = JsonConvert.SerializeObject(_configuration.Connections);
        _previouslyConfiguredProjects = JsonConvert.SerializeObject(_configuration.Projects);
    }

    private void UpdateText(object? sender, EventArgs e)
    {
        if (_configuration.Connections.Count == 0 && _configuration.Projects.Count == 0)
        {
            DisplayedTextId = InitialSetupEmptyConf;
            DisplayedIconType = IconType.Status;
            return;
        }

        if (_configuration.Connections.Count == 0)
        {
            DisplayedTextId = InitialSetupEmptyConnections;
            DisplayedIconType = IconType.Connection;
            return;
        }

        if (_configuration.Projects.Count == 0)
        {
            if (SetupViewModel.Connections.Connections.Any(x => !x.TestConnection.LastTestDidSucceed))
            {
                DisplayedTextId = InitialSetupUntested;
                DisplayedIconType = IconType.Dummy;
            }
            else
            {
                DisplayedTextId = InitialSetupTested;
                DisplayedIconType = IconType.Project;
            }
        }
        else
        {
            var anyConnectedBuildProviderSetup = _configuration.Projects.Any(x => x.BuildConnectionNames.Any());
            var anyConnectedSourceControlProviderSetup = _configuration.Projects.Any(x => x.SourceControlConnectionName.Any());

            if (!anyConnectedSourceControlProviderSetup && !anyConnectedBuildProviderSetup)
            {
                DisplayedTextId = InitialSetupConnectionNotAsBuildOrSource;
                DisplayedIconType = IconType.Status;
            }
            else if (!anyConnectedSourceControlProviderSetup)
            {
                DisplayedTextId = InitialSetupConnectionNotAsSource;
                DisplayedIconType = IconType.Branch;
            }
            else if (!anyConnectedBuildProviderSetup)
            {
                DisplayedTextId = InitialSetupConnectionNotAsBuild;
                DisplayedIconType = IconType.Definition;
            }
            else
            {
                DisplayedTextId = InitialSetupCompleteConfig;
                DisplayedIconType = IconType.Settings;
            }
        }
    }

    private readonly IConfiguration _configuration;

    private string _displayedTextId = "";
    private IconType _displayedIconType;
    private bool _animateDisplay;
    private double _opacity;
    private string _previouslyConfiguredConnections;
    private string _previouslyConfiguredProjects;
    private const string InitialSetupCompleteConfig = nameof(InitialSetupCompleteConfig);
    private const string InitialSetupConnectionNotAsBuild = nameof(InitialSetupConnectionNotAsBuild);
    private const string InitialSetupConnectionNotAsBuildOrSource = nameof(InitialSetupConnectionNotAsBuildOrSource);
    private const string InitialSetupConnectionNotAsSource = nameof(InitialSetupConnectionNotAsSource);

    private const string InitialSetupEmptyConf = nameof(InitialSetupEmptyConf);
    private const string InitialSetupEmptyConnections = nameof(InitialSetupEmptyConnections);
    private const string InitialSetupTested = nameof(InitialSetupTested);
    private const string InitialSetupUntested = nameof(InitialSetupUntested);
}