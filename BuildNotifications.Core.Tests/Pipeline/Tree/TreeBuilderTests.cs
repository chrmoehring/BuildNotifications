﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuildNotifications.Core.Config;
using BuildNotifications.Core.Pipeline.Cache;
using BuildNotifications.Core.Pipeline.Tree;
using BuildNotifications.Core.Pipeline.Tree.Arrangement;
using BuildNotifications.PluginInterfaces.Builds;
using BuildNotifications.PluginInterfaces.SourceControl;
using NSubstitute;
using Xunit;

namespace BuildNotifications.Core.Tests.Pipeline.Tree;

public class TreeBuilderTests
{
    internal static TreeBuilder Construct(params GroupDefinition[] definitions)
    {
        var groupDefinition = new BuildTreeGroupDefinition(definitions);

        var config = Substitute.For<IConfiguration>();
        config.GroupDefinition.Returns(groupDefinition);

        return new TreeBuilder(config);
    }

    private IBuild CreateBuild(IBuildDefinition definition, IBranch branch, string id)
    {
        var build = Substitute.For<IBuild>();
        build.Definition.Returns(definition);
        build.BranchName.Returns(branch.FullName);
        build.Id.Returns(id);

        return build;
    }

    [Fact]
    public void BuildShouldCreateEmptyTreeWhenDefinitionIsEmpty()
    {
        // Arrange
        var sut = Construct();

        var builds = Enumerable.Empty<IBuild>();

        // Act
        var actual = sut.Build(builds);

        // Assert
        Assert.Empty(actual.Children);
    }

    [Theory]
    [InlineData(new[] { GroupDefinition.Branch, GroupDefinition.BuildDefinition, GroupDefinition.Status })]
    [InlineData(new[] { GroupDefinition.BuildDefinition, GroupDefinition.Branch, GroupDefinition.Status })]
    public void BuildShouldCreateEmptyTreeWhenInputsAreEmpty(GroupDefinition[] groupDefinitions)
    {
        // Arrange
        var sut = Construct(groupDefinitions);

        var builds = Enumerable.Empty<IBuild>();

        // Act
        var actual = sut.Build(builds);

        // Assert
        Assert.Empty(actual.Children);
    }

    [Theory]
    [InlineData(GroupDefinition.Branch)]
    [InlineData(GroupDefinition.BuildDefinition)]
    [InlineData(GroupDefinition.None)]
    public void BuildShouldCreateFlatListWhenGroupingByOneCriteria(GroupDefinition grouping)
    {
        // Arrange
        var sut = Construct(grouping, GroupDefinition.Status);

        var definition = Substitute.For<IBuildDefinition>();

        var builds = new List<IBuild>();

        var b1 = Substitute.For<IBuild>();
        b1.Definition.Returns(definition);
        b1.BranchName.Returns("branch");
        builds.Add(b1);

        // Act
        var actual = sut.Build(builds);

        // Assert
        var expectedCount = builds.Count;
        Assert.Equal(expectedCount, actual.Children.Count());
    }

    [Fact]
    public void BuildTreeShouldMatchGroupDefinition()
    {
        // Arrange
        var sut = Construct(GroupDefinition.Source, GroupDefinition.Branch, GroupDefinition.BuildDefinition);
        var nightlyDefinition = Substitute.For<IBuildDefinition>();
        var builds = new List<IBuild>();

        var b1 = Substitute.For<IBuild>();
        b1.Definition.Returns(nightlyDefinition);
        b1.BranchName.Returns("stage");
        builds.Add(b1);

        // Act
        var actual = sut.Build(builds);

        // Assert
        var parser = new BuildTreeParser(actual);

        Assert.All(parser.ChildrenAtLevel(0), x => Assert.IsAssignableFrom<IBuildTree>(x));
        Assert.All(parser.ChildrenAtLevel(1), x => Assert.IsAssignableFrom<ISourceGroupNode>(x));
        Assert.All(parser.ChildrenAtLevel(2), x => Assert.IsAssignableFrom<IBranchGroupNode>(x));
        Assert.All(parser.ChildrenAtLevel(3), x => Assert.IsAssignableFrom<IDefinitionGroupNode>(x));
        Assert.All(parser.ChildrenAtLevel(4), x => Assert.IsAssignableFrom<IBuildNode>(x));
        Assert.NotEmpty(parser.ChildrenAtLevel(4));
    }

