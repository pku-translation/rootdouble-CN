using JetBrains.Annotations;
using YamlDotNet.Serialization;

namespace CSYetiTools.Base.Branch
{
    public abstract class Content
    {
        [YamlMember(Order = 0)]
        public int Index { get; set; }
    }

    public class TextContent : Content
    {
        [YamlMember(Order = 1, DefaultValuesHandling = DefaultValuesHandling.OmitNull)]
        public string? Character { get; set; }
        [YamlMember(Order = 2, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public string Content { get; set; } = null!;
        [YamlMember(Order = 3, DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public string? RawContent { get; set; }

        [UsedImplicitly]
        public TextContent()
        { }

        public TextContent(int index, string? character, string content, string? rawContent)
        {
            Index = index;
            Character = character;
            Content = content;
            RawContent = rawContent;
        }
    }
    
    public class JumpContent : Content
    {
        [YamlMember(Order = 1)]
        public int Script { get; set; }
        [YamlMember(Order = 2)]
        public int Entry { get; set; }

        [UsedImplicitly]
        public JumpContent()
        { }

        public JumpContent(int index, int scriptIndex, int entryIndex)
        {
            Index = index;
            Script = scriptIndex;
            Entry = entryIndex;
        }
    }

    public class CallContent : Content
    {
        [YamlMember(Order = 1)]
        public int Script { get; set; }
        [YamlMember(Order = 2)]
        public int Entry { get; set; }

        [UsedImplicitly]
        public CallContent()
        { }

        public CallContent(int index, int scriptIndex, int entryIndex)
        {
            Index = index;
            Script = scriptIndex;
            Entry = entryIndex;
        }
    }

    public class ReturnContent : Content
    {

    }
}
