# Quiznight? (Godot) — project notes for Claude

Called **Quiznight?** since 0.9.0b (was "Quiznight!"): only what players see changed (logo, window title set in `App._Ready`,
icon `tools/make_icon.py`, release titles). File and folder names stay "Quiznight"/"QuizNight!" (`config/name` names the
exported data folder the updater's file lists use, `config/custom_user_dir_name` the saved quizzes), so don't rename those.

A desktop app for hosting quiz nights with friends. **Everyone plays on their own PC** (talking on Discord): nobody
watches a shared TV or a stream of the host's screen, so don't design for that. The **host is the quizmaster and never
plays** the quiz. Players join with a
4-character room code on their own PC.

This is the Godot 4.7 / C# port of the web version at `C:\Users\Xbox3\Desktop\Quiznight!`
(React + Node, v0.3.2a). That folder is **reference only** — don't edit it. Its CLAUDE.md explains
the original design decisions; most still apply.

## Architecture decisions (and why)

- **Steam networking with the Spacewar app ID (480).** Internal program for friends, never
  published, so Valve's public test app is enough. Consequences: everyone needs Steam running and
  shows as "playing Spacewar"; lobby searches see every Spacewar lobby, so ours are tagged
  (`quiznight`, `quiznight_room` = a hash of the room code, `quiznight_host` lobby data).
- **The host's app is the server.** `HostSession` runs the `Room`/`Game` in-process; players
  connect to the host's Steam ID through Steam's relay (`SteamNetworkingSockets`). The lobby only
  makes the room code findable. Host leaves = room closed.
- **LAN mode (plain TCP, port 7777)** exists for testing several copies on one PC (Steam can't
  connect you to yourself) and for offline play on the same Wi-Fi. Exported games hide it (`App.LanUnlocked`) until
  the developer code `61969` is typed on the home screen (until the game closes); editor runs always have it,
  `--lan-locked` shows the exported look.
- **Quiz files extend the web version's format** (`.quiznight.json`, now format 5: multi choice, image reveal, timeline,
  put in order, write the answer (read as classic + `Typed` since 0.10.3b), typed buzzer questions, bonus points), so old exports import as-is.
  Buzzer kinds with `Typed` play like write the answer (`Question.IsWritten`); a bonus is given by the host at the reveal
  (`Game.ToggleBonus`), and the host can change scores any time (`HostSession.AdjustScore`).
  `QuizJson` reads forgivingly (= migration) and writes only each type's fields.
  **Share codes** (`Shared/ShareCode.cs`) carry a quiz without sound/video data (names kept, so the importer is told
  what to add) or built-in map pictures: `ShareCode.Pack` (Brotli'd quiz JSON) is uploaded to the results repo
  (`shares/<8 chars>.qn`, `Storage/QuizShare.cs`, same token) and the short code `QN-XXXX-XXXX` fetches it; offline it
  falls back to the long code `QN1-` + base64url of the packed quiz. `--share-check` does a real round trip (delete the
  printed code's file from the repo afterwards). A Workshop (Cloudflare) was considered and dropped.
  **Export** writes a `.quiznight.zip`: `quiz.json` plus `media/<question id>.<ext>` for each sound/video
  (`QuizStore.Export`/`ReadZip`); single `.json` files still import.
  Built-in map images are not stored in files (they ship with the game, `assets/maps/`). Each one's source and
  scale is noted in `MapPresets.cs`; `tools/stitch_map.gd` builds them from downloaded tiles (run one Godot at a
  time: two at once hang). All map images (also WoW's) are imported as **"keep"** and decoded by
  `Images.LoadBuiltinImage` (WebP + mipmaps): Godot's texture import stored them 2–7× bigger. Exports leave
  `assets/maps/` out of the .pck and, since 0.12.8b, don't ship them at all: **map packs** (`Storage/MapPacks.cs`), one
  zip per map (WoW: the whole `wow/` folder) on the public releases repo's `maps` pre-release with a signed `maps.json`,
  downloaded when a map is picked, a quiz using it opened or hosted (`Ui/MapDownload`), kept in `user://maps/`.
  Players get them from the host with the quiz's files (`Game.ShareFiles`, asset `builtin:ID`; WoW as a zip unpacked
  into the player's media cache, `MapPacks.AddTemp`). **A new or changed map needs `tools/map_packs.sh --test`** (the test repo's maps,
  which test builds use), then plain `tools/map_packs.sh` once it's public; `release.sh` stops if they're out of date. Dev runs use res:// unless `--map-packs`; `--map-packs-check` with
  `QUIZNIGHT_MAPS_FEED=<folder from map_packs.sh --dry-run>` tries a download. New map images need the same 3-line `importer="keep"` .import file,
  and a thumbnail for the editor's map grid: `assets/map-thumbs/<same name>.webp`, 400 px wide (made with Pillow), also
  "keep" — those stay in the .pck via the export `include_filter` (`Images.MapThumb`).
  Give a new map `AddedIn: "x.y.zb"` in `MapPresets.cs`: the grid marks it NEW for that MAJOR.MINOR (`--pretend-new-map ID` to see it).
  **Race tracks** (0.14, `MapPreset.Category = MapPresets.RaceTracks`, ids `f1-<name>`): drawn by `tools/race_tracks.py DATA_DIR`
  (track line from bacinger/f1-circuits, MIT; OpenStreetMap around it, ODbL; no corner names, they'd give answers away;
  a chequered start/finish and a direction arrow). It prints each width for `MapPresets.cs`; `REVERSE`/`START` fix lines the
  source has backwards or starting off the start line. The editor's map grid shows Game maps / Race tracks / Your maps as tabs
  (`QuestionEditorScreen.MapTab`, dev `--map-grid --map-tab "Race tracks"`).
- **Picture questions** (`QuestionType.Picture`, format 6): `Question.Slides` (one = a still picture, more = a slideshow);
  answered like classic (`Question.IsClassicLike` covers both). The host flips with `HostSession.SetSlide`
  (`Game.Slide`, ◀ ▶ / arrow keys); `PublicQuestion.ImageKey` is the shown slide's key (`q3/slide1`), and players can
  only fetch slides up to the shown one while the question is open.
  Once the host has shown every slide (`PublicQuestion.AllSlidesShown`), players flip through them on their own.
- **Step by step** (`QuestionType.Steps`, format 7): `Question.Hints` (2–5, hardest first), played as a buzzer question.
  The host flips with `Game.NextHint` (→); a new hint clears an all-wrong round of buzzes. Right is worth
  `hints − hint index` points (`PublicQuestion.WorthNow`). Cards: `StepsParts`/`HintCard` (flip tween, `HintTracker`).
- **Interactive** (`QuestionType.Interactive`, format 7): players record the task with their mic (`Ui/MicRecorder`: a muted
  "QuizMic" bus with `AudioEffectCapture` → 16 kHz mono WAV, ≤ `RecordingMaxSec`; needs `audio/driver/enable_input`), sent in
  parts (`Msg.Recording`, `HostSession.ReceiveRecording` → `Game.SubmitRecording`). Phases (`InteractivePhase`): recording
  (timer) → listening (host `SetClip`/`PlayClip`; every screen's `ClipPlayer` plays when `InteractiveView.Plays` goes up;
  recordings are fetched like pictures, asset key `q3/rec0`) → voting (`Judging.Vote`: votes are `Answer`s with the clip
  index, a point each, not your own) or the host's `PickFavourite` (`Judging.Host`, the question's points).
  Since format 13 there's also `Judging.List` ("type as many as you can"): `Question.IsList` (and `IsInteractive` is false
  for it, so none of the recording machinery runs): players send their whole list each time (`AnswerValue.Texts`, `Done`),
  the host ticks answers at the reveal (`Game.ToggleListItem`, `RevealEntry.ItemsRight`); lines of `AnswerText` are ticked
  by themselves.
