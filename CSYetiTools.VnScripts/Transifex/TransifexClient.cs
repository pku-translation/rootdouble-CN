using CSYetiTools.Base;
using Flurl.Http;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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

    private const string BaseUrl = "https://rest.api.transifex.com/";

    private const int Timeout = 30;

    private readonly FlurlClient _flurlClient;

    private readonly string _organization;

    public TransifexClient(string organization, string? apiToken)
    {
        if (string.IsNullOrWhiteSpace(apiToken)) {
            apiToken = Environment.GetEnvironmentVariable("TX_TOKEN");
        }
        if (string.IsNullOrWhiteSpace(apiToken)) {
            throw new ArgumentException("No API token found, please use env TX_TOKEN to specify API token.");
        }

        _organization = organization;
        _flurlClient = new FlurlClient().WithOAuthBearerToken(apiToken).WithTimeout(Timeout);
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
        => throw new NotImplementedException("API V2 deprecated.");
    //=> Get<ProjectInfo[]>(_flurlClient.Request(BaseUrl, "projects/").SetQueryParams(new { start, end }));

    public Task<ProjectInfo> GetProject(string slug)
        => throw new NotImplementedException("API V2 deprecated.");
    //=> Get<ProjectInfo>(_flurlClient.Request(BaseUrl, "project", slug).SetQueryParam("details"));

    public Task<ResourceInfo[]> GetResources(string projectSlug)
        => throw new NotImplementedException("API V2 deprecated.");
    //=> Get<ResourceInfo[]>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resources"));

    public Task<ResourceInfo> GetResource(string projectSlug, string resourceSlug)
        => throw new NotImplementedException("API V2 deprecated.");
    //=> Get<ResourceInfo>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug));

    public async Task<string> GetRawTranslations(string projectSlug, string resourceSlug, string language, TranslationMode mode = TranslationMode.Default)
    {
        var requestBody = new {
            data = new {
                relationships = new {
                    language = new {
                        data = new {
                            id = $"l:{language}",
                            type = "languages"
                        }
                    },
                    resource = new {
                        data = new {
                            id = $"o:{_organization}:p:{projectSlug}:r:{resourceSlug}",
                            type = "resources"
                        }
                    },
                },

                type = "resource_translations_async_downloads"
            }
        };

        var response = await _flurlClient
            .Request(BaseUrl, "resource_translations_async_downloads")
            .PostAsync(JsonContent.Create(requestBody, new MediaTypeHeaderValue("application/vnd.api+json")));
        var responseBody = await response.GetJsonAsync();
        string downloadId = responseBody.data.id;
        while (true) {
            var queryResponse = await _flurlClient.Request(BaseUrl, "resource_translations_async_downloads", downloadId).WithAutoRedirect(false).GetAsync();
            if (queryResponse.StatusCode == 200) {
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            else if (queryResponse.StatusCode == 303) {
                var location = queryResponse.Headers.First(kv => kv.Name == "Location").Value;
                var result = await _flurlClient.Request(location).GetStringAsync();
                return result;
            }
        }

        // var response = await Get<WrappedResponse>(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "/")
        //     .SetQueryParam("mode", mode.ToString().ToLower()));
        // if (response.Mimetype != "application/json")
        //     throw new FormatException("Response is not json");
        // return response.Content;
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
        await Task.FromResult(0);
        throw new NotImplementedException("API V2 deprecated.");
        // var request = _flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "strings/").SetQueryParam("details");
        // if (key != null) request.SetQueryParam("key", key);
        // if (context != null) request.SetQueryParam("context", true);
        // return JsonConvert.DeserializeObject<TranslationStringInfo[]>(await Get(request), Utils.JsonSettings)!;
    }

    public Task<string> PutTranslationStrings(string projectSlug, string resourceSlug, string language, TranslationStringsPutInfo[] translations)
        => throw new NotImplementedException("API V2 deprecated.");
    //=> PutJson(_flurlClient.Request(BaseUrl, "project", projectSlug, "resource", resourceSlug, "translation", language, "strings/"), translations);
}
