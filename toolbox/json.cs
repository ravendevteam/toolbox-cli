using System.Text.Json.Serialization;

namespace  toolbox;

public abstract class Package(string name, string version, string url, string description)
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = name;

    [JsonPropertyName("version")]
    public string Version { get; set; } = version;

    [JsonPropertyName("url")]
    public string Url { get; set; } = url;

    [JsonPropertyName("description")]
    public string Description { get; set; } = description;
}

public class PackageList(List<Package> packages)
{
    [JsonPropertyName("packages")]
    public List<Package> Packages { get; set; } = packages;
}