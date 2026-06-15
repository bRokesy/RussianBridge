using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class GoogleSheetsLessonSettings
{
    public const string DefaultLessonsSpreadsheetUrl = "https://docs.google.com/spreadsheets/d/1yRbTMZg8SgXaNTiQQNOsASyODc1wdJIttJ746R5QI9s/edit?usp=sharing";

    [Tooltip("Load lesson data from Google Sheets CSV instead of serialized lesson assets.")]
    public bool enabled = true;

    [Tooltip("Request timeout in seconds for sheet and media downloads.")]
    public float requestTimeout = 20f;

    [Tooltip("One or more Google Sheets sources. A normal spreadsheet URL can load all lesson worksheets automatically.")]
    public List<GoogleSheetCsvSource> sources = new List<GoogleSheetCsvSource>
    {
        new GoogleSheetCsvSource
        {
            name = "RussianBridge Lessons",
            spreadsheetIdOrUrl = DefaultLessonsSpreadsheetUrl,
            gid = "0",
            loadAllWorksheets = true
        }
    };

    public bool HasSources()
    {
        return sources != null && sources.Any(source => source != null && source.HasValue());
    }
}

[Serializable]
public class GoogleSheetCsvSource
{
    [Tooltip("Optional label used only in logs.")]
    public string name;

    [Tooltip("A Google spreadsheet id, or a full Google Sheets URL.")]
    public string spreadsheetIdOrUrl;

    [Tooltip("Sheet tab gid. Used when spreadsheetIdOrUrl is only an id, or when the URL has no gid.")]
    public string gid = "0";

    [Tooltip("When enabled, all lesson tabs in the spreadsheet are discovered and merged automatically.")]
    public bool loadAllWorksheets = true;

    [Tooltip("Optional direct CSV URL. If set, this is used before spreadsheetIdOrUrl.")]
    public string csvUrl;

    public bool HasValue()
    {
        return !string.IsNullOrWhiteSpace(csvUrl) || !string.IsNullOrWhiteSpace(spreadsheetIdOrUrl);
    }

    public string BuildCsvUrl()
    {
        return BuildCsvUrl(gid);
    }

    public string BuildCsvUrl(string gidOverride)
    {
        string raw = !string.IsNullOrWhiteSpace(csvUrl) ? csvUrl : spreadsheetIdOrUrl;
        return GoogleSheetsLessonLoader.ToCsvUrl(raw, gidOverride);
    }

    public bool CanDiscoverWorksheets()
    {
        if (!loadAllWorksheets || !string.IsNullOrWhiteSpace(csvUrl) || string.IsNullOrWhiteSpace(spreadsheetIdOrUrl))
            return false;

        string spreadsheetId;
        return GoogleSheetsLessonLoader.TryGetSpreadsheetId(spreadsheetIdOrUrl, out spreadsheetId);
    }

    public string DisplayName => string.IsNullOrWhiteSpace(name) ? BuildCsvUrl() : name;
}

