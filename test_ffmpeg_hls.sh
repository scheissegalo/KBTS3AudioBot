#!/bin/bash
# Test script for FFmpeg HLS playback
# Usage: ./test_ffmpeg_hls.sh [HLS_URL]

HLS_URL="${1:-https://manifest.googlevideo.com/api/manifest/hls_playlist/expire/1767241934/ei/bqRVaYbaC8ymj-8Pr4O6-Qk/ip/66.9.172.19/id/b5a06d4f447dd89a/itag/93/source/youtube/requiressl/yes/ratebypass/yes/pfa/1/sgoap/clen%3D66612814%3Bdur%3D4115.922%3Bgir%3Dyes%3Bitag%3D140%3Blmt%3D1762486483713016/sgovp/clen%3D17183950%3Bdur%3D4115.849%3Bgir%3Dyes%3Bitag%3D134%3Blmt%3D1762493704566322/rqh/1/hls_chunk_host/rr5---sn-q4fl6nsr.googlevideo.com/xpc/EgVo2aDSNQ%3D%3D/met/1767220334,/mh/Wm/mm/31,29/mn/sn-q4fl6nsr,sn-hp57kndz/ms/au,rdu/mv/m/mvi/5/pl/24/rms/au,au/initcwndbps/990000/siu/1/bui/AYUSA3CnOPmIhyT9gpGER1dwODW0ubCZmw3MUF6O6THgLdz0u0f2neFzbsUdhTZfH6wS5Dygzw/spc/wH4Qq9SVaVD7fUeIY8H_porg3hEv5ChuXOhrjACCve9bpDsRWEOlRkd7IaUptdlNFPzsrasQeyAgA9jOxLHjWNTj/vprv/1/ns/JpYLkiIicwV7YdcKXyH6l6YR/playlist_type/CLEAN/dover/11/txp/5432534/mt/1767219914/fvip/2/keepalive/yes/fexp/51355912,51552689,51565515,51565681,51580968/n/KlQAiGPj0cgLNk_xwJ/sparams/expire,ei,ip,id,itag,source,requiressl,ratebypass,pfa,sgoap,sgovp,rqh,xpc,siu,bui,spc,vprv,ns,playlist_type/sig/AJfQdSswRAIgaP8iePrDsHhwqdrFz24qLFvQdTgp1R3dxNdq4COIuJUCIGZ6va8w_yZBi1ENd6F5Ys2R19OR9UyKiEouQdV7f1HQ/lsparams/hls_chunk_host,met,mh,mm,mn,ms,mv,mvi,pl,rms,initcwndbps/lsig/APaTxxMwRgIhAPH8iaOLfX1xdibAvAmi-N2q39N1SFR9mnP2OMbTyNx0AiEAwg-NLS6n233wKP5FnUx3MWdLoBaJscUzH5IX6PWCBNM%3D/playlist/index.m3u8}"

echo "Testing FFmpeg HLS playback with corrected command..."
echo "URL: $HLS_URL"
echo ""
echo "Command:"
echo "ffmpeg -hide_banner -nostats -threads 1 -http_persistent 0 -multiple_requests 0 -fflags +discardcorrupt -live_start_index 0 -i \"$HLS_URL\" -ss 00:00:00.000 -ac 2 -ar 48000 -f s16le -acodec pcm_s16le -"
echo ""
echo "Playing audio (press Ctrl+C to stop)..."
echo ""

# Test 1: Play directly (requires audio output)
# ffmpeg -hide_banner -nostats -threads 1 -http_persistent 0 -multiple_requests 0 -fflags +discardcorrupt -live_start_index 0 -i "$HLS_URL" -ss 00:00:00.000 -ac 2 -ar 48000 -f s16le -acodec pcm_s16le - | aplay -r 48000 -f S16_LE -c 2

# Test 2: Save to WAV file for inspection
OUTPUT_FILE="test_output_$(date +%s).wav"
echo "Saving to $OUTPUT_FILE for 10 seconds..."
timeout 10 ffmpeg -hide_banner -nostats -threads 1 -http_persistent 0 -multiple_requests 0 -fflags +discardcorrupt -live_start_index 0 -i "$HLS_URL" -ss 00:00:00.000 -ac 2 -ar 48000 -f wav "$OUTPUT_FILE" 2>&1 | head -20

if [ -f "$OUTPUT_FILE" ]; then
    echo ""
    echo "File created: $OUTPUT_FILE"
    echo "You can play it with: aplay $OUTPUT_FILE"
    echo "Or check its properties with: ffprobe $OUTPUT_FILE"
    echo ""
    echo "Checking if audio starts from beginning (first 2 seconds)..."
    # Use ffprobe to check the start time
    ffprobe -v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 "$OUTPUT_FILE" 2>/dev/null
else
    echo "ERROR: Output file was not created. Check FFmpeg errors above."
fi