    [Fact]
    public void BuildTreeWithMultipleBuildsChangingStatusShouldCreateDelta()
    {
        // Arrange
        var sut = Construct(GroupDefinition.Source, GroupDefinition.Branch, GroupDefinition.BuildDefinition);

        var ciDefinition = Substitute.For<IBuildDefinition>();
        var stageBranch = Substitute.For<IBranch>();
        var builds = new List<IBuild>();

        var build1 = CreateBuild(ciDefinition, stageBranch, "1");
        builds.Add(build1);

        var build2 = CreateBuild(ciDefinition, stageBranch, "2");
        builds.Add(build2);

        var build3 = CreateBuild(ciDefinition, stageBranch, "3");
        builds.Add(build3);

        // Act
        var firstResult = sut.Build(builds);
        var newBuild1 = CreateBuild(ciDefinition, stageBranch, "1");
        newBuild1.Status.Returns(BuildStatus.Failed);

        var newBuild2 = CreateBuild(ciDefinition, stageBranch, "2");
        newBuild2.Status.Returns(BuildStatus.Succeeded);

        var newBuild3 = CreateBuild(ciDefinition, stageBranch, "3");
        newBuild3.Status.Returns(BuildStatus.Cancelled);

        var updatedBuilds = new List<IBuild> { newBuild1, newBuild2, newBuild3 };
        var newTree = sut.Build(updatedBuilds);
        var currentBuildNodes = newTree.AllChildren().OfType<IBuildNode>();

        var oldStatus = firstResult.AllChildren().OfType<IBuildNode>().ToDictionary(x => x.Build.CacheKey(), x => x.Status);
        var delta = new BuildTreeBuildsDelta(currentBuildNodes, oldStatus, PartialSucceededTreatmentMode.TreatAsSucceeded);

        // Assert
        Assert.Single(delta.Failed);
        Assert.Single(delta.Cancelled);
        Assert.Single(delta.Succeeded);
    }

    [Fact]
    public void BuildTreeWithNoneBuildsChangingStatusShouldCreateEmptyDelta()
    {
        // Arrange
        var sut = Construct(GroupDefinition.Source, GroupDefinition.Branch, GroupDefinition.BuildDefinition);

        var ciDefinition = Substitute.For<IBuildDefinition>();
        var stageBranch = Substitute.For<IBranch>();

        var builds = new List<IBuild>();

        var build1 = CreateBuild(ciDefinition, stageBranch, "1");
        build1.Status.Returns(BuildStatus.Failed);
        builds.Add(build1);

        var build2 = CreateBuild(ciDefinition, stageBranch, "2");
        build2.Status.Returns(BuildStatus.Succeeded);
        builds.Add(build2);

        var build3 = CreateBuild(ciDefinition, stageBranch, "3");
        build3.Status.Returns(BuildStatus.Cancelled);
        builds.Add(build3);

        // Act
        var firstResult = sut.Build(builds);
        var newBuild1 = CreateBuild(ciDefinition, stageBranch, "1");
        newBuild1.Status.Returns(BuildStatus.Failed);

        var newBuild2 = CreateBuild(ciDefinition, stageBranch, "2");
        newBuild2.Status.Returns(BuildStatus.Succeeded);

        var newBuild3 = CreateBuild(ciDefinition, stageBranch, "3");
        newBuild3.Status.Returns(BuildStatus.Cancelled);

        var updatedBuilds = new List<IBuild> { newBuild1, newBuild2, newBuild3 };
        var newTree = sut.Build(updatedBuilds);
        var currentBuildNodes = newTree.AllChildren().OfType<IBuildNode>();

        var oldStatus = firstResult.AllChildren().OfType<IBuildNode>().ToDictionary(x => x.Build.CacheKey(), x => x.Status);
        var delta = new BuildTreeBuildsDelta(currentBuildNodes, oldStatus, PartialSucceededTreatmentMode.TreatAsSucceeded);

        // Assert
        Assert.Empty(delta.Failed);
        Assert.Empty(delta.Cancelled);
        Assert.Empty(delta.Succeeded);
    }

