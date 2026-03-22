---
name: ffxiv-plugin-release
description: Prepare a release for this Loot History style Dalamud plugin workspace by updating version metadata, rebuilding in Docker with a mounted Dalamud dev folder, and refreshing out/release artifacts when the user provides a version such as 0.2.1-alpha or v0.2.1-alpha.
metadata:
  short-description: Prepare a Dalamud plugin release
---

# FFXIV Plugin Release

Use this skill when the user wants to prepare a Loot History style Dalamud plugin release from this workspace and provides a version.

## What this skill does

- updates release metadata in:
  - `LootDistributionInfo/LootDistributionInfo.csproj`
  - `scyt.repo.json`
  - `README.md`
  - `LootDistributionInfo/README.md`
- derives:
  - package version, such as `0.2.1-alpha`
  - git tag, such as `v0.2.1-alpha`
  - assembly/file version, such as `0.2.1.0`
- rebuilds the Docker image
- runs the containerized test/build flow with a mounted Dalamud `dev` folder
- refreshes:
  - `out/release/latest.zip`
  - `out/release/scyt.repo.json`
- verifies the exported package version and assembly version

## How to use it

1. Expect a version from the user.
2. Run:

```bash
python3 scripts/prepare_release.py --workspace "<workspace>" --version "<version>"
```

3. If the user has a non-default Dalamud dev folder, pass it explicitly:

```bash
python3 scripts/prepare_release.py --workspace "<workspace>" --version "<version>" --dalamud-dev-path "<path>"
```

## Defaults and assumptions

- The default workspace shape is this repo:
  - `LootDistributionInfo/`
  - `LootDistributionInfo.Tests/`
  - `scyt.repo.json`
- The default Dalamud path is `<workspace>/14.0.4.1/dev`.
- Docker must already be available.
- The GitHub release asset name remains `latest.zip`.

## Expected result

After the script succeeds, report:

- version prepared
- test count and build success
- release-ready files:
  - `out/release/latest.zip`
  - `out/release/scyt.repo.json`
- the next GitHub step:
  - create the release tag
  - upload `latest.zip`

## Notes

- Accept versions with or without a leading `v`.
- Do not hand-edit the release files if the script can handle it.
- If the Dalamud dev folder is missing, stop with a clear error instead of guessing.
