﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BuildNotifications.PluginInterfaces.SourceControl;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace BuildNotifications.Plugin.Tfs.SourceControl;

internal class TfsSourceControlProvider : IBranchProvider
{
    public TfsSourceControlProvider(VssConnection connection, Guid repositoryId, Guid projectId)
    {
        _connection = connection;
        _projectId = projectId;
        _repositoryId = repositoryId;
    }

    private TfsBranch Convert(GitRef branch, TfsUrlBuilder urlBuilder) => new(branch, urlBuilder);

    private TfsPullRequests Convert(GitPullRequest branch, TfsUrlBuilder urlBuilder) => new(branch, urlBuilder);

    private async Task<List<GitPullRequest>> FetchPullRequests(GitHttpClient gitClient)
    {
        var searchCriteria = new GitPullRequestSearchCriteria
        {
            RepositoryId = _repositoryId,
            Status = PullRequestStatus.Active
        };
        var prs = await gitClient.GetPullRequestsByProjectAsync(_projectId, searchCriteria);
        return prs;
    }

    private async Task<TfsUrlBuilder> FetchUrlBuilder()
    {
        var projectClient = await _connection.GetClientAsync<ProjectHttpClient>();
        var project = await projectClient.GetProject(_projectId.ToString());

        return new TfsUrlBuilder(projectClient.BaseAddress, project.Name);
    }

    public int ExistingBranchCount => _knownBranches.Count;
    public IBranchNameExtractor NameExtractor { get; } = new GitBranchNameExtractor();

    public async IAsyncEnumerable<IBranch> FetchExistingBranches()
    {
        var urlBuilder = await FetchUrlBuilder();

        var gitClient = await _connection.GetClientAsync<GitHttpClient>();
        var branches = await gitClient.GetRefsAsync(_projectId, _repositoryId, "heads/", true);

        foreach (var branch in branches)
        {
            var converted = Convert(branch, urlBuilder);
            _knownBranches.Add(converted);
            yield return converted;
        }

        var pullRequests = await FetchPullRequests(gitClient);

        foreach (var pullRequest in pullRequests)
        {
            var converted = Convert(pullRequest, urlBuilder);
            _knownBranches.Add(converted);
            yield return converted;
        }
    }

    public async IAsyncEnumerable<IBranch> RemovedBranches()
    {
        var gitClient = await _connection.GetClientAsync<GitHttpClient>();
        var branches = await gitClient.GetBranchRefsAsync(_repositoryId);
        var pullRequests = await FetchPullRequests(gitClient);

        var names = branches.Select(b => b.Name).Concat(pullRequests.Select(p => TfsBranch.ComputePullRequestBranchName(p.PullRequestId)));
        var deletedBranches = _knownBranches.Where(known => names.All(n => known.FullName != n));

        foreach (var branch in deletedBranches)
        {
            yield return branch;
        }
    }

    private readonly HashSet<TfsBranch> _knownBranches = new(new TfsBranchComparer());

    private readonly VssConnection _connection;
    private readonly Guid _projectId;
    private readonly Guid _repositoryId;
}