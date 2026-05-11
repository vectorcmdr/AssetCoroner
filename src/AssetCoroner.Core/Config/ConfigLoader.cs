using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AssetCoroner.Core.Config;

/// <summary>
/// Loads an <see cref="AssetCoronerConfig"/> from a YAML string.
/// Returns the default configuration when the input is null, empty, or unparseable.
/// </summary>
public static class ConfigLoader
{
    /// <summary>
    /// Deserialises <paramref name="yaml"/> into an <see cref="AssetCoronerConfig"/> using
    /// underscored property name conventions. Returns <see cref="AssetCoronerConfig.Default"/>
    /// if <paramref name="yaml"/> is null, whitespace, or contains invalid YAML.
    /// </summary>
    /// <param name="yaml">Raw YAML text, or <c>null</c> if no configuration file is present.</param>
    public static AssetCoronerConfig Load(string? yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            return AssetCoronerConfig.Default;

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<AssetCoronerConfig>(yaml)
                   ?? AssetCoronerConfig.Default;
        }
        catch
        {
            return AssetCoronerConfig.Default;
        }
    }
}
