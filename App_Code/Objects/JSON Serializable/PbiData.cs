/// <summary>
/// Represents data specific to PBIs in JSON format.
/// </summary>
class PbiData : Data
{
    // ReSharper disable InconsistentNaming
    public int i { get; set; }
    public double p { get; set; }
    public string s { get; set; }
    // ReSharper restore InconsistentNaming

    public PbiData(double area, string color, double effort, int id, double priority, string state)
        : base(area, color, effort)
    {
        i = id;
        p = priority;
        s = state;
    }
}