- **After the answer** (format 13): `Question.AfterText`, `Source`, `AfterImage` show to everyone at the reveal
  (`PublicQuestion.AfterText`/`Source`/`AfterImageKey`, asset `q3/after`, shared ahead of time but unlocked only at the reveal;
  `GameParts.After`). `Question.BonusAnswer` is host-only (`GameView.HostBonusAnswer`).
- **Sync** (0.15, `Storage/QuizSync.cs`, My Quiznights' Sync button): each quiz as an export zip on the results repo's
  `sync` pre-release, named `<Steam ID>-<quiz id>.zip`, label `<UpdatedAt> <title>` (release files: replacing one keeps no
  history). `user://sync.json` remembers each quiz's UpdatedAt at the last sync, so `QuizSync.Decide` knows which side
  changed; both = asked (this PC's / the other's / both). Deleting stays local (the cloud keeps it; this PC doesn't take it
  back unless it changed elsewhere). Downloads keep UpdatedAt (`QuizStore.SaveSynced`). `--sync-check` = a real round trip
  under the account "selftest"; `--sync-only ID` syncs one quiz with your Steam account.
- **Steam avatars** (`Net/SteamAvatars`): every app asks Steam for the picture of "steam:<id>" players; `Avatar` draws it.
- **The host runs the pace.** Questions open hidden (`Game.Shown`) until the host shows them; buzzers stay
  locked (`BuzzersOpen`) until unlocked — both lobby rules. Players get no content of a hidden question, and
  an image reveal's picture revealed in step with the host's. Choice answers and order items are shuffled per game
  (`QuizRules.ShuffleChoices`/`ShuffleOrder`); the stored quiz is never changed.
- **Jokers** (`Shared.Joker`, `Game.PlayJoker`): Double points and Fast Pass, each once per player and match, allowed per
  lobby (`RoomSettings.Jokers`). Only one player per question can hold the Fast Pass (first come; others see
  `GameView.FastPassTaken`, not who); Double is open to everyone. They're played for `Game.JokerIndex` (the hidden current question, or the next one
  during a reveal), can be taken back until that question is shown, and are secret until then. Double multiplies the
  reveal's points (`RevealEntry.Doubled`; timeline points as they're placed; written ticks and bonuses too). A Fast
  Pass buzz always answers first: it takes over from whoever is answering (they wait first in line) and goes ahead of everyone waiting.
