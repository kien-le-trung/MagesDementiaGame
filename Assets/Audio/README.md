# Audio asset guide

This folder is organized by sound function so clips can be reused across all three scenes.

## Recommended minimum sound set

### Ambience

- `room_tone_loop.ogg` — quiet indoor ambience used throughout the game.
- `fish_tank_loop.ogg` — subtle bubbling/hum in MinhView's door close-up.
- `television_program_loop.ogg` — indistinct television audio; volume changes with the selected TV state.

### Music

- `quiet_tension_loop.ogg` — restrained background music for MinhView and LanView.
- `reflection_theme_loop.ogg` — warmer cue for ResolutionScene and the final reflection.

### UI

- `ui_click.wav` — normal button and hotspot selection.
- `ui_confirm.wav` — completed choice or accepted action.
- `ui_back.wav` — closing a close-up or cancelling.
- `text_blip.wav` — subtle dialogue text sound; do not play it for every character at full volume.
- `speech_scramble.wav` — short uneasy cue when Minh's transformed response appears.

### Interaction

- `item_pickup.wav` — picking up a table-task item.
- `item_place.wav` — placing an item correctly.
- `invalid_drop.wav` — placing an item in the wrong location.
- `checklist_tick.wav` — checking a completed table-task entry.
- `hotspot_reveal.wav` — revealing a new hotspot, such as the photograph under the cabinet.
- `photo_pickup.wav` — finding the photograph.
- `photo_hang.wav` — restoring the photograph to the wall.

### Environment

- `television_on.wav` — television powers on in MinhView.
- `television_off.wav` — television is turned off in LanView.
- `television_volume_down.wav` — television volume is lowered in LanView.
- `door_open.wav` — Lan arrives or Lan and Minh leave the house.

### Movement

- `footstep_wood_01.wav`
- `footstep_wood_02.wav`

Alternate these two clips for Lan and Minh. Pitch and volume variation can make the same pair sound less repetitive; Minh's steps can be slightly quieter and slower.

### Transitions

- `view_whoosh.wav` — entering or leaving the table and door close-ups.
- `scene_fade.wav` — transition between acts and the final doorway fade.
- `reflection_chime.wav` — all Resolution inspections are complete or reflection begins.

## Scene coverage

- **MinhView:** room tone, quiet tension music, fish tank, table-task sounds, TV power-on/program audio, dialogue blips, speech scramble, view transitions, and scene fade.
- **LanView:** room tone, quiet tension music, UI sounds, TV state sounds, photograph discovery/restoration sounds, footsteps for approach playback, and scene fade.
- **ResolutionScene:** room tone, footsteps, inspection confirmations, door opening, scene fade, reflection chime, and reflection music.

Full voice acting is optional and not required for this pass. The `Dialogue` folders are reserved if narration or character performances are added later.

## File conventions

- Use lowercase snake_case names, such as `television_volume_down.wav`.
- Use `.wav` for short one-shot effects and `.ogg` for long looping ambience or music.
- Make loop files seamless and avoid silence at their beginning or end.
- Keep original source files outside Unity if they contain multiple takes; place only game-ready exports here.

