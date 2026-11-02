#!/usr/bin/env bash
# Publishes the guided tour's sample quiz (scripts/Storage/TourDownload.cs): an exported .quiznight.zip ("Test Night", one
# of every question type), signed with Max's release key, as tour.quiznight.zip(.sig) on the "maps" release next to the
# map packs.
#   tools/tour_quiz.sh --test QUIZ.zip    to the private maxlvl01/quiznight-test-releases (test builds take it from there)
#   tools/tour_quiz.sh QUIZ.zip           to the public maxlvl01/quiznight-releases (only once Max says it's fine)
#   tools/tour_quiz.sh --dry-run QUIZ.zip sign into build/tour-quiz/, upload nothing (QUIZNIGHT_MAPS_FEED=that folder tries it)
#   tools/tour_quiz.sh --check [--test]   only say whether the release has it (exit 1 if not)
# The maps release must exist (tools/map_packs.sh makes it).
set -euo pipefail
cd "$(dirname "$0")/.."
MODE=""; TEST=""; QUIZ=""
for arg in "$@"; do
	case "$arg" in
		--check|--dry-run) MODE="$arg" ;;
		--test) TEST=1 ;;
		-*) echo "Unknown option $arg"; exit 1 ;;
		*) QUIZ="$arg" ;;
	esac
done
REPO="maxlvl01/quiznight-releases"
[ -n "$TEST" ] && REPO="maxlvl01/quiznight-test-releases"
TAG="maps"
if [ "$MODE" = "--check" ]; then
	if gh release view "$TAG" --repo "$REPO" --json assets -q '.assets[].name' 2>/dev/null | tr -d '\r' | grep -qx "tour.quiznight.zip.sig"; then
		echo "The tour's sample quiz is on $REPO."; exit 0
	fi
	echo "The tour's sample quiz isn't on $REPO: run tools/tour_quiz.sh${TEST:+ --test} QUIZ.zip (the tour falls back to the small built-in quiz)"; exit 1
fi
[ -f "$QUIZ" ] || { echo "Give the quiz: tools/tour_quiz.sh [--test|--dry-run] QUIZ.quiznight.zip"; exit 1; }
unzip -l "$QUIZ" quiz.json >/dev/null 2>&1 || { echo "$QUIZ isn't an exported quiz (no quiz.json)"; exit 1; }
OUT="build/tour-quiz"
rm -rf "$OUT" && mkdir -p "$OUT" && touch build/.gdignore
cp "$QUIZ" "$OUT/tour.quiznight.zip"
SIGNING_KEY="${QUIZNIGHT_SIGNING_KEY:-$HOME/.quiznight/release-signing-key.pem}"
[ -f "$SIGNING_KEY" ] || { echo "Release signing key missing at $SIGNING_KEY (set QUIZNIGHT_SIGNING_KEY)"; exit 1; }
openssl dgst -sha256 -sign "$SIGNING_KEY" -out "$OUT/tour.quiznight.zip.sig" "$OUT/tour.quiznight.zip"
openssl dgst -sha256 -verify tools/release-signing-public.pem -signature "$OUT/tour.quiznight.zip.sig" "$OUT/tour.quiznight.zip" >/dev/null || { echo "Signing failed"; exit 1; }
[ "$MODE" = "--dry-run" ] && { echo "(dry run: nothing uploaded; test with QUIZNIGHT_MAPS_FEED=$PWD/$OUT)"; exit 0; }
gh release view "$TAG" --repo "$REPO" >/dev/null 2>&1 || { echo "No maps release on $REPO: run tools/map_packs.sh${TEST:+ --test} first"; exit 1; }
gh release upload "$TAG" --repo "$REPO" --clobber "$OUT/tour.quiznight.zip" "$OUT/tour.quiznight.zip.sig"
echo "Done: the tour's sample quiz is on $REPO ($TAG)."
