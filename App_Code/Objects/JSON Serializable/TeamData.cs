/// <summary>
/// Represents data specific to teams in JSON format.
/// </summary>
class TeamData : Data
{
    // ReSharper disable InconsistentNaming
    public double v { get; set; }
    // ReSharper restore InconsistentNaming

    public TeamData(double area, string color, double effort, double velocity)
        : base(area, color, effort)
    {
        v = velocity;
    }
}