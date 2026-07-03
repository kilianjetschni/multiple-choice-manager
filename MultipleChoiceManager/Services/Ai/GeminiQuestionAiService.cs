using System.Text.Json;
using GeminiSchemaType = Google.GenAI.Types.Type;
using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;

namespace MultipleChoiceManager.Services.Ai;

public class GeminiQuestionAiService(IOptions<GeminiOptions> options) : IQuestionAiService
{
    private readonly GeminiOptions _options = options.Value;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GeneratedQuestionDto> GenerateQuestionAsync(
        byte[] pdfBytes,
        string chapterTitle,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var uploadedFile = await client.Files.UploadAsync(
            pdfBytes,
            "chapter.pdf",
            new UploadFileConfig { MimeType = "application/pdf" },
            cancellationToken);

        var contents = new Content { Parts = [] };
        contents.Parts.Add(new Part
        {
            Text = $"""
            Erstelle auf Grundlage der beigefügten PDF genau eine deutschsprachige Multiple-Choice-Frage
            für das Kapitel "{chapterTitle}".

            Regeln:
            - Die Frage muss fachlich aus der PDF ableitbar sein.
            - Es muss genau vier Antwortoptionen geben.
            - Genau eine Antwortoption muss korrekt sein.
            - Die falschen Antwortoptionen sollen plausibel, aber eindeutig falsch sein.
            - Vermeide Antwortoptionen wie "alle Antworten sind richtig" oder "keine Antwort ist richtig".
            - Antworte ausschließlich im vorgegebenen JSON-Schema.
            """
        });
        contents.Parts.Add(new Part
        {
            FileData = new FileData
            {
                FileUri = uploadedFile.Uri,
                MimeType = "application/pdf"
            }
        });

        var response = await client.Models.GenerateContentAsync(
            model: _options.Model,
            contents: contents,
            config: CreateGeneratedQuestionConfig(),
            cancellationToken: cancellationToken);

        var generatedQuestion = DeserializeGeneratedQuestion(response.Text);
        ValidateGeneratedQuestion(generatedQuestion);

        return generatedQuestion;
    }

    public async Task<string> ReviewQuestionAsync(
        string questionText,
        IEnumerable<string> answerOptions,
        CancellationToken cancellationToken = default)
    {
        var answers = string.Join("\n", answerOptions.Select((answer, index) => $"{index + 1}. {answer}"));
        var prompt = $"""
        Prüfe die folgende Multiple-Choice-Frage für eine deutschsprachige Hochschulklausur.

        Prüfkriterien:
        - sprachliche Korrektheit
        - fachliche Eindeutigkeit
        - genau eine eindeutig korrekte Antwort
        - plausible, aber eindeutig falsche Distraktoren

        Frage:
        {questionText}

        Antwortoptionen:
        {answers}

        Antworte kurz auf Deutsch. Nenne zuerst ein Gesamturteil und danach konkrete Verbesserungsvorschläge.
        """;

        var client = CreateClient();
        var response = await client.Models.GenerateContentAsync(
            model: _options.Model,
            contents: prompt,
            cancellationToken: cancellationToken);

        return response.Text?.Trim() ?? string.Empty;
    }

    private Client CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini ist nicht konfiguriert. Lege den API-Key mit `dotnet user-secrets set \"Gemini:ApiKey\" \"...\"` ab.");
        }

        return new Client(apiKey: _options.ApiKey);
    }

    private static GenerateContentConfig CreateGeneratedQuestionConfig()
    {
        var answerOptionSchema = new Schema
        {
            Type = GeminiSchemaType.Object,
            Properties = new Dictionary<string, Schema>
            {
                ["answerText"] = new() { Type = GeminiSchemaType.String, Title = "answerText" },
                ["isCorrect"] = new() { Type = GeminiSchemaType.Boolean, Title = "isCorrect" }
            },
            PropertyOrdering = ["answerText", "isCorrect"],
            Required = ["answerText", "isCorrect"],
            Title = "answerOption"
        };

        return new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = new Schema
            {
                Type = GeminiSchemaType.Object,
                Properties = new Dictionary<string, Schema>
                {
                    ["questionText"] = new() { Type = GeminiSchemaType.String, Title = "questionText" },
                    ["options"] = new()
                    {
                        Type = GeminiSchemaType.Array,
                        Items = answerOptionSchema,
                        MinItems = 4,
                        MaxItems = 4,
                        Title = "options"
                    }
                },
                PropertyOrdering = ["questionText", "options"],
                Required = ["questionText", "options"],
                Title = "generatedQuestion"
            }
        };
    }

    private GeneratedQuestionDto DeserializeGeneratedQuestion(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("Gemini hat keine Frage zurückgegeben.");
        }

        var json = responseText.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json.Trim('`').Replace("json", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        return JsonSerializer.Deserialize<GeneratedQuestionDto>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Gemini hat kein gültiges Fragen-JSON zurückgegeben.");
    }

    private static void ValidateGeneratedQuestion(GeneratedQuestionDto question)
    {
        if (string.IsNullOrWhiteSpace(question.QuestionText))
        {
            throw new InvalidOperationException("Gemini hat keinen Fragentext zurückgegeben.");
        }

        if (question.Options.Count != 4)
        {
            throw new InvalidOperationException("Gemini muss genau vier Antwortoptionen zurückgeben.");
        }

        if (question.Options.Count(option => option.IsCorrect) != 1)
        {
            throw new InvalidOperationException("Gemini muss genau eine korrekte Antwortoption zurückgeben.");
        }

        if (question.Options.Any(option => string.IsNullOrWhiteSpace(option.AnswerText)))
        {
            throw new InvalidOperationException("Gemini hat eine leere Antwortoption zurückgegeben.");
        }
    }
}
