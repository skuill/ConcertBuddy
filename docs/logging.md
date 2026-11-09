# Logging

The bot logs with Serilog, settings are in the `Serilog` section of `appsettings.json`. It writes to the console only: in a container Docker keeps the log (`docker logs CONCERT_BUDDY`) and rotates it by the `logging` options of the compose file. A local `appsettings.Development.json` may add the `File` sink.

## What the bot logs

At the `Information` level every update gets one line:

```
Handled update: Message /search in Private chat from user 3f9a1c0b7d2e4a51: Found in 812 ms
```

It holds the update type, the command, the chat type, a pseudonymous user key, the result (`Found`, `NotFound`, `Error`) and the duration.

- What the user typed is not in this line. Search texts, artist names and song names are written at the `Debug` level only.
- The Telegram user id is never logged. The user key is the start of HMAC-SHA256 of the id with the secret `Configuration:UserKeySecret`: the same user always gets the same key, and without the secret the key cannot be traced back to the id. Without the secret the line shows `-`.
- Changing the secret gives every user a new key, so counting starts anew.

## Counting users

Unique users of the last day and of the last 30 days, on the server:

```
docker logs CONCERT_BUDDY --since 24h 2>&1 | grep -o 'from user [0-9a-f]*' | sort -u | wc -l
docker logs CONCERT_BUDDY --since 720h 2>&1 | grep -o 'from user [0-9a-f]*' | sort -u | wc -l
```

The count covers what Docker still keeps (the compose file keeps up to 20 files of 10 MB).

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
