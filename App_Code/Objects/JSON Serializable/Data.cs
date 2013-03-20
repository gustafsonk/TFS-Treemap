/// <summary>
/// Represents data for a node in a treemap in JSON format.
/// 'area' and 'color' get a $ sign prepended to them in fastJSON/JsonSerializer.cs/WritePair
/// </summary>
class Data
{
    // ReSharper disable InconsistentNaming
    public double e { get; set; }
    public double area { get; set; }
    public string color { get; set; }
    // ReSharper restore InconsistentNaming

    public Data(double area, string color, double effort)
    {
        this.area = area;
        this.color = color;
        e = effort;
    }
}