#!/usr/bin/env bash
set -euo pipefail

mkdir -p images

master="master.png"

# specs: name width height
specs=(
  "header-mobile-600x320 600 320"
  "header-mobile@2x-1200x640 1200 640"
  "header-tablet-900x420 900 420"
  "header-tablet@2x-1800x840 1800 840"
  "header-desktop-1440x720 1440 720"
  "header-desktop@2x-2880x1440 2880 1440"
)

for spec in "${specs[@]}"; do
  name=$(awk '{print $1}' <<<"$spec")
  w=$(awk '{print $2}' <<<"$spec")
  h=$(awk '{print $3}' <<<"$spec")
  echo "Generating ${name} -> ${w}x${h}"
  size="${w}x${h}^"
  # JPEG
  magick "$master" -resize "$size" -gravity center -extent "${w}x${h}" -strip -interlace Plane -sampling-factor 4:2:0 -quality 85 "images/${name}.jpg"
  # WebP
  magick "$master" -resize "$size" -gravity center -extent "${w}x${h}" -strip -quality 75 "images/${name}.webp"
  # AVIF
  magick "$master" -resize "$size" -gravity center -extent "${w}x${h}" -strip -quality 50 "images/${name}.avif"
done

echo "Done."