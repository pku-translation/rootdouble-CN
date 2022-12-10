namespace CSYetiTools.VnScripts;

public sealed class TranslationSettings
{
    public bool DebugSource { get; init; }
    public bool DebugChunkNum { get; init; }
    public bool KeepComment { get; init; }

    public static readonly TranslationSettings Default = new() {
        DebugChunkNum = false,
        DebugSource = false,
        KeepComment = false,
    };

    public static readonly TranslationSettings Debug = new() {
        DebugChunkNum = true,
        DebugSource = false,
        KeepComment = true,
    };

    public static readonly TranslationSettings DebugWithSource = new() {
        DebugChunkNum = true,
        DebugSource = true,
        KeepComment = true,
    };
}
