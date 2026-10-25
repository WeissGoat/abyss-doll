# Play the newest P3 build from day 1 to the end of the Demo

You are a playtester. Drive the newest Windows build with `python tools/agent/p3cmd.py` (start with `launch`, then
`help` for the command list; every command is one line and answers with a text report). Play it like a player would,
and report where the flow breaks, stalls or is unclear. Do not change any file in the repository.

## What to do

1. `launch`, start a new game from the title, and play the prologue (`PlayPrologue` is fine).
2. Play the town / dive loop day by day as a careful but progress-minded player:
   - before a dive, read what `Describe` says about money, rent, gear, Zero's state and which layers are open;
   - dive (`RunDive`, or step it yourself), decide when to retreat and when to go deeper, take sensible loot;
   - at home, sell, repair, pay rent, upgrade the chassis or buy gear when you can afford it, answer Zero's lines
     (`ResolveDayDialogs`, `Reply`, `Talk`), and go to the next day.
   - Do NOT use `AddMoney`, `AddItem`, `Heal`, `EasyFights` or `PrepareDemoEnding` in this phase.
3. Goal: beat the layer-3 boss (the mycelium oracle) and reach the end of the Demo. After it falls you should see, in
   order: a 3-panel comic at the stairs, Zero's song line at the door, a special night talk, then a card
   「第一章 · 完」 with buttons 继续经营 / 回到标题. Note each one as you see it.
4. If by in-game day 30, or after 90 minutes of real time, you have not beaten the layer-3 boss, stop playing
   normally, write down how far you got and why (money, gear, HP, which boss stopped you), then call
   `PrepareDemoEnding` and finish the ending sequence so it is checked anyway. Say clearly in the report that you did.
5. Boss intros: the layer-2 boss (spore foundry) and the layer-3 boss (mycelium oracle) should each play a short
   full-screen video before the fight. When a boss fight starts, take one `screenshot` at 640 width (absolute path
   under `tmp/agent_reports/demo_playthrough/`) and say whether a video was showing.
6. Take one screenshot (640) of each ending beat: the comic, the end card.
7. `quit` the build before you answer. Then read the build log
   (`%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project P3\Player.log`) for errors and exceptions.

## Report

- First line: `VERDICT: PASS` if you reached the end card and nothing blocked the flow, otherwise `VERDICT: FAIL`.
- A day-by-day table: day, what you did, money before / after, rent status, deepest layer, result of the dive.
- Every place where you were stuck, confused, or had to guess what a button or number meant (quote the screen text).
- Whether the boss intro videos and the ending beats appeared, with the screenshot paths.
- Errors and exceptions from the log (first line of each, count of repeats).
- Whether you had to use `PrepareDemoEnding`, and if so how far normal play got.
