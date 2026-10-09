using System.ClientModel.Primitives;
using System.Text.Json;
using Convy.Application.Common.Interfaces;
using Convy.Domain.ValueObjects;
using Convy.Infrastructure.Persistence;
using Convy.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

namespace Convy.Infrastructure.Tests.Services;

public class OpenAiLunaMigrationTests
{
    [Theory]
    [InlineData("dos litros de leche", "Leche", 2, "litros", "Leche")]
    [InlineData("two bottles of milk", "Milk", 2, "bottles", "Milk")]
    [InlineData("no compres pan", null, null, null, null)]
    [InlineData("do not buy bread", null, null, null, null)]
    [InlineData("quita pan, añade huevos", "Huevos", null, null, null)]
    [InlineData("remove bread, actually add eggs", "Eggs", null, null, null)]
    [InlineData("leche, leche", "Leche", null, null, "Leche")]
    [InlineData("milk, milk", "Milk", null, null, "Milk")]
    [InlineData("", null, null, null, null)]
    public async Task List_fixtures_preserve_language_quantity_correction_and_matching_contract(
        string transcription, string? title, int? quantity, string? unit, string? match)
    {
        var items = title is null ? Array.Empty<object>() : [new { title, quantity, unit, matchedExistingItem = match }];
        var client = new FixtureResponses(JsonSerializer.Serialize(new { items }));
        var parser = new OpenAiVoiceItemParser(client, new("gpt-4o-mini-transcribe", "gpt-6-luna"));
        var result = await parser.ParseAsync(transcription, ["Leche", "Milk"], default);
        if (title is null) result.Items.Should().BeEmpty();
        else result.Items.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Title = title, Quantity = quantity, Unit = unit, MatchedExistingItem = match });
        AssertRequest(client.Options!, transcription);
        result.Usage.Should().BeEquivalentTo(client.Usage);
    }

    [Theory]
    [InlineData("Luis, limpia la cocina mañana", "Limpiar cocina", "Normal", "Limpiar cocina")]
    [InlineData("Luis, clean the kitchen tomorrow", "Clean kitchen", "High", "Clean kitchen")]
    [InlineData("no limpies la cocina", null, "Normal", null)]
    [InlineData("do not clean the kitchen", null, "Normal", null)]
    [InlineData("no cocina, mejor limpia el baño", "Limpiar baño", "Normal", null)]
    [InlineData("not kitchen, clean bathroom instead", "Clean bathroom", "Normal", null)]
    [InlineData("", null, "Normal", null)]
    public async Task Task_fixtures_preserve_language_correction_assignee_dates_and_existing_match(
        string transcription, string? title, string priority, string? match)
    {
        var userId = Guid.NewGuid();
        var tasks = title is null ? Array.Empty<object>() : [new { title, note = "fixture context", assignedToUserId = userId, dueDate = "2026-10-07", reminderAtUtc = "2026-10-07T07:00:00Z", priority, matchedExistingTask = match }];
        var client = new FixtureResponses(JsonSerializer.Serialize(new { tasks }));
        var parser = new OpenAiVoiceTaskParser(client, new("gpt-4o-mini-transcribe", "gpt-6-luna"));
        var result = await parser.ParseAsync(transcription, [new(userId, "Luis")], "Europe/Madrid", DateTimeOffset.Parse("2026-10-06T10:00:00Z"), default);
        if (title is null) result.Tasks.Should().BeEmpty();
        else
        {
            var task = result.Tasks.Should().ContainSingle().Subject;
            task.Title.Should().Be(title); task.AssignedToUserId.Should().Be(userId);
            task.DueDate.Should().Be(new DateOnly(2026, 10, 7));
            task.ReminderAtUtc.Should().Be(new DateTime(2026, 10, 7, 7, 0, 0, DateTimeKind.Utc));
            task.MatchedExistingTask.Should().Be(match);
        }
        AssertRequest(client.Options!, transcription);
        result.Usage.Should().BeEquivalentTo(client.Usage);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{\"items\":[")]
    public async Task Malformed_list_output_preserves_error_behavior(string output)
    {
        var parser = new OpenAiVoiceItemParser(new FixtureResponses(output), new("gpt-4o-mini-transcribe", "gpt-6-luna"));
        await FluentActions.Awaiting(() => parser.ParseAsync("milk", [], default)).Should().ThrowAsync<JsonException>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Usage_adapter_preserves_reported_cache_write_and_reasoning_subsets(bool reported)
    {
        var write = reported ? ",\"cache_write_tokens\":30" : "";
        var usage = ModelReaderWriter.Read<ResponseTokenUsage>(BinaryData.FromString($"{{\"input_tokens\":100,\"output_tokens\":20,\"total_tokens\":120,\"input_tokens_details\":{{\"cached_tokens\":50{write}}},\"output_tokens_details\":{{\"reasoning_tokens\":4}}}}"));
        var mapped = OpenAiResponsesClient.MapUsage(usage)!;
        mapped.InputTokenCount.Should().Be(100); mapped.OutputTokenCount.Should().Be(20);
        mapped.ReasoningTokenCount.Should().Be(4); mapped.TotalTokenCount.Should().Be(120);
        mapped.CacheWriteTokenCount.Should().Be(reported ? 30 : null);
        OpenAiResponsesClient.MapUsage(null).Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(20)]
    public async Task Both_cost_paths_charge_output_once_and_cache_write_at_its_own_rate(int reasoning)
    {
        var configuration = Prices();
        var estimate = new OpenAiVoiceCostEstimator(configuration).EstimateMicros(
            new VoiceParsingTelemetry(VoiceParseStatus.Success, null, 1, 100, 20, 50, reasoning, 1, 30));
        estimate.Should().Be(16); // 2 ordinary + .5 read + 3.75 write + 10 output = 16.25.
        await using var context = Context();
        await new AiUsageRecorder(context, configuration).RecordAsync(new(null, "voice", "task_parsing", "gpt-6-luna", "success", 1,
            InputTokens: 100, OutputTokens: 20, CachedTokens: 50, ReasoningTokens: reasoning, CacheWriteTokens: 30));
        var row = await context.AiUsageEvents.SingleAsync();
        row.EstimatedCostMicros.Should().Be(estimate);
        row.ReasoningTokens.Should().Be(reasoning); row.OutputTokens.Should().Be(20);
    }

    [Theory]
    [InlineData("transcription")]
    [InlineData("task_transcription")]
    public async Task Both_transcription_operations_use_duration_pricing(string operation)
    {
        await using var context = Context();
        await new AiUsageRecorder(context, Prices()).RecordAsync(new(null, "voice", operation, "gpt-4o-mini-transcribe", "success", 1, AudioDurationSeconds: 2));
        (await context.AiUsageEvents.SingleAsync()).EstimatedCostMicros.Should().Be(20);
    }

    [Theory]
    [InlineData(-1, 20, 0, 0)]
    [InlineData(100, -1, 0, 0)]
    [InlineData(100, 20, 80, 30)]
    [InlineData(100, 20, 0, -1)]
    public void Invalid_usage_never_fabricates_a_charge(int input, int output, int cached, int write) =>
        OpenAiParsingCost.Estimate(Prices(), input, output, cached, write).Should().BeNull();

    [Fact]
    public void Missing_cache_write_rate_is_unknown_and_rounding_occurs_after_combining_audio()
    {
        var config = Prices(); config["OpenAI:Costs:ParsingCacheWriteMicrosPer1KTokens"] = null;
        OpenAiParsingCost.Estimate(config, 100, 20, 50, 30).Should().BeNull();
        new OpenAiVoiceCostEstimator(Prices()).EstimateMicros(new(VoiceParseStatus.Success, .04, 1, 0, 1, 0, 1, 1)).Should().Be(1);
    }

    private static void AssertRequest(CreateResponseOptions options, string transcription)
    {
        using var json = JsonDocument.Parse(ModelReaderWriter.Write(options));
        json.RootElement.GetProperty("model").GetString().Should().Be("gpt-6-luna");
        json.RootElement.GetProperty("reasoning").GetProperty("effort").GetString().Should().Be("none");
        json.RootElement.GetProperty("store").GetBoolean().Should().BeFalse();
        json.RootElement.GetProperty("text").GetProperty("format").GetProperty("strict").GetBoolean().Should().BeTrue();
        var message = (MessageResponseItem)options.InputItems.Single();
        using var input = JsonDocument.Parse(message.Content.Single().Text);
        input.RootElement.GetProperty("transcription").GetString().Should().Be(transcription);
    }

    private static IConfigurationRoot Prices() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
        ["OpenAI:Costs:ParsingInputMicrosPer1KTokens"]="100", ["OpenAI:Costs:ParsingCachedInputMicrosPer1KTokens"]="10",
        ["OpenAI:Costs:ParsingCacheWriteMicrosPer1KTokens"]="125", ["OpenAI:Costs:ParsingOutputMicrosPer1KTokens"]="500",
        ["OpenAI:Costs:TranscriptionAudioInputMicrosPerSecond"]="10",
        ["OpenAI:Costs:ParsingReasoningMicrosPer1KTokens"]="999999" // Legacy configuration cannot double bill.
    }).Build();
    private static ConvyDbContext Context() => new(new DbContextOptionsBuilder<ConvyDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private sealed class FixtureResponses(string output) : IOpenAiResponsesClient
    {
        public CreateResponseOptions? Options { get; private set; }
        public OpenAiVoiceTokenUsage Usage { get; } = new(100, 20, 120, 50, 4, null, null, 30);
        public Task<OpenAiResponsesResult> CreateResponseAsync(CreateResponseOptions options, CancellationToken cancellationToken)
        { Options = options; return Task.FromResult(new OpenAiResponsesResult(output, Usage, "gpt-6-luna", "completed")); }
    }
}
