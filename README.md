# Multiple Choice Manager


ASP.NET-Core-MVC-Anwendung zur Verwaltung von Lehrveranstaltungen,
Kapiteln, Multiple-Choice-Fragen und Prüfungen. 

Entwickelt im Rahmen eines Uni-Projekts im Modul Web Engineering an der JMU Würzburg 
im SoSe 26.

## Funktionen

- Lehrveranstaltungen, Kapitel, Fragen und Prüfungen verwalten
- PDF-Upload pro Kapitel über Azure Blob Storage
- Fragenkatalog mit vier Antwortoptionen und genau einer korrekten Antwort
- KI-gestützte Fragengenerierung und Fragenprüfung über Gemini
- Zufällige Prüfungserstellung mit frei wählbarer Fragenanzahl ohne Dubletten
- Druckbare Prüfungsansicht

## Tech-Stack

- ASP.NET Core MVC mit Razor Views
- Entity Framework Core
- Azure SQL Database
- Azure Blob Storage
- Gemini API
- Bootstrap

## App-Zugriff

Die Anwendung kann lokal oder online gestartet werden.

**Lokal starten:**
```bash
cd MultipleChoiceManager
dotnet run
```

Die Anwendung ist dann unter http://localhost:5262/ erreichbar.

**Online:**
Die App ist unter https://multiple-choice-manager-hseyakhwdhhra7cg.westeurope-01.azurewebsites.net/ verfügbar.


## Beteiligte

| Name | Matrikelnummer |
| --- | --- |
| Kilian Jetschni | 3015753 |
| Nicolas Retsch | 3011733 |
| Omid Sedighi-Mornani | 2975460 |