- **Leaving during a game keeps the seat** (`Room.Leave`/`Disconnect`): the game pauses (`Game.Paused`: no timers,
  buzzes, answers, Show or Next; the host's media pauses) until everyone is back (`Room.Join` with the same id) or the host
  continues. New players can join a running game (`Player.JoinedAt`); during an open question they sit it out
  (`Game.LateJoin`, `GameView.LateIds`). LAN players keep one client id per app run, so rejoining gets the seat back.
- **Saved nights** (`Server/SavedNight.cs`, `Storage/NightSaves.cs`: `user://saves/<quiz id>.json`): the host's game
  (`HostSession.Autosaves`, set by `App.HostQuiz`, not tests) writes `Room.SaveJson()` on every broadcast if it changed:
  during a reveal everything up to that question, while one runs the snapshot taken when it opened (`Room.NoteOpened`);
  nothing during question 1. The quiz page / list offers Continue → `App.HostQuiz(quiz, resume: save)` → `Room.Restore`
  (saved seats `Player.Saved`, away, never dropped from the lobby; LAN seats reclaimed by name, Steam by id) → Start =
  `Room.GoOn` (Game opens at `save.Next`, used jokers restored). Deleted when the night finishes. `--continue` with
  `--host-sample` continues its save.
- **Ranking** (`Room.Ranking`, sent as `RoomState.Ranking`/`Places`): points, then who reached that score first
  (`PlayerStats.ScoreChangedAt`), then average buzz/answer time, then average km off; fully equal players share a place.
  Players' scoreboards show the top 3 and the positions around them (`GameParts.ShownPositions`), the host's everyone.
- **Results and the eternal table** (`Storage/Results.cs`, `ResultsUpload.cs`): when a game over Steam ends, an
  exported build uploads a CSV to the private repo `maxlvl01/quiznight-results` (`results/`), using the
  fine-grained token in `results-token.txt` (repo root, gitignored, shipped via the export include_filter; it can only
  write that repo). Only if the upload fails is the CSV kept in Documents/Quiznight/Results. Editor runs and LAN games
  never upload. To update the table: copy the repo's CSVs into `assets/results/` (nights before 1.0: `assets/results/beta/`,
  the Beta table) with a 3-line `importer="keep"` .import each (else Godot imports CSVs as translations); `EternalTableScreen` adds them up per player id (the same
  night twice counts once). Never ship test results there.
- **Bug reports and ideas** (`Storage/BugReports.cs`, the 🐞 button): a bug becomes a GitHub Issue in
  `maxlvl01/quiznight-results` (title "<category> (<version>)", label `bug`, log in `<details>`; Max adds `wip`/`pending`
  and closes it when the fix is out), its screenshot a file in `bugs/Reported/`. This needs the shipped token to have
  "Issues: read and write"; without it (or on any error) the bug goes as a `.md` file to `bugs/Reported/` as before.
  Ideas stay files in `features/Requested/` (moved on by hand: WIP, Pending, Resolved). Offline reports wait in
  `user://bug-reports`. Each sent report pushes to ntfy.sh, opening the issue/file. `--bug-check` sends a real test report.
- **Discord Rich Presence** (`Net/DiscordPresence`, DiscordRichPresence NuGet, app ID in the code): `App.UpdatePresence`
  every 2 s from the screen and the room (never the room code); off with `Settings.DiscordPresence`, in headless runs and
  in dev runs with arguments. The Discord app's art asset key is `logo`.
- **Guided tour** (`Ui/Tutorial.cs`, `Tutorial.Guided`): offered once on Home, Settings' "Take the tour" starts it again.
  Steps say their screen (`Step.Where`) and the overlay opens it. The sample quiz (in memory only, id `TourDownload.Id`,
  which `QuizStore.Save` skips) is "Test Night", downloaded in the background when the tour starts (`Storage/TourDownload`,
  `tour.quiznight.zip` + `.sig` on the maps release: `tools/tour_quiz.sh [--test] QUIZ.zip`), deleted when it ends; not there
  in time or offline = the built-in `Tutorial.TourQuiz()`. `--tour-wait` (with a local `QUIZNIGHT_MAPS_FEED` from
  `tools/tour_quiz.sh --dry-run`) shows the downloaded one. New screens' parts are pointed at with `Tutorial.Mark(control, "key")`. `--screen home --tutorial guided
  --tutorial-step N` for screenshots.
- **Podium** (`Ui/Screens/PodiumScreen`): built once from the first finished state (`Shared.FinalResults`), never
  again, so leaving can't change it (and `Room.Leave` keeps the seat in `Finished` too). Podium + score list big in the
  middle (one `ScaledBox`), then they move into the right third (`_t`, `Layout()`) and `CreditsRoll` plays in the left
  two thirds, ending on the awards. Awards are made on the host (`Server/Awards.cs`, with the next two as `Runners`, sent as
  `RoomState.Awards`, kinds/texts in `Shared.NightAwards`). Every podium is
  kept in `user://podiums` (`Storage/Podiums`) and its award pack drawn in the background (`NightPack`, the season pack's
  `SeasonPoster` with `Night: true`; neither in dev runs with arguments). Test lab → Podium; `--screen podium
  [--players N] [--tie] [--skip-show] [--night-pack FOLDER]`; `--screen history --pretend-podium`.
