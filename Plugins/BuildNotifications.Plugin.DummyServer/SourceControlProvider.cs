﻿using System.Collections.Generic;
using System.Linq;
using BuildNotifications.PluginInterfaces.SourceControl;

namespace BuildNotifications.Plugin.DummyServer;

internal class SourceControlProvider : IBranchProvider
{
    public SourceControlProvider(Connection connection)
    {
        _connection = connection;
    }

    public int ExistingBranchCount => _knownBranches.Count;
    public IBranchNameExtractor NameExtractor => new BranchNameExtractor();

    public async IAsyncEnumerable<IBranch> FetchExistingBranches()
    {
        var branches = await _connection.Get<IEnumerable<Branch>>(BranchesPath);

        foreach (var branch in branches)
        {
            _knownBranches.Add(branch);

            yield return branch;
        }
    }

    public async IAsyncEnumerable<IBranch> RemovedBranches()
    {
        var branches = await _connection.Get<IEnumerable<Branch>>(BranchesPath);

        var deletedBranches = _knownBranches.Except(branches, new BranchComparer());

        foreach (var branch in deletedBranches)
        {
            yield return branch;
        }
    }

    private readonly HashSet<Branch> _knownBranches = new(new BranchComparer());
    private readonly Connection _connection;
    private const string BranchesPath = "branch";
}