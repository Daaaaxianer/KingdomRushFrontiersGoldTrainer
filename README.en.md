# KingdomRushFrontiersGoldTrainer

[简体中文](README.md) | English

Portable Windows x64 gold-income trainer for Steam Kingdom Rush Frontiers.

Download the EXE from Releases. Start the original game through Steam, enter a level, then connect the trainer. Select 1–10× using the slider or buttons. Applies to enemy kills and early-wave rewards. Reset keeps the connection. Disconnecting or closing restores normal future income. A heartbeat timeout restores 1× after approximately 2.5 seconds.

The UI and native bridge are adapted from Daaaaxianer/KingdomRushGoldTrainer. The embedded DLL executes a Lua reward hook in the running game. Existing local saves are backed up before connection.

Requires Windows x64 and .NET Framework 4.8. Supported executable and Lua runtime hashes are listed in compatibility.json.

Build with Python 3.9+ and MinGW-w64 x64 GCC using `python scripts/build.py --test --gcc path/to/gcc.exe`. Output is in dist/.

Build, UI scaling, controls and simulated-session tests pass. Live attachment and rewards inside an actual level remain unverified.

MIT licensed. Unofficial project. Kingdom Rush trademarks belong to their respective owners.
