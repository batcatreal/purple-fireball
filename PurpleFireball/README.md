# Purple Fireball (Gorilla Tag BepInEx mod)

Hold right grip to summon a purple fireball, release to throw it.

## Setup
1. Install BepInEx in your Gorilla Tag folder and launch the game once.
2. Open `PurpleFireball.csproj` and set `GameDir` to your Gorilla Tag install path.
3. Install the .NET SDK, plus the C# extension in VS Code.
4. In the VS Code terminal: `dotnet build -c Release`
5. Copy `bin/Release/net472/PurpleFireball.dll` into `Gorilla Tag/BepInEx/plugins`.
