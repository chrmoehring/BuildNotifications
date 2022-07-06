﻿using System.Globalization;
using BuildNotifications.PluginInterfaces.Builds;
using BuildNotifications.PluginInterfaces.Builds.Search;

namespace BuildNotifications.Core.Pipeline.Tree.Search;

internal class SearchBlock : ISearchBlock
{
    public SearchBlock(ISearchCriteria searchCriteria, string enteredText, string searchedTerm)
    {
        SearchCriteria = searchCriteria;
        EnteredText = enteredText;
        SearchedTerm = searchedTerm;
    }

    public override string ToString() => $"{SearchCriteria.LocalizedKeyword(CultureInfo.CurrentCulture)}: {EnteredText}";
    public ISearchCriteria SearchCriteria { get; }

    public string SearchedTerm { get; }

    public string EnteredText { get; }

    public bool IsBuildIncluded(IBuild build) => SearchCriteria.IsBuildIncluded(build, SearchedTerm);
}