# Packaging Metadata Policy

This repository publishes forked packages under the `Monigass.*` namespace.

## Required metadata for forked packages

For all published package manifests (`.nuspec` and any package metadata in `.csproj`):

- `authors` must credit the original software author: `Rico Suter`.
- `owners` must identify the package maintainer: `Monigass`.
- Package descriptions must clearly disclose this is the `Monigass/NSwag` fork and not the upstream package.
- Icon URLs must not use `raw.githubusercontent.com`; use a CDN URL instead.

## Current canonical icon URL

- `https://cdn.jsdelivr.net/gh/Monigass/NSwag@master/assets/NuGetIcon.png`

## Notes

- Keep package ids unchanged unless there is an intentional rebranding/migration decision.
- If ownership policy changes, update this document and the build metadata validation in `build/Build.Pack.cs` together.
