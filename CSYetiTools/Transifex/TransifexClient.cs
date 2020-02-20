using System;
using System.Threading.Tasks;
using Flurl;
using Flurl.Util;
using Flurl.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace CsYetiTools.Transifex
{
    public enum TranslationMode
    {
        Default,
        Reviewed,
        Translator,
        OnlyTranslated,
        OnlyReviwed,
        SourceAsTranslation,
    }

    public class ProjectApi
    {
        private TransifexClient _client;

        private string _projectSlug;

        public ProjectApi(TransifexClient client, string projectSlug)
        {
            _client = client;
            _projectSlug = projectSlug;
        }

        public ResourceApi Resource(string resourceSlug)
            => new ResourceApi(_client, _projectSlug, resourceSlug);

        public Task<ResourceInfo[]> GetResources(string projectSlug)
            => _client.GetResources(projectSlug);

        public Task<ResourceInfo> GetResource(string projectSlug, string resourceSlug)
            => _client.GetResource(projectSlug, resourceSlug);
    }

    public class ResourceApi
    {
        private TransifexClient _client;

        private string _projectSlug;

        private string _resourceSlug;
        
        public ResourceApi(TransifexClient client, string projectSlug, string resourceSlug)
        {
            _client = client;
            _projectSlug = projectSlug;
            _resourceSlug = resourceSlug;
        }

        public Task<SortedDictionary<string, TranslationInfo>> GetTranslations(string language, TranslationMode mode = TranslationMode.Default)
            => _client.GetTranslations(_projectSlug, _resourceSlug, language, mode);


        public Task<string> Test(string url, object args)
            => _client.ResourceTest(_projectSlug, _resourceSlug, url, args);

    }

    public class TransifexClient : IDisposable
    {
        private class WrappedResponse
        {
            public string Mimetype { get; set; } = "";
            public string Content { get; set; } = "";
        }

        private static string BaseUrl = "https://www.transifex.com/api/2/";

        private static int Timeout = 300;

        public static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy(true, false)
            },
            Formatting = Formatting.Indented,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
        };

        private string _apiToken;

        private FlurlClient _flurlClient;

        public TransifexClient(string? apiToken = null)
        {
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                apiToken = Environment.GetEnvironmentVariable("TX_TOKEN");
            }
            if (string.IsNullOrWhiteSpace(apiToken))
            {
                throw new ArgumentException("No API token found, please use env TX_TOKEN or args --token to specify API token.");
            }
            _apiToken = apiToken;

            _flurlClient = new FlurlClient(BaseUrl).WithBasicAuth("api", apiToken).WithTimeout(Timeout);
        }

        public ProjectApi Project(string projectSlug)
            => new ProjectApi(this, projectSlug);

        public ResourceApi Resource(string projectSlug, string resourceSlug)
            => new ResourceApi(this, projectSlug, resourceSlug);

        public void Dispose()
        {
            _flurlClient.Dispose();
        }
        
        private async Task<string> Get(IFlurlRequest request)
        {
            return await request.GetStringAsync();
        }

        private async Task<T> Get<T>(IFlurlRequest request)
        {
            var json = await request.GetStringAsync();
            return JsonConvert.DeserializeObject<T>(json, JsonSettings)!;
        }

        public Task<string> Put(string url, string content, string contentType)
        {
            return url
                .WithBasicAuth("api", _apiToken)
                .WithTimeout(30)
                .WithHeader("Content-Type", contentType)
                .PutStringAsync(content)
                .ReceiveString();
        }

        public Task<string> PutJson(string url, object data)
            => Put(url, JsonConvert.SerializeObject(data, JsonSettings), "application/json");

        public Task<ProjectInfo[]> GetProjects(int start = 1, int? end = 500)
            => Get<ProjectInfo[]>(_flurlClient.Request(BaseUrl, "projects/").SetQueryParams(new { start = start, end = end }));

        public Task<ProjectInfo> GetProject(string slug)
            => Get<ProjectInfo>(_flurlClient.Request(BaseUrl, "project", slug).SetQueryParam("details"));

        public Task<ResourceInfo[]> GetResources(string projectSlug)
            => Get<ResourceInfo[]>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resources"));

        public Task<ResourceInfo> GetResource(string projectSlug, string resourceSlug)
            => Get<ResourceInfo>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug));

        public async Task<SortedDictionary<string, TranslationInfo>> GetTranslations(string projectSlug, string resourceSlug, string language, TranslationMode mode = TranslationMode.Default)
        {
            var response = await Get<WrappedResponse>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "/")
                .SetQueryParam("mode", mode.ToString().ToLower()));
            if (response.Mimetype != "application/json")
                throw new FormatException("Response is not json");
            return JsonConvert.DeserializeObject<SortedDictionary<string, TranslationInfo>>(response.Content, JsonSettings)!;
        }

        public Task<string> ResourceTest(string projectSlug, string resourceSlug, string url, object args)
            => Get(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, url).SetQueryParams(args));
    }
}