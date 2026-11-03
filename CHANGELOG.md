# Changelog

All notable changes to Quiznight! are listed here, newest first.

Versions follow **MAJOR.MINOR.HOTFIX**:
- **MAJOR**: a big milestone or a breaking change (e.g. old quiz files no longer load).
- **MINOR**: new features that don't break anything (new question type, new screen).
- **HOTFIX**: bug fixes only.

`0.x.y` means the game is still in development: a letter `a` is an alpha, `b` a beta (from 0.7.1b on).
`1.0.0` will be the first complete, playable quiz night.
Versions up to 0.3.2a were the website; the history of those is in the website's CHANGELOG.md.
On GitHub, each version heading links to its release, where the download is.

## [Unreleased]

### Changed
- **Bug reports no longer contain your Steam ID.** They still say your Steam name (so ideas can be credited), but the ID
  and the profile link are gone, and any Steam ID that turns up in the log part (the "someone joined" lines) is replaced
  by "<steam id>".

## [0.15.1b]

### Fixed
- **The game took more and more video memory the longer it was open** (reported by Max: 2.2 GB at the start, 3.5 GB
  later). Every built-in map that was shown (in the editor, the map picker, or a game) stayed in video memory until the
  game was closed, and the big ones take about 50 to 90 MB each, so clicking through map questions added up fast. Now
  only the last four maps stay loaded and the others are let go. Opening ten maps in a row used to end at 575 MB of
  textures and kept climbing; now it stays at about 260 MB.
- **The background music kept playing over a sound you previewed in the editor** (reported by Max). Pressing ▶ Play on
  a sound or video question's file now fades the music out like a sound question does in the game, and it comes back
  when the preview stops.
- **The question jumped around while the timer counted down** (reported by Max, in the Interactive preview). The
  timer's digits aren't all the same width, so each second the question next to it had a little more or less room and
  a long one wrapped differently. The timer now always takes the room of its widest time.

## [0.15.0b] — "Cloud Nine": sync your quiz nights between your PCs, My Quiznights and a lighter host

### Added
- **Sync your quiz nights between your PCs** (idea by Max). My Quiznights has a new **Sync** button: it sends the quiz
  nights on this PC to the cloud and brings the ones from your other PCs here, sounds and videos included, so you can
  work on a quiz night on your desktop and finish it on your laptop. They're kept under your Steam account, so you never
  get a friend's half-finished quiz (or they yours); Sync needs Steam running. You press it yourself, nothing syncs
  behind your back. If a quiz night was changed on both PCs since the last sync, the game asks which one to keep (this
  PC's, the other PC's, or both). Deleting a quiz night only deletes it on that PC: the cloud keeps its copy, and this PC
  won't bring it back unless it's changed on another PC later.
  The guided tour shows the Sync button too.

### Changed
- **"My quizzes" is now "My Quiznights"** (idea by Max): the list's heading, the Home button, the quiz page's back link,
  the tour and the help texts.
- **Smoother running:** pictures sent by the host are now decoded in the background, so a big picture no longer makes
  the screen stutter when its question is shown. The host's game saves the night at most once a second instead of on
  every click at a reveal, and a few things the game checked every frame (settings, the Windows "reduce motion" option,
  idle sound bars) are now looked up once and remembered.
- **Less work for the host when lots happens at once:** every answer, buzz, joker and click used to send the whole room
  to every player straight away, and the host's game worked out the scoreboard, teams and stats again for each player.
  Now everything that changes within one frame goes out together, once, and the parts that are the same for everyone are
  worked out once per send. With many players answering at the same moment the host's game stays smooth, and the
  messages themselves are made with less copying.

## [0.14.3b]

### Fixed
- **Installing FFmpeg or LibreOffice from the converter could stop on some PCs:** when Windows' Microsoft Store source for
  winget was broken (certificate errors), winget stopped and asked which source to use. The converter's install buttons
  now tell winget to use its own source.

## [0.14.2b]

### Fixed
- **On a first start, the guided tour opened on top of "What's new"** (reported by Max). Both showed up as soon as the
  intro ended, with the tour covering the patch notes, so you had to skip one to read the other. The tour now waits
  until "What's new" (or the update window) is closed, then starts.

## [0.14.1b]

### Changed
- **Race tracks show more of what's around them:** lakes, rivers, bays and parks that were missing are now drawn, like
  the lake inside Albert Park, the river around Montreal's island, Singapore's Marina Bay and the marina at Yas Marina.

## [0.14.0b] — "Lights Out": Formula 1 race tracks, team names, deleted quizzes you can bring back and grouping by question type

### Added
- **Group your quiz by question type:** on the quiz page, "By type" now shows a heading for each type with ▲ ▼, just
  like "By category", so you can move all the map questions before the music ones and then press "Play in this order".
  The quiz remembers the order. Asked for by Max.
- **Race tracks for map questions:** all 22 circuits of the 2026 Formula 1 calendar (Bahrain and Jeddah are off it this
  year), from Albert Park to Yas Marina, each drawn true to scale so players see how many metres off their pin was. The
  editor's map list now has three tabs: Game maps, Race tracks and Your maps. Each track is drawn the same way, light
  and clear so it stands out in every skin: the track in dark grey with the pit lane beside it, a chequered start/finish
  line, an orange arrow showing which way the race runs, and the land around it (trees, water, buildings, roads). No
  corner names are written on it, so "Where is Eau Rouge?" stays a real question. Like the game maps, each track
  downloads the first time it's used. Track lines by Tomislav Bacinger (f1-circuits), map data © OpenStreetMap
  contributors. Other racing series come next.
- **Name your team:** after the draw, every team can give itself a name. In the waiting room your team shows under the
  seats: double-click the name (or press Rename) and type a new one, or roll the dice for a new name picked at random
  (there are 30 now, from Quiz Khalifa to Sherlock Homies). Anyone in the team can change it, and the host can rename
  any team. Names stay when a saved night continues; a new draw starts over with the drawn names. Asked for by Max.
- **Deleted quizzes can come back:** deleting a quiz in My quizzes now keeps it for 30 days. Right after deleting there
  is an Undo, and "Recently deleted" (under the buttons) lists them with a Restore button each.
- **Arrows in the question editor to go to the previous or next question.** Top right, next to "Preview": the left
  arrow opens the question before this one in the play order, the right arrow the one after it, and between them
  you see where you are ("3 / 15"). Ctrl+Page Up and Ctrl+Page Down do the same. If the question has changes you
  haven't saved, it asks first, with "Save them first" ticked (untick it to leave them behind). Before, you had to
  go back to the quiz page for every question. (Thanks for the idea, Max!)

### Changed
- **Ewige Tabelle:** the yellow column (the one the table is ranked by) now stays next to the place and the name while
  the other numbers scroll sideways, so you can always see who has what. Asked for by Max.
- **My quizzes:** the delete button is a small red key with a trash can, like the other buttons on the card, instead
  of a loose can that said "Delete?" under the mouse. Hold the mouse on it to read what it does. Asked for by Max.
- The test builds are renumbered: 0.13.5d1 to d3 are 0.13.6d1 to d3 now, because 0.13.5b went out on its own with only
  the move of the downloads to github.com/maxlvl01/quiznight. This build has that too.
- The guided tour now walks through a real quiz: "Test Night", 30 questions with one of every question type, pictures, sound and video (Max's idea). It's too big to ship with the game (13 MB), so it downloads in the background while the tour's first steps are on the home screen and the quiz list, and it's deleted again when the tour ends. It only lives in memory: it never shows up in My quizzes, and saving during the tour (Ctrl+S) writes nothing. If it hasn't arrived by the time the tour opens the quiz (slow internet, offline), the tour uses the small built-in sample quiz as before.
- **My quizzes reads the folder again every time you open it**, and has a "Refresh" button. The list used to be read
  once per run, so a quiz file you copied into the quizzes folder yourself only showed up after restarting the game.
  New and changed files now show at once, and deleted ones disappear. (Thanks for the idea, Max!)
- **"Sound" questions are called "Audio", and "Map pin" questions "Map"**, in the editor and everywhere else the type
  is named. Nothing else changes: your quizzes keep working as they are. (Thanks for the idea, Max!)

### Fixed
- **"My quizzes" broke onto two lines when an update was out** (reported by Max). The "Update to …" button sat in the
  same row as the heading and the five buttons, squeezing the heading and stretching the buttons. It now sits above
  them, next to "← Home".
- **Deleting a quiz could lose it for good without asking** (reported by Max). Once "Don't ask me again" was ticked on
  the delete question, one click on the red trash key deleted the quiz straight away, and the file was gone. Now a
  deleted quiz is kept for 30 days and can be restored (see above), and the question says so.
- **Short drop-downs still showed a scrollbar for a moment when they opened** (reported by Max). The fix in 0.13.5 looked
  for the list's scroll area in the wrong place (it sits one level deeper), so it never hid the bar. Now it finds it:
  no scrollbar while a short list unfolds, and long lists still scroll.
- **The NEW badge on a new map in the editor's map list covered the whole picture** instead of sitting in its corner.
- **The question editor froze when stepping through the questions with the arrows** past a video question (reported by
  Max). Every time a video question was shown, the editor unpacked the whole video and wrote it to the disk again, before
  you had even pressed Play. Now the video is only loaded when you press Play, and the same video isn't unpacked twice.
- **My quizzes sat off to the right**, with a wide empty strip on the left (spotted by Max). The two columns of quiz cards
  needed a little more room than the page gives them, and the extra width all went to the right side. The page is
  centred again (this fixes every page that is wider than usual).
- **The host's buttons no longer slip off the bottom of the screen.** On a long question with a picture and four
  answers (found in a test night with the paella question), "Reveal answer", "Hold to peek" and "+5 s" were pushed below
  the window, so the host saw only their top edges and had to scroll down to reveal. The question card and the buttons
  under it scrolled as one block. Now only the card scrolls and the buttons stay pinned underneath, always in view.
  Pictures in a question also give way a bit more before anything has to scroll (they can still be clicked to see
  them big), which helps players on the same questions too. (Thanks for asking for the test run, Max!)
- **"Data as of" on estimation questions gets its own line.** With an explanation next to it, the date note sat
  beside it without wrapping, so a longer note ran off the edge of its box. It now sits under the explanation and wraps,
  and the little "i" in front is a drawn icon instead of Windows' blue emoji.
- **Drop-down lists don't flash a scrollbar any more.** A short list that doesn't need scrolling showed its scrollbar
  for a few frames while it rolled open: during the animation the list is briefly smaller than its items, so the
  scrollbar popped up and vanished again. It now stays hidden while the list opens and only shows for lists that
  really scroll. (Thanks for the report, Max!)
- **Mini games: one line per rivalry.** In the lobby's mini games, each rivalry ("Ann 3 – 1 Ben · Pong · Ann ×2 in a
  row") was one long sentence that wrapped over two or three lines in the narrow column, so you couldn't tell where
  one rivalry ended and the next began. Each is now one row: the names and score, the game small on the right and a
  streak as "×2" (hover for the whole line). (Thanks for the report, Max!)

### Removed
- **German menus:** Quiznight is English only now, so the Language choice is gone from the welcome screen and the
  settings. Keeping every screen in two languages took a lot of work for each new feature. Your quizzes can still be in
  German (or any language): questions, answers and names are shown exactly as you wrote them, and units like "Tage" or
  dates like "1. November 2026" in older quizzes still work.

## [0.13.5b]

### Changed
- **Updates now come from one place: github.com/maxlvl01/quiznight.** The game's code and its downloads used to be two
  separate pages on GitHub (quiznight-godot and quiznight-releases), which was confusing. They're one now, called Quiznight.
  Nothing changes for you: this version updates itself as always and from now on asks the new page for updates. The old
  page stays and gets every new version too, so a game that hasn't updated in a while still finds its way.

## [0.13.4b]

### Changed
- **No more emoji anywhere in the game: every one is a drawn icon now.** After the calendar button, the rest of the
  game still used emoji as pictures: the trophy on "Statistics", the Ewige Tabelle's title and the match history's
  winners, the gamepad on "Mini games" and on challenges, the folder on "Award pack", the camera on "Save picture",
  the eye on the editor's "Preview", the dice on "Draw teams" and the team draw show, the sun on "Light mode", the
  magnifier, hourglass, pause sign and every warning sign. Emoji come from Windows' colour emoji font, so they looked
  pasted in from another app and ignored the skin's colours. Each one is now a small drawn icon in the game's flat
  style, taking the colour of the button or text it sits on (so they change with every skin and light mode, and
  dim with disabled buttons). (Thanks for the idea, Max!)

## [0.13.3b]

### Changed
- **The calendar button next to "Data as of" has a proper icon.** The button that opens the calendar in estimation
  questions showed a 📅 emoji, which looked like a sticker from another app: a different style and colours from every
  other button in the game. It now shows a drawn calendar (two rings on top, a row of days) in the same flat style as
  the bug and music buttons, coloured to fit every skin, and it's as tall as the date field beside it. (Thanks for the
  report, Max!)

## [0.13.2b]

### Added
- **A calendar for "Data as of"** in estimation questions. Typing the date was fiddly: the 📅 button next to the field now
  opens a month calendar, and picking a day writes it in for you ("1 November 2026", in German "1. November 2026"); today
  is marked, and if the field already holds a date the calendar opens on it. You can still type anything yourself, like
  "2024" or "March 2025". Thanks to Max for the idea (sent twice, so it clearly mattered 🙂).

### Changed
- **Unticked tick boxes are easy to see now** (Max found them ugly). An unticked box was a dark hollow that all but
  vanished on dark panels like the lobby's game rules, so "Play in teams" looked like plain text. It now has a light rim,
  the same rounded shape as a ticked one, in every skin.
- **Team mode is now one tick box: "Play in teams"** (off to start with). Picking a number of teams from Off/2/3/4/5/6
  meant working out yourself what fits your group. Now the game does it: about three players a team, even teams when it
  can, and it keeps adjusting while people join or leave (8 players → 2 teams of 4, 9 → 3 teams of 3, 10 → 3 teams of
  4/3/3). If you'd rather split differently, the lobby offers the other good options before the draw, e.g. "4 teams of 2"
  for 8 players; your pick stays until you untick it or it no longer works. Splits that would be unfair aren't offered at
  all: no team may have fewer than three quarters of another team's players. So 7 players play 4 against 3 but never
  3/2/2, and with 3 or 5 players there are no teams (the draw button says why). Thanks to Max for the idea.

### Fixed
- **Pong and billiard over the internet were laggy and out of sync** (reported by Max). Three causes, three fixes:
  - **Frames stuck behind downloads.** In the lobby the host sends everyone the quiz's pictures, sounds and videos in the
    background, and Steam sends everything to a player in one line, so a Pong frame could wait behind half a second of
    file data. Now Pong and billiard open for you once you have the quiz's files (the mini games window says so, and
    shows who's still downloading); Tetris and Reaction run on your own PC and work straight away.
  - **The ball jumped.** Your screen only showed where the host last said the ball was, guessing a little in between, and
    snapped back with every new frame. Now your screen plays the ball itself with the same rules, walls and paddles
    included, and each frame from the host is blended in over a few hundredths of a second instead of snapping. The
    other player's paddle glides instead of hopping, and the host sends twice as many frames (60 a second).
  - **"It went right through my paddle!"** Your paddle reaches the host a moment late, so the host could see a miss where
    your screen showed a hit. The host now also counts where your paddle was during the last quarter of a second.
  - Billiard balls now roll smoothly however unevenly the frames arrive: your screen plays the table back a tenth of a
    second behind the host, gliding between frames (nobody notices a tenth of a second in a game of turns).
- **The host's lobby scrolled again** (reported by Max). 0.13.1b put the new Team mode row under the jokers, which made
  the "Game rules for tonight" card taller than the window, so Start quiz ended up at the very bottom and the lobby had a
  scroll bar. Team mode now sits on the right, under the buzzer and podium sounds, where there was empty space anyway.
- **A full lobby scrolled too.** From 6 players on, the seat cards wrapped onto a second row and pushed Start below the
  window. With more than 6 seats they're now small cards on one line (picture, name, a download ring or ✓, and ×; the
  details are in the tooltips), the quiz's name moves up next to "Lobby" and "how to join" moves into the player count's
  tooltip. Even 24 players fit on a 21:9 screen now.
- "players" in the lobby's player count wasn't translated into German.

## [0.13.1b]

### Before we start
- Your mom is so fat that when she sat on the buzzer, it stayed locked for everyone.

