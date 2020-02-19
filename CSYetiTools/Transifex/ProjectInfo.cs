using System;

namespace CsYetiTools.Transifex
{
    public class ProjectInfo
    {
        public class TeamInfo
        {
            public int Id { get; set; } = 0;
            public string Name { get; set; } = "";
        }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Description { get; set; } = "";
        public string SourceLanguageCode { get; set; } = "";
        public string[] Teams { get; set; } = Array.Empty<string>();
        public TeamInfo Team { get; set; } = new TeamInfo();
    }
}