- **Quiz page** (`QuizOverviewScreen`): Ctrl/Shift+click pick rows (`_picked`, by question), a picked row's drag moves all
  of them (`QuestionRowButton.Dragged` → `MoveMany`). "By category" groups by `Quiz.CategoryOrder` (format 15), ▲ ▼ per
  category. `--screen quiz [--tour-quiz] [--list category] [--category-order a,b]`.
- **Team mode** (0.13.1d1, `RoomSettings.Teams`, 0 = off, 2–6): a "Play in teams" tick box sends `TeamNames.Auto`; until
  the draw the count follows the players (`Room.FollowPlayers` in `Tick`, `TeamNames.Suggest`: ~3 a team, even preferred)
  unless the host picked one of `TeamNames.Offered`. Only fair splits exist (`TeamNames.Splits`: smallest team ≥ 3/4 of the
  biggest, ≥ 2 a team; none for 3 or 5 players, and `Room.DrawTeams` refuses); no handicap for smaller teams (Max's call). The host draws teams at random in the lobby
  (`Room.DrawTeams`, `Player.Team`; `RoomState.TeamDraw` counts draws so every lobby plays `Ui/Screens/TeamDraw.cs`'s show
  once; newcomers join the smallest team). The game only knows `IGameHooks.TeamOf`: `Game.Mates` makes one answer per team
  (any member replaces it; `GameView.MyAnswerBy`), one buzz per team per round and jokers once per team. Scores stay per
  player; a team's is its members' sum (`Room.Teams()`, `RoomState.Teams`). Podium: `FinalResults.Teams`/`Ranked`. Team
  nights aren't uploaded (no Ewige Tabelle). Test lab → Team draw; `--screen testlab --team-draw 8,3`.
- **Mini games over the internet** (0.13.2): the host runs Pong/billiard (`ArcadeHost`, frames 60/s with `ArcadeFrame.Time`)
  and allows no Pong/billiard for players still downloading the quiz (`ArcadeHost.Downloading`, `ArcadeView.Downloading`),
  nor background files to a player in a match (`ArcadeHost.Playing`; Steam has one ordered line per player, our
  Facepunch 2.3.3 has no lanes). `PongView` predicts the ball (`PongRules.Step(predict: true)`) and fades corrections;
  the host accepts paddle positions from the last 0.25 s (`LagAllowance`). `BilliardView` plays back 0.1 s behind,
  interpolating frames. Test with two copies: the player with `--net-lag 120`.
- **Music pack** (0.13.1d1): exports leave the songs out of Quiznight.pck; they're `music.pck` next to the exe (export
  preset "Music pack", file list by `tools/music_preset.py`), mounted by `App.LoadMusicPack`. `release.sh` keeps it in
  `~/.quiznight/music-<hash of the songs>.pck` so it stays byte-identical (every .pck carries the version) and isn't
  downloaded again. `manifest.py` also gives each engine/.NET file ≥ 1 MB its own update pack.
