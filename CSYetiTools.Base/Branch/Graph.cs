using System.Collections.Generic;
using System.IO;

namespace CSYetiTools.Base.Branch
{
    public class Graph
    {
        public int Index { get; init; }
        public string SceneTitle { get; init; } = "";
        public List<int> Entries { get; init; } = new();
        public SortedDictionary<int, Node> NodeTable { get; init; } = new();

        public void Save(FilePath path)
        {
            if (!Directory.Exists(path.Parent)) {
                Directory.CreateDirectory(path.Parent);
            }
            using var stream = new StreamWriter(path, false, Utils.Utf8);
            Save(stream);
        }

        public void Save(TextWriter writer)
        {
            new YamlDotNet.Serialization.SerializerBuilder()
                .DisableAliases()
                .WithTagMapping("!text", typeof(TextContent))
                .WithTagMapping("!jump", typeof(JumpContent))
                .WithTagMapping("!call", typeof(CallContent))
                .WithTagMapping("!return", typeof(ReturnContent))
                .Build()
                .Serialize(writer, this);
        }

        public static Graph Load(FilePath path)
        {
            using var stream = new StreamReader(path, Utils.Utf8);
            return Load(stream);
        }

        public static Graph Load(TextReader reader)
        {
            var graph = new YamlDotNet.Serialization.DeserializerBuilder()
                .WithTagMapping("!text", typeof(TextContent))
                .WithTagMapping("!jump", typeof(JumpContent))
                .WithTagMapping("!call", typeof(CallContent))
                .WithTagMapping("!return", typeof(ReturnContent))
                .Build()
                .Deserialize<Graph>(reader);
            foreach (var (k, v) in graph.NodeTable) {
                v.Offset = k;
            }
            return graph;
        }
    }
}
