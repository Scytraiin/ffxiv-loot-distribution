using System.Text.Json;

using Xunit;

namespace LootDistributionInfo.Tests;

public sealed class RepositoryMetadataTests
{
    [Fact]
    public void ScytRepoJson_IsValidJsonAndContainsRequiredFields()
    {
        var root = LoadJsonDocument("scyt.repo.json").RootElement;

        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        var pluginEntry = Assert.Single(root.EnumerateArray());
        Assert.Equal("scyt.raiin", pluginEntry.GetProperty("Author").GetString());
        Assert.Equal("LootDistributionInfo", pluginEntry.GetProperty("InternalName").GetString());
        Assert.Equal(14, pluginEntry.GetProperty("DalamudApiLevel").GetInt32());
        Assert.False(pluginEntry.GetProperty("IsHide").GetBoolean());
        Assert.False(pluginEntry.GetProperty("IsTestingExclusive").GetBoolean());
    }

    [Fact]
    public void ScytRepoJson_AlignsWithPluginManifest()
    {
        var repoEntry = Assert.Single(LoadJsonDocument("scyt.repo.json").RootElement.EnumerateArray());
        var manifest = LoadJsonDocument("LootDistributionInfo.json").RootElement;

        Assert.Equal(manifest.GetProperty("Author").GetString(), repoEntry.GetProperty("Author").GetString());
        Assert.Equal(manifest.GetProperty("InternalName").GetString(), repoEntry.GetProperty("InternalName").GetString());
        Assert.Equal(manifest.GetProperty("RepoUrl").GetString(), repoEntry.GetProperty("RepoUrl").GetString());
        Assert.Equal(manifest.GetProperty("IconUrl").GetString(), repoEntry.GetProperty("IconUrl").GetString());
    }

    private static JsonDocument LoadJsonDocument(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
        return JsonDocument.Parse(File.ReadAllText(path));
    }
}
