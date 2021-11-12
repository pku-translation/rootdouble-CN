using CSYetiTools.Base;
using System.Text;

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
    private static System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
    public string SourceEntityHash { get; set; } = "";
    public string Translation { get; set; } = "";
    public string? User { get; set; }

    public TranslationStringsPutInfo()
    {

    }

    public TranslationStringsPutInfo(string key, string context, string translation, string? user = null)
    {
        var data = md5.ComputeHash(Utils.Utf8.GetBytes(key + ":" + context));
        var builder = new StringBuilder();
        foreach (var b in data) {
            builder.Append(b.ToString("x2"));
        }
        SourceEntityHash = builder.ToString();
        Translation = translation;
        User = user;
    }
}
