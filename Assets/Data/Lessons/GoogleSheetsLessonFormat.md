# Google Sheets lesson format

Publish a Google Sheets tab as CSV, or paste a normal Google Sheets URL into
`ProgressManager > Google Sheets > Sources`. The loader accepts a spreadsheet id,
a full Google Sheets URL, or a direct CSV URL.

Current default source:

```text
https://docs.google.com/spreadsheets/d/1yRbTMZg8SgXaNTiQQNOsASyODc1wdJIttJ746R5QI9s/edit?usp=sharing
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
| `image` | `Translate`, `Flashcards`. Use a URL or a `Resources` path. |
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
https://example.com/card1.png
```

For `Resources` paths, Unity requires the file to be under `Assets/Resources`.
