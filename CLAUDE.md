# ConcertBuddy (Telegram bot @concert_buddy_bot)

Notes for AI coding agents working in this repository. Personal or machine-specific notes go to `CLAUDE.local.md` (git-ignored).

## Build
- .NET 10 console app, solution `src/ConcertBuddy.sln`; target framework in `src/Directory.Build.props`, package versions only in `src/Directory.Packages.props`.
- `dotnet build src/ConcertBuddy.sln --configuration Release` (same as CI, CI does not run tests yet).

## Layout
- Bot commands: `src/ConcertBuddy.ConsoleApp/TelegramBot/Command/`; routing `TelegramBot/Handler/BotHandlers.cs`.
- External music services: `src/MusicSearcher/` (MusicBrainz, Last.fm, Setlist.fm, Yandex Music, LyricsScraperNET).
- Config keys: `Configuration.cs`, template `appsettings.template.json`. Real keys live only on the server (edited by hand): the repo `appsettings.json` keeps empty values, never put real keys in it.
- Version: `AssemblyVersion` in the ConsoleApp csproj. A release tag pushes the Docker image and deploys it over SSH (`.github/workflows/cicd.yml`).

## Tests
- xunit.v3 on Microsoft Testing Platform (`global.json`), FakeItEasy, Shouldly. How to run them: `docs/testing.md`.
- `ConcertBuddy.ConsoleApp.Tests` and `MusicSearcher.Tests`: no network, no keys, run in CI. `MusicSearcher.ContractTests` call the real services and are not part of CI.
- Mutation check for every fix or new behavior: break it, run the tests, at least one must fail, restore.
- New functionality must be covered by tests: assert whole objects and stored data, verify calls to external clients. Keep this repo's existing style.

## Conventions
- English only in code, comments, docs and commit messages.
- Branches `feature/<issue>_<desc>`, `bugfix/<issue>_<desc>`; pull requests target `master`; commit messages and PR titles start with the issue numbers, e.g. `#39 ...`.
- Follow existing patterns; do not silently change working configs (CI, Docker, lint, coverage).
- Trust code over README when they disagree.
- Never commit keys, tokens or passwords, also not in test configs: values for local runs stay in git-ignored files.
- Never open secrets: real `appsettings*.json`, `.env` values, certificates, keys, `doc/hosting/`, `doc/external_sites_credentials/`.
- Skip `bin/ obj/ .vs/ TestResults/ logs/` when searching.
