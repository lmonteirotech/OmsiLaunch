# en-GB glossary — OmsiLaunch 0.1.0-beta3 documentation

The en-GB edition is the English source converted to British spelling and usage. It is not a rewrite. Keep every sentence, qualifier, number, status and structure element exactly as in the English page. Change only spelling, a few vocabulary items listed below, and nothing inside code spans, code blocks, link targets, identifiers or UI literals.

## Register and address

- Same register as the source: neutral, precise technical English. Address the reader as "you" only where the source does, otherwise keep impersonal constructions. Do not make the text more formal or more casual.
- No old en-GB translation exists; this glossary is the only reference.

## Spelling rules (apply throughout prose)

| Rule | Apply | Examples (US → GB) | Never change |
| --- | --- | --- | --- |
| `-ize/-ization` → `-ise/-isation` (not Oxford `-ize`) | yes | localized → localised, localization → localisation, normalized → normalised, normalization → normalisation, serialize(d/s) → serialise(d/s), tokenizes → tokenises, authorized → authorised, initialize → initialise, finalize → finalise, recognize → recognise, organize → organise, synchronize → synchronise, materialize → materialise, optimize → optimise, prioritize → prioritise, summarized → summarised, minimize/maximize → minimise/maximise, customize → customise | Words that are not the `-ize` suffix: size, sized, auto-sized, seize, prize. Identifiers such as `NormalizeRoot`, `docs.localization`, `LOCALIZATION-MANIFEST.md`, `windows-ui.localization-and-status`, `System.Text.Json` serializer names |
| `-yze` → `-yse` | yes | analyze → analyse, analyzed → analysed | — |
| `-or` → `-our` | yes | behavior → behaviour, color → colour, favor → favour, honor → honour | `behavior.startup-timeout`, `LaunchSpec.Behavior`, `behavior` profile key, `Behavior` properties; words that are `-or` in British too: error, supervisor, indicator, cursor, separator, descriptor, author, integrator, editor, monitor, handler |
| `-er` → `-re` | yes | center → centre, centered → centred, meter (unit word) → metre | unit symbols (`m`, `ms`) |
| `-og` → `-ogue` | yes | catalog → catalogue, analog → analogue | `ConfigurationCatalog`, `ContentCatalog`, any identifier. **Keep** "log", "backlog" (same in GB) |
| dialog | "dialog" → "dialogue" in prose (as in Microsoft en-GB style: "dialogue box", "confirmation dialogue", "failure dialogue") | dialog → dialogue, dialogs → dialogues | `DialogResult.OK`, any identifier or UI literal |
| double `l` before suffix | yes | canceled → cancelled, canceling → cancelling, labeled → labelled, modeled → modelled, signaled → signalled, traveled → travelled | UI literal `Cancel`; `Cancelled` if it appears as an enum/identifier stays as written |
| single `l` | yes | fulfill → fulfil, enrollment → enrolment, installment → instalment | "install", "installation", "installed" (same in GB) |
| licence / license | noun → "licence", verb stays "license" | "the license" → "the licence"; "licensed under" unchanged | `LICENSE` file name, SPDX ids |
| practice / practise | noun "practice", verb "practise" | — | — |
| artifact | "artifact" → "artefact" in prose | session artifacts → session artefacts | `restore.session-artifact-removed`, file/folder names, identifiers |
| gray → grey, judgment → judgement, toward → towards, afterward → afterwards, gotten → got | yes | — | — |
| program | **keep** "program" (computer program is "program" in British English) | — | — |
| disk, check (verify), draft | keep unchanged (same in British computing usage) | — | — |
| percent | "per cent" in running prose; keep `%` with numbers and in tables | 50 percent → 50 per cent | numeric `%` |

Words the source already spells the British way (catalogue, centred, summarised, behaviour) stay as they are.

## Punctuation and typography

- Keep the source's double quotation marks ("stopping", "not runtime validated"); do not switch to single quotes. Place full stops and commas outside the closing quote unless they belong to the quoted text (logical punctuation).
- Keep the source's comma usage, including any serial commas; do not add or remove them.
- Keep dates, times, numbers, units (`64 KiB`, `250 ms`, `2 s`), ranges (`100`..`106`), version strings and evidence ids exactly as in the source.
- Keep headings in the source's capitalisation style.
- Keep "e.g."/"for example" exactly as the source wrote it.

## Terms

The source is already English; almost every term stays. "same" means the English term is kept unchanged.

