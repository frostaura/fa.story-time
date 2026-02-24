using System.Diagnostics;
using System.Text.Json;
using TaleWeaver.Api.DTOs;

namespace TaleWeaver.Api.Services;

/// <summary>
/// Implements the 5-pass story generation pipeline:
/// 1. Outline → 2. Scene Plan → 3. Scene Batch → 4. Stitch → 5. Polish
/// </summary>
public class StoryGenerationPipeline : IStoryGenerationPipeline
{
    private readonly IOpenRouterService _openRouter;
    private readonly ITtsService _ttsService;
    private readonly ILogger<StoryGenerationPipeline> _logger;

    private const int WordsPerMinute = 150;

    public StoryGenerationPipeline(
        IOpenRouterService openRouter,
        ITtsService ttsService,
        ILogger<StoryGenerationPipeline> logger)
    {
        _openRouter = openRouter;
        _ttsService = ttsService;
        _logger = logger;
    }

    public async Task<GenerationResponse> GenerateAsync(GenerationRequest request, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Starting generation pipeline for {CorrelationId}", request.CorrelationId);

        // Pass 1: Outline
        var outline = await GenerateOutlineAsync(request, ct);

        // Pass 2: Scene Plan
        var scenes = await GenerateScenePlanAsync(request, outline, ct);

        // Pass 3: Scene Batch (narration per scene)
        scenes = await GenerateSceneBatchAsync(request, scenes, ct);

        // Pass 4: Stitch
        var stitchedText = await StitchScenesAsync(request, scenes, ct);

        // Pass 5: Polish
        var polishedText = await PolishTextAsync(request, stitchedText, ct);

        // Generate images from scene visual prompts
        var imagePrompts = scenes
            .SelectMany(s => s.VisualLayerPrompts)
            .ToList();
        var images = imagePrompts.Count > 0
            ? await _openRouter.GenerateImagesAsync(imagePrompts)
            : [];

        // Synthesize audio
        byte[]? audio = null;
        try
        {
            audio = await _ttsService.SynthesizeAsync(polishedText, request.VoiceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TTS synthesis failed, returning text-only response");
        }

        sw.Stop();

        return new GenerationResponse
        {
            CorrelationId = request.CorrelationId,
            NarrationText = polishedText,
            AudioData = audio,
            Scenes = scenes,
            Images = images,
            GenerationTimeMs = sw.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Pass 1: Generate outline with beats, theme, ending, and Story Bible deltas.
    /// </summary>
    internal async Task<string> GenerateOutlineAsync(GenerationRequest request, CancellationToken ct)
    {
        _logger.LogInformation("[Pass 1/5] Generating outline for {CorrelationId}", request.CorrelationId);

        var storyBibleContext = request.StoryBible != null
            ? $"\n\nExisting Story Bible:\n{JsonSerializer.Serialize(request.StoryBible)}"
            : "";

        var systemPrompt = """
            You are a children's story architect. Generate a structured outline for a bedtime story.
            Return valid JSON with: { "beats": [...], "theme": "...", "ending": "...", "storyBibleDeltas": {...} }
            The story should be age-appropriate, engaging, and calming toward the end.
            """;

        var userPrompt = $"""
            Child's name: {request.ChildName}
            Age: {request.Age}
            Theme: {request.Theme}
            Duration: {request.DurationMinutes} minutes (~{request.DurationMinutes * WordsPerMinute} words)
            Length: {request.Length}
            {storyBibleContext}
            """;

        return await _openRouter.CompleteTextAsync(systemPrompt, userPrompt);
    }

    /// <summary>
    /// Pass 2: Expand outline to N scenes based on target duration.
    /// </summary>
    internal async Task<List<ScenePlan>> GenerateScenePlanAsync(
        GenerationRequest request, string outline, CancellationToken ct)
    {
        _logger.LogInformation("[Pass 2/5] Generating scene plan for {CorrelationId}", request.CorrelationId);

        var targetWordCount = request.DurationMinutes * WordsPerMinute;
        var estimatedScenes = Math.Max(3, targetWordCount / 200); // ~200 words per scene

        var systemPrompt = $$"""
            You are a story scene planner. Expand the outline into exactly {{estimatedScenes}} scenes.
            Return valid JSON array where each scene has:
            { "sceneNumber": N, "setting": "...", "mood": "...", "goal": "...",
               "conflictLevel": 0-10, "continuityFacts": [...],
               "visualLayerPrompts": ["...", "...", "..."],
               "musicTrackQuery": "...", "sfxKeywords": [...] }
            The final scene MUST be calm and winding down for sleep.
            """;

        var userPrompt = $"Outline:\n{outline}\n\nChild: {request.ChildName}, Age: {request.Age}";

        var response = await _openRouter.CompleteTextAsync(systemPrompt, userPrompt);

        try
        {
            var scenes = JsonSerializer.Deserialize<List<ScenePlan>>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return scenes ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse scene plan JSON, returning empty list");
            return [];
        }
    }

    /// <summary>
    /// Pass 3: Generate narration text for each scene.
    /// </summary>
    internal async Task<List<ScenePlan>> GenerateSceneBatchAsync(
        GenerationRequest request, List<ScenePlan> scenes, CancellationToken ct)
    {
        _logger.LogInformation("[Pass 3/5] Generating scene narrations for {CorrelationId}", request.CorrelationId);

        for (var i = 0; i < scenes.Count; i++)
        {
            var scene = scenes[i];
            var isFinalScene = i == scenes.Count - 1;

            var systemPrompt = $"""
                You are a children's story narrator. Write the narration for scene {scene.SceneNumber}.
                Use age-appropriate vocabulary for a {request.Age}-year-old.
                Address the child as {request.ChildName} occasionally.
                {(isFinalScene ? "This is the FINAL scene. Make it calm, gentle, and perfect for drifting off to sleep. Wind down all tension." : "")}
                Return ONLY the narration text, no JSON.
                """;

            var userPrompt = $"""
                Scene: {JsonSerializer.Serialize(scene)}
                Mood: {scene.Mood}
                Goal: {scene.Goal}
                Conflict level: {scene.ConflictLevel}/10
                Continuity: {string.Join(", ", scene.ContinuityFacts)}
                """;

            scene.NarrationText = await _openRouter.CompleteTextAsync(systemPrompt, userPrompt);
        }

        return scenes;
    }

    /// <summary>
    /// Pass 4: Join scenes with transitions; normalize tense, voice, and names.
    /// </summary>
    internal async Task<string> StitchScenesAsync(
        GenerationRequest request, List<ScenePlan> scenes, CancellationToken ct)
    {
        _logger.LogInformation("[Pass 4/5] Stitching scenes for {CorrelationId}", request.CorrelationId);

        var allNarrations = string.Join("\n\n---SCENE BREAK---\n\n",
            scenes.Select(s => s.NarrationText));

        var systemPrompt = """
            You are a story editor. Join these scenes into one flowing narrative.
            - Add smooth transitions between scenes
            - Normalize tense (past tense)
            - Ensure character names are consistent throughout
            - Maintain a consistent narrative voice
            Return ONLY the complete stitched narration text.
            """;

        return await _openRouter.CompleteTextAsync(systemPrompt, allNarrations);
    }

    /// <summary>
    /// Pass 5: Check contradictions vs Story Bible; rewrite only small sections if needed.
    /// </summary>
    internal async Task<string> PolishTextAsync(
        GenerationRequest request, string text, CancellationToken ct)
    {
        _logger.LogInformation("[Pass 5/5] Polishing text for {CorrelationId}", request.CorrelationId);

        var storyBibleContext = request.StoryBible != null
            ? $"\n\nStory Bible to check against:\n{JsonSerializer.Serialize(request.StoryBible)}"
            : "";

        var systemPrompt = $"""
            You are a story quality checker. Review this children's bedtime story.
            - Check for contradictions{(request.StoryBible != null ? " against the Story Bible" : "")}
            - Fix any inconsistencies in character names, settings, or plot
            - Ensure age-appropriate language for a {request.Age}-year-old
            - Only rewrite small sections that have issues; preserve the rest verbatim
            Return the final polished narration text.
            {storyBibleContext}
            """;

        return await _openRouter.CompleteTextAsync(systemPrompt, text);
    }
}