public static class GoogleSheetsLessonLoader
{
    private const string GoogleSheetsExportUrl = "https://docs.google.com/spreadsheets/d/{0}/gviz/tq?tqx=out:csv&gid={1}";
    private const string GoogleSheetsEditUrl = "https://docs.google.com/spreadsheets/d/{0}/edit";
    private static readonly Regex SpreadsheetIdRegex = new Regex(@"/spreadsheets/d/([^/?#]+)", RegexOptions.IgnoreCase);
    private static readonly Regex QueryValueRegex = new Regex(@"[?&#]gid=([^&#]+)", RegexOptions.IgnoreCase);
    private static readonly Regex HeaderCleanupRegex = new Regex(@"[\s\-]+");
    private static readonly Regex WorksheetMetadataRegex = new Regex("\\[21350203,\"\\[\\d+,0,\\\\\"(?<gid>\\d+)\\\\\".*?\\[\\[0,0,\\\\\"(?<title>[^\\\\\"]+)", RegexOptions.Singleline);
    private static readonly Regex GidFallbackRegex = new Regex(@"gid(?:=|%3D)(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex LessonWorksheetNameRegex = new Regex(@"^\s*lesson\s*\d+\s*$", RegexOptions.IgnoreCase);

    public static IEnumerator Load(
        GoogleSheetsLessonSettings settings,
        Action<List<LessonData>> onLoaded,
        Action<string> onError)
    {
        if (settings == null || !settings.HasSources())
        {
            onError?.Invoke("Google Sheets source is empty.");
            yield break;
        }

        List<SheetRow> allRows = new List<SheetRow>();
        int timeout = Mathf.Max(1, Mathf.RoundToInt(settings.requestTimeout));

        foreach (GoogleSheetCsvSource source in settings.sources)
        {
            if (source == null || !source.HasValue())
                continue;

            List<SheetDownload> downloads = null;
            yield return ResolveDownloads(source, timeout, result => downloads = result);

            foreach (SheetDownload download in downloads)
            {
                if (string.IsNullOrWhiteSpace(download.Url))
                    continue;

                using (UnityWebRequest request = UnityWebRequest.Get(download.Url))
                {
                    request.timeout = timeout;
                    yield return request.SendWebRequest();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        onError?.Invoke($"Failed to load lesson sheet '{download.DisplayName}': {request.error}");
                        continue;
                    }

                    string csv = request.downloadHandler.text;
                    if (LooksLikeHtml(csv))
                    {
                        onError?.Invoke($"Google Sheets source '{download.DisplayName}' returned HTML instead of CSV. Check sharing permissions and sheet URL.");
                        continue;
                    }

                    List<SheetRow> rows = CsvTableParser.Parse(csv, download.DisplayName);
                    allRows.AddRange(rows);
                }
            }
        }

        if (allRows.Count == 0)
        {
            onError?.Invoke("Google Sheets returned no lesson rows.");
            yield break;
        }

        LessonCatalogBuilder builder = new LessonCatalogBuilder(timeout);
        yield return builder.Build(allRows);

        List<LessonData> lessons = builder.GetLessons();
        if (lessons.Count == 0)
        {
            onError?.Invoke("Google Sheets rows did not produce any valid lessons.");
            yield break;
        }

        onLoaded?.Invoke(lessons);
    }

    public static string ToCsvUrl(string value, string fallbackGid)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string trimmed = value.Trim();

        if (!IsHttpUrl(trimmed))
        {
            string gid = string.IsNullOrWhiteSpace(fallbackGid) ? "0" : fallbackGid.Trim();
            return string.Format(GoogleSheetsExportUrl, trimmed, gid);
        }

        if (trimmed.IndexOf("output=csv", StringComparison.OrdinalIgnoreCase) >= 0 ||
            trimmed.IndexOf("format=csv", StringComparison.OrdinalIgnoreCase) >= 0)
            return trimmed;

        string spreadsheetId;
        if (!TryGetSpreadsheetId(trimmed, out spreadsheetId))
            return trimmed;

        string gidFromUrl = ExtractGid(trimmed);
        string gidValue = string.IsNullOrWhiteSpace(gidFromUrl) ? fallbackGid : gidFromUrl;

        if (string.IsNullOrWhiteSpace(gidValue))
            gidValue = "0";

