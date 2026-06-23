# AGENTS.md

Kontext- und Arbeitsanweisungen für KI-Coding-Agents an diesem Projekt.
Dieses Dokument fasst das Lastenheft des Web-Engineering-Gruppenprojekts in
agentenfreundlicher Form zusammen. Es ist die maßgebliche Referenz für Scope,
Datenmodell und Konventionen.

## Projektüberblick

Eine Web-Anwendung, die einen Lehrstuhl bei der Erstellung von
Multiple-Choice-Klausuren unterstützt. Verwaltet werden Lehrveranstaltungen,
deren Lehrmaterialien (PDF) und Fragenkataloge. Fragen aus dem Katalog einer
Lehrveranstaltung lassen sich zufällig zu einer Prüfung zusammenstellen. Per
KI-Integration können zu jedem Kapitel Fragen generiert und bestehende Fragen
auf sprachliche Korrektheit und Eindeutigkeit geprüft werden.

## Tech-Stack

Sofern im Repo nicht anders festgelegt, gelten folgende Annahmen — bitte mit
dem tatsächlichen Stand abgleichen und bei Abweichung diese Datei aktualisieren:

- **Backend/Framework:** ASP.NET Core MVC (Razor Views, da das Lastenheft
  explizit von „Views" und Bearbeitungsformularen spricht).
- **Persistenz:** Entity Framework Core gegen Azure SQL Database.
- **Frontend:** Razor Views + Bootstrap-Komponenten (Badges, List-Groups,
  Modals). Kein separates SPA-Framework gefordert.
- **Dateispeicher:** Azure Blob Storage für hochgeladene PDFs.
- **KI:** Gemini oder ein anderes LLM, angebunden über einen serverseitigen
  Dienst (Frage generieren / Frage prüfen).
- **Hosting:** Azure (App + Datenbank).

## Datenmodell

Kern des Modells sind die **Lehrveranstaltungen**. Entitäten und Felder:

| Entität           | Felder                                          |
|-------------------|-------------------------------------------------|
| Lehrveranstaltung | Titel, Dozentenname, Niveau (Bachelor/Master)   |
| Kapitel           | Titel, Kapitelnummer, Vorlesungsfolien (PDF)    |
| MC-Frage          | Fragentext                                      |
| MC-Antwortoption  | Antworttext, Korrektheit (richtig/falsch)       |
| Prüfung           | Datum                                           |

### Beziehungen

- Eine **Lehrveranstaltung** hat mehrere **Kapitel** und mehrere **Prüfungen**.
- Jedes **Kapitel** gehört zu genau einer Lehrveranstaltung und hat mehrere
  **MC-Fragen**.
- Jede **MC-Frage** gehört zu genau einem Kapitel und hat genau vier
  **MC-Antwortoptionen**, von denen genau eine korrekt ist.
- Jede **MC-Antwortoption** gehört zu genau einer MC-Frage.
- Jede **Prüfung** gehört zu genau einer Lehrveranstaltung.
- **MC-Frage ↔ Prüfung** ist eine **n:m-Beziehung**: Eine Frage kann in mehreren
  Prüfungen vorkommen, eine Prüfung umfasst mehrere Fragen.

### Regeln

- **Kaskadierendes Löschen:** Beim Löschen eines Datensatzes werden abhängige
  Datensätze anderer Tabellen mitgelöscht (z. B. Lehrveranstaltung → Kapitel →
  Fragen → Antwortoptionen).
- **Seed-Daten:** Die Datenbank muss zu Demozwecken einige Beispieldaten
  enthalten.

## Kernfunktionen

1. **CRUD** für Lehrveranstaltungen, Kapitel sowie Fragen/Antworten je Kapitel
   (Anlegen, Bearbeiten, Löschen — inkl. kaskadierendem Löschen).
2. **PDF-Upload je Kapitel** (Foliensatz, Artikel oder Buchkapitel), abgelegt im
   Azure Blob Storage.
3. **Fragenkatalog je Kapitel:** Eine Frage besteht aus Fragentext und vier
   Antwortoptionen, von denen genau eine korrekt ist.
4. **KI – Fragen generieren:** Wenn zu einem Kapitel eine PDF hochgeladen wurde,
   kann die KI auf deren Grundlage eine neue Frage samt Antwortoptionen erzeugen
   und der Liste hinzufügen.
5. **KI – Frage prüfen:** Eine bestehende Frage kann per KI darauf geprüft
   werden, ob Frage und Antworten sprachlich korrekt und eindeutig beantwortbar
   sind (genau eine korrekte Antwort).
6. **Prüfung zusammenstellen:** Aus der Gesamtheit aller Fragen zu den Kapiteln
   einer Lehrveranstaltung werden per Zufall **n Fragen ohne Dubletten**
   ausgewählt und mit der Prüfung verknüpft. **n ist frei konfigurierbar**
   (Demozweck).

## Benutzerschnittstelle

Bootstrap-Komponenten verwenden. Aufbau:

- **Startseite:** Menüleiste, darunter ein „Hero" mit Überschrift und kurzem
  Beschreibungstext der Anwendung.
- **Liste der Lehrveranstaltungen** (sortiert nach Titel): Einträge
  hinzufügen/bearbeiten/löschen. Je Eintrag zwei **Badge-Buttons**: Anzahl
  Kapitel und Anzahl gespeicherter Prüfungen.
- **Kapitel-Liste** (über den Kapitel-Badge erreichbar, sortiert nach
  Kapitelnummer): Oberhalb die Stammdaten der Lehrveranstaltung. Einträge
  hinzufügen/bearbeiten/löschen. Bei vorhandener Datei ein Link darauf; im
  Bearbeitungsmodus kann die Datei gelöscht werden, bei fehlender Datei wird ein
  File-Upload-Feld angezeigt. Je Kapitel ein **Badge-Button** mit der
  Frageanzahl.
- **Fragen-Liste** (über den Frage-Badge erreichbar): Jede Frage als
  **Bootstrap-List-Group** — erstes Item der Fragentext, darunter die
  Antwortoptionen; die korrekte Option ist gut erkennbar. Letztes Item enthält
  Buttons **Bearbeiten / Löschen / Prüfen**. Oberhalb der Liste ein Button
  **„Neue Frage generieren"** (KI auf Basis der hochgeladenen PDF).
    - **Bearbeiten:** separate View; Optionen haben je ein **Radio-Control** zur
      Auswahl der korrekten Option.
    - **Prüfen:** serverseitiger KI-Aufruf; Ergebnis in einem **Bootstrap-Modal**.
- **Prüfungs-Liste** (über den Prüfungs-Badge erreichbar): Oberhalb Button
  **„Neue Prüfung erstellen"** (zufällige Zusammenstellung aus n Fragen). Jeder
  Eintrag verlinkt auf eine **druckbare** Liste der zugehörigen Fragen.


## Bereitstellung

- App und Datenbank auf **Azure** bereitstellen.
- Hochgeladene Kapitel-Dateien in **Azure Storage** ablegen.
- Als KI **Gemini** oder ein anderes LLM nutzen.

## Arbeitsanweisungen für Agents

- **Scope einhalten:** Funktionen und Datenmodell nicht über das Lastenheft
  hinaus erweitern, ohne dies explizit zu kennzeichnen.
- **Sprache:** Domänenbegriffe und UI-Texte auf Deutsch (Lehrveranstaltung,
  Kapitel, Prüfung …); Code-Identifier nach Teamkonvention (siehe unten).
- **Secrets:** API-Keys (LLM), Connection-Strings und Storage-Keys niemals in
  den Code committen — über Konfiguration/User-Secrets bzw. Azure App Settings.
- **Kaskaden & n:m** beim Anpassen des Modells konsistent in EF-Core-Migrations
  abbilden; Seed-Daten nach Modelländerungen aktualisieren.
- **KI-Aufrufe** kapseln (eigener Service/Interface), damit das LLM austauschbar
  bleibt; serverseitig aufrufen, nie Keys an den Client geben.
- **Determinismus testen:** Zufallsauswahl für Prüfungen muss Dubletten
  ausschließen und mit konfigurierbarem n funktionieren.

## Konventionen

### C#-Identifier

Verwende englische, verständliche und pragmatische Namen. Bevorzuge klare Bezeichnungen gegenüber unnötigen Abkürzungen.

* Klassen, Methoden, Properties und Enums: `PascalCase`
* Lokale Variablen und Parameter: `camelCase`
* Private Felder: `_camelCase`
* Interfaces: Präfix `I`, z. B. `IUserService`
* Asynchrone Methoden: Suffix `Async`, z. B. `GetUsersAsync`
* Boolesche Werte als Frage oder Zustand formulieren, z. B. `isActive`, `hasPermission` oder `canEdit`

```csharp
public interface IOrderService
{
    Task<Order> GetOrderAsync(Guid orderId);
}

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;

    public async Task<Order> GetOrderAsync(Guid orderId)
    {
        var order = await _orderRepository.GetByIdAsync(orderId);
        return order;
    }
}
```

### Branches und Commits

Nach Änderungen, die einen neuen Commit erfordern, gib immer eine passende Commit-Nachricht nach den oben definierten Conventional-Commit-Konventionen an.

Verwende dabei das Format:

<type>: <kurze Beschreibung der Änderung>

Beispiel:

feat: add user registration endpoint

Verwende sprechende Branch-Namen mit einem passenden Präfix:

```text
feature/user-registration
fix/login-validation
refactor/order-service
docs/update-readme
chore/update-dependencies
```

Commit-Nachrichten müssen dem Standard von [Conventional Commits](https://www.conventionalcommits.org/) folgen:

```text
feat: add user registration endpoint
fix: validate empty email addresses
refactor: simplify order mapping
docs: update setup instructions
test: add authentication service tests
chore: update NuGet packages
```

### Build und Ausführung

Projekt bauen:

```bash
dotnet build
```

Anwendung lokal starten:

```bash
dotnet run
```

### Tests

Alle Tests ausführen:

```bash
dotnet test
```

### Datenbankmigrationen

Neue Migration erstellen:

```bash
dotnet ef migrations add <MigrationName>
```

Migrationen auf die Datenbank anwenden:

```bash
dotnet ef database update
```

Migrationen klar und fachlich benennen:

```bash
dotnet ef migrations add AddUserRoles
dotnet ef migrations add CreateInitialSchema
```

