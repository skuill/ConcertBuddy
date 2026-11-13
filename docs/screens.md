# Screens and buttons

How the bot shows its answers. Code: `src/ConcertBuddy.ConsoleApp/TelegramBot/Screens/` and `TelegramBot/Callback/`.

## One message per flow

A flow keeps one message and replaces what it shows: a `Screen` is the HTML text, the buttons and an optional link preview above the text. A rich screen (`IsRich`) is a rich message: headings, paragraphs, tables, buttons inside the text, a footer, but no link preview. `ScreenSender` sends a screen as a new message or edits an existing one, also from plain to rich and back; "message is not modified" (the same screen again) is not an error.

| Screen | Opens from | What happens to the message |
|---|---|---|
| Searching | a text message, an example, Try again | a text message sends it, a button edits it |
| Artist list | the search, ⬅️ ➡️, Results of the card | edited in place, page turns without "Searching…" |
| Nothing found | the search | tips and example artists |
| Service unavailable | the search, the artist card | "busy, try again", Try again repeats the same button |
| Artist card | the list, Artist of the next screens | edited in place, the Last.fm page as the preview, a short biography, Setlists, Full biography, Results back to the list |
| Full biography | the card | rich, paragraphs and the Last.fm credit (CC BY-SA), Artist |
| Setlists | the card, Setlists of a setlist | ten per screen (setlist.fm gives twenty per request), 🔜 upcoming, 📭 no songs yet, disabled arrows and the page counter, Artist |
| Setlist | the setlists | rich: a table of songs with notes, sets and encores, a summary, the info of the fans and the setlist.fm link (its terms ask for it); Copy setlist, Setlists |

The song names in a setlist and Top tracks are still the old `/track` and `/top` commands that send new messages; they become screens next (#48).

Limits found on the dev bot: the text of a copy button is at most 256 UTF-8 bytes, not characters (`BUTTON_DATA_INVALID` otherwise), so the setlist copy keeps whole lines and ends with "…".

## Button data

Telegram allows 64 bytes of callback data. `CallbackCodec` writes `action|arg|arg`:

- A payload of up to 64 bytes (UTF-8) goes into the button itself, so the button still works after a restart or a release. An MBID is 36 bytes and fits.
- A longer payload (a long search text) stays in memory for a day and the button holds `~` and a number. After a restart such a button is outdated.
- Only the last argument may contain `|`; put free text last.

| Action | Arguments | Command |
|---|---|---|
| `s` | offset, text | `SearchCallbackCommand`, a page of the search |
| `n` | offset, text | `SearchCallbackCommand`, a new search with "Searching…" first |
| `a` | MBID, offset, text of the search (empty without a search) | `ArtistCommand` |
| `b` | MBID | `BiographyCommand` |
| `l` | MBID, screen from 1 | `SetlistsCommand` |
| `t` | setlist id, MBID, screen of the setlists to go back to | `SetlistCommand` |

Data that starts with `/` belongs to the old commands (`/top`, `/track`, `/lyric`, `/delete`). A button the bot does not know (an old `/search`, `/artist`, `/biography`, `/setlists` or `/setlist`, a stored id that is gone, wrong arguments) answers "This button is outdated" as an alert.

## Answering buttons

Every button press is answered, otherwise the button keeps spinning. Screen commands answer at once, before the music services are asked, and set `AnsweredCallback`; for the other commands `BotHandlers` answers after the command, also when it failed.

## Results

A screen command reports `Found`, `NotFound`, `Unavailable` or `Outdated` for the log line of the update (`docs/logging.md`). A music service that did not answer is `Unavailable`, never "nothing found".
