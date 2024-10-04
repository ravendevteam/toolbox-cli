namespace toolbox;

using System.Collections.Generic;

public class Package
{
    public string Name { get; set; }
    public string Version { get; set; }
    public string Url { get; set; }
    public string Description { get; set; }
}

public class Packages
{
    public List<Package> PackagesList { get; set; }
}
