﻿using System.Collections.Generic;
using ReflectSettings.Attributes;

namespace BuildNotifications.Core.Config
{
    internal class ProjectConfiguration : IProjectConfiguration
    {
        public ProjectConfiguration()
        {
            BranchBlacklist = new List<string>();
            BranchWhitelist = new List<string>();
            BuildDefinitionBlacklist = new List<string>();
            BuildDefinitionWhitelist = new List<string>();
            BuildConnectionNames = new List<string>();
            SourceControlConnectionNames = new List<string>();

            ProjectName = string.Empty;
            DefaultCompareBranch = string.Empty;

            ShowPullRequests = true;
            HideCompletedPullRequests = true;
            LoadWhitelistedBranchesExclusively = false;
            LoadWhitelistedDefinitionsExclusively = false;
        }

        [IsDisplayName]
        public string ProjectName { get; set; }

        [TypesForInstantiation(typeof(List<string>))]
        public IList<string> BranchBlacklist { get; set; }
        
        [TypesForInstantiation(typeof(List<string>))]
        public IList<string> BranchWhitelist { get; set; }

        [CalculatedValues(nameof(Configuration.ConnectionNames), true)]
        public IList<string> BuildConnectionNames { get; set; }

        public IList<string> BuildDefinitionBlacklist { get; set; }
        
        [TypesForInstantiation(typeof(List<string>))]
        public IList<string> BuildDefinitionWhitelist { get; set; }

        public string DefaultCompareBranch { get; set; }

        public bool HideCompletedPullRequests { get; set; }

        public bool LoadWhitelistedBranchesExclusively { get; set; }

        public bool LoadWhitelistedDefinitionsExclusively { get; set; }

        public bool ShowPullRequests { get; set; }
        
        [CalculatedValues(nameof(Configuration.ConnectionNames), true)]
        public IList<string> SourceControlConnectionNames { get; set; }
    }
}