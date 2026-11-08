# Testing

xunit.v3 on Microsoft Testing Platform (`global.json`), FakeItEasy for fakes, Shouldly for asserts.

| Project | What | Network and keys | In CI |
|---|---|---|---|
| `src/ConcertBuddy.ConsoleApp.Tests` | Telegram helpers | none | yes |
| `src/MusicSearcher.Tests` | clients and converters, with fakes | none | yes |
| `src/MusicSearcher.ContractTests` | real MusicBrainz and Last.fm | network, Last.fm keys | no |

## Unit tests

From the repository root, with coverage as in CI:

```
dotnet test src/ConcertBuddy.ConsoleApp.Tests -- --coverage --coverage-output-format cobertura
dotnet test src/MusicSearcher.Tests -- --coverage --coverage-output-format cobertura
```

## Contract tests

They call the real services, so run them by hand when a client library changes:

```
dotnet test src/MusicSearcher.ContractTests
```

The MusicBrainz tests need no keys. The Last.fm tests are skipped without keys. Set them as user secrets:

```
dotnet user-secrets set LastFmApiKey "<key>" --project src/MusicSearcher.ContractTests
dotnet user-secrets set LastFmApiSecret "<secret>" --project src/MusicSearcher.ContractTests
```

or as the environment variables `CONCERTBUDDY_LastFmApiKey` and `CONCERTBUDDY_LastFmApiSecret`.

MusicBrainz allows about 1 request per second and may answer `503` when it is busy, so a single failed test can be a temporary problem of the service.

## Rules for new tests

- Assert whole objects and verify the calls to external clients.
- Mutation check for every fix or new behavior: break it, run the tests, at least one must fail, restore.
