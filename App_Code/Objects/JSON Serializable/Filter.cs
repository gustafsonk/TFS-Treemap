using System.Collections.Generic;

/// <summary>
/// Represents a filter level in JSON format.
/// </summary>
class Filter
{
    // ReSharper disable InconsistentNaming
    public string data { get; set; }
    public List<Filter> children { get; set; }
    // ReSharper restore InconsistentNaming
}