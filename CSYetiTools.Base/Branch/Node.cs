using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace CSYetiTools.Base.Branch
{
    public class Node
    {
        [YamlIgnore]
        public int Offset { get; set; }
        public List<Content> Contents { get; init; } = new();
        public List<int> Adjacents { get; init; } = new();
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public string? DebugInfo { get; set; }
    }
}