- **Question preview** (`Ui/Screens/QuestionPreview`, the editor's 👁 Preview): a real game over the editor, a `Room`
  (`Preview = true`: one player starts it) with only the draft question, its `HostSession` and a `ClientSession` joined
  through `Net/LoopbackTransport` (in memory), showing the player's own `PlayerGameScreen` plus host buttons on top.
  Not `App.Host`/`App.Client`; `--open TYPE --preview` opens it.
- **Game state is per viewer** (`Room.ToState(viewer)`): players never get answers before the
  reveal. Question images/custom maps are fetched by asset key (`q3/image`, `q3/map`) only for
  questions already shown, split into ≤300 KB parts (Steam's message limit is 512 KB).
- **Players see and hear what the host's screen shows** (no Discord stream needed), except answers. Sound and
  video files go to every player in the background from the game's start (`Game.SharedFiles`, `HostSession.SendMedia`:
  binary `MediaChunk` messages, AES-encrypted, paced with `TrySend`); the id and key come with the question once
  it's shown. Since 0.8.11b this covers all pictures too (question images, slides, custom maps: `SharedMedia.Asset`),
  made per room (`Room.SharedFiles`) and sent from the lobby on; `PublicQuestion.Files` unlocks the shown question's
  pictures (`ClientAssets` falls back to asking by asset key if one isn't downloaded); the lobby's Start waits for
  `RoomState.Downloads`. Steam: refused sends wait in `SteamHostTransport`'s outbox (never dropped); background files
  (`TrySend`) only go while nothing waits, at a per-connection speed that halves when the buffer runs full.
  Players report download progress (`Msg.MediaProgress`); the host starts sound/video by hand
  (`HostMedia`, "Everyone has it"), players' copies follow the play clock (`PlayerVideo`, `PlayerSound`,
  `PlayerImageReveal` via `PlayClock`) and the video is blurred until it first starts. After a wrong answer
  media stays paused until the host resumes it. Godot plays MP3/OGG/WAV and `.ogv` video only (no MP4/WebM) —
  the editor rejects others with a conversion hint.
- **Timers run on `Room.Tick()`** from `_Process` (no threads); all networking is polled on the
  main thread. Steam callbacks run inside `SteamClient.RunCallbacks()`, so awaited Steam calls
  continue *inside* a callback — never shut Steam down there (`App.Quit` defers for that reason).
- **UI is built in code** (`scripts/Ui`), not in .tscn files: one root scene `scenes/Main.tscn`
  with `App.cs`. Screens that update often use `Keyed` slots so inputs, map zoom and the music
  player survive state updates.

## Layout

| Path | What |
|------|------|
| `scripts/Shared/Quiz.cs` | Quiz/question model, limits, `QuestionIssues`/`QuizIssues`, `MapDistance` |
| `scripts/Shared/QuizJson.cs` | Reading/writing `.quiznight.json` |
| `scripts/Shared/Protocol.cs` | Limits, room-code alphabet, player colours, state DTOs, messages (`Msg`) |
| `scripts/Shared/MapPresets.cs`, `WowAtlas.cs` | Built-in maps with real scale; WoW Classic world → continent → zone |
| `scripts/Server/Game.cs`, `Room.cs` | Question flow, buzz queue, judging, answers, play clock, reveal + scoring; players/settings/phases |
| `scripts/Server/HostSession.cs` | Host side: messages ↔ room, per-viewer broadcast, asset sending |
| `scripts/Net/` | `SteamTransport` (lobbies + relay), `LanTransport` (TCP), `ClientSession` (player side, auto-reconnect), `Assets`, `ServerClock` |
| `scripts/Storage/` | `QuizStore` (user://quizzes), `Images`, `Media`, `Settings` |
| `scripts/Ui/` | `UiKit` (theme, colours, builders), `Widgets`, `MapView` (zoom/pan map), `WowMap` (seamless WoW map: world → continents → stitched zone tiles → cities), `ImageReveal` (zoom/pixel reveal) |
| `assets/maps/wow/stitched/` | OpenMap's baked zone art (207 tiles), converted by `tools/openmap_tiles.gd` from the addon's `Media/Textures/MapZones` (BLP) |
| `scripts/Ui/Screens/` | Home, QuizList, QuizOverview, QuestionEditor, HostLobby, WaitingRoom, HostGame (+ Curtain, HostNotes window), PlayerGame, Podium, Settings |
| `scripts/Dev/` | `SelfTest`, `SampleQuiz`, `DevTools` (bots, autopilot, screenshots) |

User data: `%APPDATA%\Quiznight\` (quizzes, settings.cfg, cache).

## Commands

Godot is not on PATH. Use the console exe:
`G="/c/Users/Xbox3/Desktop/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"`

```bash
dotnet build "QuizNight!.csproj"                              # compile — run before every commit
"$G" --headless --editor --path . --quit                      # (re)import assets after adding images
"$G" --headless --path . -- --selftest                        # rules, files, atlas, protocol, real TCP round trip; exit 0 = pass
"$G" --headless --path . -- --steam-check                     # needs Steam running: opens a lobby, finds it by code
"$G" --path . -- --no-steam --host-sample --autopilot --quit-at-end --shots DIR   # host the sample quiz, play it alone
"$G" --headless --path . -- --bot Ann                         # a bot player joining LAN 127.0.0.1
"$G" --path . --resolution 1600x900 -- --no-steam --open mapPin --shot out.png    # screenshot a screen
```

`--sound FILE` / `--video FILE` (with `--host-sample` or `--open`: the sound question plays that file / a video question follows it), `--screen home|quizzes|quiz|settings|testlab|dialog` (dialog: a pop-up over home), `--open classic|choice|multi|picture|estimation|mapPin|wow|sound|image|order|timeline|write` (`--map-preset ID` with mapPin), `--start-at N` (with `--autopilot`), `--bot-drag` (a windowed bot drags the timeline item with real mouse events), `--map-zoom Z [--map-at X,Y]` (WoW map), `--skin ID --space-time T --space-check` (space scene at second T; prints any shape Godot can't fill, which would log an error every frame; add Godot's `--time-scale 8` before `--` to run through its visits), `--screen testlab --dropdown-check` (does the long drop-down scroll on its first open), `--fullscreen-shape 16:9` (with Godot's `--fullscreen`: the picture in that shape, not saved), `--display-modes` (prints each
monitor's fullscreen resolutions: Windows switches the monitor itself in fullscreen, `Storage/DisplayModes.cs`), `--screen settings --window-mode fullscreen` (the settings as if that mode were picked), `--shot FILE --after SEC` (screenshot runs ignore the saved window, so `--resolution` and `--fullscreen --screen N` apply)
(see `DevTools.cs`). Full solo test: host with `--autopilot --players 2` plus two `--bot` instances.
Screenshots need a window (no `--headless`); the window opens briefly on the user's screen.
Windows shell: use `py` (not `python`) for scripts; `node -e` works for multi-line scripted edits. `--start-at N` is 1-based.

## Gotchas learned so far

- **The game starts as a small borderless window (800×450, project.godot's window size override + borderless):** that's
  the anti-cheat splash (`Ui/IntroSequence.cs`: `AntiCheatSplash`, its first frame is the boot splash, `--make-splash`).
  App then turns it into the saved window (`Prefs.ApplyWindow`) and plays the intro (`IntroSequence`: video, cards,
  title screen whose logo glides into `HomeScreen.Logo`). Runs with arguments after `--` get their window at once, no
  intro. Dev: `--splash [--intro-from N] [--intro-start]`, `--anti-cheat`. No music while `IntroSequence.HoldsMusic`.
- **Anything from outside is checked (`Shared/Guard.cs`):** pictures by their header size before decoding, packed quizzes
  and zips read capped, recordings must be the game's own WAV format, CSV cells formula-safe, file ids plain. Over Steam a
  join needs the room code (`JoinRequest.Code`) and a Steam-confirmed ID (claimed ones are refused); the lobby list only
  has `SteamService.CodeKey` (PBKDF2) of the code; kicked players stay out; `RoomSettings.FriendsOnly`. LAN host frames
  ≤ 1 MB, joins within 15 s, ≤ 48 connections. The self-test's `Safety()` covers these.
- **Never use emoji for graphics (Max's rule, 0.13.3b).** No emoji as icons on buttons, labels, pills or anywhere players
  see a picture: they look pasted in from another app. Draw a fitting icon in the game's style instead: a
  `MediaIcon.Kind` shape (`Ui/MusicPlayer.cs`: flat, in the skin's text colour, details cut out in `C.Panel2`; the bug,
  jukebox, gear and calendar buttons), a `Glyph` (`Ui/Widgets.cs`, 0.13.4b: one-colour SVG symbols, `button.On(Glyph.Kind.X)`
  tints with the button's font colours, `Glyph.Before(kind, label)` puts one in front of a text) or a coloured SVG like
  `Bolt` (filled, ink outline). Text marks like ✓ ✗ ★ ▶ are fine (monochrome font glyphs, not emoji).
- **Recording a run (only when Max asks for one):** never capture the desktop (ffmpeg gdigrab filmed his Discord, 2026-11-02). Use Godot's own
  movie writer per window: `--write-movie FILE.avi --fixed-fps 30` before the `--` (all windows come out 800×450, the splash size
  override wins over `--resolution`), host with `--host QUIZ.zip --autopilot --players N --quit-after-credits` (quits 4 s after the
  credits closed, so the AVI is finalized; a killed process leaves a broken file), bots `--bot NAME` (windowed ones get their own
  movie; they quit by themselves when the room closes, `--headless` ones need no video). A 30-question night is ~6 min; convert
  with ffmpeg (`-c:v libx264 -crf 26`, the AVIs are ~800 MB each).
- **My quizzes list:** `QuizStore.List()` caches; `Rescan()` re-reads only new or changed files (`Stamps`) and runs each time the
  screen opens (plus a Refresh button), so quiz files dropped into the folder show up without a restart.
- **Custom cursors** (`assets/cursors`, `tools/make_cursors.py`) are set in `App.UseCursors`, cleared in `App._ExitTree`
  (else Godot reports their textures as leaked at exit).

- **`default_texture_filter=3` is Nearest Mipmap** (the project setting's order differs from CanvasItem's enum), so
  scaled art looks blocky. Maps set `TextureFilter = LinearWithMipmaps` on their canvas.
- **Headless runs (bots, self-test) have a dummy renderer**: don't build textures there (`WowMapCanvas` skips drawing).
- **Sounds must be Ogg Vorbis, MP3 or WAV.** Ogg *Opus* files (common from browser/Discord downloads) fail to
  import (`valid=false` in the .import file). The self-test loads every built-in sound to catch this.
- **English only (since 0.14, Max's call):** the German UI (`LangDe.cs`, the language setting, `--lang`) was removed to
  save work; quizzes can still be written in any language. `Lang.T`/`Lang.F` stay as pass-throughs (text built from parts
  still uses `Lang.F("… {0} …", x)`, and player text keeps `.Raw()`), so a language could come back without hunting
  strings. `Units.Find` and `DatePicker.Parse` still read the German words/dates old quizzes stored.

- `GetCmdlineUserArgs()` only sees arguments after `--`.
- `RandomNumberGenerator` is ambiguous between Godot and System.Security.Cryptography — qualify it.
- In-memory `JsonValue`s don't convert number types on read; `Question.Clone()` round-trips through text.
- Stretch mode `canvas_items` with a 1280×720 base: sizes in code are base units (×1.25 at 1600×900).
- Godot C# classes need a parameterless constructor (we add `public X() : this(...)`).
- `steam_api64.dll` comes from the Facepunch package's `content/` folder and is copied to the build
  output by the csproj (`None … CopyToOutputDirectory`).
- **Exporting:** the 4.7.2 mono templates are installed (Windows x86_64). C# export needs `QuizNight!.sln`
  next to the csproj. The build lands in `build/Quiznight/`; `steam_api64.dll` ends up in the
  `data_QuizNight!_windows_x86_64` folder and Steam works from there (checked with `--steam-check`).
- **Test builds first (from 0.10.3b):** every new version goes to the private `maxlvl01/quiznight-test-releases` with
  `tools/release.sh --test` (zips only, title "… (test)"); Max downloads and tries it there. Only when he says a version is
  ready does it go public with plain `tools/release.sh` (players shouldn't download every hotfix). Going public folds the test
  versions' changelog sections since the last public release into that version's one section (`tools/fold_changelog.py`,
  run by `release.sh`; commit CHANGELOG.md afterwards), so players see one version with everything. Test builds have
  `test-channel.txt` next to the exe (release.sh puts it in test zips) and update from the test repo instead
  (`Updater.TestChannel`: the release list via the API, as pre-releases aren't "latest"; zips only; read with the shipped
  `results-token.txt`, which needs Contents: read on quiznight-test-releases). Test builds also start in developer mode, log every step
  (`Shared/Trace.Log`, timestamped: screens, connections, messages, the game's steps) with Godot's verbose output
  (`override.cfg` in test zips), and send each run's whole log to the results repo's `test-logs/` on the next start
  (`Storage/CrashReports`; public builds send crash/error logs to `crashes/` only). 0.10.3b was public for a few minutes and is a
  draft on the public repo now (nobody had downloaded it); drafts aren't deleted either.
- **Releases & updates:** `tools/release.sh` exports, zips and publishes a GitHub release on the public code repo
  `maxlvl01/quiznight` (`Updater.ReleasesRepo`; it was `quiznight-godot` until 0.13.5, the old web version is the archived
  `quiznight-web`). Games before 0.13.5 check the old downloads-only `maxlvl01/quiznight-releases` (`LegacyReleasesRepo`),
  whose newest release is 0.13.5b (the bridge: it knows the new repo), so they update to that and then follow the new
  repo; nothing is copied there any more (`release.sh`'s `LEGACY` is off). Never delete or rename it, and no release
  newer than 0.13.5b goes there (its update lists point into it). Exported games check
  it at startup (`Storage/Updater.cs`) and update themselves; "What's new" is read from the shipped
  CHANGELOG.md, so its `## [version]` / `### Added|Changed|Fixed|Removed` layout matters.
  **Updates are signed:** `release.sh` signs each zip and update list (`<file>.sig`, `openssl dgst -sha256 -sign`) with Max's
  release key, `~/.quiznight/release-signing-key.pem` (ECDSA P-256, never in git: back it up; `QUIZNIGHT_SIGNING_KEY`
  overrides the path). The game has the public half (`Updater.ReleaseKey`, also `tools/release-signing-public.pem`) and
  installs nothing whose signature doesn't check out (from 0.11.2b on; older builds don't check).
  Updates come in packs: each release has `update-windows.json` / `update-linux.json` (every file's SHA-256 and the
  pack it's in, made by `tools/manifest.py`). Packs are zips by kind (game, engine, maps, anyconverter) named by a
  hash of their contents; only new packs are uploaded (usually just the two game packs), the rest point into older
  releases — so **never delete old releases**. The game downloads only the packs with files that differ (off the
  main thread: awaiting there throttles reads to one per frame), falling back to the zip. (0.7.3b briefly had one
  `.gz` per file with `manifest-*.json`; removed again, nothing used them.) Test against a local server
  with `QUIZNIGHT_UPDATE_FEED=<url of a fake releases/latest JSON>`. `build/` has a `.gdignore` (else the editor
  imports the copied maps).
- **Exports trim .NET** (`PublishTrimmed`, `TrimMode=partial` in the csproj, ExportRelease only): only libraries marked
  trimmable shrink, our code/GodotSharp/Facepunch stay whole. Two feature switches trimming turns off must stay on, or the
  exported game breaks: `_EnableConsumingManagedCodeFromNativeHosting` (Godot starts .NET from its native host; off = the
  game crashes at start, `hostfxr_get_runtime_delegate failed`) and `JsonSerializerIsReflectionEnabledByDefault` (our
  JSON goes through reflection). `release.sh` also deletes .NET's debugger/dump files (`strip_dotnet`). Editor runs aren't
  trimmed: test trimming-related changes on an export (`--selftest` on the exported exe).
- **Slim engine (Windows):** the Windows export uses a custom export template built from the Godot 4.7.2-stable source
  (`C:\godot-src\godot-4.7.2-stable`) with `tools/engine_profile.py`: no 3D, physics, navigation, XR, Vulkan/D3D12,
  GDScript, networking modules, ZIP, regex … (37 MB instead of 105 MB). `export_presets.cfg` points at
  `C:/godot-src/templates/quiznight-windows-release.exe`; `release.sh` stops if it's missing. If the game starts using
  an engine feature that's left out (a 3D node, physics, Godot's RegEx/ZIPReader …), it works in the editor but not in the
  export: put the module back in the profile and rebuild (~9 min). Linux uses the stock template (no WSL here).
- **AnyConverter** (the user's separate Python/tkinter converter, `C:\Users\Xbox3\Desktop\AnyConverter`, not in git)
  ships in the Windows zip as `AnyConverter/AnyConverter.exe` and in the Linux zip as `AnyConverter/AnyConverter`;
  `release.sh` copies them from its `dist/` (`AnyConverter.exe`, and `linux/AnyConverter-linux-x86_64.tar.gz` from its
  GitHub Actions build; override with `ANYCONVERTER=…` / `ANYCONVERTER_LINUX=…`) and stops if one is missing. Rebuild it with `py -m PyInstaller --noconfirm AnyConverter.spec`.
  `VideoGuide` opens it with `--format ogv FILE`. FFmpeg is deliberately not bundled (too big): AnyConverter
  prompts players to install it.

## Versioning rules

Same as the web version: **MAJOR.MINOR.HOTFIX** (`0.4.0a` = alpha, `0.7.1b` = beta; the home screen stamp follows the letter), stored in
`project.godot` (`application/config/version`). **Test builds from 0.12 on are `0.12.0d1`, `0.12.0d2` …** (d = dev, then the test
build's number; `AppVersion.Build`, stamp "DEV"): they sort before every other version with the same numbers, so the public
release gets its own version (e.g. `0.12.0b`, `release.sh` refuses to publish a d version) and `fold_changelog.py` folds the
d sections into it. Test builds before 0.12 still used b numbers. Older test builds can't read d versions, so a 0.11.x test
build won't update to 0.12.0d1 by itself: download it once by hand. Add changelog entries under `[Unreleased]` in
CHANGELOG.md while working (plain language, for friends to read). Big updates (a new MINOR, x.y.0) get a name, chosen
by Claude: `## [0.8.0b] — "Jokers Wild": what's in it`; `release.sh` makes the release title "Quiznight 0.8.0b — Jokers
Wild" from it. Players see a named update as its MAJOR.MINOR ("0.11 — Opening Night", `Changelog.Release.Shown`, also the
release title), while the game's version stays the full one (0.11.3b). If the quiz file format changes,
bump `QuizRules.FormatVersion` and keep old files loading.

**Never `git push` (or push tags, create releases/PRs) without the user's explicit approval for that
specific push.** Committing locally is fine.

## Roadmap (one shared doc, not a file)

The roadmap lives in **one Claude Doc, "Quiznight? Roadmap"**: https://claude.ai/artifact/4PoK3kHoJwtFEUVHs7UKTT
(doc id `1b7a51b6-3f58-4416-96b8-41b7e6af835e`). Every chat reads and edits that doc with the docs tools (load the docs
skill first), so it never needs merging between worktrees. **Don't keep or edit a ROADMAP.md in the repo**; the file
only points to the doc. When an item is done, tick it in the doc with its version. The public, player-friendly copy is
ROADMAP.md in `maxlvl01/quiznight-releases` (move it to `maxlvl01/quiznight` when it's next refreshed), refreshed from the doc only when Max says so.
Doc ids in reads and edits are full (`m81rec0vxb3.45279`, not the relative `.45279` a view prints); a search read takes `text`,
not `query`; add an item with `insert` after a list item's id.

## Next up

1. Install export templates and make a Windows build friends can run; check Steam works from it.
2. Real test with friends on two PCs over Steam (only lobby + lookup are tested so far — not a
   relay connection between two accounts).
3. Map pin "players must buzz in first"; buzzer fairness via on-device reaction time.
4. Optional: Steam invites (join via friends list), avatars from Steam.
