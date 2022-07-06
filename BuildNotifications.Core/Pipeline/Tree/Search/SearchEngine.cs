﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using BuildNotifications.PluginInterfaces.Builds.Search;

namespace BuildNotifications.Core.Pipeline.Tree.Search;

internal class SearchEngine : ISearchEngine
{
    public SearchEngine()
    {
        _defaultCriteria = new DefaultSearchCriteria(Enumerable.Empty<ISearchCriteria>(), Enumerable.Empty<ISearchCriteria>());
    }

    private void InvokeSearchParsed(string textInput, ISpecificSearch result) => SearchParsed?.Invoke(this, new SearchEngineEventArgs(result, textInput));

    private IEnumerable<ISearchBlock> ParseIntoBlocks(string textInput)
    {
        var sb = new StringBuilder();
        var currentCriteria = _defaultCriteria;

        foreach (var character in textInput)
        {
            sb.Append(character);

            if (character == SpecificToGeneralSeparator)
            {
                var enteredText = sb.ToString();

                // the separator is not part of the searched text
                sb.Remove(sb.Length - 1, 1);
                var searchedTerm = RemoveSpareSpaces(sb.ToString());

                yield return new SearchBlock(currentCriteria, enteredText, searchedTerm);
                currentCriteria = _defaultCriteria;
                sb.Clear();
                continue;
            }

            if (character != KeywordSeparator)
                continue;

            var asString = sb.ToString();
            var matchingCriteria = _searchCriteria.FirstOrDefault(c => asString.EndsWith($"{c.LocalizedKeyword(CultureInfo.CurrentCulture)}:", StringComparison.OrdinalIgnoreCase));

            if (matchingCriteria == null)
                continue;

            var keywordLength = $"{matchingCriteria.LocalizedKeyword(CultureInfo.CurrentCulture)}:".Length;

            sb.Remove(sb.Length - keywordLength, keywordLength);

            var textUntilKeyword = sb.ToString();
            yield return new SearchBlock(currentCriteria, textUntilKeyword, RemoveSpareSpaces(textUntilKeyword));

            sb.Clear();
            currentCriteria = matchingCriteria;
        }

        var enteredRest = sb.ToString();
        yield return new SearchBlock(currentCriteria, enteredRest, RemoveSpareSpaces(enteredRest));
    }

    private string RemoveSpareSpaces(string input) => string.Join(" ", input.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    public IReadOnlyList<ISearchCriteria> SearchCriterions => _searchCriteria;

    public void AddCriteria(ISearchCriteria criteria, bool includeInDefaultCriteria = true)
    {
        _searchCriteria.Add(criteria);

        if (!includeInDefaultCriteria)
            _ignoredCriterionsForDefaultSearch.Add(criteria);

        _defaultCriteria = new DefaultSearchCriteria(_searchCriteria, _ignoredCriterionsForDefaultSearch);
    }

    public ISpecificSearch Parse(string textInput)
    {
        var specificSearch = new SpecificSearch(ParseIntoBlocks(textInput), textInput);
        InvokeSearchParsed(textInput, specificSearch);
        return specificSearch;
    }

    public event EventHandler<SearchEngineEventArgs>? SearchParsed;

    private readonly List<ISearchCriteria> _searchCriteria = new();

    private readonly List<ISearchCriteria> _ignoredCriterionsForDefaultSearch = new();

    private ISearchCriteria _defaultCriteria;

    public const char KeywordSeparator = ':';

    public const char SpecificToGeneralSeparator = ',';
}