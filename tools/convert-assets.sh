#!/usr/bin/env bash
# convert-assets.sh — one-shot importer for the dummy art kit.
#
# Provenance: these sprites come from the author's own prior StarRaid iterations
# in ~/Projects/Archive/StarRaid (reference-only; never modified). They are
# magenta (#FF00FF) colour-keyed BMPs; this script keys them to PNG-with-alpha
# and lays them out under client/assets/ as first-class, committed dummy assets
# so the build never depends on the archive path. Replace freely later — the
# game loads whatever PNGs sit in assets/.
#
# Re-run after editing the mapping; idempotent (overwrites the same outputs).
# Requires ImageMagick (`magick`). Override sources via env if the archive moves.
set -euo pipefail

ARCHIVE="${STARRAID_ARCHIVE:-$HOME/Projects/Archive/StarRaid}"
GIT="${STARRAID_GFX_GIT:-$ARCHIVE/starraid-git/Client/GFX}"
BACK="${STARRAID_GFX_BACK:-$ARCHIVE/starraid-legacy/OLD Versions/Back/gfx}"

OUT="$(cd "$(dirname "$0")/.." && pwd)/assets"
FUZZ="8%"   # catch anti-aliased magenta fringe

key() { # src dst
  local src="$1" dst="$2"
  mkdir -p "$(dirname "$dst")"
  magick "$src" -fuzz "$FUZZ" -transparent '#FF00FF' "$dst"
  echo "  $dst"
}

echo "ships:"
key "$GIT/ships/aCC1.bmp" "$OUT/ships/ship1.png"   # own ship (default)
key "$GIT/ships/aCD1.bmp" "$OUT/ships/ship2.png"   # neighbour default
key "$GIT/ships/aCH1.bmp" "$OUT/ships/ship3.png"
key "$GIT/ships/aCZ1.bmp" "$OUT/ships/ship4.png"

echo "radar:"
key "$GIT/sfRadar.bmp"                 "$OUT/radar/radar.png"
key "$GIT/sfRadarSpotGoodBig.bmp"      "$OUT/radar/spot_good_big.png"
key "$GIT/sfRadarSpotGoodSmall.bmp"    "$OUT/radar/spot_good_small.png"
key "$GIT/sfRadarSpotBadBig.bmp"       "$OUT/radar/spot_bad_big.png"
key "$GIT/sfRadarSpotBadSmall.bmp"     "$OUT/radar/spot_bad_small.png"
key "$GIT/sfRadarSpotNeutralBig.bmp"   "$OUT/radar/spot_neutral_big.png"
key "$GIT/sfRadarSpotNeutralSmall.bmp" "$OUT/radar/spot_neutral_small.png"
key "$GIT/sfRadarSpotUnknownBig.bmp"   "$OUT/radar/spot_unknown_big.png"
key "$GIT/sfRadarSpotUnknownSmall.bmp" "$OUT/radar/spot_unknown_small.png"

echo "select:"
key "$GIT/sfSelect50.bmp"  "$OUT/select/select50.png"
key "$GIT/sfSelect100.bmp" "$OUT/select/select100.png"
key "$GIT/sfSelect200.bmp" "$OUT/select/select200.png"

echo "bg:"
key "$GIT/bg/mapStarSmall.bmp"  "$OUT/bg/star_small.png"
key "$GIT/bg/mapStarMiddle.bmp" "$OUT/bg/star_middle.png"
key "$GIT/bg/mapStarBig.bmp"    "$OUT/bg/star_big.png"

echo "ui:"
key "$GIT/sfMenueLogo.bmp"    "$OUT/ui/menu_logo.png"
key "$GIT/sfMenueActive.bmp"  "$OUT/ui/menu_active.png"
key "$GIT/sfMenuePassive.bmp" "$OUT/ui/menu_passive.png"
key "$GIT/sfMenueBG.bmp"      "$OUT/ui/menu_bg.png"
key "$GIT/sfMenueBorder.bmp"  "$OUT/ui/menu_border.png"
key "$GIT/sfMenueBottom.bmp"  "$OUT/ui/menu_bottom.png"

echo "ui/action:"
for a in laserSingle laserMulti gunSingle gunMulti comScan comEmp; do
  key "$GIT/action/$a.bmp" "$OUT/ui/action/$a.png"
done
for f in frameOn frameOff frameLoading; do
  key "$GIT/action/frame/$f.bmp" "$OUT/ui/action/$f.png"
done

echo "shield (directional arcs):"
key "$BACK/mapSchilde.bmp" "$OUT/shield/shields.png"

echo "cursor:"
key "$GIT/sfCursor.bmp"    "$OUT/cursor/cursor.png"
key "$GIT/sfMouseSpot.bmp" "$OUT/cursor/mouse_spot.png"

echo "done."
