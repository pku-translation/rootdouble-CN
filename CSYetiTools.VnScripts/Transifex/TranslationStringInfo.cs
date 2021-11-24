namespace CSYetiTools.VnScripts.Transifex;

public class TranslationStringInfo
{
    public string Comment { get; set; } = "";
    public string Key { get; set; } = "";
    public string StringHash { get; set; } = "";
    public string Context { get; set; } = "";
    public string SourceString { get; set; } = "";
    public string Translation { get; set; } = "";
    public string User { get; set; } = "";
}

public class TranslationStringsPutInfo
{

    public string SourceEntityHash { get; set; } = "";
    public string Translation { get; set; } = "";
    public string? User { get; set; }

    public TranslationStringsPutInfo()
    {

    }

    public TranslationStringsPutInfo(string key, string context, string translation, string? user = null)
    {
        SourceEntityHash = TransifexClient.GetHash(key, context);
        Translation = translation;
        User = user;
    }
}
