#!/usr/bin/env bash
# Publishes the built-in maps as downloads (scripts/Storage/MapPacks.cs): one zip per map plus the signed maps.json,
# on a "maps" release (a pre-release, never "latest", so the update check doesn't see it).
#   tools/map_packs.sh --test     to the private maxlvl01/quiznight-test-releases: test builds take their maps from
#                                 there, so new or changed maps are tried first, like the game itself
#   tools/map_packs.sh            to the public maxlvl01/quiznight (only once Max says the maps are fine; games before
#                                 0.13.5 keep using the old maxlvl01/quiznight-releases maps release)
#   … --check                     only say whether the published maps match assets/maps (exit 1 if not)
#   … --dry-run                   build and sign, upload nothing
# Run it before a (test) release whenever a map was added or changed: games from 0.12.8b on don't ship the maps.
# Old zips stay on the release (games that fetched an older maps.json may still want them).
set -euo pipefail
cd "$(dirname "$0")/.."
MODE=""; TEST=""
for arg in "$@"; do
	case "$arg" in
		--check|--dry-run) MODE="$arg" ;;
		--test) TEST=1 ;;
		*) echo "Unknown option $arg"; exit 1 ;;
	esac
done
REPO="maxlvl01/quiznight"
[ -n "$TEST" ] && REPO="maxlvl01/quiznight-test-releases"
TAG="maps"
OUT="build/map-packs"
rm -rf "$OUT" build/maps-prev && mkdir -p "$OUT" build/maps-prev && touch build/.gdignore
gh release download "$TAG" --repo "$REPO" --pattern maps.json -D build/maps-prev 2>/dev/null || echo "(no maps release yet)"
NEW=$(py -I tools/map_packs.py "$OUT" build/maps-prev/maps.json | tr -d '\r')
if [ "$MODE" = "--check" ]; then
	if [ -n "$NEW" ] || ! cmp -s <(py -I -c "import json,sys;print(json.dumps(json.load(open(sys.argv[1]))['packs'],sort_keys=True))" "$OUT/maps.json") \
		<(py -I -c "import json,sys;print(json.dumps(json.load(open(sys.argv[1]))['packs'],sort_keys=True))" build/maps-prev/maps.json 2>/dev/null); then
		echo "The maps on $REPO don't match assets/maps: run tools/map_packs.sh${TEST:+ --test} first"; exit 1
	fi
	echo "The maps on $REPO are up to date."; exit 0
fi
SIGNING_KEY="${QUIZNIGHT_SIGNING_KEY:-$HOME/.quiznight/release-signing-key.pem}"
[ -f "$SIGNING_KEY" ] || { echo "Release signing key missing at $SIGNING_KEY (set QUIZNIGHT_SIGNING_KEY)"; exit 1; }
openssl dgst -sha256 -sign "$SIGNING_KEY" -out "$OUT/maps.json.sig" "$OUT/maps.json"
openssl dgst -sha256 -verify tools/release-signing-public.pem -signature "$OUT/maps.json.sig" "$OUT/maps.json" >/dev/null || { echo "Signing maps.json failed"; exit 1; }
echo "New packs: ${NEW:-none}"
[ "$MODE" = "--dry-run" ] && { echo "(dry run: nothing uploaded; test with QUIZNIGHT_MAPS_FEED=$PWD/$OUT and --map-packs)"; exit 0; }
if ! gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1; then
	gh release create "$TAG" --repo "$REPO" --prerelease --latest=false --title "Built-in maps" \
		--notes "The game's built-in maps, downloaded by the game when a quiz uses one (from 0.12.8). Nothing to download by hand."
fi
FILES=()
for f in $NEW; do FILES+=("$OUT/$f"); done
# (the packs first, then the list that points to them)
[ ${#FILES[@]} -gt 0 ] && gh release upload "$TAG" --repo "$REPO" --clobber "${FILES[@]}"
gh release upload "$TAG" --repo "$REPO" --clobber "$OUT/maps.json" "$OUT/maps.json.sig"
echo "Maps published on $REPO."
