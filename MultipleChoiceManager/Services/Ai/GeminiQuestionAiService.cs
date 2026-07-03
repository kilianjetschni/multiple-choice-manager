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
            - Prüfe zuerst, ob die PDF ausreichend fachliche Grundlage für eine eindeutige Frage bietet.
            - Wenn die PDF leer, nicht lesbar, thematisch unklar oder fachlich zu dünn ist, setze canCreate auf false,
              beschreibe den Grund in rejectionReason und erfinde keine Frage.
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

    public async Task<QuestionReviewResultDto> ReviewQuestionAsync(
        string questionText,
        IEnumerable<AnswerOptionReviewDto> answerOptions,
        CancellationToken cancellationToken = default)
    {
        var answers = string.Join(
            "\n",
            answerOptions.Select((answer, index) =>
                $"{index + 1}. [{(answer.IsCorrect ? "als korrekt markiert" : "als falsch markiert")}] {answer.Text}"));
        var prompt = $"""
        Prüfe die folgende Multiple-Choice-Frage für eine deutschsprachige Hochschulklausur.

        Prüfkriterien:
        - sprachliche Korrektheit
        - fachliche Eindeutigkeit
        - genau eine als korrekt markierte Antwort
        - die als korrekt markierte Antwort ist fachlich tatsächlich korrekt
        - plausible, aber eindeutig falsche Distraktoren

        Frage:
        {questionText}

        Antwortoptionen:
        {answers}

        Antworte ausschließlich im vorgegebenen JSON-Schema.
        """;

        var client = CreateClient();
        var response = await client.Models.GenerateContentAsync(
            model: _options.Model,
            contents: prompt,
            config: CreateReviewConfig(),
            cancellationToken: cancellationToken);

        var review = DeserializeReview(response.Text);
        ValidateReview(review);

        return review;
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
                    ["canCreate"] = new() { Type = GeminiSchemaType.Boolean, Title = "canCreate" },
                    ["rejectionReason"] = new() { Type = GeminiSchemaType.String, Title = "rejectionReason" },
                    ["questionText"] = new() { Type = GeminiSchemaType.String, Title = "questionText" },
                    ["options"] = new()
                    {
                        Type = GeminiSchemaType.Array,
                        Items = answerOptionSchema,
                        MaxItems = 4,
                        Title = "options"
                    }
                },
                PropertyOrdering = ["canCreate", "rejectionReason", "questionText", "options"],
                Required = ["canCreate", "rejectionReason", "questionText", "options"],
                Title = "generatedQuestion"
            }
        };
    }

    private static GenerateContentConfig CreateReviewConfig()
    {
        var stringArraySchema = new Schema
        {
            Type = GeminiSchemaType.Array,
            Items = new Schema { Type = GeminiSchemaType.String }
        };

        return new GenerateContentConfig
        {
            ResponseMimeType = "application/json",
            ResponseSchema = new Schema
            {
                Type = GeminiSchemaType.Object,
                Properties = new Dictionary<string, Schema>
                {
                    ["overallVerdict"] = new() { Type = GeminiSchemaType.String, Title = "overallVerdict" },
                    ["summary"] = new() { Type = GeminiSchemaType.String, Title = "summary" },
                    ["hasCriticalIssues"] = new() { Type = GeminiSchemaType.Boolean, Title = "hasCriticalIssues" },
                    ["strengths"] = stringArraySchema,
                    ["issues"] = stringArraySchema,
                    ["suggestions"] = stringArraySchema
                },
                PropertyOrdering = ["overallVerdict", "summary", "hasCriticalIssues", "strengths", "issues", "suggestions"],
                Required = ["overallVerdict", "summary", "hasCriticalIssues", "strengths", "issues", "suggestions"],
                Title = "questionReview"
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

    private QuestionReviewResultDto DeserializeReview(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("Gemini hat keine Prüfung zurückgegeben.");
        }

        var json = responseText.Trim();
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json.Trim('`').Replace("json", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        }

        return JsonSerializer.Deserialize<QuestionReviewResultDto>(json, _jsonOptions)
            ?? throw new InvalidOperationException("Gemini hat kein gültiges Prüfungs-JSON zurückgegeben.");
    }

    private static void ValidateGeneratedQuestion(GeneratedQuestionDto question)
    {
        if (!question.CanCreate)
        {
            var reason = string.IsNullOrWhiteSpace(question.RejectionReason)
                ? "Die PDF bietet keine ausreichende Grundlage für eine fachlich saubere Frage."
                : question.RejectionReason;

            throw new InvalidOperationException($"Die Fragenerstellung wurde abgebrochen: {reason}");
        }

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

    private static void ValidateReview(QuestionReviewResultDto review)
    {
        if (string.IsNullOrWhiteSpace(review.OverallVerdict))
        {
            throw new InvalidOperationException("Gemini hat kein Gesamturteil zurückgegeben.");
        }

        if (string.IsNullOrWhiteSpace(review.Summary))
        {
            throw new InvalidOperationException("Gemini hat keine Zusammenfassung zurückgegeben.");
        }
    }
}
