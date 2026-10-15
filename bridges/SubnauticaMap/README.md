# SubnauticaMap VR bridge

Standalone BepInEx plugin that makes the [SubnauticaMap](https://www.nexusmods.com/subnautica/mods/12) map mod usable in VR: map panning (laser drag), zoom (right stick), note creation (trigger click), and a fix that keeps the mod's scan-range circle in the map plane. It requires the SubmersedVR main plugin and stays idle when the SubnauticaMap mod is not installed. No in-game options: installing the plugin enables it.

## Build

- .NET SDK (target net472). The BepInEx NuGet feed (see `NuGet.Config`) provides the BepInEx, Unity and game assemblies; `SteamVR.dll` is referenced from the main project's `SubmersedVR/SteamVR/` folder.
- `dotnet build SubnauticaMapBridge.csproj`
- Output: `bin/<Configuration>/net472/SubnauticaMapBridge.dll` (the PostBuild target also copies it to `SubmersedVR/build/BepInEx/plugins/`).

## Install

Copy `SubnauticaMapBridge.dll` to `<Subnautica>\BepInEx\plugins\`. Test log lines are prefixed `[diag]` in `<Subnautica>\LogOutput.log`.
