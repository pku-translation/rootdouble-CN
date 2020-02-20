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

        public Task<TranslationStringInfo[]> GetTranslationStrings(string language, string? key = null, string? context = null)
            => _client.GetTranslationStrings(_projectSlug, _resourceSlug, language, key, context);

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
        
        private Task<string> Get(IFlurlRequest request)
        {
            return request.GetStringAsync();
        }

        private async Task<T> Get<T>(IFlurlRequest request)
        {
            return JsonConvert.DeserializeObject<T>(await request.GetStringAsync(), JsonSettings)!;
        }

        public Task<string> Put(IFlurlRequest request, string content, string contentType)
        {
            return request
                .WithHeader("Content-Type", contentType)
                .PutStringAsync(content)
                .ReceiveString();
        }

        public async Task<T> Put<T>(IFlurlRequest request, string content, string contentType)
        {
            return JsonConvert.DeserializeObject<T>(await Put(request, content, contentType))!;
        }

        public Task<string> PutJson(IFlurlRequest request, object data)
            => Put(request, JsonConvert.SerializeObject(data, JsonSettings), "application/json");

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

        public async Task<TranslationStringInfo[]> GetTranslationStrings(string projectSlug, string resourceSlug, string language, string? key = null, string? context = null)
        {
            var request = _flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "strings/").SetQueryParam("details");
            if (key != null) request.SetQueryParam("key", key);
            if (context != null) request.SetQueryParam("context", true);
            return JsonConvert.DeserializeObject<TranslationStringInfo[]>(await Get(request), JsonSettings)!;
        }

        public Task<string> ResourceTest(string projectSlug, string resourceSlug, string url, object args)
            => Get(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, url).SetQueryParams(args));
    }
}