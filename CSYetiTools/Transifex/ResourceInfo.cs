using System;

namespace CsYetiTools.Transifex
{
    public class ResourceInfo
    {
        public string SourceLanguageCode { get; set; } = "";
        public string Name { get; set; } = "";
        public string I18nType { get; set; } = "";
        public string Priority { get; set; } = "";
        public string Slug { get; set; } = "";
        public string[] Categories { get; set; } = Array.Empty<string>();
    }
}