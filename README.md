SubmersedVR
===========

**NOTE: This mod is still work in progress. Please don't expect a polished or bug-free experience.**

A mod aiming to make [Subnautica] an immersive VR game by modernizing its VR Support.

If you want to play the game in its original VR Mode with a traditional Xbox or PS4 Controller, I recommend using the [Subnautica VR Enhancements Mod] instead.

Installation/Setup
-------------------

- Make sure you have [BepInEx for Subnautica](https://github.com/toebeann/BepInEx.Subnautica) installed and setup.
- Also make sure you don't have [VREnhancements][Subnautica VR Enhancements Mod] or the old [Motion Controls Mod][SN1MC] installed, since they aren't compatible.
- Download the latest version of the mod from [Github Releases](https://github.com/Okabintaro/SubmersedVR/releases).
    Make sure to download the zip filed called `SubmersedVR_VERSION.zip` and not the Source Code.
- Copy the whole contents of the zip file into your Subnautica folder e.g. `C:\SteamLibrary\steamapps\common\Subnautica`
    - You can find the location of your game in steam by right-clicking it, then -> Properties -> Local Files -> Browse.
- Start the game within SteamVR. **The Oculus Runtime won't work and is not supported!**
    - For OculusRift or Oculus Link Users: You can force the SteamVR runtime by adding `-vrmode openvr` [to the advanced launch options of the Subnautica game](https://help.steampowered.com/en/faqs/view/7D01-D2DD-D75E-2955). Please make sure you did that before launching the game.

Development Setup
-----------------

- Install the [.net SDK](https://dotnet.microsoft.com/download)
- Change the `SubnauticaDir` Property in `SubmersedVR/SubmersedVR.csproj` to the Subnautica Installation
- Run `dotnet restore` to fetch all dependencies and setup the project
- Run `dotnet build` to build and install the mod

### For VSCode

- Install C# Extension
- (Tip/Optional) [Enable Decompilation support](https://github.com/OmniSharp/omnisharp-roslyn/pull/1751):
    Set `RoslynExtensionsOptions:EnableDecompilationSupport` to true in `omnisharp.json`.

### For Visual Studio

- You should be able to simply open up the `SubmersedVR/SubmersedVR.csproj`.

FAQ
---

#### I found a bug, where do I report it?

Please report it on the [Github Issues](https://github.com/Okabintaro/SubmersedVR/issues).

#### What features are planed for the future? I have an idea!

We want to keep track of future releases and features/ideas on [Github Issues](https://github.com/Okabintaro/SubmersedVR/issues) aswell.
For a rough roadmap, take a look at the [Milestones](https://github.com/Okabintaro/SubmersedVR/milestones?direction=asc&sort=title&state=open).
Feel free to suggest changes if it's not already reported yet.

#### Will this work with Below Zero?

No. At least not yet. We might see how hard it is to port it to Below Zero, but it's not a priority right now.

Credits/Thanks
--------------

This mod began as modifications and fixes for the [SN1MC] Mod from [ihatetn931] and later was mostly rewritten and cleaned up for this project.
Lots of thanks to him opening up the source code for me to get into VR modding.

Furthermore, there are some fixes and improvements cherry-picked from the [Subnautica VR Enhancements Mod] by [IWhoI].

Another thank goes to the great VR modding work from [Raicuparta].
I looked at their mods code quite often when I was stuck and probably will again in the future.

License
-------

The code of the project is licensed under the MIT License. See LICENSE.

[SN1MC]: https://github.com/ihatetn931/SN1MC
[ihatetn931]: https://github.com/ihatetn931/SN1MC
[Subnautica]: https://unknownworlds.com/subnautica/
[Subnautica VR Enhancements Mod]: https://github.com/IWhoI/SubnauticaVREnhancements
[IWhoI]: https://github.com/IWhoI
[Raicuparta]: https://github.com/Raicuparta
