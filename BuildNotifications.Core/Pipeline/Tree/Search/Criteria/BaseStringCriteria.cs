﻿using System;
using System.Collections.Generic;
using System.Linq;
using BuildNotifications.Core.Config;
using BuildNotifications.PluginInterfaces.Builds;

namespace BuildNotifications.Core.Pipeline.Tree.Search.Criteria;

internal abstract class BaseStringCriteria : BaseSearchCriteria
{
    protected BaseStringCriteria(IPipeline pipeline)
        : base(pipeline)
    {
    }

    protected override bool IsBuildIncludedInternal(IBuild build, string input)
    {
        if (!StringMatcher.SearchPattern.Equals(input, StringComparison.InvariantCulture))
            StringMatcher.SearchPattern = input;

        return StringMatcher.IsMatch(StringValueOfBuild(build));
    }

    protected abstract IEnumerable<string> ResolveAllPossibleStringValues(IPipeline pipeline);

    protected abstract string StringValueOfBuild(IBuild build);

    protected override IEnumerable<string> SuggestInternal(string input, StringMatcher stringMatcher)
    {
        return _validValues.Where(stringMatcher.IsMatch).OrderBy(k => _stringComparer.Compare(input, k));
    }

    protected override void UpdateCacheForSuggestions(IPipeline pipeline)
    {
        _validValues.Clear();
        foreach (var value in ResolveAllPossibleStringValues(pipeline))
        {
            _validValues.Add(value);
        }
    }

    protected readonly StringMatcher StringMatcher = new();

    private readonly HashSet<string> _validValues = new();

    private readonly StringComparer _stringComparer = StringComparer.FromComparison(StringComparison.CurrentCultureIgnoreCase);
}