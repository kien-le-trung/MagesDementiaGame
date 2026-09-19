# MAGES Care Game — Implementation Brief

## Product goal

Build a short, polished 5–8 minute educational vertical slice. The player first experiences a confusing interaction as **Minh**, then sees its context as caregiver **Lan**, makes caregiving choices, and replays the same event from Minh’s perspective with consequences made tangible.

The game should communicate:

1. Behaviour that appears irrational can be understandable from the recipient’s subjective experience.
2. Caregiver actions can increase or reduce confusion and distress.
3. Good care does not “fix” dementia; it makes the environment and interaction more navigable and preserves participation.

Do not build a general dementia simulator, multiple episodes, scoring system, or LLM interaction for this prototype.

## Episode

Minh is distressed because a meaningful family photograph is missing. Lan, his granddaughter, moved it while cleaning and wants him to come to lunch. A loud television, unclear speech, an unfamiliar-feeling caregiver, and pressure to leave make the encounter difficult to interpret.

### Act 1 — Recipient experience

Player controls Minh without initially receiving a diagnosis or explanation. They encounter a missing photo, loud television, incomplete speech, uncertain recognition of Lan, and limited response options. The goal is experiential understanding, not winning.

### Act 2 — Caregiver intervention

Player controls Lan shortly before the first act. Context is now clear. They make three plausible caregiving decisions:

| Category | Options |
| --- | --- |
| Environment | Leave TV on; lower volume; turn off TV and restore photo |
| Approach | Call from a distance; approach quickly; enter view and introduce herself |
| Response | Correct Minh; generic reassurance; acknowledge concern and help |

Embed choices in play: television/photo interactions, approach near Minh, then dialogue response. Avoid framing choices as obviously cruel versus perfect.

### Act 3 — Modified recipient replay

Replay the same event as Minh, using the same room, event order, character path, and core dialogue. The prior choices alter the recipient’s experience and available agency.

### Reflection

Show specific action → consequence cards, not an “empathy score.” Mention that dementia experiences vary. Include restart.

## Architecture

Use two Unity scenes and three narrative phases:

```text
RecipientScene (baseline) → CaregiverScene (decisions) → RecipientScene (replay) → Reflection UI
```

### Persistent session

Use one persistent `GameSession` singleton for this small prototype. It is the shared notebook across scene loads and stores:

- Current phase: baseline, intervention, replay, reflection
- The three caregiver choices
- Calculated replay effects
- Optional completion/restart state

### Responsibilities

| System | Owns |
| --- | --- |
| `GameSession` | Phase, saved choices, replay effects, scene progression |
| `RecipientSceneController` | Determines baseline/replay mode; applies configuration; runs episode beats |
| `CaregiverSceneController` | Presents context; enables decisions; verifies completion; moves to replay |
| `OutcomeCalculator` | Converts independent choices into composable effects |
| `DialogueController` | Dialogue, clarity variants, response UI |
| `EnvironmentController` | TV, photograph, lighting, room objects |
| `PerceptionController` | Applies subjective audio/visual/interaction presentation |
| `NPCController` | Lan movement and simple animation |
| `InteractionController` | Object prompts and interactions |
| `ReflectionController` | Specific explanations of observed consequences |

Do not create every controller on day one. Start with a few scripts and split them only when a responsibility becomes real.

## Choice → consequence model

Do not author 27 complete branches. Each choice independently modifies a compact set of recipient-experience dimensions:

```text
noise · speech clarity · recognition · trust · distress · agency · object familiarity
```

Examples:

| Choice | Primary consequences |
| --- | --- |
| Leave TV on | More competing noise; less clear speech |
| Turn TV off / restore photo | Clearer speech; more familiar environment; lower distress |
| Approach quickly | More uncertainty and distress |
| Introduce herself in view | Better recognition and trust |
| Correct Minh | Higher distress; lower trust |
| Validate and assist | Lower distress; more willingness/ability to participate |

Apply the resulting dimensions in four layers:

