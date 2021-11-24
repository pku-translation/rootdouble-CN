using CSYetiTools.Base;
using Flurl.Http;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CSYetiTools.VnScripts.Transifex;

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
    private readonly TransifexClient _client;

    private readonly string _projectSlug;

    public ProjectApi(TransifexClient client, string projectSlug)
    {
        _client = client;
        _projectSlug = projectSlug;
    }

    public ResourceApi Resource(string resourceSlug)
        => new(_client, _projectSlug, resourceSlug);

    public Task<ResourceInfo[]> GetResources(string projectSlug)
        => _client.GetResources(projectSlug);

    public Task<ResourceInfo> GetResource(string projectSlug, string resourceSlug)
        => _client.GetResource(projectSlug, resourceSlug);
}

public class ResourceApi
{
    private readonly TransifexClient _client;

    private readonly string _projectSlug;

    private readonly string _resourceSlug;

    public ResourceApi(TransifexClient client, string projectSlug, string resourceSlug)
    {
        _client = client;
        _projectSlug = projectSlug;
        _resourceSlug = resourceSlug;
    }

    public Task<string> GetRawTranslations(string language, TranslationMode mode = TranslationMode.Default)
        => _client.GetRawTranslations(_projectSlug, _resourceSlug, language, mode);

    public Task<SortedDictionary<string, TranslationInfo>> GetTranslations(string language, TranslationMode mode = TranslationMode.Default)
        => _client.GetTranslations(_projectSlug, _resourceSlug, language, mode);

    public Task<TranslationStringInfo[]> GetTranslationStrings(string language, string? key = null, string? context = null)
        => _client.GetTranslationStrings(_projectSlug, _resourceSlug, language, key, context);

    public Task<string> PutTranslationStrings(string language, TranslationStringsPutInfo[] translations)
        => translations.Length != 0
            ? _client.PutTranslationStrings(_projectSlug, _resourceSlug, language, translations)
            : throw new ArgumentException("Empty translations");

    public Task<string> Test(string url, object args)
        => _client.ResourceTest(_projectSlug, _resourceSlug, url, args);

}

public class TransifexClient : IDisposable
{
    private static System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
    public static string GetHash(string key, string context)
    {
        var data = md5.ComputeHash(Utils.Utf8.GetBytes(key + ":" + context));
        var builder = new StringBuilder();
        foreach (var b in data) {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }

    [UsedImplicitly]
    private class WrappedResponse
    {
        public string Mimetype { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private const string BaseUrl = "https://www.transifex.com/api/2/";

    private const int Timeout = 30;

    private readonly FlurlClient _flurlClient;

    public TransifexClient(string? apiToken = null)
    {
        if (string.IsNullOrWhiteSpace(apiToken)) {
            apiToken = Environment.GetEnvironmentVariable("TX_TOKEN");
        }
        if (string.IsNullOrWhiteSpace(apiToken)) {
            throw new ArgumentException("No API token found, please use env TX_TOKEN to specify API token.");
        }

        _flurlClient = new FlurlClient(BaseUrl).WithBasicAuth("api", apiToken).WithTimeout(Timeout);
    }

    public ProjectApi Project(string projectSlug)
        => new(this, projectSlug);

    public ResourceApi Resource(string projectSlug, string resourceSlug)
        => new(this, projectSlug, resourceSlug);

    public void Dispose()
    {
        _flurlClient.Dispose();
    }

    private static Task<string> Get(IFlurlRequest request)
    {
        return request.GetStringAsync();
    }

    private static async Task<T> Get<T>(IFlurlRequest request)
    {
        return JsonConvert.DeserializeObject<T>(await request.GetStringAsync(), Utils.JsonSettings)!;
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
        => Put(request, JsonConvert.SerializeObject(data, Utils.JsonSettings), "application/json");

    public Task<ProjectInfo[]> GetProjects(int start = 1, int? end = 500)
        => Get<ProjectInfo[]>(_flurlClient.Request(BaseUrl, "projects/").SetQueryParams(new { start, end }));

    public Task<ProjectInfo> GetProject(string slug)
        => Get<ProjectInfo>(_flurlClient.Request(BaseUrl, "project", slug).SetQueryParam("details"));

    public Task<ResourceInfo[]> GetResources(string projectSlug)
        => Get<ResourceInfo[]>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resources"));

    public Task<ResourceInfo> GetResource(string projectSlug, string resourceSlug)
        => Get<ResourceInfo>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug));

    public async Task<string> GetRawTranslations(string projectSlug, string resourceSlug, string language, TranslationMode mode = TranslationMode.Default)
    {
        var response = await Get<WrappedResponse>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "/")
            .SetQueryParam("mode", mode.ToString().ToLower()));
        if (response.Mimetype != "application/json")
            throw new FormatException("Response is not json");
        return response.Content;
    }

    public async Task<SortedDictionary<string, TranslationInfo>> GetTranslations(string projectSlug, string resourceSlug, string language, TranslationMode mode = TranslationMode.Default)
        => JsonConvert.DeserializeObject<SortedDictionary<string, TranslationInfo>>(await GetRawTranslations(projectSlug, resourceSlug, language, mode), Utils.JsonSettings)!;
    // {
    //     var response = await Get<WrappedResponse>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "/")
    //         .SetQueryParam("mode", mode.ToString().ToLower()));
    //     if (response.Mimetype != "application/json")
    //         throw new FormatException("Response is not json");
    //     return JsonConvert.DeserializeObject<SortedDictionary<string, TranslationInfo>>(response.Content, Utils.JsonSettings)!;
    // }

    public async Task<TranslationStringInfo[]> GetTranslationStrings(string projectSlug, string resourceSlug, string language, string? key = null, string? context = null)
    {
        var request = _flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "strings/").SetQueryParam("details");
        if (key != null) request.SetQueryParam("key", key);
        if (context != null) request.SetQueryParam("context", true);
        return JsonConvert.DeserializeObject<TranslationStringInfo[]>(await Get(request), Utils.JsonSettings)!;
    }

    public Task<string> PutTranslationStrings(string projectSlug, string resourceSlug, string language, TranslationStringsPutInfo[] translations)
        => PutJson(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "strings/"), translations);

    public Task<string> ResourceTest(string projectSlug, string resourceSlug, string url, object args)
        => Get(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, url).SetQueryParams(args));
}
