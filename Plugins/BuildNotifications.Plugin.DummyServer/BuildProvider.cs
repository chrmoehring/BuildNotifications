﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildNotifications.PluginInterfaces;
using BuildNotifications.PluginInterfaces.Builds;

namespace BuildNotifications.Plugin.DummyServer;

internal class BuildProvider : IBuildProvider
{
    public BuildProvider(Connection connection)
    {
        _connection = connection;
    }

    public IUser User => new User("Me");

    public async IAsyncEnumerable<IBaseBuild> FetchAllBuilds(int buildsPerGroup)
    {
        var builds = await _connection.Get<IEnumerable<Build>>(BuildPath);

        foreach (var build in builds)
        {
            _knownBuilds.Add(build);

            yield return build;
        }
    }

    public async IAsyncEnumerable<IBaseBuild> FetchBuildsForDefinition(IBuildDefinition definition)
    {
        var builds = await _connection.Get<IEnumerable<Build>>(BuildPath);

        foreach (var build in builds)
        {
            _knownBuilds.Add(build);

            if (build.Definition.Equals(definition))
                yield return build;
        }
    }

    public async IAsyncEnumerable<IBaseBuild> FetchBuildsChangedSince(DateTime date)
    {
        var builds = await _connection.Get<IEnumerable<Build>>(BuildPath);

        foreach (var build in builds)
        {
            _knownBuilds.Add(build);

            if (!build.LastChangedTime.HasValue || build.LastChangedTime > date)
                yield return build;
        }
    }

    public async IAsyncEnumerable<IBuildDefinition> FetchExistingBuildDefinitions()
    {
        var definitions = await _connection.Get<IEnumerable<BuildDefinition>>(DefinitionPath);

        foreach (var buildDefinition in definitions)
        {
            _knownBuildDefinitions.Add(buildDefinition);

            yield return buildDefinition;
        }
    }

    public async IAsyncEnumerable<IBuildDefinition> RemovedBuildDefinitions()
    {
        var definitions = await _connection.Get<IEnumerable<BuildDefinition>>(DefinitionPath);

        var deletedDefinitions = _knownBuildDefinitions.Except(definitions, new BuildComparer());

        foreach (var definition in deletedDefinitions)
        {
            yield return definition;
        }
    }

    public async IAsyncEnumerable<IBaseBuild> RemovedBuilds()
    {
        var builds = await _connection.Get<IEnumerable<Build>>(BuildPath);

        var deletedBuilds = _knownBuilds.Except(builds, new BuildComparer());

        foreach (var build in deletedBuilds)
        {
            yield return build;
        }
    }

    public Task UpdateBuilds(IEnumerable<IBaseBuild> builds) => Task.CompletedTask;

    private readonly HashSet<BuildDefinition> _knownBuildDefinitions = new(new BuildComparer());
    private readonly HashSet<Build> _knownBuilds = new(new BuildComparer());

    private readonly Connection _connection;
    private const string BuildPath = "build/extern";
    private const string DefinitionPath = "definition";
}