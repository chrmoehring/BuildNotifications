﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BuildNotifications.PluginInterfaces;
using BuildNotifications.PluginInterfaces.Builds;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using BuildStatus = BuildNotifications.PluginInterfaces.Builds.BuildStatus;

namespace BuildNotifications.Plugin.Tfs.Build;

internal class TfsBuildProvider : IBuildProvider
{
    public TfsBuildProvider(VssConnection connection, Guid projectId)
    {
        _connection = connection;
        _projectId = projectId;
    }

    private int CalculateProgress(Timeline timeLine)
    {
        if (!timeLine.Records.Any())
            return 0;

        var percentagePerStep = 100.0 / timeLine.Records.Count;
        var completedSteps = timeLine.Records.Count(x => x.State == TimelineRecordState.Completed);

        var currentStepFactor = 0.0;
        var currentStep = timeLine.Records.FirstOrDefault(x => x.State == TimelineRecordState.InProgress);
        if (currentStep != null)
            currentStepFactor = (currentStep.PercentComplete ?? 0) / 100.0;

        if (currentStepFactor < 0)
            currentStepFactor = 0;
        if (currentStepFactor > 1)
            currentStepFactor = 1;

        return (int)Math.Round(completedSteps * percentagePerStep + percentagePerStep * currentStepFactor);
    }

    private TfsBuildDefinition Convert(BuildDefinition definition) => new(definition);

    private TfsBuild Convert(Microsoft.TeamFoundation.Build.WebApi.Build build) => new(build);

    private async Task<IList<Microsoft.TeamFoundation.Build.WebApi.Build>> FetchMaxAmountOfBuilds(BuildHttpClient buildClient, TeamProjectReference project, int buildsPerGroup)
    {
        var definitions = await buildClient.GetFullDefinitionsAsync(project.Id);
        var maxBuildsToFetch = Math.Min(Math.Max(definitions.Count, 1) * buildsPerGroup, MaxBuildsAllowedByApi);
        var builds = await buildClient.GetBuildsAsync(project.Id, queryOrder: BuildQueryOrder.QueueTimeDescending, top: maxBuildsToFetch, maxBuildsPerDefinition: buildsPerGroup);
        return builds;
    }

    private async Task<TeamProjectReference> GetProject()
    {
        if (_project == null)
        {
            var projectClient = await _connection.GetClientAsync<ProjectHttpClient>();
            var project = await projectClient.GetProject(_projectId.ToString());
            _project = project;
        }

        return _project;
    }

    private void UpdateBuildLinks(IList<TfsBuild> builds)
    {
        foreach (var tfsBuild in builds)
        {
            if (tfsBuild.Links is not TfsLinks links)
                continue;

            var definition = _knownDefinitions.FirstOrDefault(d => d.Id == tfsBuild.Definition.Id);
            if (definition != null)
                links.UpdateLinks(definition);
        }
    }

    private async Task UpdateBuildTimeLines(List<TfsBuild> builds, BuildHttpClient buildClient, TeamProjectReference project)
    {
        var buildList = builds
            .Where(b => b.Status == BuildStatus.Running || b.Status == BuildStatus.Pending)
            .ToList();

        var timeLines = await Task.WhenAll(buildList.Select(build =>
            buildClient.GetBuildTimelineAsync(project.Id, build.BuildId)));

        for (var i = 0; i < buildList.Count; ++i)
        {
            var build = buildList[i];
            var timeLine = timeLines[i];

            if (timeLine == null)
                continue;

            var progress = CalculateProgress(timeLine);
            build.Progress = progress;
        }
    }

    public IUser User => _user ??= new TfsUser(_connection.AuthenticatedIdentity);

    public async IAsyncEnumerable<IBaseBuild> FetchAllBuilds(int buildsPerGroup)
    {
        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();

        _buildsPerGroup = buildsPerGroup;
        var builds = await FetchMaxAmountOfBuilds(buildClient, project, buildsPerGroup);

        foreach (var build in builds)
        {
            var converted = Convert(build);
            _knownBuilds.Add(converted);
            yield return converted;
        }
    }

