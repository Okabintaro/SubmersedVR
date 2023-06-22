# TODO/Notes

- [ ] Cleanup Code
    - [ ] Split up VRCameraRig.cs
        - There is too much unrelated stuff in there
    - See how much Code can be reused between games and put them in separate files

## Original

- https://github.com/users/Okabintaro/projects/1

## Below Zero Port

- [ ] Edges of View are blurry in menu
    - Wasn't the case before setting XRSettings.enabled to true in last commit
        - Double check and look at uses
- [ ] Subtitles too far down on HUD
- [ ] SeaTruck
    - [ ] Proper seat/player alignment
- [ ] Check all the new content
- [ ] Sometimes PDA rotation breaks

- [ ] Automatic Patching/Enabling of VR Mode
    - I basically used this: https://github.com/Raicuparta/unity-vr-patcher
    - Should maybe try to collaborate with old VR Mod Authors
        - https://github.com/ethanfischer/SubnauticaBelowZeroVR
        - https://www.nexusmods.com/subnauticabelowzero/mods/118
 [ ] Check if things are compatible with other mods, QMods?
    - Currently I just use Subnautica BepInEx, which works just as fine