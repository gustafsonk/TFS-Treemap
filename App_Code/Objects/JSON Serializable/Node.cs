using System.Collections.Generic;

/// <summary>
/// Represents a node in the treemap in JSON format.
/// </summary>
class Node
{
    // ReSharper disable InconsistentNaming
    public string id { get; set; }
    public string name { get; set; }
    public Data data { get; set; }
    public List<Node> children { get; set; }
    // ReSharper restore InconsistentNaming
}