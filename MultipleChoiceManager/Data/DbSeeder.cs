using Microsoft.EntityFrameworkCore;
using MultipleChoiceManager.Models;

namespace MultipleChoiceManager.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        if (await context.Courses.AnyAsync())
        {
            return;
        }

        var courses = new List<Course>
        {
            CreateWebEngineeringCourse(),
            CreateMachineLearningCourse(),
            CreateDatabasesCourse()
        };

        // Fester Seed, damit die Demo-Prüfungen reproduzierbar zusammengestellt werden.
        var random = new Random(42);

        foreach (var course in courses)
        {
            course.Exams.AddRange(CreateExams(course, random));
        }

        context.Courses.AddRange(courses);
        await context.SaveChangesAsync();
    }

    private static List<Exam> CreateExams(Course course, Random random)
    {
        var allQuestions = course.Chapters.SelectMany(ch => ch.Questions).ToList();
        var exams = new List<Exam>();

        var examDates = new[]
        {
            new DateTime(2026, 2, 12),
            new DateTime(2026, 7, 23)
        };

        foreach (var date in examDates)
        {
            var questionCount = Math.Min(5, allQuestions.Count);
            var selectedQuestions = allQuestions
                .OrderBy(_ => random.Next())
                .Take(questionCount)
                .ToList();

            exams.Add(new Exam
            {
                Date = date,
                Questions = selectedQuestions
            });
        }

        return exams;
    }

    private static Question CreateQuestion(string text, string correctAnswer, params string[] wrongAnswers)
    {
        var question = new Question { Text = text };
        question.AnswerOptions.Add(new AnswerOption { Text = correctAnswer, IsCorrect = true });
        question.AnswerOptions.AddRange(wrongAnswers.Select(answer => new AnswerOption { Text = answer, IsCorrect = false }));
        return question;
    }

    private static Course CreateWebEngineeringCourse() => new()
    {
        Title = "Web Engineering",
        LecturerName = "Prof. Dr. Anna Müller",
        Level = CourseLevel.Bachelor,
        Chapters =
        {
            new Chapter
            {
                Title = "Grundlagen des World Wide Web",
                ChapterNumber = 1,
                Questions =
                {
                    CreateQuestion(
                        "Wofür steht die Abkürzung HTTP?",
                        "Hypertext Transfer Protocol",
                        "Hyperlink Text Processing",
                        "High Transfer Text Protocol",
                        "Hypertext Terminal Program"),
                    CreateQuestion(
                        "Welcher Statuscode signalisiert eine erfolgreiche HTTP-Anfrage?",
                        "200",
                        "301",
                        "404",
                        "500"),
                    CreateQuestion(
                        "Welche Aufgabe hat das Domain Name System (DNS)?",
                        "Es übersetzt Domainnamen in IP-Adressen.",
                        "Es verschlüsselt die Datenübertragung im Web.",
                        "Es speichert Cookies auf dem Client.",
                        "Es rendert HTML-Seiten im Browser."),
                    CreateQuestion(
                        "Welches Merkmal beschreibt das HTTP-Protokoll?",
                        "Es ist zustandslos.",
                        "Es hält dauerhaft eine Sitzung pro Nutzer.",
                        "Es funktioniert nur mit verschlüsselten Verbindungen.",
                        "Es überträgt ausschließlich Binärdaten.")
                }
            },
            new Chapter
            {
                Title = "HTML und CSS",
                ChapterNumber = 2,
                Questions =
                {
                    CreateQuestion(
                        "Welches HTML-Element definiert die wichtigste Überschrift einer Seite?",
                        "<h1>",
                        "<header>",
                        "<title>",
                        "<top>"),
                    CreateQuestion(
                        "Wofür steht CSS?",
                        "Cascading Style Sheets",
                        "Computer Styled Sections",
                        "Creative Style System",
                        "Cascading Script Syntax"),
                    CreateQuestion(
                        "Wie bindet man eine externe CSS-Datei in HTML ein?",
                        "Mit einem <link>-Element im <head>.",
                        "Mit einem <css>-Element im <body>.",
                        "Mit dem Attribut style=\"file\" am <html>-Element.",
                        "Mit einem <import>-Element im <footer>.")
                }
            },
            new Chapter
            {
                Title = "ASP.NET Core MVC",
                ChapterNumber = 3,
                Questions =
                {
                    CreateQuestion(
                        "Wofür steht das \"M\" im MVC-Muster?",
                        "Model",
                        "Module",
                        "Middleware",
                        "Method"),
                    CreateQuestion(
                        "Welche Komponente nimmt in ASP.NET Core MVC eingehende Requests entgegen?",
                        "Der Controller",
                        "Die View",
                        "Das Model",
                        "Die Razor-Engine"),
                    CreateQuestion(
                        "Welche Dateiendung haben Razor-Views?",
                        ".cshtml",
                        ".razor.html",
                        ".aspx",
                        ".view")
                }
            }
        }
    };

    private static Course CreateMachineLearningCourse() => new()
    {
        Title = "Machine Learning",
        LecturerName = "Prof. Dr. Jonas Weber",
        Level = CourseLevel.Master,
        Chapters =
        {
            new Chapter
            {
                Title = "Einführung in das maschinelle Lernen",
                ChapterNumber = 1,
                Questions =
                {
                    CreateQuestion(
                        "Was kennzeichnet überwachtes Lernen (Supervised Learning)?",
                        "Das Training erfolgt mit gelabelten Daten.",
                        "Das Modell lernt ausschließlich durch Belohnungen.",
                        "Es werden keine Trainingsdaten benötigt.",
                        "Die Daten werden ohne Zielvariable gruppiert."),
                    CreateQuestion(
                        "Welches der folgenden Verfahren ist ein Klassifikationsalgorithmus?",
                        "Logistische Regression",
                        "K-Means",
                        "Hauptkomponentenanalyse (PCA)",
                        "Lineare Regression"),
                    CreateQuestion(
                        "Was beschreibt der Begriff Overfitting?",
                        "Das Modell passt sich zu stark an die Trainingsdaten an und generalisiert schlecht.",
                        "Das Modell ist zu einfach, um Muster zu erkennen.",
                        "Die Trainingsdaten enthalten zu wenige Merkmale.",
                        "Das Modell benötigt zu viel Rechenzeit beim Training.")
                }
            },
            new Chapter
            {
                Title = "Neuronale Netze",
                ChapterNumber = 2,
                Questions =
                {
                    CreateQuestion(
                        "Welche Funktion hat eine Aktivierungsfunktion in einem neuronalen Netz?",
                        "Sie führt Nichtlinearität in das Netz ein.",
                        "Sie initialisiert die Gewichte des Netzes.",
                        "Sie reduziert die Anzahl der Neuronen.",
                        "Sie speichert die Trainingsdaten."),
                    CreateQuestion(
                        "Wie heißt das Standardverfahren zur Berechnung der Gradienten in neuronalen Netzen?",
                        "Backpropagation",
                        "Forward Chaining",
                        "Gradient Boosting",
                        "Random Search"),
                    CreateQuestion(
                        "Welche Netzarchitektur eignet sich besonders für Bilddaten?",
                        "Convolutional Neural Network (CNN)",
                        "Entscheidungsbaum",
                        "K-Nearest-Neighbors",
                        "Naive Bayes")
                }
            }
        }
    };

    private static Course CreateDatabasesCourse() => new()
    {
        Title = "Datenbanksysteme",
        LecturerName = "Prof. Dr. Sabine Fischer",
        Level = CourseLevel.Bachelor,
        Chapters =
        {
            new Chapter
            {
                Title = "Relationale Datenmodelle",
                ChapterNumber = 1,
                Questions =
                {
                    CreateQuestion(
                        "Was ist ein Primärschlüssel?",
                        "Ein Attribut, das jeden Datensatz einer Tabelle eindeutig identifiziert.",
                        "Ein Attribut, das auf eine andere Tabelle verweist.",
                        "Ein Index zur Beschleunigung von Abfragen.",
                        "Eine Spalte, die nur eindeutige Textwerte enthält."),
                    CreateQuestion(
                        "Welche Beziehung bildet eine Zwischentabelle (Join-Tabelle) ab?",
                        "Eine n:m-Beziehung",
                        "Eine 1:1-Beziehung",
                        "Eine 1:n-Beziehung",
                        "Eine rekursive Beziehung"),
                    CreateQuestion(
                        "Was bewirkt kaskadierendes Löschen (ON DELETE CASCADE)?",
                        "Abhängige Datensätze werden beim Löschen des Elterndatensatzes mitgelöscht.",
                        "Gelöschte Datensätze werden in einer Archivtabelle gesichert.",
                        "Das Löschen wird verhindert, solange abhängige Datensätze existieren.",
                        "Fremdschlüssel werden beim Löschen auf einen Standardwert gesetzt.")
                }
            },
            new Chapter
            {
                Title = "SQL-Grundlagen",
                ChapterNumber = 2,
                Questions =
                {
                    CreateQuestion(
                        "Mit welchem SQL-Befehl werden Daten aus einer Tabelle gelesen?",
                        "SELECT",
                        "READ",
                        "FETCH",
                        "GET"),
                    CreateQuestion(
                        "Welche Klausel filtert Zeilen in einer SQL-Abfrage?",
                        "WHERE",
                        "ORDER BY",
                        "GROUP BY",
                        "HAVING COUNT"),
                    CreateQuestion(
                        "Was bewirkt der SQL-Befehl INSERT INTO?",
                        "Er fügt neue Datensätze in eine Tabelle ein.",
                        "Er aktualisiert bestehende Datensätze.",
                        "Er legt eine neue Tabelle an.",
                        "Er verbindet zwei Tabellen miteinander.")
                }
            }
        }
    };
}
