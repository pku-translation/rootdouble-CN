using System.Text;

namespace CsYetiTools.Transifex
{
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

        public string SourceEntryHash { get; set; } = "";
        public string Translation { get; set; } = "";
        public string? User { get; set; } = null;
        public TranslationStringsPutInfo(string key, string context, string translation, string? user = null)
        {
            var data = md5.ComputeHash(Utils.Utf8.GetBytes(key + ":" + context));
            var builder = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                builder.Append(data[i].ToString("x2"));
            }
            SourceEntryHash = builder.ToString();
            Translation = translation;
            User = user;
        }
    }
}
