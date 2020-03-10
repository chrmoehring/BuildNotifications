﻿using System;
using BuildNotifications.PluginInterfaces.Builds;

namespace BuildNotifications.Core.Pipeline.Tree
{
    internal class BuildNode : TreeNode, IBuildNode
    {
        public BuildNode(IBuild build)
        {
            Build = build;

            UpdateProperties(build);
        }

        private void UpdateProperties(IBuild otherBuild)
        {
            LastChangedTime = otherBuild.LastChangedTime;
            QueueTime = otherBuild.QueueTime;
            Status = otherBuild.Status;
            Progress = otherBuild.Progress;
        }

        public DateTime? LastChangedTime { get; private set; }
        public DateTime? QueueTime { get; private set; }
        public int Progress { get; private set; }
        public BuildStatus Status { get; private set; }

        public IBuild Build { get; }

        public override void UpdateWithValuesFrom(IBuildTreeNode nodeToInsert)
        {
            var otherBuild = (nodeToInsert as IBuildNode)?.Build;
            if (otherBuild == null)
                return;

            UpdateProperties(otherBuild);
        }

        public override bool Equals(IBuildTreeNode other)
        {
            return base.Equals(other) && Build.Id.Equals((other as BuildNode)?.Build?.Id, StringComparison.InvariantCulture);
        }
    }
}