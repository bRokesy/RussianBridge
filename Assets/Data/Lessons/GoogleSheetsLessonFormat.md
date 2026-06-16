# Google Sheets lesson format

Paste a normal Google Sheets URL into `ProgressManager > Google Sheets > Sources`.
The loader accepts a spreadsheet id, a full Google Sheets URL, or a direct CSV
URL.

Preferred workbook layout:

- one worksheet per lesson;
- worksheet names should be `Lesson1`, `Lesson2`, `Lesson3`, and so on;
- every lesson worksheet must keep the same header row;
- service worksheets such as `Summary` and `README` are allowed and skipped by
  the loader.

Current default source:

```text
https://docs.google.com/spreadsheets/d/1yRbTMZg8SgXaNTiQQNOsASyODc1wdJIttJ746R5QI9s/edit?usp=sharing
```

Runtime CSV endpoints used by Unity are discovered from the public spreadsheet
page. Each lesson worksheet is then loaded through a CSV endpoint like this:

```text
https://docs.google.com/spreadsheets/d/1yRbTMZg8SgXaNTiQQNOsASyODc1wdJIttJ746R5QI9s/gviz/tq?tqx=out:csv&gid=<worksheet_gid>
```

Required columns:

| column | value |
| --- | --- |
| `lesson_number` | Lesson order, starting from `1`. |
| `lesson_name` | Name shown in progress UI. |
| `exercise_order` | Exercise order inside the lesson, starting from `1`. |
| `exercise_type` | `FillBlank`, `MakeSentence`, `Translate`, `Writing`, or `Flashcards`. |

Common optional columns:

| column | used by |
| --- | --- |
| `exercise_title` | Any exercise type. |
| `task_title` | `FillBlank`, `MakeSentence`. |
| `hint` | `FillBlank`, `MakeSentence`. |
| `image` | `Translate`, `Flashcards`. Use a URL, a `Resources` path, or an editor-only project asset path. |
| `audio` | `Translate`, `Writing`, `Flashcards`. Use a URL or a `Resources` path. |

Type-specific columns:

| type | columns |
| --- | --- |
| `FillBlank` | `sentence_with_blanks`, `correct_answers`, `word_bank` |
| `MakeSentence` | `shuffled_words`, `correct_sentences` |
| `Translate` | `foreign_word`, `correct_translation`, `options`, `image`, `audio` |
| `Writing` | `correct_words`, `audio` |
| `Flashcards` | `foreign_word`, `translation`, `example_foreign`, `example_translation`, `front_audio`, `back_audio`, `is_grammar_cards` |

Use `|` inside a cell for lists:

```text
word_bank: edu|pyu|idu
correct_answers: idu|pyu
options: idti|pit|chitat
correct_sentences: Ya idu domoy|Ya domoy idu
```

Media cells can be either:

```text
Images/Lesson1/card1
Assets/Resources/Images/Lesson1/card1.png
Assets/Sprites/Lesson/Lesson1/L1_Ex2.png
https://example.com/card1.png
```

For `Resources` paths, Unity requires the file to be under `Assets/Resources`.
Project asset paths such as `Assets/Sprites/...` work in Editor Play Mode only;
use URLs or `Assets/Resources/...` paths for player builds.
