# JobSeek

Fit a candidate to job offers. The part that runs today extracts skills from an offer with a language model, checks that output before it would be trusted, and scores the offer against the candidate in plain C#.

There is no web form, CV upload, or chat yet. Those are listed under [Roadmap](#roadmap).

## What works today

| Piece | What it does |
|---|---|
| `AttributesOfferService` | Sends the offer text to the model and reads skills back as a fixed structure. The master skill list is written into the response schema as the allowed skill names, so the model can only return skills from that dictionary. Temperature is 0. |
| Release tests | The same offers go through the model. Must-have and Should-have rules block a release. A Nice-to-have miss is a known limit. |
| `AttributeComparerService` | Scores a candidate against an offer. Required skills count for 0.8, nice-to-have skills for 0.2. |
| `CategoryOfferService` | Maps a parsed offer onto the fields the comparer uses. |
| `SkillConsolidationService` | Collapses extracted skill names that mean the same thing into one master list. |
| `JobSeek.Sandbox` | Console entry point for the programs above. |

Stack: .NET 10, C#, ASP.NET-style contracts, `Microsoft.Extensions.AI`, OpenAI structured output.

```mermaid
flowchart LR
  offer["Offer text"] --> extract["AttributesOfferService"]
  extract --> skills["Structured skills"]
  skills --> score["AttributeComparerService"]
  candidate["Candidate skills"] --> score
  score --> ranked["Ranked offers"]
```

## Run the offline tests

These tests do not call a model.

```powershell
dotnet test
```

`JobSeek.runsettings` excludes `TestCategory=Llm`.

## Run the release tests

These tests spend tokens and can change when the model changes. Set `OPENAI_JOBSEEK_API_KEY`. `LLM_MODEL` defaults to `gpt-4o-mini`.

```powershell
$env:LLM_MODEL = "gpt-4o-mini"
dotnet test Tests/JobSeek.Prompter.Services.ReleaseTests --filter "TestCategory=Llm&TestCategory!=Freeform"
```

That run calls `ExtractAttributes` with the master list declared in the tests. The older freeform tests stay in the project for comparison and do not run with that filter. Run them on their own; a freeform run replaces the same model card.

```powershell
dotnet test Tests/JobSeek.Prompter.Services.ReleaseTests --filter "TestCategory=Freeform"
```

Each model writes a card under `Tests/JobSeek.Prompter.Services.ReleaseTests/Leaderboard/`. The latest run of one model replaces only that model's file. [summary.md](Tests/JobSeek.Prompter.Services.ReleaseTests/Leaderboard/summary.md) keeps one row per model.

A failed Must-have or Should-have check means the model is not ready to ship. A failed Nice-to-have check does not block the release by itself.

- **Must have.** The result is safe to store. The offer id comes back, a skill already tagged on the offer keeps its name, level, and list, and a technology named in the offer lands on required or nice-to-have when it is on the master list. A technology that is not on that list is left out.
- **Should have.** The result stays technical. Benefits, soft traits, and spoken languages stay out. A skill that is only named is not treated as senior. A technology that was not pre-tagged is still extracted.
- **Nice to have.** The wording is harder: "senior expert", Polish text returned as English skill names, negation, and a long offer with many buried skills.

Latest master-list run, 2026-10-14:

| Model | Passed/Total | Release gate | Known gap |
|---|---|---|---|
| gpt-4o-mini | 14/16 | Must and Should passed | Negation, NH-05, and the buried offer, NH-06 |

On this run, `gpt-4o-mini` passes the release gate. Card: [gpt-4o-mini](Tests/JobSeek.Prompter.Services.ReleaseTests/Leaderboard/gpt-4o-mini.md).

The [gpt-4.1-nano](Tests/JobSeek.Prompter.Services.ReleaseTests/Leaderboard/gpt-4.1-nano.md) card is the freeform run from 2026-10-07 (14/16). It is not part of this master-list run.

## Run the sandbox

From the repository root:

```powershell
dotnet run --project JobSeek.Sandbox -- --help
dotnet run --project JobSeek.Sandbox -- prompter --help
dotnet run --project JobSeek.Sandbox -- comparator --help
dotnet run --project JobSeek.Sandbox -- categorizator --help
dotnet run --project JobSeek.Sandbox -- normalizator --help
```

`prompter` needs `OPENAI_JOBSEEK_API_KEY`.

## Build the master skill list

`normalizator` reads required and nice-to-have skill names from `data/offer-attributes.json` and writes `data/skill-master-list.json`.

```powershell
dotnet run --project JobSeek.Sandbox -- normalizator
```

Run it from the repository root. Before consolidation the extracted names were 596 separate skills. After the normalizator, `data/skill-master-list.json` has 306.

Two local embedding models were scored on the same pairs. Names that should become one skill: `javascript` / `js`, `unit tests` / `unit testing`, and Polish `bazy relacyjne` / `relational databases`. Names that should stay apart: `java` / `javascript`, `c#` / `c++`, and `react` / `kubernetes`. `multilingual-e5-small` gave high scores to unrelated skills as well as to aliases, so one cutoff could not both merge the aliases and keep Java apart from JavaScript. `paraphrase-multilingual-MiniLM-L12-v2` leaves a gap between those groups. The cutoff is 0.57.

`prompter` sends that list as the allowed values for each skill name. Offer skills and candidate skills then share one dictionary, which is what the comparer matches on.

## Cost optimization

`gpt-4o-mini` bills input at $0.15 per million tokens. A later request that starts with the same tokens reuses that prefix: the first cache step is 1,024 tokens, and further steps are 128 tokens. Cached tokens cost $0.075 per million. The user message is billed at the full input price on every offer.

### 1. Keep offer-specific text in the user message

The response schema and the system prompt are shared by every offer. For the current master list of 306 skills the schema is 2,793 tokens. The system prompt is about 700 tokens. The shared prefix is 2,793 + 700 = **3,493** tokens.

Tagged skills and the raw description change on every offer. `OfferAttributesPrompt.User` carries both. The system prompt stays the same from offer to offer, so those 3,493 tokens can be cached. Offer-specific text in the system prompt ends the identical prefix at that point, and the tokens after it are billed at $0.15 per million.

A cache hit is counted in whole steps of 128. 3,493 ÷ 128 is 27 steps plus a remainder:

- 27 × 128 = **3,456** tokens at the cached price
- 3,493 − 3,456 = **37** tokens at the normal input price

Cached tokens count as half a token in the bill. A hit on this prefix is billed as 3,456 × 0.5 + 37 = **1,765** token-equivalents.

| Shared prefix | Full-price tokens | Cached tokens | Billed as | Price |
|---|---:|---:|---:|---:|
| Changes with the offer | 3,493 | 0 | 3,493 | $0.000524 |
| Cache hit | 37 | 3,456 | 1,765 | $0.000265 |

The prices are 3,493 / 1,000,000 × $0.15 = $0.00052395, and 3,456 / 1,000,000 × $0.075 + 37 / 1,000,000 × $0.15 = $0.00026475.

The user message is about **700** tokens. It contains the static required and nice-to-have skills plus the job description, and those tokens change every offer, so they are billed at $0.15 per million on every call.

| User message | Full input | Cache-hit input | Billed as | Price, no cache | Price, hit | Cut |
|---|---:|---:|---:|---:|---:|---:|
| 700 | 4,193 | 700 + 1,765 | 2,465 | $0.000629 | $0.000370 | **41.2%** |

That is **1,728** token-equivalents less ($0.000259 per offer). One thousand cache hits cost about **$0.26** less ($0.629 versus $0.370). The first offer in a run pays the full prefix price and fills the cache.

## Download the embedding model

The model files are not in the repo. `JobSeek.Normalizator.Services/Models/` is gitignored, so download them once on each machine. From the repository root:

```powershell
$dir = "JobSeek.Normalizator.Services/Models/paraphrase-multilingual-MiniLM-L12-v2"
New-Item -ItemType Directory -Force -Path $dir | Out-Null
Invoke-WebRequest -Uri "https://huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2/resolve/main/sentencepiece.bpe.model" -OutFile "$dir/sentencepiece.bpe.model"
Invoke-WebRequest -Uri "https://huggingface.co/Xenova/paraphrase-multilingual-MiniLM-L12-v2/resolve/main/onnx/model_quantized.onnx" -OutFile "$dir/model.onnx"
```

Save the quantized ONNX file as `model.onnx`. The normalizator and its unit tests look for `model.onnx` and `sentencepiece.bpe.model`.

## Layout

```text
JobSeek.Categorizator.Contracts / Services   offer and candidate models, mapping
JobSeek.Prompter.Contracts / Services        structured skill extraction
JobSeek.Comparator.Contracts / Services      ranking
JobSeek.Normalizator.Contracts / Services    master skill list
JobSeek.Sandbox                              console runner
Tests/JobSeek.Comparator.Services.UnitTests  offline ranking tests
Tests/JobSeek.Normalizator.Services.UnitTests offline consolidation and normalization tests
Tests/JobSeek.Prompter.Services.ReleaseTests ship / no-ship checks
```

## Roadmap

Not built yet.

1. A candidate enters a profile and receives ranked offers.
2. A candidate uploads a CV and receives ranked offers.
3. A candidate talks to an agent that narrows the offers.
4. The best 10–15 offers get a short written explanation from the model.

Saved offer text is what the tests use today. A live source of offers is not chosen yet.