    public async IAsyncEnumerable<IBaseBuild> FetchBuildsForDefinition(IBuildDefinition definition)
    {
        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();

        if (definition is not TfsBuildDefinition tfsDefinition)
        {
            Debug.Fail("Incompatible build definition given");
            yield break;
        }

        var builds = await buildClient.GetBuildsAsync(project.Id, new[] { tfsDefinition.NativeId });
        foreach (var build in builds)
        {
            yield return Convert(build);
        }
    }

    public async IAsyncEnumerable<IBaseBuild> FetchBuildsChangedSince(DateTime date)
    {
        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();

        var builds = await buildClient.GetBuildsAsync2(project.Id, minFinishTime: date, queryOrder: BuildQueryOrder.QueueTimeAscending);
        foreach (var build in builds)
        {
            var converted = Convert(build);
            _knownBuilds.Add(converted);
            yield return converted;
        }

        // ReSharper disable BitwiseOperatorOnEnumWithoutFlags
        const Microsoft.TeamFoundation.Build.WebApi.BuildStatus statusFilter = Microsoft.TeamFoundation.Build.WebApi.BuildStatus.InProgress | Microsoft.TeamFoundation.Build.WebApi.BuildStatus.Postponed | Microsoft.TeamFoundation.Build.WebApi.BuildStatus.NotStarted | Microsoft.TeamFoundation.Build.WebApi.BuildStatus.None;
        // ReSharper restore BitwiseOperatorOnEnumWithoutFlags
        builds = await buildClient.GetBuildsAsync2(project.Id, statusFilter: statusFilter);
        foreach (var build in builds)
        {
            var converted = Convert(build);
            _knownBuilds.Add(converted);
            yield return converted;
        }

        var inProgressBuilds = _knownBuilds.Where(b => b.Status == BuildStatus.None
                                                       || b.Status == BuildStatus.Pending
                                                       || b.Status == BuildStatus.Running).ToList();
        foreach (var strangeBuild in inProgressBuilds)
        {
            var build = await buildClient.GetBuildAsync(project.Id, strangeBuild.BuildId);
            var converted = Convert(build);

            // Replace entry in HashSet so this build won't be updated in the next run
            // if it just completed.
            _knownBuilds.Remove(converted);
            _knownBuilds.Add(converted);

            yield return converted;
        }
    }

    public async IAsyncEnumerable<IBuildDefinition> FetchExistingBuildDefinitions()
    {
        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();

        var definitions = await buildClient.GetFullDefinitionsAsync(project.Id);

        foreach (var definition in definitions)
        {
            var converted = Convert(definition);
            _knownDefinitions.Add(converted);
            yield return converted;
        }
    }

    public async IAsyncEnumerable<IBuildDefinition> RemovedBuildDefinitions()
    {
        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();
        var definitions = await buildClient.GetDefinitionsAsync(project.Id);

        var deletedDefinitions = _knownDefinitions.Where(known => definitions.All(d => d.Id != known.NativeId));

        foreach (var definition in deletedDefinitions)
        {
            yield return definition;
        }
    }

    public async IAsyncEnumerable<IBaseBuild> RemovedBuilds()
    {
        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();
        var builds = await FetchMaxAmountOfBuilds(buildClient, project, _buildsPerGroup);

        var deletedBuilds = _knownBuilds.Where(known => builds.All(build => build.Id != known.BuildId));

        foreach (var build in deletedBuilds)
        {
            yield return build;
        }
    }

    public async Task UpdateBuilds(IEnumerable<IBaseBuild> builds)
    {
        var buildList = builds.OfType<TfsBuild>().ToList();

        var project = await GetProject();
        var buildClient = await _connection.GetClientAsync<BuildHttpClient>();

        await UpdateBuildTimeLines(buildList, buildClient, project);
        UpdateBuildLinks(buildList);
    }

    private readonly HashSet<TfsBuildDefinition> _knownDefinitions = new(new TfsBuildDefinitionComparer());
    private readonly HashSet<TfsBuild> _knownBuilds = new(new TfsBuildComparer());

    private readonly VssConnection _connection;
    private readonly Guid _projectId;

    private IUser? _user;
    private TeamProject? _project;
    private int _buildsPerGroup;

    private const int MaxBuildsAllowedByApi = 5000;
}