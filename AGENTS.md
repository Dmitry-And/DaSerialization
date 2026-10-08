# DaSerialization agent entry point

DaSerialization is a manual, versioned binary serialization library with optional Unity integration. Read [README.md](README.md) for advertised capabilities and [the local development/architecture guide](docs~/README.md) for source navigation and contracts.

Keep this repository usable independently of consuming games. Preserve type IDs, serializer-version compatibility, stream layout and container ownership. Do not put StoneDrop-specific schemas here; consumers provide their own serializers.

Current work is source-only documentation: the user said not to run anything yet. Do not run builds, tests, Unity, conversions or asset tools until authorized. The notes are static findings dated 2026-10-08, not verified runtime results. Preserve existing files and unrelated changes; commit this repository separately from any parent submodule pointer update.

Keep detailed documentation in `docs~/` so Unity excludes it from asset importing. Keep this root `AGENTS.md` discoverable; its generated `.md.meta` is ignored by Git. Do not move documentation back to an importable `docs/` folder.
