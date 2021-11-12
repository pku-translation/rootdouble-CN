using CSYetiTools.Base.Branch;
using System.Collections.Generic;

namespace CSYetiTools.Commandlet;

internal enum NodeColor
{
    White,
    Gray,
    Black,
}

internal class RawNode
{
    public int Offset { get; init; }
    public List<Content> Contents { get; } = new();
    public List<int> Adjacents { get; } = new();
    public int? Next { get; set; }
    public bool AutoNext { get; set; } = true;

    public string? DebugInfo
    {
        get
        {
            var str = new List<string>();
            if (IsEntry) str.Add("entry");
            if (IsBackRef) str.Add("backref");
            if (IsCallTarget) str.Add("call-target");
            if (str.Count > 0) {
                return "<" + string.Join(", ", str) + ">";
            }
            return null;
        }
    }

    public NodeColor Color { get; set; }

    public bool IsEntry { get; set; }

    public bool IsCallTarget { get; set; }

    public bool IsBackRef { get; set; }

    //public bool IsReturn { get; set; }

    public bool IsImportant
        => Contents.Count > 0 || IsEntry || IsCallTarget || IsBackRef; // || IsReturn;
}
