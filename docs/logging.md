# Logging

The bot logs with Serilog, settings are in the `Serilog` section of `appsettings.json`.

## One line per update

At the `Information` level every update gets one line:

```
Handled update: Message /search in Private chat: Found in 812 ms
```

It holds the update type, the command, the chat type, the result (`Found`, `NotFound`, `Error`) and the duration. The text of a user is not in this line: search texts are written only at the `Debug` level.

## Levels

| Source | Level |
|---|---|
| Default | `Information` |
| `Microsoft`, `System` | `Warning` |
| `Microsoft.Hosting.Lifetime` | `Information` |
| `LyricsScraperNET` | `Error` |
| `Polly` | `Warning`, set in the code: one line per retry |

## When Telegram is not available

The first failed attempt gets one line, `Warning` for a temporary problem and `Error` for any other. The next attempts are at the `Debug` level. When polling works again, one `Information` line says so. Between the attempts the bot waits `retry_after` if Telegram sent it, otherwise 1, 2, 4 and up to 60 seconds. An invalid token (`401`) stops the bot.
