"""Fills the "Music pack" export preset's file list with every song in assets/music (and its season folders).

    py -I tools/music_preset.py

The songs ship as their own music.pck next to the game (release.sh exports it with this preset), not in Quiznight.pck,
so an update that doesn't touch the music doesn't download it again (the game mounts it at startup: App.LoadMusicPack).
release.sh runs this first, so a new song needs nothing else.
"""
import os, re

songs = sorted("res://" + os.path.join(root, n).replace(os.sep, "/")
               for root, _, names in os.walk("assets/music") for n in names if n.endswith(".ogg"))
cfg = open("export_presets.cfg", encoding="utf-8").read()
start = cfg.index('name="Music pack"')
line = 'export_files=PackedStringArray(' + ", ".join(f'"{s}"' for s in songs) + ')'
head, tail = cfg[:start], cfg[start:]
tail = re.sub(r'export_files=PackedStringArray\([^)]*\)', lambda _: line, tail, count=1)
open("export_presets.cfg", "w", encoding="utf-8", newline="\n").write(head + tail)
print(f"Music pack: {len(songs)} songs")
