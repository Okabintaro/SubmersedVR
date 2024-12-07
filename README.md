# SubmersedVR

> ⚠️ **Note:** This mod is a work in progress. Please do not expect a polished or bug-free experience.

SubmersedVR is a mod designed to modernize and enhance the VR support for [Subnautica](https://unknownworlds.com/subnautica/), making it a more immersive VR experience.

If you prefer the original VR mode and want to play with a traditional Xbox or PS4 controller, we recommend using the [Subnautica VR Enhancements Mod](https://github.com/IWhoI/SubnauticaVREnhancements) instead.

---

## Installation/Setup

### Prerequisites

1. **Subnautica Installation**: Install the latest version of Subnautica from Steam or Epic Games Store.
   - **Beta Branches (legacy/experimental)**: Not supported. Switch to the default branch.
   - **Microsoft Store Version**: Outdated and incompatible.
   - **Epic Games Version with AirLink**: Additional setup is required. Refer to [this issue comment](https://github.com/Okabintaro/SubmersedVR/issues/42#issuecomment-1605399270) for detailed instructions.
2. **BepInEx**: Download and set up [BepInEx for Subnautica](https://github.com/toebeann/BepInEx.Subnautica).
3. **Incompatibilities**: Ensure the following mods are NOT installed:
   - [Subnautica VR Enhancements Mod](https://github.com/IWhoI/SubnauticaVREnhancements)
   - [Motion Controls Mod](https://github.com/ihatetn931/SN1MC)

### Installation Steps

1. **Download the Mod**:
   - Get the latest release from [GitHub Releases](https://github.com/Okabintaro/SubmersedVR/releases).
   - **Important**: Download the file named `SubmersedVR_VERSION.zip`, NOT the source code.

2. **Extract Files**:
   - Extract the contents of the zip file into your Subnautica installation directory (e.g., `C:\SteamLibrary\steamapps\common\Subnautica`).
   - To find your game directory:
     - Steam: Right-click the game > **Properties** > **Local Files** > **Browse**.

3. **Important Notes**:
   - If files are not copied correctly, the mod might load partially, and your controllers may not function due to missing bindings.
   - ⚠️ **Mod Managers Warning**: Tools like Vortex may skip important steps and cause a broken installation.

4. **Launch the Game**:
   - Start Subnautica in SteamVR. **The Oculus Runtime is not supported.**
   - For Oculus Rift or Link users:
     - Add `-vrmode openvr` to the game's launch options. [Learn how to set launch options](https://help.steampowered.com/en/faqs/view/7D01-D2DD-D75E-2955).
     - Start SteamVR manually before launching the game.

---

## Development Setup

For developers looking to contribute or customize the mod, follow these steps:

### Prerequisites

- Install the [.NET SDK](https://dotnet.microsoft.com/download).

### Steps

1. Clone the repository and update the `SubnauticaDir` property in `SubmersedVR/SubmersedVR.csproj` to your Subnautica installation path.
2. Run the following commands:
   ```bash
   dotnet restore
   dotnet build
   ```
3. The mod will build and install automatically into the configured Subnautica directory.

### IDE Setup

#### VSCode
1. Install the C# Extension.
2. (Optional) Enable decompilation support for better debugging:
   - Add this setting to `omnisharp.json`:
     ```json
     {
       "RoslynExtensionsOptions": {
         "EnableDecompilationSupport": true
       }
     }
     ```

#### Visual Studio
- Open the solution file `SubmersedVR/SubmersedVR.csproj` and build the project.

---

## FAQ

### The game doesn't work. What could be wrong?

- Verify the following:
  - You are using the latest Subnautica version from Steam.
  - Beta branches (`legacy` or `experimental`) are NOT selected.
  - Mod managers like Vortex are not causing an incomplete installation.

- Known good setup as of 07/12/2024
  - [BepInEx for Subnautica v5.4.23-payload.2.3.0](https://github.com/toebeann/BepInEx.Subnautica/releases/tag/v5.4.23-payload.2.3.0)
  - AirLink or SteamVR is running with the `-vrmode openvr` launch option.
  - Launch the game from the SteamVR dashboard.

### I Found a Bug. Where Can I Report It?

If you encounter a bug, please report it on the [GitHub Issues page](https://github.com/Okabintaro/SubmersedVR/issues). To help us identify and resolve the issue efficiently, please include the following:

1. **Detailed Description**:
   - Describe the issue clearly. Include what you expected to happen and what actually happened.

2. **Steps to Reproduce**:
   - Provide a step-by-step guide on how to reproduce the issue.

3. **Logs**:
   - Attach the logs from your game. You can find the logs at:
     - **Steam**:  
       `C:\Program Files (x86)\Steam\steamapps\common\Subnautica\BepInEx\LogOutput.log`
       - (If your Steam library is located elsewhere, navigate to your Subnautica installation folder using **Properties > Local Files > Browse** and find the `LogOutput.log` file in the `BepInEx` folder.)
     - **Epic Games**:  
       Locate your Subnautica folder, then find `BepInEx\LogOutput.log`.

### What features are planned for the future?

- Check the [GitHub Milestones](https://github.com/Okabintaro/SubmersedVR/milestones?direction=asc&sort=title&state=open) for a roadmap.
- If you have a new idea, feel free to suggest it via the [Issues page](https://github.com/Okabintaro/SubmersedVR/issues).

### Will this work with Below Zero?

- Yes! A fork of this mod is available for Below Zero, maintained by @jbusfield.
  - Check out the [Below Zero fork releases](https://github.com/jbusfield/SubmersedVR/releases).

---

## Credits/Thanks

- **[ihatetn931](https://github.com/ihatetn931/SN1MC)**: Original developer of the SN1MC mod, which served as a foundation for this project.
- **[IWhoI](https://github.com/IWhoI)**: Creator of Subnautica VR Enhancements Mod, whose fixes and features inspired this mod.
- **[Raicuparta](https://github.com/Raicuparta)**: Developer of other VR mods whose work guided many improvements in SubmersedVR.

---

## License

This project is licensed under the MIT License. See the LICENSE file for more details.
