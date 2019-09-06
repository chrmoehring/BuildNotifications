﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using BuildNotifications.Core.Pipeline.Tree;
using BuildNotifications.Core.Pipeline.Tree.Arrangement;
using BuildNotifications.PluginInterfaces.Builds;
using BuildNotifications.ViewModel.Utils;

namespace BuildNotifications.ViewModel.Tree
{
    public abstract class BuildTreeNodeViewModel : BaseViewModel
    {
        protected BuildTreeNodeViewModel(IBuildTreeNode nodeSource)
        {
            NodeSource = nodeSource;
            Children = new RemoveTrackingObservableCollection<BuildTreeNodeViewModel>(TimeSpan.FromSeconds(0.8));
            Children.CollectionChanged += ChildrenOnCollectionChanged;
            SetChildrenSorting(_currentSortingDefinition);

            RemoveOneChildCommand = new DelegateCommand(RemoveOneChild);
            AddAndRemoveCommand = new DelegateCommand(RemoveOneChild);
            HighlightCommand = new DelegateCommand(Highlight);
            CurrentTreeLevelDepth = nodeSource.Depth;
        }

        public ICommand AddAndRemoveCommand { get; set; }

        public ICommand AddOneBuildCommand { get; set; }

        public BuildStatus BuildStatus => CalculateBuildStatus();

        public DateTime ChangedDate => CalculateChangedDate();

        public RemoveTrackingObservableCollection<BuildTreeNodeViewModel> Children { get; }

        public int CurrentTreeLevelDepth { get; private set; }

        public string DisplayName => CalculateDisplayName();

        public ICommand HighlightCommand { get; set; }

        public int MaxTreeDepth { get; set; }

        // object this ViewModel originates from
        public IBuildTreeNode NodeSource { get; }

        public ICommand RemoveOneChildCommand { get; set; }

        // only display status for the lowest and third lowest levels
        public bool ShouldColorByStatus
        {
            get
            {
                var thisLevelToDeepest = MaxTreeDepth - CurrentTreeLevelDepth;

                return thisLevelToDeepest == 0 || thisLevelToDeepest == 2;
            }
        }

        public virtual void BackendPropertiesChanged()
        {
            CurrentTreeLevelDepth = NodeSource.Depth;
        }

        protected virtual BuildStatus CalculateBuildStatus() => _buildStatus;

        private BuildStatus _buildStatus;

        private void UpdateBuildStatus()
        {
            BuildStatus newStatus;
            if (!Children.Any())
                newStatus = BuildStatus.None;
            else
            {
                if (ChildrenAreBuilds)
                    newStatus = MostCurrentBuildStatus(Children.OfType<BuildNodeViewModel>());
                else
                    newStatus = Children.ToList().Max(x => x.BuildStatus);
            }

            if (_buildStatus == newStatus)
                return;

            _buildStatus = newStatus;
            OnPropertyChanged(nameof(BuildStatus));
        }

        private static IList<BuildStatus> BuildStatusToIgnore { get; } = new List<BuildStatus> {BuildStatus.Cancelled, BuildStatus.Pending};

        private BuildStatus MostCurrentBuildStatus(IEnumerable<BuildNodeViewModel> buildNodes)
        {
            var byDateDescending = buildNodes.OrderByDescending(x => x.ChangedDate).ToList();
            var status = BuildStatus.None;

            foreach (var build in byDateDescending)
            {
                var buildStatus = build.BuildStatus;
                if (BuildStatusToIgnore.Contains(buildStatus))
                {
                    if (status == BuildStatus.None)
                        status = buildStatus;
                    continue;
                }

                status = buildStatus;
                break;
            }

            return status;
        }

        protected virtual DateTime CalculateChangedDate() => _changedDate;

        private DateTime _changedDate;

        private void UpdateChangedDate()
        {
            var newDate = !Children.Any() ? DateTime.MinValue : Children.ToList().Max(x => x.ChangedDate);
            if (_changedDate == newDate)
                return;

            _changedDate = newDate;
            OnPropertyChanged(nameof(ChangedDate));
        }

        protected virtual string CalculateDisplayName()
        {
            return ToString();
        }

        protected void SetSortings(List<SortingDefinition> sortingDefinitions, int index = 0)
        {
            if (ChildrenAreBuilds)
                return;

            foreach (var child in Children)
            {
                child.SetSortings(sortingDefinitions, index + 1);
            }

            if (index >= sortingDefinitions.Count)
                return;

            var newSorting = sortingDefinitions[index];

            if (newSorting == _currentSortingDefinition)
                return;

            _currentSortingDefinition = newSorting;
            SetChildrenSorting(_currentSortingDefinition);
        }