1. **Environmental:** TV volume, photograph presence, lighting, competing sounds.
2. **Perceptual:** authored clear/partial/fragmented subtitles, portrait recognition, stable object labels.
3. **Emotional:** tone, proximity, mild visual treatment.
4. **Interactive:** response options, time to respond, ability to ask for clarification, willingness to follow Lan.

Prioritize interactive consequences. The lesson is stronger when better care expands Minh’s ability to participate—not merely when the screen looks less distorted.

## Shared narrative beats

Use the same named beats in baseline and replay:

1. Minh notices the missing photograph.
2. Television noise becomes salient.
3. Lan enters.
4. Lan speaks about lunch.
5. Minh reacts to the photo / unfamiliar situation.
6. The interaction resolves or ends.

Use simple coroutines or event completion to advance beats. Avoid long, fully timed sequences when the player must interact.

## Incremental build plan

Every phase must finish with a playable build. Preserve the core loop:

```text
confusion → context → decision → changed experience → explanation
```

### Phase 0 — Narrative spec

Write an event table before Unity: objective event, Minh’s baseline perception, Lan’s knowledge, each decision, each effect, and reflection text.

**Done when:** the whole five-minute episode is explainable without inventing missing events.

### Phase 1 — Complete ugly loop

Use text, buttons, and blank backgrounds only. Implement baseline → caregiver choices → saved state → conditional replay → reflection → restart.

**Done when:** changing a caregiver choice visibly changes replay text.

### Phase 2 — One causal mechanic

Implement only television noise: make the decision in Act 2 change replay audio, subtitle clarity, and one reflection card.

**Done when:** turning off the TV makes the same Act 3 speech distinctly easier to understand.

### Phase 3 — Minimal playable room

Add basic 2D movement, camera, collisions, interact prompts, TV/photo objects, and a transition point. Keep placeholder art.

**Done when:** the player can physically make the environmental choice.

### Phase 4 — All three decision categories

Add approach and response choices as in-world interaction and dialogue. Have all choices combine through the effect model.

**Done when:** all three decisions create independently visible replay differences.

### Phase 5 — Same-event replay

Implement the shared narrative beats in both recipient runs. Reuse room layout, character path, timing, and composition.

**Done when:** players immediately recognize Act 3 as the altered replay of Act 1.

### Phase 6 — Perceptual storytelling

Add subtle, purposeful effects one at a time: authored fragmented subtitles, audio competition, uncertain portrait/object labels, mild color/vignette changes, and altered response affordances.

**Done when:** players can articulate why the situation feels confusing, while it remains playable.

### Phase 7 — Reflection and framing

Add concise choice/consequence cards, alternative strategies, variation disclaimer, and replay.

**Done when:** a player can name at least two practical caregiving principles afterward.

### Phase 8 — Polish

Replace placeholders only after all logic works. Prioritize readable UI, character silhouettes, photo/TV feedback, portrait states, transitions, and then animation/music.

## Scope guardrails

**Must have:** three phases; two perspectives; three decisions; persistent choices; compositional replay; actionable reflection; restartable build.

**If time permits:** movement, TV audio, photo interaction, portrait states, three subtitle-clarity levels, basic pixel animation, transitions.

**Explicitly postpone:** LLM dialogue, multiple episodes/endings, complex NPC pathfinding, detailed scoring, analytics, voice acting, save/load, online deployment, procedural narrative.

## Three-day target

| Day | Deliverable |
| --- | --- |
| Day 1 | Complete but ugly end-to-end loop with state persistence and conditional replay |
| Day 2 | Functional vertical slice with movement, embedded decisions, and combinational consequences |
| Day 3 | Perceptual polish, reflection, playtesting, bug fixes, stable demo build |

## Future LLM extension (not in prototype)

Keep the consequence model finite and explainable. A future LLM may let the caregiver express free-form responses, but it should be classified into fixed caregiving dimensions (e.g., validates emotion, introduces self, offers choice, confronts, uses short instruction) before affecting the same outcome model. Do not let an LLM generate game logic directly.
