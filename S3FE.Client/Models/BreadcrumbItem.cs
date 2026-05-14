namespace S3FE.Client.Models;

public sealed class BreadcrumbItem(string label, string prefix)
{
    public string Label { get; } = label;
    public string Prefix { get; } = prefix;
}