        protected bool ChildrenAreBuilds { get; private set; }

        private void ChildrenOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (BuildTreeNodeViewModel child in e.NewItems)
                    {
                        child.PropertyChanged += OnChildPropertyChanged;
                        if (child is BuildNodeViewModel)
                            ChildrenAreBuilds = true;
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (BuildTreeNodeViewModel child in e.OldItems)
                    {
                        child.PropertyChanged -= OnChildPropertyChanged;
                    }

                    if (!Children.Any())
                        ChildrenAreBuilds = false;
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var child in Children)
                    {
                        child.PropertyChanged -= OnChildPropertyChanged;
                    }

                    if (!Children.Any())
                        ChildrenAreBuilds = false;
                    break;

                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (ChildrenAreBuilds)
                        SetSpecificBuildChildrenProperties();
                    return;
                default:
                    return;
            }

            if (ChildrenAreBuilds)
                SetSpecificBuildChildrenProperties();

            UpdateBuildStatus();
            UpdateChangedDate();
        }

        private void Highlight(object obj)
        {
            bool? targetValue = null;
            foreach (var build in Children.OfType<BuildNodeViewModel>())
            {
                if (targetValue == null)
                    targetValue = !build.IsHighlighted;

                build.IsHighlighted = targetValue.Value;
            }
        }

        private void OnChildPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(BuildStatus)))
            {
                UpdateBuildStatus();
                if (_currentSortingDefinition == SortingDefinition.StatusAscending || _currentSortingDefinition == SortingDefinition.StatusDescending)
                    Children.InvokeSort();
            }

            if (e.PropertyName.Equals(nameof(ChangedDate)))
            {
                UpdateChangedDate();
                if (_currentSortingDefinition == SortingDefinition.DateAscending || _currentSortingDefinition == SortingDefinition.DateDescending)
                    Children.InvokeSort();
            }
        }

        private void RemoveOneChild(object parameter)
        {
            var someBuild = Children.FirstOrDefault(x => !x.IsRemoving);
            if (someBuild != null)
                Children.Remove(someBuild);
        }

        private void SetSpecificBuildChildrenProperties()
        {
            var buildChildren = Children.OfType<BuildNodeViewModel>().ToList();

            if (!buildChildren.Any())
            {
                ChildrenAreBuilds = false;
                return;
            }

            SetBuildLargeStatus(buildChildren);
            SetBuildHollowStatus(buildChildren);
        }

        private void SetBuildLargeStatus(IReadOnlyCollection<BuildNodeViewModel> buildChildren)
        {
            foreach (var child in buildChildren)
            {
                child.IsLargeSize = false;
            }

            buildChildren.Last().IsLargeSize = true;
        }

        private void SetBuildHollowStatus(IReadOnlyCollection<BuildNodeViewModel> buildChildren)
        {
            foreach (var child in buildChildren)
            {
                child.DisplayAsHollow = false;
            }

            var reversed = buildChildren.ToList();
            reversed.Reverse();

            var failedOrInconclusiveBuilds = new List<BuildNodeViewModel>();
            var latestBuildsDidFail = false;

            foreach (var build in reversed)
            {
                var status = build.BuildStatus;

                if (status == BuildStatus.Failed)
                {
                    failedOrInconclusiveBuilds.Add(build);
                    latestBuildsDidFail = true;
                    continue;
                }

                if (status == BuildStatus.Cancelled)
                {
                    failedOrInconclusiveBuilds.Add(build);
                    continue;
                }

                break;
            }

            if (!latestBuildsDidFail)
                return;

            foreach (var build in failedOrInconclusiveBuilds)
            {
                build.DisplayAsHollow = true;
            }
        }

        private void SetChildrenSorting(SortingDefinition sortingDefinition)
        {
            switch (sortingDefinition)
            {
                case SortingDefinition.AlphabeticalDescending:
                    Children.Sort(x => x.DisplayName);
                    break;
                case SortingDefinition.AlphabeticalAscending:
                    Children.SortDescending(x => x.DisplayName);
                    break;
                case SortingDefinition.StatusAscending:
                    Children.Sort(x => x.BuildStatus);
                    break;
                case SortingDefinition.StatusDescending:
                    Children.SortDescending(x => x.BuildStatus);
                    break;
                case SortingDefinition.DateAscending:
                    Children.Sort(x => x.ChangedDate);
                    break;
                case SortingDefinition.DateDescending:
                    Children.SortDescending(x => x.ChangedDate);
                    break;
            }
        }

        private SortingDefinition _currentSortingDefinition = SortingDefinition.DateAscending;
    }
}