### Added
- **A new podium song: the Throne Room from Star Wars.** Pick "Throne Room (Star Wars)" as the podium sound in the lobby (or as your own in Settings): the medal ceremony from the end of A New Hope plays while the podium rises and the credits roll. It's levelled to the same loudness as the F1 podium. (Thanks for the idea, maxlvl01!)
- **Team mode, like a pub quiz.** In the lobby's rules the host picks how many teams (2 to 6, or Off) and presses "🎲 Draw teams": everyone is put in a team at random, and every screen in the lobby plays the draw as a show. A reel spins through everyone's names, slows down and lands on one, who pops into their team, a bit faster each time, until "The teams are set!" with confetti. Click to skip it. "🎲 Draw again" draws new teams as often as you like until the start; someone who joins later goes into the smallest team. The teams are The Quizzly Bears, Smarty Pints, Know-It-Owls, Trivia Newton-John, Les Quizérables and The Brainy Bunch. During the quiz a team answers together: whoever locks in first gives the team's answer, and anyone in the team can change it with "✎ Change our answer" until time is up. Everyone sees who answered for them. On buzzer questions the first of a team to buzz answers for it, and teammates can't buzz in that round. Jokers count once per team, whoever plays them. The side column shows the teams with their points (their members' together) above everyone's own, and the podium, the credits and the award pack show the teams with their members; the night's awards still go to players. Team nights don't count for the Ewige Tabelle, so the solo standings stay fair. (Thanks for the idea, maxlvl01!)
- **The team draw in the Test lab**, to watch the show without a lobby: 8 players in 3 teams, 5 in 2, or 16 in 6.
- **Estimation questions can say more.** Two new fields in the editor: "Info for players" (e.g. "Hours played per game, from Steam") and "Data as of" (e.g. "1 November 2026"), so a quiz played months later doesn't count a number that has moved on since. Both show under the question for host and players. Quiz files are now format 16; older quizzes open as before. (Thanks for the idea, maxlvl01!)
- **Map packs in Settings.** A new "Map packs" card lists every built-in map with its size on your PC or its download size, with "⬇ Download" or "Delete" for each and "⬇ Download all". The maps still download by themselves when a quiz needs one; this is for getting them ahead of time (say before a night with a slow connection) or freeing the space. (Thanks for the idea, maxlvl01!)
- **Real spotlights (experimental).** Settings → Video → "Real spotlights with shadows" turns the home screen's two spotlights into real lights: the logo catches the light, and the buttons and cards throw shadows onto the background as the lamps sweep. Off by default; it may cost frame rate on older PCs. Godot has no ray tracing for 2D, so these are its 2D lights with shadow casters, which look the part. (Thanks for the idea, maxlvl01!)
- **New category: Pinke Geilheit**, for insiders and in-jokes. (Thanks for the idea, maxlvl01!)

### Changed
- **The spotlights shine on the logo now**, on the home screen and (new) on the title screen: two beams from the top corners that sway a little around the logo and end in a round pool of light on it, instead of sweeping across the whole top of the screen. The pool was always meant to be there but sat behind the logo's box, so you never saw it; it's on the logo now. With Settings → "Real spotlights" on, the real lights also only light the logo and the background behind it (not the buttons any more), and only the logo throws a shadow. (Thanks for the idea, maxlvl01!)
- **The lines over the podium are easier to read.** "THE TrackGlow PODIUM" and "… · FINAL RESULTS" were thin, small letters that looked scuffed once the podium was zoomed up (text doesn't get sharper when it's scaled); they're bigger and heavier now. (Thanks for reporting it, maxlvl01!)
- **Smaller updates** (issue #3: "the last two updates were slow"). Every update downloaded the whole game file again, 42 MB, mostly the music, which hardly ever changes; 0.13 also sent the whole engine again (31 MB) because a few of .NET's files had changed. Now the songs are a file of their own (music.pck) that only downloads again when the music changes, and every big engine or .NET file is its own small download, so an update fetches only what really changed. The first update after this one fetches the music file once. (Thanks for reporting it, maxlvl01!)

### Fixed
- **Pop-up pages like "What's new" can't get wider than they're meant to be any more.** Besides the long line in 0.13.1d2's fix, the page itself grew as wide as its widest line, so on 21:9 it reached past the game's 16:9 frame under the buttons at the top. It now stays at its width whatever is in it. (Thanks for reporting it, maxlvl01!)
- **"What's new" ran off the right edge of the screen** (on 16:9 and other screens not much wider). The line with the update's name ("0.13 — "Sauce Please": the podium as a show…") was written in one line without wrapping, and that line alone was wider than the window, so the whole page got as wide as it and every line was cut off on the right. It wraps now, and the page fits the window. (Thanks for reporting it, maxlvl01!)

### Removed
- Herobrine.

## [0.13.0b] — "Sauce Please": the podium as a show with credits and awards, a preview for every question, answer pictures and fun facts, a guided tour and Discord status

### Added
- **Pick several questions on the quiz page.** Ctrl+click picks a question (or drops it again), Shift+click picks everything between the last one and this one; a plain click still opens it. Dragging a picked question takes all the picked ones along, in their order, and the bar above the list moves them to the top or the end, or deletes them together. (Thanks for the idea, maxlvl01!)
- **Your own order for the categories.** "By category" on the quiz page now shows each category as its own group with ▲ ▼: put them in the order you want to play them (say General knowledge, then Games, then Video Game Music, an interactive part, and Formula 1 to end), then "Play in this order" sorts the questions that way, each category's questions staying in their order. The quiz remembers the order (quiz files are now format 15; older quizzes open as before). Categories you haven't moved come after, A–Z, and questions without one last. (Thanks for the idea, maxlvl01!)
- **An award pack for every night.** While the podium plays, the game draws the night's pictures in the background, like the Ewige Tabelle's award pack: the standings as a 1920 × 1080 poster with the winner, and a square card for every award with 2nd and 3rd. "🗂 Award pack" on the podium and in the match history's "Your podiums" opens the ZIP (in Documents/Quiznight/Pictures). A podium watched again that has none gets "Make award pack". (Thanks for the idea, maxlvl01!)
- **Podiums in the Test lab**, for trying the podium without a quiz night: a made-up night with 6, 2 or 16 players, or a tie at the top.
- **The podium is a show now, with credits.** When the quiz ends, the podium comes in big: 3rd place rises out of the floor, then 2nd, then the winner with confetti. After a moment it shrinks, and everyone else's places and tonight's awards come in below it. Then the credits roll, like at the end of a party game: the quiz, the quizmaster, everyone who played with their place, and the awards, ending on "Thanks for playing!". A click skips the podium's show, and Skip or Esc ends the credits. "▶ Credits" plays them again. (Thanks for the idea, maxlvl01!)
- **Tonight's awards.** The podium names the best of the night at each thing the stats count: most right answers, fastest buzzer, quickest answers, biggest haul on one question, map master, the closest pin (Bullseye), best guesser on estimations, best speller, mind reader (right at the first hint), time traveller (timeline items), the host's favourite (points from the host), and the most wrong buzzes (Trigger-happy). An award needs enough tries to mean something (an average needs at least two), only shows when someone won it, and players who are equal share it.
- **Watch a podium again.** Every podium you see is kept on your PC, as host or player. The match history has them under "Your podiums" with "▶ Watch again": the podium with its show and credits, as it was that night.
- **Save the podium as a picture.** "📷 Save picture" on the podium saves the screen without its buttons in Documents/Quiznight/Pictures and opens the folder.
- **Discord shows what you're doing.** While Discord is open on your PC, your friends see "Playing Quiznight?" with the game's logo and what you're up to: in the menus, making a quiz, looking at the stats, or at a quiz night (hosting or playing: in the lobby with how many players, "Question 3 of 20", at the podium), with how long you've been at it. The room code is never shown. If Discord isn't running nothing happens, and Settings → "Show what I'm doing on Discord" turns it off. (Thanks for the idea, maxlvl01!)
- **The answer as a picture.** Every question can now have its answer as a big picture, e.g. for "Which game is this Steam review about?" the game's store page. In the editor it's the first field of the "At the reveal" part under the question (which also has the fun fact, source and picture from 0.13.0d1). At the reveal everyone sees it big under the answer, host and players. Like the other pictures it comes to players with the quiz's files from the lobby on, locked until that reveal, so nobody can peek. Quiz files are now format 14; older quizzes open as before. (Thanks for the idea, maxlvl01!)
- **A guided tour through the game.** The first time the game starts, a tour walks you through it in one go: joining a quiz night, the bug button, your quizzes, a sample quiz (only for the tour, never saved) with its question types, list, sorting and why categories matter, then one of its questions in the editor (blanks, round settings, category, the reveal part with answer picture, fun fact and source, Preview, saving, shortcuts), and back home: "Have fun creating and quizzing!". The screen goes dark except for the thing each step is about, and the tour opens each screen by itself. Esc or Skip ends it any time. It replaces the two short tours of the quiz page and the editor; Settings → "▶ Take the tour" starts it again. (Thanks for the idea, maxlvl01!)
- **Preview a question while you make it.** The question editor has a "👁 Preview" button next to "Save question". It opens the question exactly as players see it in a match: the real player screen, with its timer, pictures, map, buzzer or answer buttons, and you can play it yourself. The top bar has what you'd do as host: reveal the answer, play or pause the sound or video, show the next hint or picture, mark your own buzz right or wrong, and start again. It shows the question as it is in the editor right now, saved or not. Behind it a tiny game runs on your PC with you as the only player, connected inside the game (no network, no lobby), so nothing is saved, sent or counted in the stats. Escape or Close goes back to the editor. (Thanks for the idea, maxlvl01!)
- **A real resolution setting for fullscreen.** In Settings → Video → Fullscreen, "Picture shape" is now "Resolution": every resolution your monitor supports (e.g. 1920 × 1080 or 2560 × 1080 on a 3440 × 1440 screen), like in other games. Until now fullscreen always ran at the monitor's own resolution and only let you pick a shape with black bars, because the game can't draw itself at fewer pixels on its own. Now the monitor itself switches to the picked resolution while the game is in fullscreen, and back to your desktop's when you leave fullscreen, Alt+Tab out (the game goes to the taskbar and switches again when you come back) or close the game. Borderless keeps "Picture shape", as a borderless window always covers the desktop as it is. (Thanks for the idea, maxlvl01!)
- **Fun facts, sources and a picture after the answer.** Every question in the editor now has "+ Fun fact, source or picture for after the answer" under the form. Open it to write a fun fact or a short story, add where it's from (a link, which players can click to open in their browser, or a few words), and a picture: say the game's whole Steam store page for a "which game do these related games point to?" question, or the album cover after a song. Nothing of it shows while the question runs. At the reveal, everyone (host and players) gets a "Good to know" card with it under the answer. The picture comes to players with the quiz's other files from the lobby on, locked until that reveal, so nobody can peek. (Thanks for the ideas, maxlvl01!)
- **The bonus question has an answer field.** Under "What for?" there's now "Its answer (only you see it)". Until now you could set a bonus like "Also name the developer" but had nowhere to write down the answer, so playing a quiz weeks later meant guessing your own bonus. The host's screen shows it next to the bonus line, also at the reveal where you hand out the bonus; players never get it. (Thanks for the idea, maxlvl01!)
- **New Interactive kind: "Type as many as you can".** In the Interactive question's "How it's played" pick the new third option. You give a task ("Name all F1 drivers with at least one world title") and a time (30 seconds to start with). Players type one answer at a time (Enter adds it, × takes one out) until the time is up or they press "I'm done". At the reveal, the host sees every player's answers one by one and clicks each to tick or untick it: every ticked answer is worth the question's points ("Points per right answer"), and Double points doubles them. If you write the right answers into the editor (one per line, optional), matching answers are ticked by themselves, small typos included, so you only fix the odd one. No microphone needed. Players on an older version are told in the lobby that they can't play it. (Thanks for the idea, maxlvl01!)
- **+5 s for the host.** While a question with a time limit runs (or a timeline turn), the host has a "+5 s" button: everyone's clock gets five more seconds, e.g. when they need a moment longer to decide. Click it as often as needed. The clocks on every screen jump up straight away. (Thanks for the idea, maxlvl01!)
- **New categories:** Video game music, Film music, Books, Football (the European one), Formula 1, Race tracks, and "Günther Grass". (Thanks for the ideas, maxlvl01!)
- **The quiz overview shows each question's category** under its name, next to the type. A question without one gets a quiet yellow "no category yet", since the stats per category need one. The editor's field is now just called "Category". (Thanks for the idea, maxlvl01!)
- **Host attributes in the profile.** A new "Hosting" group says how hard someone's questions are when they host, rated 1–20 against everyone else who hosted: **Tough questions** (how seldom players get them right), **Brain teasers** (how long players think about them), **Stumpers** (how many nobody got at all). Hover one to see the numbers behind it. Together they're the 11th spot on the profile's pitch, "Hosting", which was an empty gap until now. (Thanks for the idea, maxlvl01!)
- **Hosted nights in the profile's form.** The "Form" strip with the last five nights now also shows the nights someone hosted, as a gold box with the host's microphone, instead of leaving them out. (Thanks for the idea, maxlvl01!)

### Changed
- **The last old-looking parts got the new look.** Everything you click now looks like the buttons: an ink outline and a darker side under the face, like a real key. (Thanks, maxlvl01!)
  - **Scroll bars:** a small key you take hold of, in an inked groove, instead of a thin grey line.
  - **The update arrow** next to the version on the home screen sits on a small key, which turns teal when an update is out.
  - **Slider knobs** (volumes, the music player's song bar) are round keys instead of a plain white dot.
  - **Tick boxes** are keys too; Settings' microphone test is a switch in the same style.
  - **Progress bars** have the ink outline: the update download, the microphone's level, a picture reveal.
- **The podium plays the way it was meant to.** The podium comes in big in the middle with everyone's score list under it. Then both move into the right third of the screen, and the credits roll on the left two thirds. When they end, tonight's awards stay there, with the buttons under them. The sponsor now comes at the end of the credits ("Sponsored by", just before "Thanks for playing!"), like after a film. A click skips to the credits, Esc or Skip skips the credits. (Thanks, maxlvl01!)
- **The question editor's "At the reveal" is one short line now.** It says what's filled in (the answer as a picture, a fun fact, the source, a picture), and "+ Add" / "✎ Edit" opens the fields in a pop-up. The whole part used to sit in the form, with two big picture previews, so the form scrolled and the guided tour's steps 14–16 showed the wrong part of the screen. The tour also scrolls a part taller than the window to its top now (Round settings showed its empty bottom), and waits for a screen it just opened. (Thanks for the report, maxlvl01!)
- **Sorting the quiz's questions is one drop-down, with a new "By category".** Above the question list, "Show" had three buttons side by side (Play order, By type, Order made) that took the whole row. It's now one drop-down, and "By category" sorts the questions A–Z by their category, with the ones that still have none at the end. As before, "Play in this order" makes the shown order the play order. (Thanks for the idea, maxlvl01!)
- **In the Ewige Tabelle only the numbers scroll.** Since 0.13.0d1 the table scrolls sideways when it has more columns than fit, but the place and the names scrolled away with them, so further right you no longer knew whose row you were reading. Now the place and name stay where they are and only the numbers beside them scroll. (Thanks for the idea, maxlvl01!)
- **+ and − next to every points field.** Every question type's points (and the order questions' "points per item") now has the − and + buttons the bonus points already had, instead of a big field you had to click into and type. (Thanks for the idea, maxlvl01!)
- **Buzz times count from when the buzzers open, every time.** When the host locked the buzzers and unlocked them again, or let everyone buzz again after wrong answers, a buzz was still timed from when the question first appeared: buzzing 0.5 s after the second unlock counted as 20 s. That ruined the "Ø buzz time" in the stats, which also decides between players with the same points. Now the clock starts again with every unlock, both in the buzz order and in the stats. (Thanks for the idea, maxlvl01!)
- **The Ewige Tabelle opens sorted by quiz points.** "Ranked by" starts at Quiz points instead of F1 championship points; the championship tables are still one pick away. (Thanks for the idea, maxlvl01!)
- **One switch to test your microphone.** Settings had "Check the level" and "Hear yourself" as two switches for one test. Now "Test my microphone" does both: the level bar and hearing yourself, a moment late. Wear headphones while testing. (Thanks for the idea, maxlvl01!)
- **Button clicks are quieter** (half as loud as the other sound effects), so nobody has to turn down all the sound effects, and with them the buzzer, just because of the clicks. (Thanks for the idea, maxlvl01!)
- **The 🐞 and jukebox buttons sit in the same place on every screen shape.** 0.12.7b gave them a free spot (the top bar in a game and in the editor, the bottom right on the quiz overview, the Settings bar) on 16:9 screens, but on 21:9 and 32:9 monitors they stayed in the bottom left corner beside the picture. Now they use the screen's spot on ultrawide monitors too, so they never jump around. (Thanks for the idea, maxlvl01!)
- **The space skin is lighter on your PC.** It drew its whole scene 60 times a second, every one of its ~260 stars as a full circle, and on wide monitors blurred the whole background picture 60 times a second even while nothing moved. Now the scene is drawn 30 times a second (it moves slowly, so it looks the same), tiny stars are drawn as dots, the background is only redrawn when the mouse focus moves, and the animated background's blur takes half the work. (Thanks for the idea, maxlvl01!)
- **Bug reports (🐞) now reach Max as GitHub Issues**: before, each one landed as a loose file in a folder he had to sort
  through by hand, so it was easy to lose track of what was fixed. Now every report opens its own issue (titled with what
  it's about and your version, your text, the screenshot shown in it and the log folded away), which he can label and close
  once the fix is out. Nothing changes for you: same window, same button. If the issue can't be made, the report still
  goes in the old way, and if you're offline it still waits and goes on the next start. Ideas (💡) go in as before.

### Fixed
- **Players who left disappeared from the podium.** If someone closed the game or left on the podium, they vanished from it for everyone, which ruined screenshots. Leaving the room took their seat away (that's how the lobby works), and the podium was drawn again from the room as it was then. Now leaving on the podium keeps the seat, like during the game, and every screen takes the final standings once, when the quiz ends, so nobody drops off it any more. (Thanks for the report, maxlvl01!)
- **Interactive and step-by-step questions had no category field.** In the other types the category comes with the bonus field in the round settings, and these two have no bonus, so the field was missing too. As every question needs a category for the stats, they all showed up as "no category yet" in the quiz overview, with no way to fix it. Both now have the category in their round settings. (Thanks for the report, maxlvl01!)
- **"+ Fun fact, source or picture for after the answer" sat at the very bottom of the editor.** It came under both columns, below the round settings, so on most screens you had to scroll to find it while the space under the question's own fields stayed empty. It now sits right under the question's fields, and opens there too. (Thanks for the report, maxlvl01!)
- **Long names pushed the whole game screen off to the side.** On the host's screen, "Somebody-with-a-long-name is answering" at the bottom left, and on players' screens the "Tonight so far" table on the Get ready screen, could be wider than the window. The screen then grew past the window's right edge: everything shifted right and the boxes on the right were cut off (especially in German, where the table's headings are longer). The cause: a wide part made the whole column it sits in wider, and the window couldn't fit it. Now names that would push the layout end in "…" (the full name is in the tooltip), and the table scrolls sideways when it doesn't fit. (Thanks for the reports, maxlvl01 and Grownz!)
- **Names were cut off in the host's score list.** The host's scores have − and + keys beside each name, so in the same narrow column as the players' list, names like "Sxy_Grownz" became "Sxy_Gr…". The host's side column is now wider. (Thanks for the report, maxlvl01!)
- **The Ewige Tabelle ran off the right side of the window.** With many columns (e.g. a question kind's stats) the table was wider than the page, so the page grew past the window: the last columns and the "Match history" tab at the top right were cut off. The table now scrolls sideways instead. (Thanks for the report, maxlvl01!)
- **The map pictures in the map pin editor were cut off.** A map's line "12 km wide · ⬇ downloads when picked" (new in 0.12.8b) was longer than its card. As a button doesn't grow with what's in it, the text and the picture above it ran out of the card on the right. The line now wraps onto a second one. (Thanks for the report, maxlvl01!)
- **Deleting a map pin question kept its map.** Its map stayed in the quiz, and an uploaded map stayed in the editor's map list for good. Now the map goes with the question if no other question uses it, and if it's a downloaded built-in map nothing else of yours uses, the delete question offers "Also delete its map from this PC". (Thanks for the report, maxlvl01!)
- **Players could leave the lobby by accident.** A right-click on empty space goes back a screen, and in the lobby that meant leaving the room straight away, also when it happened while playing a mini game. Now leaving the lobby asks first, a right-click inside the mini games does nothing, and Escape (or the mouse's back button) closes the mini games window first. (Thanks for the report, maxlvl01!)
- **The game didn't remember which monitor it was on.** It opened on the monitor picked in Settings, so moving the window to the other monitor (dragging it, or Win+Shift+arrow) was forgotten on the next start. Now the monitor the game is on is saved whenever it changes. (Thanks for the report, BassGaming!)
- **The 🐞 icon kept the wrong colour after changing the skin.** It took the skin's text colour once when it was made, at the start: white in a dark skin, so after switching to Daylight it was white on white, and the other way round dark on dark. It now takes the current skin's colour every time it's drawn. (Thanks for the report, maxlvl01!)
- **The background looked wrong on 32:9 screens.** Beside the screen, each side of the "Pinke Geilheit Spacing Team" picture shows its own part: the astronaut on the left, the ship on the right. On a 32:9 monitor the sides are about three times as wide as on 21:9, so the right side showed so much of the picture that the astronaut appeared on both sides. The right side now never shows more than the ship's corner and zooms in further on wide screens. (Thanks for the report, maxlvl01!)
- **The mini games' high scores only showed the leader's time.** Everyone else had only their gap to the leader ("+0.120") and to the one ahead, not their own time. Each line now shows the player's own time or score, with the gaps beside it. (Thanks for the report, maxlvl01!)

## [0.12.8b]

### Added
- **Multiple choice can score per right answer.** Until now a multiple choice question gave its points only to players who picked exactly the right answers: two right out of three got nothing, the same as picking all wrong ones. The question's settings now have **Points per right answer** (on for new multiple choice questions): every right pick scores the points, and every wrong pick takes as many off again, so ticking every answer doesn't pay. A player never goes below 0 unless "Wrong answers cost points" is on too. The points field then reads "Points per right answer" and has + and − buttons like the bonus points. Players see how it scores before they lock in. Questions made before keep scoring only the exact set (quiz file format 12; older games read such a question the old way). (Thanks for the idea!)
- **Hear yourself while testing the microphone.** Settings' microphone test has a new **Hear yourself** switch next to the level bar: it plays your microphone back live, a moment late and as loud as Microphone volume makes your recordings, so you can tell how you'll sound in an Interactive question. Until now the only way was the Test lab in developer mode: record, stop, play it back. Wear headphones, or your speakers feed back into the microphone (the hint under the bar says so). (Thanks for the idea!)

### Changed
- **The built-in maps are a download now, not part of the game.** All 20 game maps (Skyrim, GTA, Zelda, PUBG, WoW Forever …) used to come with every install: about 46 MB that everyone downloaded, even people who never make a map question. Now the game downloads a map the first time it's needed: when you pick it in the editor (the map list says "⬇ downloads when picked"), open a quiz that uses it ("Download now" on the quiz page), or open the lobby for it. Each map is downloaded once and kept. Players don't download anything themselves: the host sends the maps a quiz uses along with its pictures, sounds and videos, the lobby's Start waits for them like for the rest, and they're thrown away after the night. Deleting a quiz asks whether to **also delete its maps** from your PC (only maps no other quiz of yours uses). When this update installs, the maps your saved quizzes use are kept and the rest are deleted. The maps come from the releases page, signed with the same key as updates, so nobody can slip in another file. (Thanks for the idea!)
- **The Beta table has Beta Quiz #2** (28 October, three players).

### Fixed
- **Players sometimes missed "The host closed the room" on LAN.** When the host closed the room while players were still downloading the quiz's files, the message was queued behind megabytes of video and thrown away with the connection, so players only saw "Lost the connection" and kept trying to reconnect for a minute. Now the files' unsent pieces are dropped and the closing message goes out first.

## [0.12.7b]

### Added
- **Fullscreen in another picture shape.** In Settings, Fullscreen and Borderless now have **Picture shape**: the whole screen as before, or 16:9, 16:10, 4:3, 21:9 or 32:9 with black bars around it. Handy on an ultrawide to see the game exactly as friends on a normal 16:9 screen do. It's still drawn as sharp as your monitor. Until now the size picker was simply greyed out in fullscreen ("Fullscreen always uses your monitor's resolution"), because Godot can't switch the monitor itself to another resolution. The shape is saved separately from the window size, so switching between windowed and fullscreen keeps both. (Thanks for the idea!)

### Fixed
- **The 🐞 and jukebox buttons covered other buttons on 16:9 screens.** On the quiz overview they sat on top of "Copy share code", in the question editor on the form's buttons (e.g. "+ Add item", "Replace…"), in Settings on the edge of the button bar, and in a game on the question card. They always sat in the bottom left corner, which on a wide monitor is empty space beside the screen, but on a 16:9 screen (or a 16:10 or 4:3 one) is part of the screen itself. Now screens with something in that corner give them a free spot: the top bar in a game and in the question editor, the bottom right on the quiz overview, and next to "Reset to defaults" in Settings. The jukebox's list opens downwards from a top bar. On a monitor wider than 16:9 nothing changes. The first-quiz tutorial no longer says the bug button is "in the bottom left". (Thanks for the report!)
- **"Put in order" with "Everyone at once" can now have a year or other unit on each item.** The box next to each item (e.g. "1993", "120 cm") only existed for the timeline mode, so a quiz maker couldn't add years to a list that everyone sorts at the same time. The editor only drew the box when "Take turns on a timeline" was picked, and the quiz file and the answer text ignored the labels in the other mode. The box is now there in both modes (optional, and not shown to players while they sort), is saved in the quiz file, and the answer shows "Doom (1993) → Half-Life (1998) → …". (Thanks for the report!)

### Coming next
- **A short break until Sunday.** Max is close to the weekly limit, so until Sunday nobody works on bug fixes or new ideas. Keep sending them with the 🐞 button: they're all read after that.
- **Picking a lower resolution in fullscreen** (drawn with fewer pixels and stretched to fill the screen, like in other games): next week.

## [0.12.6b]

### Added
- **More categories to pick from in the editor.** "Science" was too broad: a question about frogs, one about black holes and one about acids all landed in the same category, so the stats per category (and the "Science champion" award) couldn't tell a biology whiz from a space nerd. The category list now also has **Biology**, **Chemistry**, **Physics**, **Space & astronomy** and **Human body & medicine**, right under Science, plus **Maths**. "Science" stays for questions that fit none of them, and questions that already have it keep it. (Thanks for the idea!)

## [0.12.5b]

### Changed
- **Bug reports and ideas land in Max's to-do folders.** Until now each 🐞 report went into a folder named after the screen it came from, and ideas into one per topic, so there was no way to see what's being worked on. Now bugs arrive in "Reported" and ideas in "Requested", and Max moves them on through "WIP" (in a test build), "Pending" (waiting for the public release) and "Resolved". Where the report came from (or what the idea is about) is now in its file name. Reports that couldn't be sent from an older version (no internet) go to the new folders too when they're sent on the next start.

## [0.12.4b]

### Added
- **A quiz night survives a crash or an update.** The host's game now saves the night after every question: everyone's points, their stats, the jokers they've used, who joined late, the lobby's rules and when the night started. If the game crashes, or something is so broken that you have to update in the middle of the night, nothing is lost:
  - The quiz's page shows **Unfinished quiz night** with when it was saved, how far it got ("after question 6 of 15"), who's leading, and a **Continue** button. "My quizzes" has a **▶ Continue at question 7** button on that quiz too.
  - Continue opens a new lobby (with a new code) that already has everyone's seat and points. It says "A saved quiz night goes on at question 7 of 15", and each seat shows the player's points and "Not back yet" until they join again. Over Steam they get their seat back with the same Steam account; on LAN with the same name. Players see "The host continues a saved quiz night…" in the waiting room.
  - **Continue quiz** goes on at the question after the last one that was finished. Whoever isn't back keeps their seat and points and can still join later; new players can join too and play from there. Jokers already used stay used, and the night's results at the end count the whole night, from its first question.
  - The save holds everything up to the last finished question: during a reveal that's the question being revealed (with its bonuses), while a question runs it's the night as it was before that question, so that question starts again. Nothing is saved before the first question is over.
  - **Continue later** under "End quiz early" closes the room on purpose (to update the game, say), after telling you what's kept. The save is removed when the night ends (the podium or "End quiz early"), and **Discard** throws it away by hand.

### Changed
- **The timeline cards got the chunky look** of the rest of the game. On "When did these games come out?" the cards are keys with a dark outline and an edge underneath, like the buttons, instead of flat boxes with a thin line. The item to place is an orange key, the year sits on a small orange chip under each name (the starting card is tinted green with a green chip), a gap someone tried and got wrong is a round red ✗ key instead of a loose ✗, and the locked-in gap a round teal ▼ key. A player's own guess in the timeline is a teal key, the item in the tray above it an orange one.

## [0.12.3b]

### Added
- **Fair buzzers, whatever your ping.** Until now, whoever's buzz *reached the host* first answered first, so a player with a slow connection lost close races even when they pressed first. Now each player's own game measures their reaction time, from the moment the buzzers opened on *their* screen to their press, and sends it with the buzz. After the first buzz the host waits a short moment (0.3 s) for buzzes still on their way, then the fastest reaction answers; later buzzes join the line by their time too. A time can never be slower than what the host saw, and at most 0.5 s faster (more than any ping), so a faked time can't win by more than that. A Fast Pass still goes first. Players on older versions are timed when their buzz arrives, as before.
- **Ctrl+S saves without closing the question.** In the question editor Ctrl+S saves and you stay on the question; a green "✓ Saved" shows next to the button for a moment. "Save question" still saves and goes back to the quiz.
- **A desktop shortcut on the first start.** The very first time the game starts on a PC (the downloaded game, not editor runs), it puts a "Quiznight" shortcut on the desktop, unless there already is one. It only happens once, so if you delete it, it stays deleted.
- **Tutorials for making your first quiz.** The first time you open a new quiz, a short tour shows how a quiz is made: the screen goes dark except for one thing at a time, which glows, and a bubble next to it explains it (the title, the question types, the list of questions, hosting and sharing, the shortcuts). The first time you open a question, a second tour goes through the editor: filling in the blanks, the round settings, **bonus points**, **categories** (and "Keep it secret"), saving, and the shortcuts (Ctrl+Z/Y, Ctrl+S, Ctrl+V for pictures, dropping files onto the window). The first tour ends with how to report a bug or give feedback with the 🐞 button. Next ›, ‹ Back, Skip and the arrow keys/Enter/Esc work in every step.
- **Turn tutorials off, or see them again.** "Turn off tutorials" in any bubble stops them all. Settings has **Show tutorials** (on/off) and **↺ Show tutorials again**, which brings back every tour you've already seen.
- **Max gets a push on his phone and PC for every bug report and idea**, so he sees them straight away instead of when he next looks in the reports folder. Reports that couldn't go at first (no internet) push when they're sent on the next start, marked "sent late". Tapping the push opens the report.
- **Reports and feedback say who sent them**: your Steam name and Steam ID (with a link to your profile), so Max can ask you about a bug and credit your idea in the changelog when it makes it into the game. Without Steam the report has your in-game name. The 🐞 window and Settings → What the game sends say so.

### Changed
- **Answer time per buzz starts at 15 s** (was 10 s) in new lobbies.
- **The reaction test counts the average of your five tries**, not the best one: one lucky press no longer makes a record. A false start no longer uses up a try; that try is just done again. The high scores list averages from now on (the best single tries from before don't compete with them).

### Fixed
- **The tutorials setting looked broken.** In Settings, the "Show tutorials" tick box had no text and "↺ Show tutorials again" was a huge, tall key. Settings lets its tick boxes wrap onto two lines, and this one sat in a row next to the button with no room of its own, so it shrank to nothing, its text wrapped one letter per line out of sight, and the row stretched the button with it. The tick box now has its own line, with the button under it.
- **Pong was missing from the mini games menu.** It was only a small button next to each player's name in the 1 vs 1 list, so with nobody else (on 0.12 or later) in the lobby it didn't show at all, and next to Billiard it was easy to miss. 1 vs 1 now starts with a **Pong / Billiard** choice that's always there (Billiard then shows which billiard game), and each player has one "Challenge to …" button.

## [0.12.2b]

### Added
- **Give feedback, not just bugs.** The 🐞 button now first asks what you'd like to send: **Report a bug** (as before) or **Give feedback**. Feedback is for ideas: pick what it's about (a map, a question type, quality of life, something else), write it, press Send, and it goes to Max sorted by that. No log is added to feedback, and the screenshot is off unless you tick it. "‹ Back" goes back to the choice.
- **Leave developer mode:** double-click the teal DEV badge on the home screen. LAN play and the developer buttons are hidden again until the code is typed again (or, in a test build, until the game restarts).

### Fixed
- **"Order made" changed after "Play in this order".** Questions made before 0.12 don't know when they were made, so "Order made" showed them in their play order. After "Play in this order" changed that order, "Order made" changed with it and the original order was lost. Now such questions get a made time from where they are the first time 0.12.2b opens the quiz, so the order stays put. A quiz whose order was already changed keeps its current order as its "made" order.

## [0.12.1b]

### Fixed
- **Bug reports came with an empty log.** The report reads the end of the game's log file while the game runs, but the released game only wrote its log to the file when an error happened, so the file was still empty when 🐞 read it (test builds always wrote it straight away, which is why it looked fine there). The game now writes every log line to the file at once, so reports carry the last 200 lines.

## [0.12.0b] — "Mission Control": a space skin, lobby mini games, bug reports from every screen, the WoW Forever map and a reveal the host controls

### Added
- **Space skin** (Settings → Skin). Behind the home screen: twinkling stars, a black hole with a glowing disk turning around it, planets (a desert planet with two suns, an ice planet, a gas giant with a cloud city glinting in it, a forest moon, a lava planet, a ringed one), and now and then a visitor: X-wings chased by TIE fighters, a freighter, a shuttle, a Star Destroyer far off, Wheatley and the Space Core tumbling past ("Space!"). Rarely a Death Star turns up, and once in a while it blows up a planet (it's back later). Pressing Start on the title screen launches a rocket. Only on the home screen, so nothing moves while you play; with Background movement off it all stands still.
- **Animated background:** "Pinke Geilheit Spacing Team (animated)" in Settings → Background picture. It stops on its frame when Background movement is off.
- **Your own background picture:** "My own picture…" next to the background list takes a picture from your PC. The game keeps its own copy, so it stays even if you move or delete the file.
- **Microphone volume** in Settings, with a level meter ("Check the level"): 50 % is as your microphone gives it, more makes quiet recordings louder. The bar turns red when it's too loud and would crackle. The microphone is only on while the meter is switched on.
- **Sort the quiz's question list:** "Play order", "By type" or "Order made" above the list. It only changes how the list is shown; "Play in this order" makes the shown order the play order (Ctrl+Z takes it back). Questions made from now on remember when they were made; older ones come first in "Order made".
- **Test lab:** three drop-downs to try (a short, a medium and a long scrolling one), and "Up close" to look at the buzzer big, with a size slider.
- **Report a bug, from every screen.** The new 🐞 button in the bottom left (above the jukebox's) opens a small window: write what happened, press Send. The game knows where you were and files it there by itself ("Editor › Map pin", "Game (player) › Buzzer question", "Mini games › 8-ball" …), adds a screenshot of the screen as it was, its version and the end of its log (your Windows user name left out), and sends it to Max. Without internet it goes the next time you start the game. Nothing is ever sent unless you press Send.
- **Reaction test in the mini games.** Wait for the buzzer to light up, then buzz as fast as you can: five tries, your best counts, buzzing too early is a false start. It's timed on your own PC, so lag doesn't matter.
- **High scores like F1 timing,** for Reaction and Tetris: tonight's and of all time, the leader with their time or score and everyone else with the gap to the leader and to the one ahead; purple for the best of all time, green for a personal best. The lobby's bests go with the night's results, so the all-time lists grow from night to night.
- **Pick your microphone in Settings** (Audio → Microphone), in case the game records with the wrong one for Interactive questions.
- **Videos at most 720p.** The editor shows when a video is bigger ("This video is 1920×1080") and shrinks it with one button, and AnyConverter and the ffmpeg command now make videos 720p at most. Smaller videos are quicker to send to everyone before the night, and still sharp.
- **Mini games in the lobby.** While you wait for the quiz to start, "🎮 Mini games" (under the seats, in the lobby and the waiting room) opens a window with **Tetris** to play alone, and **Pong** and **Billiard** (five games: quick pool, 8-ball, 9-ball, 4-ball and three-cushion, with their real rules: solids and stripes, the lowest ball first, fouls, carom without pockets) to play 1 vs 1: challenge anyone in the lobby, or the host; they get a "… challenges you" note to accept or turn down. The host's game runs the matches, like the quiz, so Pong stays fair over Steam. The lobby keeps score of who beat whom how often and who's on a winning streak ("Ann 3 – 1 Ben · Pong · Ann ×2 in a row"), and everyone's best Tetris score. When the host starts the quiz, every mini game ends at once and a match that was cut off counts for nobody. Players on versions before 0.12 don't get the mini games.
- **8-ball and 9-ball start with the lag**, like in a real pool hall: both players shoot a ball at the same time off the far cushion and back, and whoever's ball stops closest to their own end breaks (a ball that doesn't reach the far cushion loses the lag).
- **The jukebox can shuffle and repeat.** New buttons in the jukebox and the home screen's music player: shuffle (on: random order; off: the songs by title) and repeat (the same song again and again); both are remembered. **Back now starts the song over**, and pressing it twice quickly goes to the previous song, like a car radio. A **progress bar** shows how far into the song it is, the time played and the time left; click or drag on it to jump.
- **Test lab** (developer mode and test builds only): a button on the home screen with quick checks — buzzers with a reaction test, every song and sound side by side, the microphone, each question type's editor, a LAN game with bots (also one that pretends to be on an old version, for the lobby's version check), the map switches, and the screens that only show once (welcome, intro, What's new, the yellow dots). Bots accept mini game challenges and play along.
- **The TrackGlow Podium.** The final results are now proudly presented by our sponsor, like everything in American sports. And the buzzer's silver base has "TRACKGLOW" engraved in it.
- **Gilneas and Riverglades have names on the WoW Forever map.** Hovering them used to say "Eastern Kingdoms" (or a neighbour like Silverpine Forest), as the game only knew Classic's zones; now they're named on hover and you can jump to them. Their outlines are rough for now, until WoW Forever's own zone data is out.
- **Choose how much intro you want.** The very first start (the welcome screen) asks what to show next time: everything, everything except the joke parts (the anti-cheat window, the World Premiere video, the publisher and the sponsor), or nothing at all, straight to the title screen. You can change it any time in Settings → "Intro when the game starts".
- **Yellow dots on what's new.** After an update, new things you might be looking for get a small yellow dot (this time the intro option in Settings, and the Settings button that leads there). "Mark all as read" under the version on the home screen takes them all away. Only things that came after the version you had before show up.
- **The lobby checks everyone's game version.** Joining now tells the host which Quiznight version you have. A player on an older version gets an orange "⚠ 0.11.4b" under their name, and if the quiz uses something their version doesn't have (say an Interactive question or a newer map), the lobby says which question, and Start asks before going ahead. Before, an old game just joined and things went wrong in the middle of the night. Games too different to talk to each other at all can't join and are told to update.
- **Changed maps have to be checked again.** When an update changes a built-in map's picture or scale, every map pin question on it gets a "⚠ map changed" mark in the quiz list and the quiz overview, and the quiz can't be hosted until each one is checked: open the question, make sure the correct spot is still right (move it if not) and tick "Checked". Setting or moving the spot counts as checked too.

### Changed
- **The host reveals the answer, not the game.** On every question that isn't a buzzer question (A B C D, multiple choice, estimation, map pin, put in order, everyone types, Interactive voting), the answer used to pop up the moment the last player locked in, or when the time ran out, so the host never got to build any suspense. Now answering just closes: the timer goes away, players see "Everyone's in! The host reveals the answer in a moment…" under their own answer (or "Time's up!" if they didn't make it), and the host gets a big "Reveal answer (Space)" button with "The answers are in. Reveal it when you're ready." Nothing is scored until the host presses it. The host can still reveal early while people are answering, like before. Buzzer questions and the timeline already waited for the host and stay as they were.
- **Pop-ups have a proper close button.** The × in the top right of pop-ups (like the question type explanations) was Godot's own thin grey cross, sitting a bit off in the title bar. It's now a small red key like the game's other buttons, with a thick white ×, in the middle of the title bar's height.
- **The space skin really moves now.** It was animated before, but so slowly it looked like a picture: the planets drifted 4 pixels in a minute and a half. Now the stars drift past (the bright, nearer ones faster), a shooting star streaks by every few seconds, and every planet wanders round its spot and turns: its craters, forests, ice fields and lava lakes slide across its face and round the back. The gas giant's cloud bands flow (next ones the other way), with a storm and the cloud city going round with them, and the forest moon now orbits the gas giant, passing in front of it and behind it. Glints of ice go round the ringed planet's ring. Clumps of hot gas race round the black hole and specks of dust spiral in until it swallows them. The Death Star glides in from the side instead of fading in. With Background movement off it all still stands still.
- **The TRACKGLOW engraving on the buzzer looks cut into the metal again.** Since 0.12.0d4 it looked like three copies of the word on top of each other, most of all on the big "Up close" buzzer: the dark edge above and the lit edge below were drawn a fixed few pixels away from the letters, and the letters' strokes are thinner than that. The edges are now a sliver of the letters' size, so you see one word with a fine lit edge underneath.
- **"Mark all as read" is up in the top row** next to Settings, instead of a line under the page.
- **Drop-down lists roll out from under their button quickly, and roll back in when they close.**
- **The TRACKGLOW engraving on the buzzer is easier to see,** and it's on every buzzer now, also the small ones.
- **Test lab:** the buzzer is free to press (it plays your buzzer sound); the reaction test is its own button.
- **Every song and sound is equally loud.** Before, the menu songs were up to 6 dB apart (some sounded half as loud as others) and the sound effects up to 22 dB (one podium jingle was barely audible, the other blasting). All of them are now levelled to the same loudness, songs with songs and sound effects with sound effects, and the welcome voice line sits just above the music. Overall the music is a little quieter than the loudest songs were; turn up Music in Settings if you liked it louder.
- **Image reveal is now part of Picture questions.** A picture question is shown either as a picture or slideshow, or as a **reveal** that starts zoomed in or pixelated and gets clearer. Your image reveal questions are still there and play as before (also with friends on older versions); in the statistics they count under Picture → Reveal.
- **Drop-down lists hang right under their button.** Long lists (categories, units) used to open wherever they fit, even over the button and up the whole screen. Now they always open under the button (above only if there's no room), show up to ten rows and scroll for the rest, and unfold more softly.
- **Buttons don't grow under the mouse or shrink when you click them any more.** They already look pressed in when you click, and that's enough.
- **The game says what it is:** "Quiz nights with your friends — everyone on their own PC." (it used to mention the couch and streaming, which isn't how it's played).
- **The WoW map is now called "WoW Forever"** in the map picker (quizzes made with "WoW Classic – Azeroth" show the new name by themselves; nothing else about them changes).
- **Zooming into the WoW map stays sharp and looks the same all the way down.** Before, between the world map and the zones (around zoom 1.5–4) the map showed the small old world and continent pictures blown up: blurry, other colours, and the land suddenly looked like old Classic again until the zone art came in. Now all of OpenMap's zone art is put together into one big map (in two sizes) that shows from the first zoom steps on, so the land looks the same at every zoom, only more detailed the closer you get. (The sea between the continents is still the world map; the zone names on hover for Forever's new zones like Gilneas come once WoW Forever is out.)
- **Licence:** from this version on Quiznight? is under the PolyForm Noncommercial licence instead of MIT: anyone may use, change and share it, but nobody may sell it. It's free and always will be — no purchases, no ads, no donations.

### Fixed
- **The Settings window in a lobby or game was cut off on the right, Close button included.** Its drop-down lists were as wide as their longest entry ("Screen 2 · 3440 × 1440 (main screen)", "Pinke Geilheit Spacing Team (animated)"), which made the page wider than the window on smaller or 4:3 windows: the right column and the Close button ran off its edge. The lists now fit their column and shorten a long entry with "…" (the full names are in the open list).
- **The game could freeze the PC with the space skin on.** Each planet's shadow is a crescent whose outline touched itself at its two tips (the same point twice), which Godot can't fill: it logged an error for that every single frame, about 60 a second. A test build writes every error into its log in detail, with where in the code it came from, and that pile-up is what froze things. The crescent now leaves the doubled points out, and the space scene runs without a single error.
- **The Star Destroyer flew over the picture beside the screen.** On wide monitors the space scene could draw past the edge of the screen's 16:9 area: the Star Destroyer comes in from beyond the right edge and showed over the background picture on the side. The space and seasonal scenes are now cut off at the screen's edge.
- **Long drop-down lists only scrolled from the second time you opened them.** The first time, the list stayed as tall as all its items, ran off the bottom of the window, and you couldn't get to the rest. Godot sets a drop-down list's smallest size to all its items when it opens, so making it shorter (10 rows, scrolling) was ignored until a later open. That smallest size is now cleared first, and the rows are measured from the list's font (the list's own height was already cut to the window, which made the rows come out too short).
- **The home page could be scrolled a little** (more so with the "Mark all as read" line): it was a few pixels taller than the window. It fits now.
- **Test lab: the buzzer only played its sound on the first press, and the reaction test then never started timing.** The buzzer took one buzz only, like in a game; the Test lab's now takes every press.
- **Long drop-down lists ran off the screen and couldn't scroll** on bigger windows (e.g. the categories). Godot wouldn't let a list be shorter than all its rows, so it grew back to full length; now it's cut to fit the window and scrolls.
- **Test lab:** the welcome screen couldn't be closed (Escape and right-click did nothing); the microphone test kept the microphone on (now there's a Stop button and a countdown, and the microphone is only on while recording).
- **Test builds didn't update by themselves, and sent no logs.** 0.12.0d1 and 0.12.0d2 said "You have the newest version" even when a newer test build was out: they were built without the game's key for the private repos (it isn't stored with the code and didn't get copied in), and without it a test build can't see the test releases at all, nor send its logs or quiz night results. The build tool now stops instead of only warning when the key is missing. **Download this version once by hand**; from here on test builds update themselves again.
- **The TRACKGLOW engraving on the buzzer** ran into the silver plate's lower edge; it's in the top strip above the key now.
- **A black box round drop-down lists.** Opening and closing a list quickly could leave a black rectangle round it for a moment; its little window is see-through round its rounded corners now.

## [0.11.5b]

### Fixed
- **Quiz night results weren't sent for the eternal table, and crash reports weren't sent either.** 0.11.4b was built without the game's key for the results list (it isn't stored with the code and didn't get copied in), so at the end of a night over Steam the results only went to Documents/Quiznight/Results instead of the eternal table, and errors weren't reported. This version has the key again; the build tool now refuses to make a version without it. Results from 0.11.4b nights are still in that folder, nothing is lost.

## [0.11.4b]

### Added
- **A Halloween song for October:** "The Sleepy Sneaky Sway" by Luella Gren. All through October it's the first song after the welcome voice line when you open the game (instead of "Easy Breezy"), and it plays with the others in the shuffle after that. From 1 November it's gone again until next October, the same way the Christmas songs only play in December. It's in the credits (the "i" button in the music player) with its Epidemic Sound link.

## [0.11.3b] — "Opening Night": an intro like a console game's, a title screen, New Year, credits, signed updates and undo

### Added
- **Undo and redo in the editors:** Ctrl+Z takes back the last change in the question editor and in the quiz overview (moving, deleting and renaming questions), Ctrl+Y or Ctrl+Shift+Z does it again. Typing counts as one step while you're at it, so one Ctrl+Z takes back a word or a sentence, not a single letter. A text field you're typing in undoes its own typing first.
- **"Only my Steam friends can join"** as a lobby rule: over Steam, players who aren't on the host's Steam friends list can't come in, even with the code.
- **An intro when the game starts,** like a console game's:
  - the **Pinke Geilheit Neural-Level Anti-Cheat** opens first, in a small window of its own while the game loads, like Easy Anti-Cheat's (it checks nothing, it just looks very serious);
  - then the game's window: the Xbox "World Premiere" sting, Made with Godot, maxlvl01 presents, Pinke Geilheit Publishing (with its moving logo), music by Epidemic Sound, sponsored by TrackGlow, and the credits;
  - and a **title screen**: the animated logo big in the middle and "Press any button". Pressing one plays the welcome line, and the logo glides up into its place on the home screen while the menu fades in around it.
  - A key or a click skips the card showing, and Escape skips straight to the title screen. With "less motion" switched on, the intro is left out.
- **The title screen has a TrackGlow button** in the corner that opens IBassGaming's Patreon.
- **Credits:** an "i" button in the home screen's music player opens them: every song and its artist, every map's source, the engine, the fonts and the full licence texts.
- **Happy New Year skin,** 27 December to 7 January, right after Christmas: midnight blue and gold, the same snow as at Christmas (also on the lobby and the BETA sticker), fireworks bursting over the home screen and the lobby, and a party hat on the logo.
- **Custom mouse cursors** in the game's style: white with a dark outline and a small shadow, like the buttons. There is one for every kind of pointer: the arrow, the hand over buttons, the grabbing hand while dragging, the text cursor, "not allowed", busy, the crosshair and the resize and move arrows.
- **Settings: what the Neural-Level Anti-Cheat does.** It reads your thoughts and bans you if you even think about cheating.
- **A third buzzer sound, "Buzzer 3",** to pick in Settings and in the lobby. Its recording is quieter than the other two, so the game plays it 12 dB louder to match.
- **Two more seasonal skins:** **Kölle Alaaf** for the Cologne carnival, on 11 November (the season opens at 11:11) and in the street carnival week from Weiberfastnacht to Veilchendienstag (worked out from Easter each year): red and white, confetti tumbling down, streamers along the top, and a jester's cap with bells on the logo. And **Valentine's Day** all of February: rose and pink, hearts floating up, hearts on the logo.
- **Right-click on empty space goes back a screen,** like Escape or the mouse's back button (not on the home screen, and not on buttons, text fields, sliders or lists).
- **Six new skins** (Settings → Skin): **Formula 1** (carbon black, F1 red and white), **AMOLED black** (true black, for OLED screens), **Daylight** (a light skin that's easy on the eyes: warm off-white instead of glaring white, dark plum text and outlines), **#effzeh** (red and white, for 1. FC Köln fans), and two that only show up in their season: **Halloween** in October (pumpkin orange and purple, cobwebs in the corners, bats flapping across, carved pumpkins, and the logo gets pumpkins, a cobweb and a bat) and **Christmas** from 1 to 26 December (pine green, red and gold, snow falling, fairy lights along the top, and the logo gets a snow cap and a Santa hat). After its season a seasonal skin goes back to Quiznight Purple. The decorations sit behind everything you need to read, and stand still with "less motion".
- **Settings during a game:** a gear in the top bar of the lobby, the waiting room and the host's and players' game screens opens Settings as a window over it, so you can turn the volume down or change the window size without leaving the room. Close, Escape or the mouse's back button close it again (asking first if you changed something without applying it). The skin and the dyslexia-friendly font are only changed from the home screen's Settings, because they rebuild every screen.
- **A music player on the home screen,** under "Host a quiz night": the song playing, back, play/pause, next and a button for the jukebox's list of songs. The long text in the host card is gone, so the card is smaller. (The note button in the corner stays on every other screen.)
- **"Fußspuren?"** joins the title screen's easter eggs.
- **"My profile" on the Ewige Tabelle,** next to the tabs: it opens your own profile (needs Steam, which is how your results are found).
- **Hosted nights on every profile, where FM shows international caps:** a microphone under the name with "Hosted 2 quiz nights" (or "Never hosted"), instead of a number among the stats.
- **Profiles for people who aren't in the Ewige Tabelle yet** (they only hosted so far, or it's your own "My profile"): the full page instead of "No quiz nights here", with empty attributes until they play.
- **The match history shows when a night started** instead of when it ended (the results now save the start time). Nights from before this version only saved the end, so they show just the day.
- **The form shows five places:** nights not played yet are dashed empty squares, so a new player's form is five empty slots.
- **Two test nights from 24 October are in the Beta table and the match history** (21:25 and 22:40, hosted by maxlvl01).

### Changed
- **Dropdowns:** a chunky arrow in the game's style that points up while the list is open, and the list unfolds smoothly instead of popping up.
- **Updates are signed.** Every update is now signed with Max's release key, which never leaves his PC, and the game checks the signature before installing anything. Before, the game trusted whatever GitHub handed it: someone who got into the GitHub account (or swapped files on the way) could have sent every player a game of their own. Now an update without a valid signature isn't installed, and the game says so.
- **Security check of everything online,** with these fixes:
  - **Nobody gets into a room without its code.** Before, a game that knew the host's Steam ID could connect straight to the host and take a seat without ever typing the code. Now the host checks the code on every join.
  - **Room codes aren't readable in Steam's lobby list any more.** The list (which anyone with Steam can read) had every open room's code in it as it is. Now it only has a one-way hash of the code that takes many hours to work back.
  - **Nobody can pretend to be someone else.** Over Steam, only the account Steam itself names gets a seat. Before, a game could claim another player's Steam ID if Steam didn't say who it was, and take over their seat and points. On LAN, a game can't claim a Steam ID at all.
  - **Kicked players stay out.** Before, a kicked player could simply join again with the code.
  - **Pictures, share codes and quiz files can't crash the game by size.** A tiny picture can claim to be 60 000 × 60 000 pixels, and a few KB of share code or zip can unpack to gigabytes. The game checks a picture's size before decoding it and stops unpacking at what a quiz can be.
  - **A host can't fill a player's disk.** The files a host sends before the game (sound, video, pictures) are limited to what a quiz can hold, and their names to plain ids.
  - **Recordings must be plain sound as the game records it** (16 kHz mono). Before, anything that looked a little like a WAV file was passed on to every player's screen.
  - **LAN: no more huge messages or idle connections.** A connection that announced a 256 MB message got that much memory reserved, and connections that never joined stayed open. Now a player's messages are limited to what the game sends (1 MB), connections must join within 15 seconds, and at most 48 can be open.
  - **Results files are spreadsheet-safe.** A player name like "=HYPERLINK(…)" would run as a formula when a results CSV is opened in Excel; such cells now get a ' in front, which the game takes off again when reading them.
  - **Only debug builds can be pointed at a different update server** (a testing setting that released builds also listened to).
- **The Fast Pass always answers first.** Before, it only took over from someone who had buzzed less than a second earlier; anyone who had been answering for longer kept their turn, and the Fast Pass waited second in line. Now whoever holds the Fast Pass answers first whenever they buzz. The one who was answering goes back to the front of the line, and anyone already marked wrong stays out. It's played blind, before the question is shown, so it stays a gamble.
- **Questions in a quiz are moved by dragging** (a line shows where the question lands), or by typing its new number into the number box in front of it. The ↑ and ↓ buttons are gone.
- **The welcome line runs straight into the music.** There was a gap of about two seconds. The recording ended in near-silence, and some songs start with up to 1.4 seconds of silence. The line's silent end is trimmed, the music comes in just before the line ends, and songs start where their sound starts.
- **No music during the intro.** The music waits for the title screen to be left and for the welcome line.
- **A new version shows on the logo instead of a line under it:** the logo's outline pulses green while an update is out (hover over it to see which version; the green arrow next to the version number installs it). The line "An update is available" made the home screen longer, so it started to scroll.
- **Bonus points have − and + buttons** next to the number (it can still be typed in), and the box is only as wide as two digits.
- **Image reveal editor fits on one screen:** the bonus and the category were at the bottom of the left column and made the editor scroll. They're in the round settings on the right now, like every other question type, and the picture and its preview sit side by side.
- **Map pin: the map pictures in the grid have rounded corners** like the "Upload a new map" tile (they were square and ran into the card's rounded edge), and the WoW map is just called "WoW Classic – Azeroth" (its long name was cut off).
- **WoW map: the world view is the classic parchment map** (Kalimdor and the Eastern Kingdoms on old paper, as in WoW Classic). Zooming into continents, zones and cities works as before, and pins in existing quizzes stay where they were.
- **Seasonal decorations only on the home screen and in the lobby:** the snow, fairy lights, cobwebs, bats, pumpkins, confetti and hearts were behind every screen, also the question editor and the game. Now they're only on the home screen and in the lobby (the logo keeps its own decorations).
- **Christmas: snow on the lobby** (a snow cap on the lobby's panel, with snow falling over it, for the host and the players) and a little snow on the BETA sticker.
- **No question editor scrolls any more** (checked for every question type, in English and German): short fields sit side by side in the round settings (time limit and points, start time and points), explanations moved to the roomier left side, and the notes under check boxes became tooltips. Before, pick one, multiple choice, estimation, map pin, sound, put in order and timeline were a little too tall and scrolled.
- **Developer mode:** a "Test update" button next to the DEV badge shows the home screen as if an update were out (the logo pulses green), and stops it again.
- **Seasonal skins come on by themselves:** in their season (Halloween in October, Christmas in December, the carnival days, Valentine's Day in February) the game shows that skin, and afterwards the skin you chose is back. Untick "Seasonal skins" in Settings to keep your own all year. They can't be picked by hand any more (developer mode can pick every skin, any time).
- **The Classic skin is called Alpha now** (saved settings keep it).
- **The voice line at the start is a little louder, and the music waits for it:** a few seconds of music used to play behind the intro, stop for the voice line and start again. Now the first song starts once the voice line is over.
- **"Magic of December" only plays at Christmas time** (1 to 26 December), like "Merriest Christmas of All".
- **The map cards in a map pin question are keys like the other buttons;** the chosen map stays pushed in. Long map names wrap onto a second line instead of being cut off ("Cyberpunk 2077 – Night City").
- **Developer mode shows as a small "DEV" badge** in the home screen's corner instead of a notice that pushed the page down (and made it scroll).
- **The joker buttons are small keys with just an icon:** the lightning bolt for the Fast Pass and a big ×2 for Double points. Their name and what you can do with them are in the tooltip, and one line underneath says what matters now (e.g. which question a played joker is on).
- **The first download is much smaller.** The music is now encoded at 128 kbps from the original recordings (was ~150 kbps; it sounds the same), the F1 Podium sound is a compressed Ogg instead of a 23 MB WAV (it was 134 seconds of uncompressed audio), the built-in maps are re-compressed (a little more compression; when you zoom in all the way you can't tell the difference), the game leaves out the parts of .NET it never uses and .NET's debugging tools (the folder next to the game went from 79 MB to 23 MB), and on Windows the game runs on a slimmed-down engine without the parts Quiznight never uses (3D physics, VR, Vulkan, …): `Quiznight.exe` went from 105 MB to 51 MB. This update itself is bigger than usual, once, because those files all changed.
- **Buttons look like real 3D keys:** under the coloured top there's a side in a darker shade of the button's own colour, with a light edge along the top and a soft shine. Pressing one pushes it down into its side.
- **The buzzer is a big square red key on a silver base,** in the same style, with the Quiznight Q? embossed on it instead of "BUZZ!".
- **A B C D and multiple choice answers are keys in their own colours** (red, blue, orange, green) instead of dark boxes with a coloured letter; a picked answer stays pushed in. The host's screen shows them the same way.
- **The jukebox's buttons are drawn icons** (back, play/pause, next) instead of the ⏮ ⏭ characters, which didn't look right in every font. The song playing is marked with a drawn note.
- **The music waits for the voice intro.** When the game opens, the voice line plays on its own and the first song starts from the beginning once it's done (before, the song already played underneath, turned down).
- **"Add a question" lists the question types alphabetically** (by the name you see, so in German too), and the type cards are keys like the buttons.
- **Numbers are in the game's own font:** scores, places, timers and the room code used the system's Consolas, which looked out of place. They're Fredoka now, with all digits the same width so columns still line up, and the score next to the host's − and + buttons sits in the middle of its row.
- **The BETA stamp is a sticker on the logo's corner,** so "Quiznight?" sits exactly in the middle of the home screen again (the stamp next to it pushed it to the left).

### Fixed
- **"Now playing" covered buttons:** in the quiz overview, the card slid in over "Copy share code" at the bottom left (it read "y share code"). It now slides in above a screen's bottom buttons.
- **German text for the "Step by step" points note** was missing; it showed in English.
- **Halloween bats no longer flood the log with errors:** when a bat flapped its wings all the way down, the wing folded over itself and Godot couldn't draw it (one "Invalid polygon data" error every frame, also in the test logs). The wings now flap a little less far down.
- **Christmas lights and snow, and Halloween's bats, reached past the game onto the space picture** on ultrawide monitors. They stay inside the 16:9 screen now; the bats fade in and out at its edges.
- **The lobby code and name fields disappeared** after switching "Play over" to LAN and back to Steam: rebuilding the join card freed the two fields along with their old rows. They're kept now.
- **The Fast Pass lost to someone who buzzed at the same moment.** Bob had the Fast Pass, but Ann's buzz arrived a few milliseconds earlier (both showed 2.6 s), so Ann was already answering and the Fast Pass only put Bob first among those still waiting, behind her. A Fast Pass now also takes over from someone who started answering less than a second earlier (they couldn't have said anything yet); they're next in line instead. Someone who has been answering for longer keeps their turn, as before.
- **The text on big buttons sat too low** ("My quizzes", "Join!"). The dark edge under each button counted as part of the button when the text was centred, so the text was centred on the button plus its edge and ended up below the middle of the coloured part. It's now centred on the coloured part.
- **The place and points over a hovered bar on a profile were unreadable on the tallest bar:** there was no room above it, so the white text landed on the gold bar. The bars leave room for it now.
- **"What's new" showed the notes in a narrow column** on the left half of the window. The window's scroll area didn't give its content the full width, so the wrapped text was only as wide as the heading. It now uses the whole width, and opens at the top instead of sometimes scrolled to the end.

### Removed
- **"Write the answer" as its own question type.** It was the same as a classic question with "Everyone types the answer", so that's what it is now: older quizzes and share codes with "Write the answer" questions open them as classic questions played that way (question, answers, picture, time limit, points and bonus kept), and old nights' stats for it count under Classic → Everyone types.

## [0.10.2b]

### Added
- **A jukebox on every screen.** The little note button in the bottom left opens it: switch the music off (it stays off next time you start the game) or on again, skip to the next or previous song, or pick a song from the list of what this screen plays. The "Now playing" card slides in right next to it.
- **The mouse's thumb buttons go back and forward,** like in a browser. Back works on every screen except the home screen, the same as Escape. Forward opens the screen you just came back from again (from Settings back to the home screen, then forward to Settings again). Going somewhere new by clicking starts the history over, like a browser does.
- **Ctrl+S** applies your changes in Settings and saves the question in the question editor (the same as pressing Apply or "Save question").
- **A Quit button on the home screen,** next to Settings. Pressing Escape on the home screen now asks whether you want to quit the game, too. Before, there was no way to close Quiznight from inside the game.
- **A welcome screen on the very first start.** Before the intro and the home screen, a new player can pick their language, colour vision help and the dyslexia-friendly font, so they can read the game from the start. It only shows on a PC that has never run Quiznight before: updating never brings it up.
- **A new skin: Pinke Geilheit,** soft pastel pink and lavender on dusky rose.
- **Sounds for right and wrong answers.** When the host marks a buzz right or wrong, everyone hears it, the same for each timeline placement. At the reveal of questions everyone answers, each player hears whether they got it, and the host's screen plays the right sound if anyone did (the wrong one if nobody did). Questions already judged buzz by buzz stay quiet at their reveal, so nothing plays twice. They follow the Sound effects slider.
- **Three new songs** for the menus, the lobby and quizzes: "Before Chill" (Yomoti), "Drive-Through Dinner" (Magnus Ringblom) and "Sunny San Francisco" (Oakwood Station). They're in the jukebox list too.
- **Olf?** joined the title screen's easter eggs.

### Changed
- **The old Pinke Geilheit skin is now called Quiznight Purple.** Nothing about it changed, and if you had it picked, you still have it; the name now goes to the new pink skin.
- **Volume sliders renamed.** "Game music" (menus, lobby and the podium sound) is now just **Music**, and the slider that was called "Music" (songs and clips in sound and video questions) is now **Video**, so it's clear which is which.
- **All volume sliders start at 50 %.** That is the new standard for everyone: if you already played, your volumes are set to 50 % once with this update, and the home screen tells you so (turn them back up in Settings if you liked it louder). New players start at 50 % too.
- **The F1 Podium sound is 10 dB louder.** It was much quieter than the menu music around it.
- **The award pack explains itself.** "Save award pack" used to have a long tooltip nobody read. Pressing it now opens a window that says what's in the pack, with two real examples drawn from the table you're looking at (the standings poster and the first award card), and a Generate button that makes it.
- **The bars under "Points per night" on a profile light up under the mouse** and show that night's place and points above the bar (the tooltip still adds the date and the quiz).

### Fixed
- **Settings (and every other screen) looked broken with the dyslexia-friendly font.** The text was spread out with big gaps between all the letters, so the video settings ran off the right edge of their card and got cut off. To make OpenDyslexic fit, the game squashed its letters a little sideways, but Godot kept each letter's full width, so the squashed letters just stood further apart. The letters now keep their real shape and sit a little closer instead, and the small headings in capitals no longer get extra spacing on top of that.
- **Settings said "Unsaved changes" as soon as it opened** when the saved window size didn't fit the monitor (it picks one that fits). That swap no longer counts as a change you made.

## [0.10.1b]

### Added
- **Escape goes back.** On every screen except the home screen, Escape takes you one screen back, like the "← …" link at the top: from a question to the quiz, from the quiz to My quizzes, from Settings or the Ewige Tabelle to the home screen. In a room it does what leaving means there: a player in the waiting room leaves. For things you can't undo, it asks first: closing the lobby, leaving a running game, or (as the host) ending the quiz early. Open windows, drop-downs and the big picture view close first, as before.
- **Crash reports.** If Quiznight crashes, or runs into an error in its code, the next start sends the end of that run's log (the last 400 lines, plus version, system and date) to Max, so he can see what went wrong and fix it. Your Windows user name is cut out of the file paths in it, and each log is sent only once. Only the real game does this, never test runs. Settings says so under Language. (Quiznight has always written these logs to %APPDATA%\Quiznight\logs; until now nobody ever saw them.)

### Changed
- **Thinner outlines on small buttons.** The party show look gave every button a thick ink outline and a big solid edge under it. On big buttons that looks great; on small ones like the host's − and + next to the scores, ↑ ↓ × in the question list, or the pills and chips, it looked crowded and heavy. Small buttons, drop-downs and toggles now have a thin outline and only a hint of an edge; pills and chips have no edge at all. Big buttons and cards stay chunky.
- **A drawn lightning bolt for the Fast Pass.** The Fast Pass joker used the ⚡ emoji, which looked like it came from another app next to the drawn icons. It's now a drawn yellow bolt with an ink outline, on the joker buttons, the lobby rule, the scoreboard and the buzz order.
- **The music steps back for previews.** When you press ▶ to hear a buzzer or podium sound (in Settings or the lobby), the menu music now fades out, so you actually hear the sound, and fades back in once it's over or you stop it. The same happens while the podium sound plays.
- **"Top 3 score (3 / 2 / 1)" is gone from the scoring list,** because "Podium (3 / 2 / 1)" under Points per place does the same. Questions that already use it keep it (it still shows for them).

### Fixed
- **The profile page's "Form" column was squashed.** With fewer than five nights played, the column on the right got only as wide as its few pills, so "First quiz night: 24.10.2026" and "Beta (before 1.0)" broke after every word into a tall, messy stack. The column now always has room for five pills, and those lines fit.
- **The preset list's dot never moved.** In the Scoring window, "Start from a preset…" always kept the selection dot, whichever preset you picked, because the list jumped back to it after every choice. The dot now stays on the preset your points match (and goes back to "Start from a preset…" once you change them).
- **Big buttons grew out of their space.** The party show look makes buttons grow a little under the mouse. On wide ones, the question rows in a quiz and the cards in "Add a question", that made them poke out of their list, so their left edge got cut off. Rows and cards that big now keep still (their colour still changes under the mouse).

## [0.10.0b] — "Season Ticket": a party show look, music, seasons, profiles, awards and sentences in parts

### Added
- **A whole new look: the party show.** Quiznight? now looks like a TV game show. Buttons and cards are chunky, with thick outlines and a solid edge under them, so they look like you can press them (and they squash when you do, and grow a little under the mouse). The screens stand on a stage with a warm spotlight glow. There are two new fonts: Fredoka for text (round and friendly) and Lilita One for headings, names and big buttons. The colours are still Quiznight's purple, orange and teal.
- **The logo is an answer field.** On the home screen, "Quiznight?" sits in a text box with a blinking cursor, and two spotlights sweep over it. Every now and then it deletes itself and types something else… you'll see.
- **A real buzzer.** The BUZZ! button is now an arcade button: a red dome with a shine on a chrome ring. It squashes down when you press it and turns grey while the buzzers are locked. The icon for buzzer questions is a little dome too.
- **Music.** Five songs from Epidemic Sound play in the menus, the lobby and during quizzes, shuffled. "Easy Breezy" by Megan Wofford always opens the game. During sound, video and interactive questions the music pauses, so it never covers the clip or your microphone, and it carries on afterwards. When a new song starts, a small "Now playing" card shows its title and artist in the bottom left. In December (1st to 26th) a Christmas song joins in. The music has its own volume: "Game music" in Settings.
- **A voice welcomes you** when you open the game (with a bit of reverb), and the music steps back while it talks.
- **Button sounds.** Buttons click when you press them.
- **A splash screen.** While the game loads you see the Quiznight? logo instead of Godot's, and when it's ready the "?" hops and the logo fades into the home screen. A click or any key skips it. "What's new" and the update question now wait until it's gone.
- **Accessibility** (a new card in Settings):
  - **Colour vision:** for red-green (deuteranopia or protanopia) or blue-yellow (tritanopia) colour blindness. The game shifts its colours so the ones you can't tell apart become different: right and wrong, the A–D answers, medals and player colours.
  - **Dyslexia-friendly font:** every text in OpenDyslexic, letters with heavy bottoms so they don't flip or swim.
  - **Screen readers** (like NVDA or Narrator) can read the game: buttons, texts and fields as always, and now also the buzzer, profile pictures, question type icons and the graphs (as a list of who's where). During a game, the reader announces what happens: the next question and its type, the question once it's shown, when the buzzers open, when it's your turn, and the answer with your points at the reveal.
  - **Less motion:** if you turned off animations in Windows (Settings → Accessibility → Visual effects), the splash, the spotlights, the typing logo and the bouncing buttons keep still.
- **A new way to play step by step: one sentence in parts.** In the editor, pick "One sentence in parts" and type the question in 2 to 5 pieces, in reading order (for example "This city, founded in 1237," / "was split by a wall from 1961 to 1989" / "and is Germany's capital today."). Players see the first part; the rest of the sentence stays blurred. The host reads on with "Read on (→)". Buzz in as soon as you know it: the earlier, the more points. With 5 parts, a right buzz after the first one gets 5 points, after the second 4, and so on. Double points doubles that, and a Fast Pass jumps the queue like always. As soon as someone buzzes, the reading stops: the host can only read on once that answer is judged, and after a wrong answer everyone can buzz again on the next part. Players never get the hidden parts (their screens only get the parts read so far), so nobody can peek.
- **Seasons.** The Ewige Tabelle has a "Table" picker: all time, one season, or the beta. Season 1 runs from 1.0 until the end of 2027; after that every calendar year is its own season (Season 2 is 2028, and so on).
- **Championship points like in racing.** "Ranked by" lets you rank the table the way F1 would: every quiz night, your place gets points (F1 since 2010: 25 for a win, 18 for second, 15 for third …). The other systems from the scoring presets are there too (older F1 systems, MotoGP, IndyCar, NASCAR, Mario Kart, 3/2/1), and "Quiz points" ranks by the points from the questions like before. The championship points are the last column, with the quiz points next to them.
- **The Ewige Tabelle per question type, subtype and category.** The "Questions" picker shows the table for one kind of question, with the columns that matter for it. Pick "Map pin" for pins, Ø km off, best pin ever and pins under 1 km; "Estimation" for Ø % off, closest guess and bullseyes (under 1 % off); a buzzer type for buzzes, wrong buzzes, buzz hit rate, Ø buzz time and fastest buzz ever; "Step by step" for the hint people get it at on average and how often they get it at the very first one; "Timeline" for placements and how many were right; typed answers for how many were spelled perfectly. Every type can be narrowed to its subtype (buzzer or everyone types, A B C D or multiple choice, each map, timeline or everyone at once, hints or sentence …) and to each category. These stats count from this version on: the nights before didn't save what happened in each question, so they can't be split up.
- **Position and points graphs.** "Show: Positions" draws everyone's place after each quiz night, like an F1 lap chart, with the names at both ends. "Show: Points" draws everyone's points climbing from 0. Click a name (on the chart or in the list beside it) to highlight that player, and hover the chart to see the night and everyone's place or points then. A night in the match history also gets both graphs, question by question (for nights from 0.10.0b on).
- **Profile pages, Football Manager style.** Click a name in the Ewige Tabelle, the match history or a rival line to open that player's profile:
  - **The top:** a big profile picture, a role line ("Speed buzzer · Map specialist"), their place in the table, nights, wins, podiums, "Hosted" (how many nights they were the quizmaster, where FM shows international caps), points and the form of their last five nights.
  - **Attributes rated 1 to 20** against everyone in the table: buzzer speed, quick answers, composure (how rarely their buzzes are wrong), spelling, consistency, winning habit, big-game player, joker timing, and one per question type (Map sense, Estimating, Knowledge, Ear, Deduction …).
  - **A radar chart** of the question type attributes, and a **football pitch** with the eleven question types as the eleven positions: green where they're a natural, red where it's not their game.
  - **A scouting report:** their three strengths and three weaknesses (the types, subtypes and categories where they beat or trail everyone's hit rate the most), records (fastest buzz, closest pin, closest guess, most points and most right answers in a night, longest winning streak, Double points played and what they brought) and their rival: the player they finished right next to most often.
  - **Below that:** their points per night as bars (gold, silver and bronze for podiums) and their career, night by night (click a night to open it).
- **Spelling is measured for real.** When you type an answer, the game already forgave small typos. Now it also notes how many letters were off from the closest accepted answer (capitals and accents don't count). The Spelling attribute and the "Spelled right" column use that: the share of right typed answers without a single typo.
- **Points from the host, and the "Host's Favourite Puppy".** Bonuses the host hands out at the reveal and score changes by hand (+ / − by the scores) now count as "points from the host". The profile shows them, and whoever got the most wears the "Host's Favourite Puppy" badge, on their profile and next to their name in the Ewige Tabelle.
- **Award pack.** "Save award pack" in the Ewige Tabelle makes a ZIP of pictures to share at the end of a season (Documents/Quiznight/Pictures; it opens the folder when it's done). There's a poster of the standings, with the champion in the spotlight, the podium and the full table. Then there's an award card for the best at every question type (Map specialist, Golden ear, Detective …) and every category, the Host's Favourite Puppy, the fastest buzzer and the spelling bee, each with the winner and who came second and third.
- **Question categories (optional).** Every question can get a category in its round settings: one of the suggestions (Geography, History, Music, Film & TV, Games, Sport …) or your own. Players only see it if the host turns on "Show question categories" in the lobby, because it makes questions easier. Then it shows on the "Get ready" screen and next to the question. Tick "Keep it secret" on a question whose category would give the answer away. Categories always count for the stats per category, shown or not.

### Fixed
- **The metal pipe buzzer was late.** Its sound file started with 1.37 seconds of silence before the clang, so the buzz sounded long after someone had buzzed. (An earlier fix was meant to cut it but didn't.) The silence is gone: the clang now comes right away.
- **Videos had big black bars.** A video question's player filled the whole width of the question and painted everything around the video black, so a normal 16:9 clip sat between two wide black bars and looked like a broken player. Now the video is as big as it fits at its own shape, with an outline around just the video and nothing painted around it.

### Changed
- **Answer time and buzz time are two separate numbers.** Until now one "Ø answer time" mixed both: a buzz counted its time to buzz (about a second), a typed or picked answer its time to lock in (often 10 seconds or more). So whoever buzzed a lot looked fast, and that mixed number decided ties. Now "Ø answer time" only counts answers you locked in and recordings you sent, and "Ø buzz time" counts buzzes, each with its own column in the Ewige Tabelle, a night's table and tonight's Stats. Ties go by answer time first, then buzz time. Nights before this version only have the old mixed number (shown as answer time, buzz time "–").
- **Points are the last column of the Ewige Tabelle**, like in a football table: the averages come first, the points (or championship points) at the right end. The per-type views end with the points from that type.
- **The results of a night save every question.** Besides everyone's totals, a night's results now have one line per player and question (its type, subtype and category, the points, buzzes, times, distances, typos, hints and jokers). All the stats above come from those lines.
- **Quiz files are format 9 now** (they can save categories and the sentence mode). Quizzes from older versions load as they are.

## [0.9.6b] — aligned headings, a peek at 0.10.0b

### Fixed
- **The Ewige Tabelle's column headings line up with their numbers.** The headings are buttons (click one to sort), and buttons have a little space on their left, so every heading sat a few pixels right of the numbers below it ("Nights" started after the "1"). The headings no longer have that space, and the number columns are now centred under their heading, so heading and numbers sit on one line down the column. "Player" stays left, above the pictures.

### Coming next
- **0.10.0b is on its way, and the Ewige Tabelle is about to get very nerdy.** Who's the fastest buzzer of all time? Whose map pin landed closest ever? Who always gets step by step on the very first hint? Soon you can look at the table one question type at a time, with all the stats nobody asked for. Then click a name: every player gets a profile page like a Football Manager scout report, with attributes rated 1 to 20, a form guide of the last nights, and a verdict on what kind of quizzer they are. Speed buzzer? Map specialist? Estimation sniper? We'll see. And quizmasters might get a way to tell you what a question is about before it starts… if they feel generous. One catch: those stats only start counting with 0.10.0b, so the nights before won't show up there.

## [0.9.5b] — Ewige Tabelle arrows, places by column

### Added
- **Arrows in the Ewige Tabelle.** Next to each place you now see how it changed with the newest quiz night: a green ▲2 if you went up two places, a red ▼1 if you dropped one, and a blue "New" if it was your first night. Hover an arrow to see your place before. It works for any column you sort by.

### Changed
- **The places follow the column you sort by.** In the Ewige Tabelle, # always showed everyone's place by points, so when you sorted by answer time the fastest player could be at the top marked "3rd". Now the best in the sorted column is 1st (whichever way round you sort it), equal values share a place, and only points use the faster answer time to break ties.
- **Places sit in coloured boxes, with lines between the rows.** In the Ewige Tabelle, a night's full table in the match history and tonight's Stats, the places were plain coloured text that sat higher than the profile pictures next to them. Now each place is a small box: gold, silver and bronze for the top 3, plain after that, centred with the picture. A thin line between the rows makes it easy to read a row across.

### Fixed
- **Players on the same points were in the wrong order in the Ewige Tabelle.** Two players on 11 points could show "3rd" above "2nd". The places break ties by the faster average answer time, but the rows were put in order by name, so the two didn't match. The rows now follow the places.

## [0.9.4b] — statistics with a match history, update check while open

### Added
- **The game looks for updates while it's open.** Until now it only checked once, when you started it, so if a new version came out while you were building a quiz you didn't find out until the next restart. Now it checks every minute in the background. When there's a new version, the home screen shows it like before, and "Update to 0.9.4b" appears in the top bar of "My quizzes", the quiz overview and the question editor. Clicking it first warns you that the game restarts to update, so you can save the question you're working on. It never pops up by itself, and not during a game.
  - Why it doesn't run out of checks: GitHub only allows 60 checks an hour from one internet connection, which two PCs checking every minute would use up. So the minute check only looks at the address of the newest release page (which has no such limit) and asks GitHub's update service only once that shows a newer version.
- **Statistics, with a match history.** The "Ewige Tabelle" link on the home screen is now "🏆 Statistics". It opens the Ewige Tabelle like before, and a switch at the top right goes to the new match history and back (Statistics opens on whichever you looked at last). The match history shows every quiz night so far, newest first, with the quiz's name, who hosted it ("by Max"), the date and a little podium of the top 3's profile pictures (hover one for the name and points). Click a night (or "Details →") to open it on its own screen with the full table: everyone's profile picture, points, how many questions they scored on, average answer time, and how far off their map pins and estimations were. "← Match history" takes you back. Nights before 1.0 carry a "Beta" tag. Who hosted is only known from this version on (older results didn't save it), so the first nights show without a host. In the Ewige Tabelle, the switch between the real table and the beta table now says "From 1.0" and "Beta", so it isn't named "Ewige Tabelle" twice.
- **Your name in bold.** In the Ewige Tabelle and the match history your own name (found by your Steam account) is in bold, so you find yourself at a glance. The other names are no longer bold; the top 3 still have their gold, silver and bronze places.

## [0.9.3b] — how it works, tonight's stats, sortable Ewige Tabelle

### Added
- **"How it works" while you wait.** The "Get ready…" screen players see before the host shows a question used to be mostly empty space. It now says what kind of question is coming and how it works in one sentence, and it knows the variant: a buzzer question tells you to buzz and say it out loud, A B C D to pick one before the time runs out, multiple choice that several can be right, a timeline that you take turns, and so on. It also warns you when wrong answers cost points.
- **Tonight's stats.** Next to "Scores" on the side of the host's and the players' screens there's now a "Stats" link. It opens a table of everyone's night so far: place, points, how many questions they scored on, their average answer time, how far off their map pins were on average and how far off their estimations were (in % of the right number). Players can also switch the "Get ready…" screen between "How it works" and "Tonight so far" to see the same table there.
- **Ewige Tabelle: sort by any column.** Click a column's heading (Wins, Ø points, Ø answer time …) to sort the table by it; click again for the other way round. The # column keeps everyone's real place by points, so sorting never changes who's first. Players without a value in that column (say, no estimations yet) go to the bottom.
- **Ewige Tabelle: profile pictures** in front of every name (Steam pictures; players from LAN games get their initials in a colour of their own).
- **Ewige Tabelle: Ø % off** for estimations: how far off someone's guesses were on average, in % of the right number, so a guess of 110 when the answer is 100 counts as 10 % off. The results of a night now record this too. Nights played before this version didn't, so the column only fills from now on.
- **New tie rule "Faster answer wins"** for map pin and estimation questions. Two players with the same distance used to share a place (or both get nothing, depending on the rule). Now you can choose that whoever locked in first gets the better place. New map pin questions use it by default; for existing ones you can pick it under "If there is a tie".
- **Wrong answers can cost points** in A B C D and multiple choice questions: tick "Wrong answers cost points" in the editor and a wrong answer loses as many points as a right one gets (doubled with Double points, like the win would be). Not answering costs nothing. It's off unless you tick it, and players see a warning on the question when it's on.

### Changed
- **Put in order scores every item in its right place.** An "everyone at once" order question only gave points for a perfect order, so someone with 3 of 4 right got the same as someone with none right, which felt harsh. Now every item in its right place gets the question's points (4 items, 1 point each: 3 right = 3 points). The editor calls it "Points per item in the right place". Existing order questions score this way too, so one that was worth 2 for a perfect order now gives up to 2 per item.
- **Optional minus points for wrong places.** Tick "Wrong places cost points" in an order question and every item in a wrong place takes the question's points off again. It's off unless you tick it; players see a warning on the question when it's on. (Timeline questions are unchanged: they already score per item.)
- **Map pin ties go by the distance you see.** Two pins were only a tie if their distance was exactly the same down to the last decimal, which basically never happens, while the reveal showed both as, say, "312 m". Now pins at the same shown distance (to the metre, or 0.1 % on maps without a scale) count as a tie, and the tie rule decides.

## [0.9.2b] — scoring per place, units, UI polish

### Added
- **Scoring per place for estimation and map pin questions.** Until now these questions had two ways to score: only the closest player wins, or the top 3 get 3, 2 and 1 times the question's points. Now there's an "Edit" button next to "Scoring" in the editor. It opens a window where you set the points for every place yourself: 1st, 2nd, 3rd and as many places as you like ("+ Place", "− Place").
  - Start from a built-in preset: F1 since 2010 (25-18-15-12-10-8-6-4-2-1), the older F1 systems (2003–2009, 1991–2002, 1961–1990, 1950–1959), MotoGP, IndyCar, NASCAR Cup, Mario Kart 8, or a simple podium 3 / 2 / 1.
  - Change any place, give it a name and "Save as my preset": it's kept on your PC and shows up in the Scoring list of every question from then on. Your own presets can be deleted again in the same window.
  - The points are saved inside the question itself, so a quiz you share scores exactly the same on someone else's PC, even if they don't have your preset.
  - Places after the last one get nothing. Players who are exactly as close as each other share the better place's points (unless the question says "Nobody gets the point" for ties). Double points doubles whatever a place gets.
  - During the game the top bar says "up to 25 points" instead of a fixed number, since what you get depends on your place.
- **Units are a drop-down.** The unit field of an estimation used to be a free text box with "e.g. hours" in it, which made it easy to type the same unit three different ways across a quiz. Now it's a list grouped by kind: length (mm to light years, AU and parsecs), mass, time, speed, area, volume, temperature, energy and power (including horsepower and PS), data, money and a few others. "Custom…" at the bottom still lets you type any unit, and units from older quizzes that aren't in the list (like "keys") show up there, so nothing changes for existing questions.

### Changed
- **Decimal numbers keep every digit.** Estimation answers were shown with at most two digits after the point, so an answer of 0.00000034 AU showed as "0" at the reveal and a guess that was 0.00000002 off looked like "spot on". Now every digit you type is kept and shown, both in the editor and at the reveal. You can type 2.5 or 2,5 (both work, for the editor and for players' guesses), and the game shows a comma when it's in German. Tiny rounding errors computers make (0.1 + 0.2 = 0.30000000000000004) are hidden, so differences read cleanly.
- **The answer timer on "Your turn!" is readable.** The small "Time to answer" label was orange text on the orange panel, so you could barely see it. It's now dark like the rest of the panel's text, and a bit bigger.
- **Score lists don't cut off clan tags any more.** In the narrow score rows a name like "[LCB] BassGaming" became "[LCB] BassGami…", so the actual name was the part that got lost. Score rows now show the name without the tag; hover over it to see the whole name. Seats in the lobby and the podium still show the tag on its own line above the name.
- **Step by step says "up to 5 points".** The top bar always said "5 points", although a right answer is worth one point less after each hint. It now says "up to", and "Right now: 3 points" under the cards still shows the current value.
- **Interactive questions count recordings, then votes.** The side panel said "Answers: 2 / 4" while players were recording, which was confusing. It now says "Recordings: 2 / 4", and "Votes: 2 / 4" while voting.
- **The same arrows everywhere.** Slideshows flipped with ◀ ▶, recordings with ◀ ▶▶. Recordings now use ◀ ▶ too, and the play button between them says "Play it", so it can't be mistaken for "next".
- **The recorder's microphone is a drawn icon.** It was the 🎤 emoji, which the game's font doesn't have, so some PCs showed a strange pen-like shape instead. It's now drawn like the question type icons.
- **"Hold to peek at the answer" matches the buttons next to it.** It was a smaller button than "Reveal answer" beside it, so the row looked uneven.
- **Lobby rules have a heading for each group.** "Jokers" had a little heading, but the two checkboxes above it didn't, so the list looked like it was missing something. They're now under "How you run it".
- **Ewige Tabelle shows metres for small distances.** The "Ø km off" column showed "0.0 km" for map pin guesses on game maps, because those are only a few hundred metres off. Anything under 1 km now shows in metres, like "29 m".
- **No tooltip on the "?" of the question type cards.** Hovering it showed a long tooltip with the same text you get when you click it, which covered the cards below. Clicking the "?" still shows the full help.

### Fixed
- **The waiting room's jokers no longer jump around.** When the host turned a joker off in the lobby, it disappeared from the players' "Tonight's rules" box, which made the box shrink and grow every time. Every joker now stays listed; one that's turned off is greyed out with "Off tonight."

## [0.9.1b] — map corner, peek and step by step fixes

### Changed
- Map pin: while your mouse isn't on it, the map in the corner is see-through, so the screenshot shows through. The pin button keeps it big, and "Hide" folds it away into a small "Map" button to look at the whole screenshot.
- The host's "Hold to peek at the answer" shows the answer in a box above the button instead of stretching the row, and is gone on timeline questions (the order is right there on the cards).

### Fixed
- Step by step: the hint cards on players' screens are smaller, so the buzzer fits without scrolling.
- Host lobby: "Ready" and the × to remove someone are lined up again.

## [0.9.0b] — "Mic Drop": two new question types and everything from the first real round

### Added
- New question type **Interactive**: give everyone a task like "Imitate the Super Mario jump sound". Players record themselves with their microphone (up to 15 seconds), listen back and send it. Then the host plays every recording one by one, on the host's screen and on every player's PC. Players vote for the best (a point per vote, not for yourself), or the host picks a favourite, who gets the question's points. Double points doubles it.
- New question type **Step by step**: up to 5 hints, the hardest first, that the host flips over like cards. Players buzz in when they know it. Right on the first hint is worth 5 points (as many as there are hints), each hint later one less. Every new hint gives everyone another go.
- Steam profile pictures: avatars in the lobby, the game and on the podium now show everyone's Steam picture.
- Map pin questions look like GeoGuessr for players: the screenshot is big, and the map sits in the bottom-right corner. It grows while your mouse is over it, ⤢ keeps it big, "Full" puts it over the whole picture.
- Picture slideshows: once the host has shown every picture, players can flip through them on their own screen.
- Ewige Tabelle: our first quiz night is in! Nights before 1.0 count for the **Beta table**; the real Ewige Tabelle starts with 1.0, and you can always switch to the Beta table there.
- "Let everyone buzz again" for the host when everyone who buzzed was wrong.

### Changed
- Pressing play on a sound or video, and the start of an image reveal, unlock the buzzers by themselves. Resuming after everyone guessed wrong gives everyone another go.
- New map pin questions score the top 3 (3, 2 and 1 points, doubled with Double points) instead of only the closest. Existing ones can switch under "Round settings" in the editor.
- Leave the name field empty to join with your Steam name.

### Fixed
- Clan tags pushed the "Ready" line of that seat lower than the others.

## [0.8.12b] — download percent fix

### Fixed
- The lobby's download percent went negative (around 30–40 %) on quizzes with big videos, so Start never unlocked. The download itself was fine.

## [0.8.11b] — downloads in the lobby

### Added
- Players get the whole quiz (pictures, maps, sounds and videos) while they wait in the lobby. Each seat shows "Downloading 45 %" or "Ready", and Start waits until everyone is ready ("Start anyway" if someone's download is stuck). Nobody can peek: each file stays locked until its question is shown.
- The rings next to the names during a question now count its pictures too, not just sound and video.
- Clan tags: a tag in square brackets before your name ("[OTB] maxlvl01") doesn't count toward the name's length and shows as its own small line above the name.
- "Leave" on the "Connection lost — reconnecting…" bar, so nobody is stuck when the host is gone.

### Fixed
- Over Steam, pictures could stay on "Loading image…" while the timer ran, and "End quiz early" could leave players stuck in the question. The game's own messages got lost while sound and video files were being sent; now nothing is lost and game messages always go first.
- The podium song stops when you leave the podium screen.

## [0.8.10b] — joining fix

### Fixed
- Over Steam, only one player could join a room: the next one ended up in the same seat (they kept swapping instead of each getting their own). Now everyone gets a seat of their own.

## [0.8.9b] — Quiznight? and picture questions

### Added
- New question type **Picture**: show one picture (e.g. a Steam review to read) or several as a slideshow. You flip through them with ◀ ▶ or the arrow keys, and every player's screen follows; players only ever see the pictures you've shown so far. Answered like a classic question: buzzer, everyone types, A B C D or multiple choice, and a bonus works too. Drop several screenshots on the editor at once, or paste them one after another, to build a slideshow.

### Changed
- Quiznight is now **Quiznight?** — like an invitation to a quiz night. The logo, the window title and a new icon say so.

## [0.8.8b] — icon and sound previews

### Changed
- Quiznight has its own icon: a Q and the orange "!" in the logo's font, on purple (the game file, the taskbar and the window).
- Sound previews (buzzer and podium, in Settings and the lobby): ▶ turns into ■ while it plays, so you can stop it. Only one plays at a time, and leaving the screen stops it.

## [0.8.7b] — hotfix 1

### Changed
- The Eternal table is called "Ewige Tabelle" now, in English too.

## [0.8.6b] — map grid

### Changed
- Map pin editor: you pick the map from a grid of pictures (name and width under each) instead of a drop-down. "Change map" brings the grid back; your own uploaded maps are in it too.
- Maps that come with an update get an orange NEW badge in the map grid for that update.

## [0.8.5b] — scores and lobby

### Changed
- Tick boxes are drawn in the skin's colours: blue in Pinke Geilheit, teal in Classic.
- Places read 1st, 2nd, 3rd … (German 1., 2., 3.) on the podium, scoreboards and in the buzzer line, instead of plain numbers or #4.
- In the score tables, 1st, 2nd and 3rd are gold, silver and bronze, and when someone overtakes someone the rows slide past each other into their new places. At the reveal, a green ▲ or red ▼ shows how many places each player moved with that question.
- Only one player per question can use the ⚡ Fast Pass: whoever plays it first. Everyone else's button says it's taken (not by whom). ×2 Double points can still be used by anyone.
- Players: click "⋮ 3 more" in the scores to see everyone (it scrolls). It folds back a few seconds after your mouse leaves it.
- The host's lobby looks like the players' waiting room: big room code, a seat for everyone (× removes a player), and the game rules in two compact columns at the bottom. It fits without scrolling.
- The waiting room shows tonight's rules at the bottom: how the host runs the questions and buzzers, the answer time and which jokers are allowed. It updates as soon as the host changes something.
- Dates in My quizzes read 22.10.2026 (day.month.year).

## [0.8.4b] — new colours

### Changed
- New look, the "Pinke Geilheit" skin: a deep purple background, orange instead of yellow for buttons and highlights, and blue outlines. Prefer the old colours? Settings → Skin → Classic.
- The update button next to the version is just an arrow now: grey (click it to check for updates), green when there is one (click it to install).
- Timelines start with a random item each game, instead of one you pick in the editor.

## [0.8.3b] — fixes

### Fixed
- Pop-up titles sit on a solid bar now, instead of on whatever was behind the pop-up.
- Clicking a question type's name or icon (not just the space around it) opens it again.

## [0.8.2b] — short share codes and update check

### Added
- **Short share codes**: "Copy share code" now gives a code like **QN-7K4P-9XZ2** that fits in any Discord message.
  The quiz is stored with Max's results online; pasting the code (My quizzes → Paste share code) fetches it again.
  Without internet you get the long code with the whole quiz in it, like before.
- **Check for updates** next to the version number on the home screen. When a new version is out it turns green
  ("Update to …") and a note under the logo says so.

### Changed
- **Export** makes a zip: the quiz plus its sound and video files as real files, about a third smaller than before.
  Import file reads these zips and the older .json files.
- The trash can sits in a quiz's top-right corner, just a red can. Under the mouse it grows and asks "Delete?".
- The **?** on the question types sits level with the name, without a box.

### Fixed
- The "Don't ask me again" box in the delete question sat on top of the text.

## [0.8.1b] — tidier quiz screens

### Changed
- **Deleting a quiz**: the trash can on a quiz in My quizzes. It asks first, unless you tick "Don't ask me again"
  (Settings → Video → "Ask before deleting a quiz" turns the question back on).
- **Picking a question type**: the cards show just the name. The **?** on each one tells what it does (hover) and
  explains its variants and options (click), e.g. buzzer, A B C D and multiple choice for a classic question.
- German: "Put in order" is now called **Einordnen**.

## [0.8.0b] — "Jokers Wild": jokers, big rooms, fair places and the eternal table

### Added
- **Jokers**: each player can play each joker once per match, before a question starts (during the reveal before it,
  or while the host still hides it). Until the question is shown you can take it back; then everyone sees it.
  The host picks which ones are allowed in the lobby.
  - **×2 Double points**: whatever the question gives you counts twice.
  - **⚡ Fast Pass** (buzzer questions only): when you buzz, you go ahead of everyone waiting in line.
  - During a reveal the top bar says whether the **next question is a buzzer one**, so you know when a Fast Pass works.
- **Leaving and joining during a game**:
  - If someone drops out, **the game pauses** (timers, buzzers, answers and the video or song) until they're back,
    so they lose nothing. The host can also **continue without them**. Leaving and coming back with the code keeps
    your seat and points.
  - **New players can join a running game** with the code. They watch the question that's running and play from
    the next one.
  - The scores and the podium show **who left (and at which question) and who joined later**.
- **Download rings**: on sound and video questions, a ring next to each name in the scores fills up while that
  player gets the file, and turns into a tick when they have it.
- **The room code** is in the top bar for the host and the players. Click it to copy it.
- **Change your name in the waiting room**: double-click it, type, Enter. The waiting room shows everyone who's in
  plus one open seat, instead of every seat the room has.
- **Confetti** on the final results.
- **Fairer places**: equal points are decided by who got there first, then by the faster average buzz/answer time
  (to the thousandth of a second), then by how close your map pins were on average. Hover a name in the scores to see
  their average answer time.
- **Shorter scores for players**: the top 3, then "⋮" and the players right around you (the one ahead, you, the one
  behind), so you see your closest competition even in a big room. The host still sees everyone.
- **Results go to Max**: after a quiz night the host's game sends everyone's results to Max for the eternal table
  (only if that fails, e.g. without internet, it keeps them in Documents/Quiznight/Results).
- **🏆 Eternal table** (link on the home screen): every quiz night whose results came with the game, added up per
  player: nights, wins, podiums, points, average points, average answer time and average km off.
- **Podium sounds**: the new **F1 Podium** (now the default) and the first one, **#SoBack**. The host picks one in the lobby, and everyone
  hears it with the final results. In Settings you can pick your own and untick "As a player, play the one the host
  picked" to always hear yours.
- **A new background picture** for wide monitors, the **Pinke Geilheit Spacing Team** (now the default): the astronaut
  beside the screens on the left, a corner of her ship on the right. Settings → Video → Background picture switches back to the
  Cosmic Cliffs.
- **Click a question's picture** (e.g. the screenshot of a map question) to see it as big as your screen allows.
- **Share codes**: "Copy share code" (on a quiz, or in My quizzes) copies the whole quiz as one line of text. Paste it
  to a friend or to your other PC, where **My quizzes → Paste share code** imports it. Pictures are in the code;
  sound and video files aren't (too big), so the game tells you which ones to add again. A long code that Discord
  turns into a message.txt file can be imported with **Import file**.

### Changed
- **No more "Max players" setting**: every room has room for 24 players.
- **The host sees a hidden question right away** (no more frosted glass), so you can read it out before pressing
  Show question. Players still see "Get ready…" until then.
- **Names are at most 12 characters** (longer Steam names are shortened), so the scores stay tidy. Initials skip
  symbols ("[OTB] maxlvl" is OM).
- **LAN play is hidden** on the home screen: it was for testing on one PC. Games are played over Steam.
- **The space background on wide monitors moves much less** and follows the mouse more slowly (it could cause motion
  sickness). Settings → Video → Background movement still turns it off completely.

## [0.7.4b] — small fixes

### Changed
- Small fixes and polish.

## [0.7.3b] — everyone sees and hears it all, faster updates, smaller download

### Added
- **Players see and hear everything the host's screen shows** (except the answers), so nobody needs to watch a
  Discord stream:
  - **Videos and sounds play on the players' PCs too**, in step with the host's: they play, pause and jump along
    with it. Players can turn the sound off. The game sends them to everyone in the background from the start of
    the quiz (locked, so nobody can watch or listen before its question).
  - **The host starts them**: next to the Start button you see who's still downloading, or "Everyone has it".
    Until then, players see the video blurred, so nobody can sit on the buzzer.
  - **Image reveal**: players see the picture getting clearer on their own screen, in step with the host's.
  - **At the reveal** players see everyone's answers, like on the host's screen.
- **Space background on wide monitors**: the Cosmic Cliffs (James Webb Space Telescope) fill the space beside the
  screens, slightly out of focus, sharp where the mouse is. **Settings → Video → Background movement** turns its
  movement off.

### Changed
- **Everyone needs this version:** the host and all players should update to 0.7.3b before playing together.
- **Sound, video and image reveal questions stay paused after a wrong answer** (when nobody else is in line): the host decides
  whether to resume it or to reveal the answer if nobody knows it.
- **Much faster updates.** The game now downloads only the parts that changed (usually about 1 MB instead of the
  whole game), several at once, and no longer slows the download down while it shows the progress bar. The update to this
  version still comes the old way (the whole zip), after that it's quick.
- **Smaller download: about 160 MB instead of 240 MB.** The built-in maps now ship as their original image files
  (they're also next to the game in a `maps` folder) instead of being stored again bigger inside the game file.
  They look the same or slightly sharper, nothing was made smaller or blurrier.
- **Wide screens:** on ultrawide monitors (21:9, 32:9) every screen now stays 16:9 in the middle instead of
  stretching across the whole monitor.

### Fixed
- Videos keep their shape: in the editor preview and in the game they're no longer squashed or stretched to fit the
  box (black bars fill the rest).

### Removed
- Leftovers of an old sound pack that was never used in the game.

## [0.7.2b] — everyone types, bonus points, change scores

### Added
- **"Everyone types" instead of buzzers.** Classic buzzer, sound, video and image reveal questions can now be played
  with everyone typing their answer instead of buzzing in: pick **How players answer → Everyone types** in the editor
  (for classic questions it's **Everyone types the answer** next to Buzzer). Answers that match yours are ticked
  automatically like in "Write the answer", everyone who's right gets the points, and at the reveal you can tick or
  untick any answer. These questions have a time limit.
- **Bonus points.** Every question can have an optional bonus, e.g. "+2: Also name the game". Players see it under the
  question. At the reveal you press **Give bonus** next to anyone who earned it (press again to take it back).
- **Change scores at will.** The scoreboard on your screen has **−** and **+** next to every player, so you can add or
  take away points any time during the game.

### Changed
- Quiz files are now format 5 (for typed questions and bonuses). Older files still load.
- The sample quiz's sound question is played with everyone typing and has a bonus; question 1 has a bonus too.

### Fixed
- When you tick or untick a typed answer at the reveal, the player's own screen now updates too.

## [0.7.1b] — beta, with 15 new game maps

### Added
- **15 new built-in maps for Map pin questions**, all with their real size, so "how far off" is right:
  - **Zelda:** Breath of the Wild (Hyrule), Tears of the Kingdom (Hyrule and the Depths).
  - **Elden Ring:** the Lands Between and the Land of Shadow (DLC).
  - **Red Dead Redemption 2**, **Cyberpunk 2077** (Night City), **GTA San Andreas** and **Forza Horizon 6** (Japan).
  - **PUBG:** Erangel, Miramar, Taego, Vikendi, Deston and Sanhok.

  Pick them under "Choose a map…" in a map pin question (the list is now sorted A–Z). Most have no place names on
  them; the Red Dead and Elden Ring maps show some, like in the game.
- **AnyConverter on Linux:** the Linux download now comes with AnyConverter too, and the video guide's
  **Open AnyConverter** button works there. AnyConverter itself is a newer, faster version.

### Changed
- **Timeline: drag and drop.** On your turn the game to place sits above the timeline as a card: drag it into the gap
  where you think it goes. The gaps light up while you drag. Changed your mind? Drag it to another gap, as often as
  you like, then press **Lock in**. Clicking a gap still works too.
- **The players' screen now looks like the host's:** a bar on top with the question number, type and points
  (and your name and score), the question and your answer in a card, and a column on the right with the buzz order,
  the timeline's turn order or who has answered, and the scores.
- **Quiznight is in beta now.** The stamp next to the logo says BETA instead of ALPHA, and versions end in "b".
- **No more scrolling in the game screens and the question editor.** Pictures and maps now take the space that's
  left on the screen instead of a fixed size, so everything fits in the window — on the host's screen, on the
  players' screens and in the editor. On the map pin reveal, players see the answer and the scores next to a big map.
- **Settings is more compact:** each volume is one row (hover it for what it does), so the whole screen fits.
- The empty "Image (optional)" box in the editor is smaller, and the question box is two lines high.

### Fixed
- On the timeline, "◀ Oldest" and "Newest ▶" now sit right at the ends of the timeline instead of at the edges of
  the screen, for the host and the players.
- **The Skyrim map no longer has a big empty border** on the right and at the bottom. Pins in quizzes you already
  made move along with it, and distances stay correct.
- The sample quiz's "Where is Whiterun?" pin is now actually on Whiterun.

## [0.6.1a] — the timeline goes at the host's pace

### Changed
- **The timeline now goes at your pace.** When a player locks in a spot, nothing is shown as right or wrong until
  you press **Reveal** (or Space). After the result, the next turn only starts when you press **Next turn** (Space),
  and after the last item **Show results**. A turn that runs out of time also waits for you. Players see "Locked
  in!" and then "Right! +1" or "Not quite." on their own screen.

### Fixed
- After an item was placed, the ✗ from an earlier wrong guess no longer stays on the timeline in the wrong spot.

## [0.6.0a] — Timeline: take turns putting things in order

### Added
- **Put in order: new "Timeline" way to play.** In the editor, switch "How it's played" to **Timeline — take turns**.
  Give each item what it shows once it's placed (e.g. its year), and mark the one that **starts** on the timeline (it
  can be anywhere: oldest, newest or in between). Up to 10 items.
  - Players take turns, fewest points first. On your turn you click the gap where the next item goes and lock in.
  - Right: it's placed with its year, you score right away and place the next one too (or switch off "Whoever is
    right keeps going" to pass the turn every time).
  - Wrong: the gap is crossed out and the next player tries the same item. If nobody gets it, it's placed for free.
  - Each turn has its own timer. The host can skip a player or place the item for free.
  - The sample quiz has a new question 12 to try it.

### Changed
- Quiz files are now format 4 (for the timeline). Older files still load.

## [0.5.2a] — AnyConverter included

### Added
- **AnyConverter comes with Quiznight (Windows).** It's in the new AnyConverter folder next to the game. When you add a
  video question with an MP4 (or WebM, MOV, MKV), the guide now has an **Open AnyConverter** button: it opens with your
  video and .ogv already picked, you press Convert, then "Use the converted video". No more typing commands into a
  terminal. The first time, AnyConverter helps you install FFmpeg, the free tool that does the converting.

## [0.5.1a] — lobby fix

### Fixed
- **The lobby now has the two host-control checkboxes** ("I show each question myself" and "I unlock the buzzers
  myself"). Both were always on before and couldn't be switched off. Untick them for a faster game where questions
  appear right away and the buzzers are open from the start.

## [0.5.0a] — more question types, host control, seamless WoW map

### Added
- **The game updates itself.** When a new version is out, Quiznight downloads it at start, restarts and
  then shows you what's new (also under "What's new" at the bottom of the home screen).
- **Linux version** (64-bit, with Steam): download the `-linux.zip`, unzip it and start `Quiznight.x86_64`
  (if it won't start, make it runnable first: `chmod +x Quiznight.x86_64`).
- **German translation:** the whole game is now also in German. Pick the language under Settings;
  on a German Windows it starts in German by itself. Quiz questions stay as they were written.
- **Settings screen** (button at the top right of the home screen):
  - Volume for everything, for music in questions (songs and clips in sound/video questions), for game music
    and for sound effects.
  - Which screen the game opens on, window mode (windowed, fullscreen, borderless) and window size. Sizes come
    in 16:9, 16:10, 21:9, 32:9 and 4:3, and only the ones that fit your screen are offered (plus its own
    resolution), so a 1440p screen can't pick 4K.
  - Language: English or Deutsch.
  - Changes only count once you press **Apply** (volumes and language preview right away). **Discard** goes
    back, **Reset to defaults** fills in the standard values, and leaving with unsaved changes asks first.
- **Video questions explain how to use an MP4:** pick or drop an MP4 (or WebM, MOV, MKV) and a short
  3-step guide opens. It shows exactly what to paste to turn it into the .ogv format the game can play
  (with the free tool ffmpeg), then adds the converted video with one click. Also there as a link
  under "Choose file…".
- **Buzzer sounds:** a sound plays when someone buzzes in — on the host's screen and for every player. Pick "Soft beep", "Metal pipe",
  your own sound file or no sound under Settings (the default), or in the lobby for one night. Follows the
  Sound effects volume. If the host uses their own sound file, players hear their own default buzzer instead.
- **Podium jingle:** a short tune plays on the host's screen when the final results appear. It has its own
  "Game music" volume slider in Settings (separate from the music in quiz questions).
- **Seamless WoW map:** WoW map pin questions now use one big map, like the OpenMap addon. Scroll from the
  whole world into a continent, then into every zone stitched together, and on into the cities; drag to move,
  or jump straight to a zone. No more clicking through world → continent → zone. Pins can go anywhere. At
  the reveal, the map zooms to the answer and everyone's guesses. Zone art: the OpenMap addon (thanks!).
- **New question type: Image reveal.** Upload a picture and choose how it's hidden: **zoomed right in** (click
  the spot it starts on) and slowly zooming out, or **pixelated** and slowly getting sharper, over as many
  seconds as you like (5–120). It shows on the host's screen only; players buzz in, it pauses while someone
  answers, and at the reveal everyone sees the whole picture and how many seconds it took. The editor has a
  live preview.
- **New question type: Put in order.** Type 3–6 things in the right order (e.g. games by release date) and
  what the top and bottom mean ("Oldest" / "Newest"). Players get them shuffled and sort them with ▲ ▼. A
  perfect order scores; the reveal shows how many each player had in the right place.
- **New question type: Write the answer.** Everyone types their answer. Answers matching yours are ticked
  automatically — case, accents, "the" and small typos don't matter, and you can accept several answers
  ("Mount Everest / Everest"). At the reveal you can tick or untick any answer and the points follow.
- **You decide when each question appears.** A new question starts hidden: on your screen it's frosted over
  and players only see "Get ready…". Read it out first (the new **Host notes** window shows the question and
  answer — if you share only the game window in Discord, nobody else sees it), then press **Show question**
  or Space. Timers, music, videos and image reveals only start then.
- **You unlock the buzzers.** On buzzer questions the buzzers stay locked (grey) until you press **Unlock
  buzzers** or Space, and you can lock them again any time. Both are lobby rules you can switch off.
- **Multiple choice questions:** a third kind of classic question where several answers can be right. Tick every
  correct answer in the editor; players pick all they think are right and then lock in. Only players who pick
  exactly the right ones get the points.

### Changed
- The game runs at most at 60 FPS, so it no longer heats up your PC for nothing.
- Zoomed-in maps (Skyrim, GTA …) are drawn smoothly instead of blocky.
- **A B C D answers are shuffled every game.** In the editor you just type the answers and tick the right one —
  no letters anymore. When you host, the order is mixed up, so the right answer isn't always B just because
  that's where you typed it. Each new game gets a new order.
- Quiz files are now format 3 (for multiple choice). Older files still load; files with multiple choice
  questions show up as buzzer questions in older versions of the game.

## [0.4.0a] — Quiznight is now a Godot app

### Added
- **Quiznight runs as a desktop app** (Godot 4.7, C#) instead of a website. Everything the website could do
  is in: the quiz editor, all five question types, the lobby, buzzers, reveals, scores and the podium.
- **Play over Steam:** the host opens a lobby and gets a 4-letter code; friends enter the code and connect
  through Steam's network — no port forwarding, works over the internet. Uses Valve's free test app
  (Spacewar), so Steam shows everyone as "playing Spacewar".
- **LAN mode:** play on the same Wi-Fi without Steam, or open several windows on one PC to test.
- If a player's connection drops, the game reconnects by itself and they keep their seat.
- A sample quiz with one question of every kind, for trying things out.

### Changed
- Players now play on their own PC with the app instead of a phone browser.
- Quiz files keep the same format: quizzes exported from the website import as they are. Built-in game maps
  are no longer copied into quiz files (they ship with the game), so files with game maps are much smaller.
- Videos have to be `.ogv` files (Godot can't play MP4 or WebM). Sound can be MP3, OGG or WAV.
- When the last player who hadn't answered leaves, the answer is revealed right away instead of waiting
  for the timer.

[0.15.1b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.15.1b
[0.15.0b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.15.0b
[0.14.3b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.14.3b
[0.14.2b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.14.2b
[0.14.1b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.14.1b
[0.14.0b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.14.0b
[0.13.5b]: https://github.com/maxlvl01/quiznight/releases/tag/v0.13.5b
[0.13.4b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.13.4b
[0.13.3b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.13.3b
[0.13.2b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.13.2b
[0.13.1b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.13.1b
[0.13.0b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.13.0b
[0.12.8b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.8b
[0.12.7b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.7b
[0.12.6b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.6b
[0.12.5b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.5b
[0.12.4b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.4b
[0.12.3b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.3b
[0.12.2b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.2b
[0.12.1b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.1b
[0.12.0b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.12.0b
[0.11.5b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.11.5b
[0.11.4b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.11.4b
[0.11.3b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.11.3b
[0.10.2b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.10.2b
[0.10.1b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.10.1b
[0.10.0b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.10.0b
[0.9.6b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.6b
[0.9.5b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.5b
[0.9.4b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.4b
[0.9.3b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.3b
[0.9.2b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.2b
[0.9.1b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.1b
[0.9.0b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.9.0b
[0.8.12b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.12b
[0.8.11b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.11b
[0.8.10b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.10b
[0.8.9b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.9b
[0.8.8b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.8b
[0.8.7b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.7b
[0.8.6b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.6b
[0.8.5b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.5b
[0.8.4b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.4b
[0.8.3b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.3b
[0.8.2b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.2b
[0.8.1b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.1b
[0.8.0b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.8.0b
[0.7.4b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.7.4b
[0.7.3b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.7.3b
[0.7.2b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.7.2b
[0.7.1b]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.7.1b
[0.6.1a]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.6.1a
[0.6.0a]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.6.0a
[0.5.2a]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.5.2a
[0.5.1a]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.5.1a
[0.5.0a]: https://github.com/maxlvl01/quiznight-releases/releases/tag/v0.5.0a
