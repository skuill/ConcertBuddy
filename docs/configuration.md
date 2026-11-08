# Configuration

The bot runs on the .NET Generic Host. Settings come, later ones win, from:

1. `appsettings.json`
2. `appsettings.<Environment>.json`, the environment name is taken from the variable `ASPNETCORE_ENVIRONMENT`
3. user secrets (only in the `Development` environment)
4. environment variables, with `__` instead of `:`, for example `Configuration__TelegramToken`
5. command line arguments, for example `--Configuration:TelegramToken=...`

Template with all settings: `src/ConcertBuddy.ConsoleApp/appsettings.template.json`.

## Section `Configuration`

| Setting | Required | Without it |
|---|---|---|
| `TelegramToken` | yes | the bot does not start and logs `Configuration:TelegramToken is required.` |
| `SetlistFmApiKey` | no | setlists are not found |
| `LastFmApiKey`, `LastFmApiSecret` | no | no biographies, no top tracks and no Top tracks button |
| `YandexToken` | no | tracks are sent without audio |

## Stopping

The bot stops cleanly on `SIGTERM` or Ctrl+C. It does not read the console, so a container needs neither `tty` nor `stdin_open`.
