using System.Text.Json;
using System.Text.Json.Nodes;
using PriceForge.IntegrationTests.Factory;

namespace PriceForge.IntegrationTests.WebOpenApiTests;

[Trait("Category", "Integration")]
public class OpenApiContractTests : IClassFixture<ApiFactory>
{
    private const string DocumentUrl = "/swagger/v1/swagger.json";
    private const string SnapshotFileName = "openapi.json";
    private const string UpdateVariable = "UPDATE_OPENAPI";

    private static readonly JsonSerializerOptions WriteOptions = new() {WriteIndented = true};

    private readonly HttpClient _client;

    public OpenApiContractTests(ApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Document_MatchesCommittedSnapshot()
    {
        HttpResponseMessage response = await _client.GetAsync(DocumentUrl);
        response.EnsureSuccessStatusCode();

        JsonNode actual = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
        string snapshotPath = Path.Combine(FindRepositoryRoot(), SnapshotFileName);

        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            await WriteSnapshotAsync(snapshotPath, actual);
            return;
        }

        Assert.True(File.Exists(snapshotPath),
                    $"{SnapshotFileName} is missing. Run the tests with {UpdateVariable}=1 and commit the file.");

        JsonNode expected = JsonNode.Parse(await File.ReadAllTextAsync(snapshotPath))!;

        Assert.True(JsonNode.DeepEquals(expected, actual),
                    $"API contract differs from {SnapshotFileName}. If the change is intended, "
                  + $"run the tests with {UpdateVariable}=1 and commit the updated file.");
    }

    private static async Task WriteSnapshotAsync(string path, JsonNode document)
    {
        string json = document.ToJsonString(WriteOptions).Replace("\r\n", "\n") + "\n";
        await File.WriteAllTextAsync(path, json);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PriceForge.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root with PriceForge.sln not found");
    }
}
