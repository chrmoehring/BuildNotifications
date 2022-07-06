﻿using BuildNotifications.Core.Pipeline.Tree;

namespace BuildNotifications.ViewModel.Tree;

public class BranchGroupNodeViewModel : BuildTreeNodeViewModel
{
    public BranchGroupNodeViewModel(IBranchGroupNode node)
        : base(node)
    {
        _node = node;
    }

    public string BranchName => _node.BranchName;
    public bool IsPullRequest => _node.IsPullRequest;

    protected override string CalculateDisplayName() => BranchName;

    private readonly IBranchGroupNode _node;
}