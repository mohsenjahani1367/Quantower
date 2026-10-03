#!/usr/bin/env bash
set -euo pipefail
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$repo_root"
python3 - <<'PY'
import hashlib, json, pathlib, tarfile, urllib.request
root = pathlib.Path.cwd()
version = json.loads((root / 'global.json').read_text())['sdk']['version']
destination = root / '.local/dotnet'
binary = destination / 'dotnet'
if not binary.exists():
    metadata = json.load(urllib.request.urlopen('https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json', timeout=30))
    sdks = [sdk for release in metadata['releases'] for sdk in release.get('sdks', [release['sdk']])]
    sdk = next(sdk for sdk in sdks if sdk['version'] == version)
    artifact = next(f for f in sdk['files'] if f['rid'] == 'linux-x64' and f['name'].endswith('.tar.gz'))
    archive = root / '.local/sdk.tar.gz'
    archive.parent.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(artifact['url'], timeout=60) as response, archive.open('wb') as output:
        while chunk := response.read(1024 * 1024):
            output.write(chunk)
    with archive.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha512').hexdigest()
    if digest.lower() != artifact['hash'].lower():
        raise RuntimeError('Official SDK SHA-512 mismatch; refusing extraction')
    destination.mkdir(parents=True, exist_ok=True)
    with tarfile.open(archive) as bundle:
        bundle.extractall(destination, filter='data')
    archive.unlink()
    print('Installed checksum-verified SDK', version)
PY
export DOTNET_CLI_HOME="$repo_root/.local/cli"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
"$repo_root/.local/dotnet/dotnet" build VolumeIntelligence.sln --configuration Release --nologo -m:2
