#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
export DOTNET_CLI_HOME="$repo_root/.local/cli"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
dotnet_bin="$repo_root/.local/dotnet/dotnet"
if [[ ! -x "$dotnet_bin" ]]; then dotnet_bin="/workspace/.tools/dotnet/dotnet"; fi
if [[ ! -x "$dotnet_bin" ]]; then dotnet_bin="$(command -v dotnet)"; fi
"$dotnet_bin" build VolumeIntelligence.sln --configuration Release --nologo -m:2
"$dotnet_bin" run --project tests/VolumeIntelligence.Tests --configuration Release --no-build
mkdir -p artifacts
validation_dir="$(mktemp -d "$repo_root/artifacts/replay.XXXXXX")"
"$dotnet_bin" run --project src/VolumeIntelligence.Cli --configuration Release --no-build -- replay tests/fixtures/trades.synthetic.jsonl --config tests/fixtures/replay-settings.json --output "$validation_dir/first.json"
"$dotnet_bin" run --project src/VolumeIntelligence.Cli --configuration Release --no-build -- replay tests/fixtures/trades.synthetic.jsonl --config tests/fixtures/replay-settings.json --output "$validation_dir/second.json"
cmp "$validation_dir/first.json" "$validation_dir/second.json"
python3 - "$validation_dir/first.json" <<'PY'
import json,sys
r=json.load(open(sys.argv[1]));p=r['profiles'][0]
assert p['totalVolume']==10 and p['poc']==101 and p['val']==100 and p['vah']==102 and p['delta']==2
assert r['metrics']['received']==4 and r['metrics']['applied']==3 and r['metrics']['duplicates']==1
print('CLI golden replay and byte-identical repeat: PASS')
PY
printf 'Replay evidence: %s\n' "$validation_dir"
