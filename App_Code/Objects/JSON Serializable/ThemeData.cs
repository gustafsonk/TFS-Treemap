/// <summary>
/// Represents data specific to themes in JSON format.
/// </summary>
class ThemeData : Data
{
    // ReSharper disable InconsistentNaming
    public double p { get; set; }
    // ReSharper restore InconsistentNaming

    public ThemeData(double area, string color, double effort, double priority)
        : base(area, color, effort)
    {
        p = priority;
    }
}