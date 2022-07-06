﻿using System;
using System.IO;
using BuildNotifications.Core.Utilities;
using NLog.Fluent;

namespace BuildNotifications.Core.Config;

public class ConfigurationSerializer : IConfigurationSerializer
{
    public ConfigurationSerializer(ISerializer serializer)
    {
        _serializer = serializer;
    }

    public IConfiguration Load(string fileName, out bool success)
    {
        Configuration configuration;
        if (File.Exists(fileName))
        {
            try
            {
                var json = File.ReadAllText(fileName);
                configuration = _serializer.Deserialize<Configuration>(json);
                success = true;
            }
            catch (Exception e)
            {
                Log.Warn().Message("Failed to load existing config").Exception(e).Write();
                configuration = new Configuration();
                success = false;
            }
        }
        else
        {
            Log.Info().Message($"File {fileName} does not exist. Using default configuration").Write();
            configuration = new Configuration();
            success = false;
        }

        return configuration;
    }

    public bool Save(IConfiguration configuration, string fileName)
    {
        var json = _serializer.Serialize(configuration);
        var directory = Path.GetDirectoryName(fileName);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            Log.Info().Message($"Creating directory for config \"{directory}\" as it does not exist.").Write();
        }

        Log.Info().Message("Saving current configuration.").Write();
        try
        {
            Log.Debug().Message($"Writing to path \"{fileName}\".").Write();
            File.WriteAllText(fileName, json);
        }
        catch (Exception e)
        {
            Log.Fatal().Message("Failed to persist configuration.").Exception(e).Write();
            return false;
        }

        return true;
    }

    private readonly ISerializer _serializer;
}