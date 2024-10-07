using System.Text.Json.Serialization;

namespace  toolbox;
    
public class Package
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("url")]
    public Dictionary<string, string> Url { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
    
    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; }
    
    [JsonPropertyName("requirepath")]
    public bool RequirePath { get; set; }
    
    [JsonPropertyName("shortcut")]
    public bool Shortcut { get; set; }
    
    [JsonPropertyName("os")]
    public List<string> OsList { get; set; }
}

public class PackageList
{
    [JsonPropertyName("packages")]
    public List<Package> Packages { get; set; }
    [JsonPropertyName("updateurl")]
    public string UpdateUrl { get; set; }
}