﻿using System;
using Microsoft.TeamFoundation.SourceControl.WebApi;

namespace BuildNotifications.Plugin.Tfs.SourceControl;

public class TfsRepository
{
    public TfsRepository(GitRepository gitRepository)
    {
        RepositoryName = gitRepository.Name;
        Id = gitRepository.Id.ToString();
    }

    public TfsRepository()
    {
        Id = string.Empty;
    }

    public string Id { get; set; }

    public string? RepositoryName { get; set; }

    public override bool Equals(object? obj)
    {
        var other = obj as TfsRepository;
        return other?.Id.Equals(Id, StringComparison.InvariantCulture) == true;
    }

    public override int GetHashCode() =>
        // ReSharper disable once NonReadonlyMemberInGetHashCode
        Id.GetHashCode(StringComparison.InvariantCulture);

    public override string ToString() => RepositoryName ?? string.Empty;
}