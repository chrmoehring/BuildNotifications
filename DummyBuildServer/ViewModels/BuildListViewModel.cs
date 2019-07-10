﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using BuildNotifications.Plugin.DummyBuildServer;
using BuildNotifications.PluginInterfaces.Builds;
using JetBrains.Annotations;

namespace DummyBuildServer.ViewModels
{
    internal class BuildListViewModel : ViewModelBase
    {
        public BuildListViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            SelectedBranch = mainViewModel.Branches.Branches.FirstOrDefault();
            SelectedUser = mainViewModel.Users.Users.FirstOrDefault();
            SelectedDefinition = mainViewModel.BuildDefinitions.Definitions.FirstOrDefault();

            UpdateBuildCommand = new DelegateCommand(UpdateBuild, IsBuildSelected);
            EnqueueBuildCommand = new DelegateCommand(EnqueueBuild, IsBuildDataSelected);
            RemoveBuildCommand = new DelegateCommand(RemoveBuild, IsBuildSelected);
        }

        public IEnumerable<BuildStatus> AvailableBuildStatuses
        {
            get
            {
                yield return BuildStatus.Pending;
                yield return BuildStatus.Running;
                yield return BuildStatus.Cancelled;
                yield return BuildStatus.Succeeded;
                yield return BuildStatus.PartiallySucceeded;
                yield return BuildStatus.Failed;
            }
        }

        public int BuildProgress { get; set; }
        public ObservableCollection<BuildViewModel> Builds { get; } = new ObservableCollection<BuildViewModel>();
        public ICommand EnqueueBuildCommand { get; }
        public ICommand RemoveBuildCommand { get; }
        public BranchViewModel SelectedBranch { get; set; }
        public BuildViewModel? SelectedBuild { get; set; }
        public BuildStatus SelectedBuildStatus { get; set; }
        public BuildDefinitionViewModel SelectedDefinition { get; set; }
        public UserViewModel SelectedUser { get; set; }
        public ICommand UpdateBuildCommand { get; }

        private void EnqueueBuild(object arg)
        {
            var user = SelectedUser.User;
            var branch = SelectedBranch.Branch;
            var definition = SelectedDefinition.Definition;

            var build = new Build
            {
                LastChangedTime = DateTime.Now,
                QueueTime = DateTime.Now,
                Definition = definition,
                BranchName = branch.Name,
                RequestedBy = user,
                Status = BuildStatus.Pending,
                Id = (++_idCounter).ToString()
            };

            _mainViewModel.AddBuild(build);
            Builds.Add(new BuildViewModel(build));
        }

        private bool IsBuildDataSelected(object arg)
        {
            return SelectedBranch != null && SelectedDefinition != null && SelectedUser != null;
        }

        private bool IsBuildSelected(object arg)
        {
            return SelectedBuild != null;
        }

        [UsedImplicitly]
        private void OnSelectedBuildChanged()
        {
            BuildProgress = SelectedBuild!.Progress;
            SelectedBuildStatus = SelectedBuild.Build.Status;
        }

        private void RemoveBuild(object obj)
        {
            _mainViewModel.RemoveBuild(SelectedBuild!.Build);
            Builds.Remove(SelectedBuild);

            SelectedBuild = null;
        }

        private void UpdateBuild(object arg)
        {
            var build = SelectedBuild!.Build;
            build.Status = SelectedBuildStatus;
            build.Progress = BuildProgress;
            build.LastChangedTime = DateTime.Now;

            _mainViewModel.UpdateBuild(build);
        }

        private readonly MainViewModel _mainViewModel;

        private static int _idCounter;
    }
}