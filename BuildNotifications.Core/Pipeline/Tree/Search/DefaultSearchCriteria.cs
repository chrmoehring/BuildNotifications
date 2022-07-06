﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BuildNotifications.Core.Text;
using BuildNotifications.PluginInterfaces.Builds;
using BuildNotifications.PluginInterfaces.Builds.Search;

namespace BuildNotifications.Core.Pipeline.Tree.Search;

/// <summary>
/// Searches through multiple given criteria. If one matches, the build is included.
/// </summary>
public class DefaultSearchCriteria : ISearchCriteria
{
    public DefaultSearchCriteria(IEnumerable<ISearchCriteria> includedCriterions, IEnumerable<ISearchCriteria> ignoredCriterionsForBuildInclusion)
    {
        _ignoredCriterionsForBuildInclusion = ignoredCriterionsForBuildInclusion;
        _includedCriterions = includedCriterions.ToList();
        _criterionsAsSuggestions = _includedCriterions.Select(s => new SearchCriteriaAsSuggestion(s)).ToList();
    }

    public int MaxSuggestions { get; set; } = 5;

    public int SuggestionsToTakeFromEachCriteria { get; set; } = 2;

    public IEnumerable<(string keyword, string exampleTerm)> ExamplesFromEachSubCriteria()
    {
        foreach (var searchCriteria in _includedCriterions)
        {
            foreach (var example in searchCriteria.LocalizedExamples.Take(ExamplesToTakeFromEachCriteria))
            {
                yield return (searchCriteria.LocalizedKeyword(CultureInfo.CurrentCulture), example);
            }
        }
    }

    private IEnumerable<ISearchCriteriaSuggestion> SuggestionsFromEachCriteria(string input)
    {
        foreach (var searchCriteriaSuggestion in _criterionsAsSuggestions)
        {
            if (string.IsNullOrEmpty(input) || searchCriteriaSuggestion.Suggestion.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
                yield return searchCriteriaSuggestion;
        }

        foreach (var searchCriteria in _includedCriterions)
        {
            foreach (var suggestion in searchCriteria.Suggest(input).Take(SuggestionsToTakeFromEachCriteria))
            {
                yield return suggestion;
            }
        }
    }

    string ISearchCriteria.LocalizedKeyword(CultureInfo forCulture) => string.Empty;

    string ISearchCriteria.LocalizedDescription(CultureInfo forCulture) => StringLocalizer.SearchDefaultDescription;

    public IEnumerable<ISearchCriteriaSuggestion> Suggest(string input) => SuggestionsFromEachCriteria(input).Distinct().Take(MaxSuggestions);

    public bool IsBuildIncluded(IBuild build, string input) => string.IsNullOrWhiteSpace(input) || _includedCriterions.Except(_ignoredCriterionsForBuildInclusion).Any(c => c.IsBuildIncluded(build, input));

    public IEnumerable<string> LocalizedExamples => ExamplesFromEachSubCriteria().Select(t => t.exampleTerm);
    private readonly IEnumerable<ISearchCriteria> _ignoredCriterionsForBuildInclusion;

    private readonly IReadOnlyList<ISearchCriteria> _includedCriterions;

    private readonly IReadOnlyList<ISearchCriteriaSuggestion> _criterionsAsSuggestions;

    private const int ExamplesToTakeFromEachCriteria = 1;
}