| English | en-GB | note |
| --- | --- | --- |
| session | session | same |
| session owner / owner | session owner / owner | same |
| client (mode) | client (mode) | same |
| owner process | owner process | same |
| launch | launch | same (noun and verb) |
| plan (noun/verb) | plan | same |
| runnable / not runnable | runnable / not runnable | same; `NOT RUNNABLE` in output stays as literal |
| start | start | same |
| stop | stop | same |
| end session | end session | same; UI literal `End session` unchanged |
| canonical stop | canonical stop | same |
| restore | restore | same |
| recovery | recovery | same |
| pending (journal) | pending (journal) | same |
| journal | journal | same; "journaled" → "journalled" in prose only |
| transaction | transaction | same |
| overlay | overlay | same |
| backup | backup | same (noun); verb "back up" |
| snapshot | snapshot | same |
| installation | installation | same |
| installation root | installation root | same |
| package | package | same |
| release package | release package | same |
| manifest | manifest | same |
| permanent plugin | permanent plugin | same |
| native bridge | native bridge | same |
| runtime | runtime | same |
| runtime operation | runtime operation | same |
| runtime command | runtime command | same |
| runtime slot / mailbox | runtime slot / mailbox | same |
| control plane | control plane | same |
| local control | local control | same |
| named pipe | named pipe | same |
| frame (protocol) | frame | same |
| envelope (JSON) | envelope | same |
| handle | handle | same |
| stale handle | stale handle | same |
| capability | capability | same |
| capability registry | capability registry | same |
| bounded list | bounded list | same; keep BUG-05 semantics exactly (largest prefix that fits, `returned_count`, `truncated=true`, successful reply) |
| truncated | truncated | same |
| row | row | same |
| evidence | evidence | same (uncountable) |
| runtime-validated | runtime-validated | same; keep hyphenation as in source |
| statically validated | statically validated | same |
| offline test | offline test | same |
| gate (documentation gate) | gate | same |
| tray icon | tray icon | same; do not replace with "system tray" |
| notification area | notification area | same |
| status window | status window | same |
| confirmation dialog | confirmation dialogue | British spelling in prose; identifiers (`StopConfirmationWindow`, `DialogResult`) unchanged |
| message box | message box | same |
| failure dialog | failure dialogue | British spelling in prose |
| tooltip | tooltip | same |
| context menu | context menu | same |
| Explorer restart | Explorer restart | same |
| entry point | entry point | same; `entrypoint` in code unchanged |
| new map | new map | same |
| saved situation | saved situation | same |
| map | map | same |
| splash screen | splash screen | same |
| Internet Textures | Internet Textures | same (OMSI feature name, capitalised) |
| session profile | session profile | same |
| preset | preset | same |
| setting | setting | same |
| player vehicle | player vehicle | same |
| road vehicle | road vehicle | same |
| human (pedestrian/passenger object) | human | same (OMSI term) |
| timetable | timetable | same |
| track entry | track entry | same |
| tour entry | tour entry | same |
| ticket | ticket | same |
| driver | driver | same |
| fleet number | fleet number | same |
| registration (plate) | registration (plate) | same; "registration plate" is valid British usage, do not change to "number plate" |
| repaint | repaint | same |
| spawn | spawn | same |
| camera lock | camera lock | same |
| device reset | device reset | same |
| render thread | render thread | same |
| texture | texture | same |
| exit code | exit code | same |
| error code | error code | same |
| diagnostic | diagnostic | same |
| diagnostics directory | diagnostics directory | same |
| timeout | timeout | same |
| placeholder | placeholder | same |
| flag | flag | same |
| route | route | same |
| command word | command word | same |
| integrator | integrator | same |
| caller | caller | same |
| known limitation | known limitation | same |
| accepted risk | accepted risk | same |
| stable beta | stable beta | same; `STABLE_BETA` literal unchanged |
| experimental | experimental | same; `EXPERIMENTAL` unchanged |
| partial | partial | same; `PARTIAL` unchanged; Steam LAA stays "not runtime-validated", never "unsupported" |
| unavailable | unavailable | same; `UNAVAILABLE` unchanged |
| deprecated / legacy | deprecated / legacy | same |
| not normative | not normative | same |
| source of truth | source of truth | same |

## Additional vocabulary

| English (US) | en-GB | note |
| --- | --- | --- |
| localized / localization | localised / localisation | prose only |
| catalog | catalogue | prose only |
| behavior | behaviour | prose only |
| serialized / serializes | serialised / serialises | prose only |
| normalized / normalization | normalised / normalisation | prose only |
| tokenizes | tokenises | prose only |
| authorized | authorised | prose only |
| centered | centred | prose only |
| canceled | cancelled | prose only |
| artifact(s) | artefact(s) | prose only |
| journaled | journalled | prose only |
| license (noun) | licence | prose only; file `LICENSE` unchanged |
| percent | per cent | prose only |
| program | program | unchanged in British computing usage |
| log, backlog, error, supervisor, cursor | unchanged | already British |