    [Theory]
    [InlineData(BuildStatus.Succeeded)]
    [InlineData(BuildStatus.PartiallySucceeded)]
    [InlineData(BuildStatus.Failed)]
    [InlineData(BuildStatus.Cancelled)]
    public void BuildTreeWithOneBuildWithUpdatedStatusShouldCreateDelta(BuildStatus expectedResult)
    {
        // Arrange
        var sut = Construct(GroupDefinition.Source, GroupDefinition.Branch, GroupDefinition.BuildDefinition);

        var ciDefinition = Substitute.For<IBuildDefinition>();
        var stageBranch = Substitute.For<IBranch>();

        var builds = new List<IBuild>();

        var build1 = CreateBuild(ciDefinition, stageBranch, "1");
        builds.Add(build1);

        var build2 = CreateBuild(ciDefinition, stageBranch, "2");
        builds.Add(build2);

        var build3 = CreateBuild(ciDefinition, stageBranch, "3");
        builds.Add(build3);

        // Act
        var firstResult = sut.Build(builds);
        var newBuild2 = CreateBuild(ciDefinition, stageBranch, "2");
        newBuild2.Status.Returns(expectedResult);

        var updatedBuilds = new List<IBuild> { build1, newBuild2, build3 };
        var newTree = sut.Build(updatedBuilds);
        var currentBuildNodes = newTree.AllChildren().OfType<IBuildNode>();

        var oldStatus = firstResult.AllChildren().OfType<IBuildNode>().ToDictionary(x => x.Build.CacheKey(), x => x.Status);
        var delta = new BuildTreeBuildsDelta(currentBuildNodes, oldStatus, PartialSucceededTreatmentMode.TreatAsSucceeded);

        // Assert
        switch (expectedResult)
        {
            case BuildStatus.Cancelled:
                Assert.Single(delta.Cancelled);
                break;
            case BuildStatus.Succeeded:
            case BuildStatus.PartiallySucceeded:
                Assert.Single(delta.Succeeded);
                break;
            case BuildStatus.Failed:
                Assert.Single(delta.Failed);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(expectedResult), expectedResult, null);
        }
    }

    [Theory]
    [InlineData(BuildStatus.Succeeded, PartialSucceededTreatmentMode.Ignore)]
    [InlineData(BuildStatus.PartiallySucceeded, PartialSucceededTreatmentMode.TreatAsSucceeded)]
    [InlineData(BuildStatus.PartiallySucceeded, PartialSucceededTreatmentMode.TreatAsFailed)]
    [InlineData(BuildStatus.PartiallySucceeded, PartialSucceededTreatmentMode.Ignore)]
    [InlineData(BuildStatus.Failed, PartialSucceededTreatmentMode.Ignore)]
    [InlineData(BuildStatus.Cancelled, PartialSucceededTreatmentMode.Ignore)]
    public void BuildTreeWithUpdatesStatusShouldNotProduceDeltaForDifferentStatus(BuildStatus expectedResult, PartialSucceededTreatmentMode partialSucceededTreatmentMode)
    {
        // Arrange
        var sut = Construct(GroupDefinition.Source, GroupDefinition.Branch, GroupDefinition.BuildDefinition);

        var ciDefinition = Substitute.For<IBuildDefinition>();
        var stageBranch = Substitute.For<IBranch>();

        var builds = new List<IBuild>();

        var build1 = CreateBuild(ciDefinition, stageBranch, "1");
        builds.Add(build1);

        var build2 = CreateBuild(ciDefinition, stageBranch, "2");
        builds.Add(build2);

        var build3 = CreateBuild(ciDefinition, stageBranch, "3");
        builds.Add(build3);

        // Act
        var firstResult = sut.Build(builds);
        var newBuild2 = CreateBuild(ciDefinition, stageBranch, "2");
        newBuild2.Status.Returns(expectedResult);

        var updatedBuilds = new List<IBuild> { build1, newBuild2, build3 };
        var newTree = sut.Build(updatedBuilds);
        var currentBuildNodes = newTree.AllChildren().OfType<IBuildNode>();

        var oldStatus = firstResult.AllChildren().OfType<IBuildNode>().ToDictionary(x => x.Build.CacheKey(), x => x.Status);
        var delta = new BuildTreeBuildsDelta(currentBuildNodes, oldStatus, partialSucceededTreatmentMode);

        // Assert
        switch (expectedResult)
        {
            case BuildStatus.Cancelled:
                Assert.Empty(delta.Succeeded);
                Assert.Empty(delta.Failed);
                break;
            case BuildStatus.Succeeded:
                Assert.Empty(delta.Failed);
                Assert.Empty(delta.Cancelled);
                break;
            case BuildStatus.PartiallySucceeded:
                if (partialSucceededTreatmentMode == PartialSucceededTreatmentMode.TreatAsSucceeded)
                {
                    Assert.Empty(delta.Failed);
                    Assert.Empty(delta.Cancelled);
                }
                else if (partialSucceededTreatmentMode == PartialSucceededTreatmentMode.TreatAsFailed)
                {
                    Assert.Empty(delta.Succeeded);
                    Assert.Empty(delta.Cancelled);
                }
                else
                {
                    Assert.Empty(delta.Succeeded);
                    Assert.Empty(delta.Failed);
                    Assert.Empty(delta.Cancelled);
                }

                break;
            case BuildStatus.Failed:
                Assert.Empty(delta.Succeeded);
                Assert.Empty(delta.Cancelled);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(expectedResult), expectedResult, null);
        }
    }
}