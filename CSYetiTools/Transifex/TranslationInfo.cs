namespace CsYetiTools.Transifex
{
    public class TranslationInfo
    {
        public string Context { get; set; } = "";
        public string? Code { get; set; }
        public string? DeveloperComment { get; set; } = null;
        public int? CharacterLimit { get; set; }
        public string String { get; set; } = "";

        public override string ToString()
        {
            return $"<TranslationInfo> \"{SExpr.EscapeString(String)}\"";
        }
    }
}
