# Screens and buttons

How the bot shows its answers. Code: `src/ConcertBuddy.ConsoleApp/TelegramBot/Screens/` and `TelegramBot/Callback/`.

## One message per flow

A flow keeps one message and replaces what it shows: a `Screen` is the HTML text, the buttons and an optional link preview above the text. A rich screen (`IsRich`) is a rich message: headings, paragraphs, tables, buttons inside the text, a footer, but no link preview. `ScreenSender` sends a screen as a new message or edits an existing one, also from plain to rich and back; "message is not modified" (the same screen again) is not an error.

| Screen | Opens from | What happens to the message |
|---|---|---|
| Start | `/start`, Menu | a new message on `/start`, edited from Menu: greeting, example artists, Help, About |
| Help | `/help`, an unknown command, Start, About | what the bot can do, Menu, About |
| About | `/about`, Start, Help | version, data sources, code, support links, Menu, Help |
| Searching | a text message, `/search <name>`, an example, Try again | a text message sends it, a button edits it |
| Artist list | the search, ⬅️ ➡️, Results (or Not this artist?) of the card | edited in place, page turns without "Searching…"; after nothing found, the list of the other keyboard layout with "Did you mean …?" |
| Nothing found | the search | tips and example artists, when the other keyboard layout finds nothing either |
| Service unavailable | the search, the artist card | "busy, try again", Try again repeats the same button; when MusicBrainz does not answer, the list and the card come from Last.fm, with listeners and a note in italics (`docs/external-services.md`) |
| Artist card | the list, a new search with one artist of exactly the typed name, Artist of the next screens | edited in place, the Last.fm page as the preview, a short biography, Setlists, Upcoming, Full biography, Top tracks, Results back to the list ("Not this artist?" when the search opened it at once) |
| Full biography | the card | rich, paragraphs and the Last.fm credit (CC BY-SA), Artist |
| Setlists | the card, Setlists of a setlist | ten per screen (setlist.fm gives twenty per request), 🔜 upcoming, 📭 no songs yet, disabled arrows and the page counter, Artist |
| Upcoming | the card, Upcoming of a setlist | the next ten concerts, soonest first, from the first setlist.fm page (its newest twenty; an artist with more concerts ahead may miss the soonest, All on setlist.fm shows them, #50 brings a real source), a note that fans add them and no tickets are sold, Artist |
| Setlist | the setlists, the upcoming concerts, a share link | rich: a table of songs with notes, sets and encores, a summary, the info of the fans and the setlist.fm link (its terms ask for it); Copy setlist, Send to a chat, Share link, Setlists (or Upcoming) |
| Shared setlist card | Send to a chat, in any chat | sent by the user through inline mode: the same table with the song names as text, Open in ConcertBuddy |
| Top tracks | the card | edited in place, the Last.fm top tracks as buttons, Artist |
| Track | a song of a setlist or of the top tracks | a **new** rich message, so the setlist stays: the audio player, Lyrics, Copy name, ❌ |
| Track lyrics | Lyrics, Hide lyrics of the track | the same message: the lyrics under the player, or without them |

A track message holds the audio itself: the bot downloads it and uploads it as "Artist - Song.mp3" (a link given to Telegram would keep the file name of the service). Lyrics and Hide lyrics rebuild the message with the file id and the performer of the audio block that comes with the button press, so they work after a restart without any memory. Without audio the message says "No audio" and still has the lyrics. When MusicBrainz is busy, Play sends a busy message with Try again, and the busy message is deleted when the track is sent.

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
| `t` | setlist id, MBID, screen of the setlists to go back to (0: back to the upcoming concerts) | `SetlistCommand` |
| `u` | MBID | `UpcomingCommand` |
| `k` | setlist id | `SharedSetlistCallbackCommand`, Try again of a share link |
| `o` | MBID | `TopTracksCommand` |
| `p` | MBID, song | `PlayCommand`, a new track message |
| `q` | MBID, song | `PlayCommand` from the busy message, which it deletes |
| `y`, `h` | MBID, song | `LyricsCommand`, show or hide the lyrics |
| `m`, `w`, `i` | none | `InfoCallbackCommand`: Start (menu), Help, About |

Data that starts with `/` belongs to the old commands; only `/delete` is left (the ❌ button). A button the bot does not know (any other old command, a stored id that is gone, wrong arguments) answers "This button is outdated" as an alert.

## Sharing a setlist

- **Send to a chat** is a `switch_inline_query` button with `s_<setlist id>`: Telegram asks for a chat and types `@bot s_<id>` there, the bot answers the inline query (`ShareCommand`) with one article, the setlist card. The song names are text: a callback button in someone else's chat would come back without a message the bot can edit. The card has Open in ConcertBuddy, a link to the bot. Other inline queries get no results for now (search by name: #14).
- **Share link** opens Telegram's share dialog with `https://t.me/<bot>?start=s_<setlist id>` and a caption. The bot username comes from `getMe` on start (`BotIdentity`): the test and the production bot differ.
- **Opening a link** sends `/start s_<setlist id>`: the bot answers "Opening the shared setlist…" and edits it into the setlist with all its buttons (`SharedSetlistCommand`); Setlists leads to the first screen of the artist's setlists.
- Inline mode must be on in BotFather (`/setinline`) for each bot, otherwise Telegram does not send the inline queries. The bot asks for inline queries in every `getUpdates` (`allowed_updates`), because Telegram keeps the list of the last request that set one.

## Commands and the bot profile

A message that starts with `/` is a command: `/start`, `/help`, `/about`, `/search <name>` (also as `/search@bot` in groups); `/search` without a name gets a hint, any other command the help. Any other text is an artist search.

On every start `BotProfile` sets the command menu (for private chats and for all groups) and the descriptions ("What can this bot do?" and the short one of the profile) from code, so they always match what the bot can do. What is set in BotFather by hand is overwritten; BotFather keeps the name, the photo and inline mode. If Telegram rejects them, one Warning is logged and the bot works on.

## The latest tap wins

Updates are handled in parallel, and a tap that waits for MusicBrainz retries can end seconds after a newer tap on the same message. So a screen command takes the next version of its message (`ScreenVersions`, by chat and message id) before it does anything, and edits the message only while that version is still the latest: an older tap that ends later drops its screen, and the message keeps the newer one (#92). A message is kept in `ScreenVersions` only while a tap on it is in progress. New messages (a search, a track) and other messages are not affected.

The older tap still does its work (the music services have no cancellation here); only its screen is dropped.

## A new search

A new search (a text message, `/search`, an example, Try again) does more than a page turn or the way back from a card (#94):

- **Confident match:** when exactly one artist of the first page has the typed name (letters and digits, any case: "metallica", "METALLICA!"), the card opens at once in the search message. Its last button is "🔄 Not this artist?", which shows the list. Two artists with the name ("Nirvana") keep the list.
- **The other keyboard layout:** when nothing is found, the bot searches the text as typed on the other layout (`KeyboardLayout`: "ьуефддшсф" is "metallica", "rbyj" is "кино"). If that finds artists, the list says "Nothing found for X. Did you mean Y?" and its buttons go on with Y; no card opens at once, because the user did not type Y. Otherwise the usual "Nothing found".

## Loading

A screen command that is not ready within a second (`ScreenEdit.LoadingDelay`) puts "⏳ Loading…" without buttons into the message, then the screen. A fast screen (the cache, a page turn) does not blink, and without buttons a waiting user does not tap again. The lyrics of a track are the exception: the message keeps its player and the toast says that the lyrics are being looked for.

## Answering buttons

Every button press is answered, otherwise the button keeps spinning. Screen commands answer at once, before the music services are asked, and set `AnsweredCallback`; slow ones (a track, the lyrics) answer with a toast ("⏳ Loading One…") and the track also sends the "sending a file" chat action; for the other commands `BotHandlers` answers after the command, also when it failed.

## Results

A screen command reports `Found`, `NotFound`, `Unavailable`, `Outdated` or `Superseded` (a newer tap on the message took over, see above) for the log line of the update (`docs/logging.md`). A music service that did not answer is `Unavailable`, never "nothing found".