        return string.Format(GoogleSheetsExportUrl, spreadsheetId, gidValue);
    }

    public static bool TryGetSpreadsheetId(string value, out string spreadsheetId)
    {
        spreadsheetId = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.Trim();
        if (!IsHttpUrl(trimmed))
        {
            spreadsheetId = trimmed;
            return true;
        }

        Match match = SpreadsheetIdRegex.Match(trimmed);
        if (!match.Success)
            return false;

        spreadsheetId = match.Groups[1].Value;
        return !string.IsNullOrWhiteSpace(spreadsheetId);
    }

    private static IEnumerator ResolveDownloads(
        GoogleSheetCsvSource source,
        int timeout,
        Action<List<SheetDownload>> onResolved)
    {
        List<SheetDownload> downloads = new List<SheetDownload>();

        if (source.CanDiscoverWorksheets())
        {
            List<WorksheetInfo> worksheets = null;
            yield return DiscoverWorksheets(source, timeout, result => worksheets = result);

            if (worksheets != null && worksheets.Count > 0)
            {
                List<WorksheetInfo> lessonWorksheets = worksheets
                    .Where(worksheet => IsLessonWorksheetName(worksheet.Title))
                    .ToList();

                if (lessonWorksheets.Count > 0)
                    worksheets = lessonWorksheets;

                foreach (WorksheetInfo worksheet in worksheets)
                {
                    downloads.Add(new SheetDownload
                    {
                        DisplayName = $"{source.DisplayName} / {worksheet.DisplayName}",
                        Url = source.BuildCsvUrl(worksheet.Gid)
                    });
                }
            }
        }

        if (downloads.Count == 0)
        {
            downloads.Add(new SheetDownload
            {
                DisplayName = source.DisplayName,
                Url = source.BuildCsvUrl()
            });
        }

        onResolved?.Invoke(downloads);
    }

    private static IEnumerator DiscoverWorksheets(
        GoogleSheetCsvSource source,
        int timeout,
        Action<List<WorksheetInfo>> onDiscovered)
    {
        string spreadsheetId;
        if (!TryGetSpreadsheetId(source.spreadsheetIdOrUrl, out spreadsheetId))
        {
            onDiscovered?.Invoke(new List<WorksheetInfo>());
            yield break;
        }

        string url = string.Format(GoogleSheetsEditUrl, spreadsheetId);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = timeout;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"GoogleSheetsLessonLoader: failed to discover worksheets for '{source.DisplayName}': {request.error}. Falling back to configured gid.");
                onDiscovered?.Invoke(new List<WorksheetInfo>());
                yield break;
            }

            List<WorksheetInfo> worksheets = ExtractWorksheetInfos(request.downloadHandler.text);
            if (worksheets.Count == 0)
                Debug.LogWarning($"GoogleSheetsLessonLoader: no worksheet metadata found for '{source.DisplayName}'. Falling back to configured gid.");
            else
                Debug.Log($"GoogleSheetsLessonLoader: discovered {worksheets.Count} worksheet(s) in '{source.DisplayName}'.");

            onDiscovered?.Invoke(worksheets);
        }
    }

    private static List<WorksheetInfo> ExtractWorksheetInfos(string html)
    {
        List<WorksheetInfo> worksheets = new List<WorksheetInfo>();
        HashSet<string> seenGids = new HashSet<string>();

        if (string.IsNullOrWhiteSpace(html))
            return worksheets;

        foreach (Match match in WorksheetMetadataRegex.Matches(html))
        {
            string gid = match.Groups["gid"].Value;
            if (string.IsNullOrWhiteSpace(gid) || !seenGids.Add(gid))
                continue;

            worksheets.Add(new WorksheetInfo
            {
                Gid = gid,
                Title = match.Groups["title"].Value
            });
        }

        if (worksheets.Count > 0)
            return worksheets;

        foreach (Match match in GidFallbackRegex.Matches(html))
        {
            string gid = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(gid) || !seenGids.Add(gid))
                continue;

            worksheets.Add(new WorksheetInfo
            {
                Gid = gid,
                Title = string.Empty
            });
        }

        return worksheets;
    }

    private static bool IsLessonWorksheetName(string title)
    {
        return !string.IsNullOrWhiteSpace(title) && LessonWorksheetNameRegex.IsMatch(title.Trim());
    }

    private static string ExtractGid(string url)
    {
        Match match = QueryValueRegex.Match(url);
        return match.Success ? UnityWebRequest.UnEscapeURL(match.Groups[1].Value) : string.Empty;
    }

    private static bool IsHttpUrl(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
               value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeHtml(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string trimmed = value.TrimStart();
        return trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
               trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeHeader(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        string normalized = value.Trim().TrimStart('\uFEFF').ToLowerInvariant();
        normalized = HeaderCleanupRegex.Replace(normalized, "_");
        return normalized.Replace(".", "_");
    }

    private static string NormalizeResourcePath(string value)
    {
        string path = value.Trim().Trim('"').Replace('\\', '/');

        if (path.StartsWith("resource:", StringComparison.OrdinalIgnoreCase))
            path = path.Substring("resource:".Length);

        const string assetsResourcesPrefix = "Assets/Resources/";
        const string resourcesPrefix = "Resources/";

        if (path.StartsWith(assetsResourcesPrefix, StringComparison.OrdinalIgnoreCase))
            path = path.Substring(assetsResourcesPrefix.Length);
        else if (path.StartsWith(resourcesPrefix, StringComparison.OrdinalIgnoreCase))
            path = path.Substring(resourcesPrefix.Length);

        int slashIndex = path.LastIndexOf('/');
        int dotIndex = path.LastIndexOf('.');
        if (dotIndex > slashIndex)
            path = path.Substring(0, dotIndex);

        return path.Trim('/');
    }

    private static List<string> SplitList(string value)
    {
        List<string> result = new List<string>();
        if (string.IsNullOrWhiteSpace(value))
            return result;

        string normalized = value.Replace("\r\n", "\n").Replace('\r', '\n');
        string separator = "|";

        if (normalized.Contains("|"))
            separator = "|";
        else if (normalized.Contains("\n"))
            separator = "\n";
        else if (normalized.Contains(";"))
            separator = ";";

        string[] parts = normalized.Split(new[] { separator }, StringSplitOptions.None);
        foreach (string part in parts)
        {
            string item = part.Trim();
            if (!string.IsNullOrEmpty(item))
                result.Add(item);
        }

        return result;
    }

    private static bool ParseBool(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = value.Trim().ToLowerInvariant();
        return normalized == "1" ||
               normalized == "true" ||
               normalized == "yes" ||
               normalized == "y" ||
               normalized == "on" ||
               normalized == "enabled";
    }

    private static bool TryParseExerciseType(string value, out LessonData.ExerciseType type)
    {
        type = LessonData.ExerciseType.FillBlank;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        string normalized = NormalizeHeader(value).Replace("_", string.Empty);
        switch (normalized)
        {
            case "fillblank":
            case "blank":
            case "fill":
                type = LessonData.ExerciseType.FillBlank;
                return true;
            case "makesentence":
            case "sentence":
            case "make":
                type = LessonData.ExerciseType.MakeSentence;
                return true;
            case "translate":
            case "translation":
            case "quiz":
                type = LessonData.ExerciseType.Translate;
                return true;
            case "writing":
            case "write":
            case "dictation":
                type = LessonData.ExerciseType.Writing;
                return true;
            case "flashcard":
            case "flashcards":
            case "cards":
                type = LessonData.ExerciseType.Flashcards;
                return true;
            default:
                return Enum.TryParse(value, true, out type);
        }
    }

    private sealed class LessonCatalogBuilder
    {
        private readonly int timeout;
        private readonly Dictionary<int, LessonBuildState> lessonsByOrder = new Dictionary<int, LessonBuildState>();
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
        private readonly Dictionary<string, AudioClip> audioCache = new Dictionary<string, AudioClip>();

        public LessonCatalogBuilder(int timeout)
        {
            this.timeout = timeout;
        }

        public IEnumerator Build(IEnumerable<SheetRow> rows)
        {
            foreach (SheetRow row in rows)
            {
                if (!row.TryGetInt(out int lessonOrder, "lesson_number", "lesson_index", "lesson_no", "lesson_id", "lesson"))
                {
                    Debug.LogWarning("GoogleSheetsLessonLoader: skipped row without lesson_number.");
                    continue;
                }

                if (!row.TryGetInt(out int exerciseOrder, "exercise_order", "exercise_index", "exercise_no", "exercise"))
                {
                    Debug.LogWarning($"GoogleSheetsLessonLoader: skipped lesson {lessonOrder} row without exercise_order.");
                    continue;
                }

                string rawType = row.Get("exercise_type", "type");
                if (!TryParseExerciseType(rawType, out LessonData.ExerciseType exerciseType))
                {
                    Debug.LogWarning($"GoogleSheetsLessonLoader: skipped lesson {lessonOrder}, exercise {exerciseOrder}; unknown type '{rawType}'.");
                    continue;
                }

                LessonBuildState lesson = GetOrCreateLesson(lessonOrder, row);
                ExerciseBuildState exercise = GetOrCreateExercise(lesson, exerciseOrder, exerciseType, row);

                if (exercise.Type != exerciseType)
                {
                    Debug.LogWarning($"GoogleSheetsLessonLoader: lesson {lessonOrder}, exercise {exerciseOrder} has mixed types. Keeping {exercise.Type}.");
                    continue;
                }

                yield return PopulateExercise(row, exercise);
            }
        }

        public List<LessonData> GetLessons()
        {
            List<LessonData> result = new List<LessonData>();

            foreach (LessonBuildState lesson in lessonsByOrder.Values.OrderBy(item => item.SortOrder))
            {
                lesson.Data.exercises.Clear();

                foreach (ExerciseBuildState exercise in lesson.Exercises.Values.OrderBy(item => item.SortOrder))
                    lesson.Data.exercises.Add(exercise.Entry);

                if (lesson.Data.Count > 0)
                    result.Add(lesson.Data);
            }

            return result;
        }

        private LessonBuildState GetOrCreateLesson(int lessonOrder, SheetRow row)
        {
            if (lessonsByOrder.TryGetValue(lessonOrder, out LessonBuildState existing))
                return existing;

            string lessonName = row.Get("lesson_name", "lesson_title", "title");
            if (string.IsNullOrWhiteSpace(lessonName))
                lessonName = $"Lesson {lessonOrder}";

            LessonData lesson = ScriptableObject.CreateInstance<LessonData>();
            lesson.name = $"Lesson{lessonOrder}_GoogleSheets";
            lesson.lessonName = lessonName;

            LessonBuildState state = new LessonBuildState
            {
                SortOrder = lessonOrder,
                Data = lesson
            };

            lessonsByOrder.Add(lessonOrder, state);
            return state;
        }

        private ExerciseBuildState GetOrCreateExercise(
            LessonBuildState lesson,
            int exerciseOrder,
            LessonData.ExerciseType type,
            SheetRow row)
        {
            if (lesson.Exercises.TryGetValue(exerciseOrder, out ExerciseBuildState existing))
                return existing;

            LessonData.LessonEntry entry = new LessonData.LessonEntry { type = type };
            UnityEngine.Object dataObject = CreateExerciseData(lesson, exerciseOrder, type, row, entry);

            ExerciseBuildState state = new ExerciseBuildState
            {
                SortOrder = exerciseOrder,
                Type = type,
                Entry = entry,
                Data = dataObject
            };

            lesson.Exercises.Add(exerciseOrder, state);
            return state;
        }

        private UnityEngine.Object CreateExerciseData(
            LessonBuildState lesson,
            int exerciseOrder,
            LessonData.ExerciseType type,
            SheetRow row,
            LessonData.LessonEntry entry)
        {
            string title = row.Get("exercise_title", "lesson_title", "lesson_name", "title");
            if (string.IsNullOrWhiteSpace(title))
                title = lesson.Data.lessonName;

            switch (type)
            {
                case LessonData.ExerciseType.FillBlank:
                    FillBlankData fillBlank = ScriptableObject.CreateInstance<FillBlankData>();
                    fillBlank.name = $"{lesson.Data.name}_Ex{exerciseOrder}_FillBlank";
                    fillBlank.lessonTitle = title;
                    entry.fillBlank = fillBlank;
                    return fillBlank;

                case LessonData.ExerciseType.MakeSentence:
                    MakeSentenceData makeSentence = ScriptableObject.CreateInstance<MakeSentenceData>();
                    makeSentence.name = $"{lesson.Data.name}_Ex{exerciseOrder}_MakeSentence";
                    makeSentence.lessonTitle = title;
                    entry.makeSentence = makeSentence;
                    return makeSentence;

                case LessonData.ExerciseType.Translate:
                    TranslateData translate = ScriptableObject.CreateInstance<TranslateData>();
                    translate.name = $"{lesson.Data.name}_Ex{exerciseOrder}_Translate";
                    translate.lessonTitle = title;
                    entry.translate = translate;
                    return translate;

                case LessonData.ExerciseType.Writing:
                    WritingData writing = ScriptableObject.CreateInstance<WritingData>();
                    writing.name = $"{lesson.Data.name}_Ex{exerciseOrder}_Writing";
                    writing.lessonTitle = title;
                    entry.writing = writing;
                    return writing;

                case LessonData.ExerciseType.Flashcards:
                    FlashcardDeckData flashcards = ScriptableObject.CreateInstance<FlashcardDeckData>();
                    flashcards.name = $"{lesson.Data.name}_Ex{exerciseOrder}_Flashcards";
                    flashcards.lessonTitle = title;
                    flashcards.isGrammarCards = ParseBool(row.Get("is_grammar_cards", "grammar_cards", "is_grammar"));
                    entry.flashcards = flashcards;
                    return flashcards;

                default:
                    return null;
            }
        }

        private IEnumerator PopulateExercise(SheetRow row, ExerciseBuildState exercise)
        {
            switch (exercise.Type)
            {
                case LessonData.ExerciseType.FillBlank:
                    PopulateFillBlank(row, (FillBlankData)exercise.Data);
                    break;

                case LessonData.ExerciseType.MakeSentence:
                    PopulateMakeSentence(row, (MakeSentenceData)exercise.Data);
                    break;

                case LessonData.ExerciseType.Translate:
                    yield return PopulateTranslate(row, (TranslateData)exercise.Data);
                    break;

                case LessonData.ExerciseType.Writing:
                    yield return PopulateWriting(row, (WritingData)exercise.Data);
                    break;

                case LessonData.ExerciseType.Flashcards:
                    yield return PopulateFlashcards(row, (FlashcardDeckData)exercise.Data);
                    break;
            }
        }

        private static void PopulateFillBlank(SheetRow row, FillBlankData data)
        {
            FillBlankData.Question question = new FillBlankData.Question
            {
                taskTitle = row.Get("task_title", "task", "prompt"),
                hint = row.Get("hint"),
                sentenceWithBlanks = row.Get("sentence_with_blanks", "sentence", "text"),
                correctAnswers = SplitList(row.Get("correct_answers", "correct_answer", "answers")),
                wordBankWords = SplitList(row.Get("word_bank", "word_bank_words", "words", "options"))
            };

            data.questions.Add(question);
        }

        private static void PopulateMakeSentence(SheetRow row, MakeSentenceData data)
        {
            MakeSentenceData.Question question = new MakeSentenceData.Question
            {
                taskTitle = row.Get("task_title", "task", "prompt"),
                hint = row.Get("hint"),
                shuffledWords = SplitList(row.Get("shuffled_words", "words", "word_bank", "options")),
                correctSentences = SplitList(row.Get("correct_sentences", "correct_sentence", "answers", "answer"))
            };

            data.questions.Add(question);
        }

        private IEnumerator PopulateTranslate(SheetRow row, TranslateData data)
        {
            TranslateData.Question question = new TranslateData.Question
            {
                foreignWord = row.Get("foreign_word", "foreign", "word", "prompt"),
                correctTranslation = row.Get("correct_translation", "translation", "answer"),
                options = SplitList(row.Get("options", "variants", "answers")).ToArray()
            };

            yield return LoadSprite(row.Get("image", "image_path", "image_url"), sprite => question.image = sprite);
            yield return LoadAudio(row.Get("audio", "audio_path", "audio_url"), clip => question.audio = clip);

            data.questions.Add(question);
        }

        private IEnumerator PopulateWriting(SheetRow row, WritingData data)
        {
            WritingData.Question question = new WritingData.Question
            {
                correctWords = SplitList(row.Get("correct_words", "correct_word", "answers", "answer")).ToArray()
            };

            List<string> audioValues = SplitList(row.Get("audio", "audio_clips", "word_clips", "audio_url"));
            List<AudioClip> clips = new List<AudioClip>();

            foreach (string audioValue in audioValues)
            {
                AudioClip clip = null;
                yield return LoadAudio(audioValue, loaded => clip = loaded);
                if (clip != null)
                    clips.Add(clip);
            }

            question.wordClips = clips.ToArray();
            data.questions.Add(question);
        }

        private IEnumerator PopulateFlashcards(SheetRow row, FlashcardDeckData data)
        {
            if (ParseBool(row.Get("is_grammar_cards", "grammar_cards", "is_grammar")))
                data.isGrammarCards = true;

            FlashcardEntry card = new FlashcardEntry
            {
                foreignWord = row.Get("foreign_word", "foreign", "front", "word", "prompt"),
                translation = row.Get("translation", "back", "answer"),
                exampleForeign = row.Get("example_foreign", "front_example", "example"),
                exampleTranslation = row.Get("example_translation", "back_example")
            };

            yield return LoadSprite(row.Get("image", "image_path", "image_url"), sprite => card.image = sprite);
            yield return LoadAudio(row.Get("front_audio", "front_audio_path", "audio", "audio_url"), clip => card.frontAudio = clip);
            yield return LoadAudio(row.Get("back_audio", "back_audio_path"), clip => card.backAudio = clip);

            data.cards.Add(card);
        }

        private IEnumerator LoadSprite(string value, Action<Sprite> onLoaded)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;

            string key = value.Trim();
            if (spriteCache.TryGetValue(key, out Sprite cached))
            {
                onLoaded?.Invoke(cached);
                yield break;
            }

            Sprite sprite = null;

            if (IsHttpUrl(key))
            {
                using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(key))
                {
                    request.timeout = timeout;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Texture2D texture = DownloadHandlerTexture.GetContent(request);
                        if (texture != null)
                        {
                            texture.name = GetNameFromUrl(key);
                            sprite = Sprite.Create(
                                texture,
                                new Rect(0f, 0f, texture.width, texture.height),
                                new Vector2(0.5f, 0.5f));
                            sprite.name = texture.name;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"GoogleSheetsLessonLoader: failed to load image '{key}': {request.error}");
                    }
                }
            }
            else
            {
                string resourcePath = NormalizeResourcePath(key);
                sprite = Resources.Load<Sprite>(resourcePath);
                if (sprite == null)
                    Debug.LogWarning($"GoogleSheetsLessonLoader: Sprite Resources path not found: '{resourcePath}'.");
            }

            spriteCache[key] = sprite;
            onLoaded?.Invoke(sprite);
        }

        private IEnumerator LoadAudio(string value, Action<AudioClip> onLoaded)
        {
            if (string.IsNullOrWhiteSpace(value))
                yield break;

            string key = value.Trim();
            if (audioCache.TryGetValue(key, out AudioClip cached))
            {
                onLoaded?.Invoke(cached);
                yield break;
            }

            AudioClip clip = null;

            if (IsHttpUrl(key))
            {
                using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(key, GetAudioType(key)))
                {
                    request.timeout = timeout;
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        clip = DownloadHandlerAudioClip.GetContent(request);
                        if (clip != null)
                            clip.name = GetNameFromUrl(key);
                    }
                    else
                    {
                        Debug.LogWarning($"GoogleSheetsLessonLoader: failed to load audio '{key}': {request.error}");
                    }
                }
            }
            else
            {
                string resourcePath = NormalizeResourcePath(key);
                clip = Resources.Load<AudioClip>(resourcePath);
                if (clip == null)
                    Debug.LogWarning($"GoogleSheetsLessonLoader: AudioClip Resources path not found: '{resourcePath}'.");
            }

            audioCache[key] = clip;
            onLoaded?.Invoke(clip);
        }

        private static AudioType GetAudioType(string url)
        {
            string path = url.Split('?')[0].ToLowerInvariant();

            if (path.EndsWith(".wav"))
                return AudioType.WAV;

            if (path.EndsWith(".ogg"))
                return AudioType.OGGVORBIS;

            if (path.EndsWith(".mp3") || path.EndsWith(".mpeg"))
                return AudioType.MPEG;

            return AudioType.UNKNOWN;
        }

        private static string GetNameFromUrl(string url)
        {
            try
            {
                Uri uri = new Uri(url);
                string fileName = Path.GetFileNameWithoutExtension(uri.LocalPath);
                return string.IsNullOrWhiteSpace(fileName) ? "GoogleSheetsMedia" : fileName;
            }
            catch (UriFormatException)
            {
                return "GoogleSheetsMedia";
            }
        }
    }

    private sealed class LessonBuildState
    {
        public int SortOrder;
        public LessonData Data;
        public readonly Dictionary<int, ExerciseBuildState> Exercises = new Dictionary<int, ExerciseBuildState>();
    }

    private sealed class ExerciseBuildState
    {
        public int SortOrder;
        public LessonData.ExerciseType Type;
        public LessonData.LessonEntry Entry;
        public UnityEngine.Object Data;
    }

    private sealed class SheetDownload
    {
        public string DisplayName;
        public string Url;
    }

    private sealed class WorksheetInfo
    {
        public string Gid;
        public string Title;

        public string DisplayName => string.IsNullOrWhiteSpace(Title) ? $"gid {Gid}" : Title;
    }

    private sealed class SheetRow
    {
        private readonly Dictionary<string, string> values;

        public SheetRow(Dictionary<string, string> values)
        {
            this.values = values;
        }

        public string Get(params string[] names)
        {
            foreach (string name in names)
            {
                string key = NormalizeHeader(name);
                if (values.TryGetValue(key, out string value))
                    return value;
            }

            return string.Empty;
        }

        public bool TryGetInt(out int value, params string[] names)
        {
            value = 0;
            string raw = Get(names);
            return int.TryParse(raw, out value);
        }
    }

    private static class CsvTableParser
    {
        public static List<SheetRow> Parse(string csv, string sourceName)
        {
            List<List<string>> records = ParseRecords(csv);
            List<SheetRow> rows = new List<SheetRow>();

            if (records.Count == 0)
                return rows;

            List<string> headers = records[0].Select(NormalizeHeader).ToList();
            if (!HasRequiredLessonHeaders(headers))
            {
                Debug.Log($"GoogleSheetsLessonLoader: skipped '{sourceName}' because it is not a lesson worksheet.");
                return rows;
            }

            for (int rowIndex = 1; rowIndex < records.Count; rowIndex++)
            {
                List<string> record = records[rowIndex];
                if (record.All(string.IsNullOrWhiteSpace))
                    continue;

                Dictionary<string, string> values = new Dictionary<string, string>();

                for (int columnIndex = 0; columnIndex < headers.Count && columnIndex < record.Count; columnIndex++)
                {
                    string header = headers[columnIndex];
                    if (string.IsNullOrWhiteSpace(header))
                        continue;

                    values[header] = record[columnIndex].Trim();
                }

                rows.Add(new SheetRow(values));
            }

            Debug.Log($"GoogleSheetsLessonLoader: parsed {rows.Count} rows from '{sourceName}'.");
            return rows;
        }

        private static bool HasRequiredLessonHeaders(List<string> headers)
        {
            return headers.Contains("lesson_number") &&
                   headers.Contains("exercise_order") &&
                   headers.Contains("exercise_type");
        }

        private static List<List<string>> ParseRecords(string csv)
        {
            List<List<string>> records = new List<List<string>>();
            if (string.IsNullOrEmpty(csv))
                return records;

            List<string> row = new List<string>();
            StringBuilder cell = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < csv.Length; i++)
            {
                char character = csv[i];

                if (inQuotes)
                {
                    if (character == '"')
                    {
                        if (i + 1 < csv.Length && csv[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        cell.Append(character);
                    }

                    continue;
                }

                if (character == '"')
                {
                    inQuotes = true;
                }
                else if (character == ',')
                {
                    row.Add(cell.ToString());
                    cell.Length = 0;
                }
                else if (character == '\r' || character == '\n')
                {
                    row.Add(cell.ToString());
                    cell.Length = 0;
                    records.Add(row);
                    row = new List<string>();

                    if (character == '\r' && i + 1 < csv.Length && csv[i + 1] == '\n')
                        i++;
                }
                else
                {
                    cell.Append(character);
                }
            }

            if (cell.Length > 0 || row.Count > 0)
            {
                row.Add(cell.ToString());
                records.Add(row);
            }

            return records;
        }
    }
}
