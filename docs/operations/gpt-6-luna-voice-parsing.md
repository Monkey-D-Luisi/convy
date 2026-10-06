# Voice parsing with GPT-6 Luna

List and task extraction use `gpt-6-luna` with Responses `reasoning.effort=none`, strict existing JSON schemas and `store=false`. Transcription remains `gpt-4o-mini-transcribe`. Prompts and their Spanish/English semantics are unchanged. Deterministic fixtures verify request serialization, parsing, matching and usage; they do not establish the live model's accuracy.

Standard short-context rates checked on 2026-10-06: input 100, cached input 10, cache write 125 and output 500 microdollars per 1K tokens. These defaults apply to Luna, not arbitrary model overrides, Batch/Flex, long-context multipliers or transcription. Override prices alongside a custom parsing model. Transcription estimates remain unknown unless their duration rate is configured.

For reported counts I=input, C=cached reads, W=cache writes, O=output:

`costMicros = roundAwayFromZero(((I-C-W)*100 + C*10 + W*125 + O*500)/1000)`.

Reasoning tokens are a subset of O and remain telemetry only. The obsolete independent reasoning price is ignored. Invalid negative counts, overlapping input subsets or missing required rates produce an unknown estimate rather than a fabricated charge. Both persisted usage costs and voice estimates share this calculation.

The installed OpenAI .NET SDK 2.10 exposes cached reads but not a typed cache-write count. Its JSON model preserves untyped `input_tokens_details.cache_write_tokens`; the adapter reads that usage field and carries the nullable count into both estimators. Missing fields remain null, not asserted provider zero; estimates then assume no reported cache-write component. No production response or provider billing was sampled in this task. Synthetic response fixtures prove both positive and missing-field behavior. Existing persisted telemetry columns and historical totals are preserved; new cache-write counts are used to calculate the stored aggregate cost but are not added to the historical database schema. Historical rows cannot be retrospectively repriced with unknown cache-write counts.

The three legacy Nano model literals in `AdminMetricsReaderTests.cs` intentionally represent historical usage rows. They prove old model labels continue to aggregate. Active/default/deployment references all use Luna.

Sources: [Luna model and pricing](https://developers.openai.com/api/docs/models/gpt-6-luna), [Responses usage](https://developers.openai.com/api/reference/resources/responses/methods/retrieve), [deprecations](https://developers.openai.com/api/docs/deprecations).
