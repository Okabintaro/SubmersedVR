
using HarmonyLib;
using UnityEngine;
using System.Collections;
using System.Linq;
using RootMotion.FinalIK;
using System;
using System.Globalization;



namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System.Collections.Generic;


    public struct TransformOffset
    {
        public TransformOffset(Vector3 pos, Vector3 angles)
        {
            Pos = pos;
            Angles = angles;
        }

        public TransformOffset(Transform transform) : this()
        {
            Pos = transform.localPosition;
            Angles = transform.localEulerAngles;
        }
        public TransformOffset(Transform transform, Vector3 pos, Vector3 angles) : this()
        {
            Pos = transform.localPosition - pos;
            Angles = transform.localEulerAngles - angles;
        }

        public Vector3 Pos { get; }
        public Vector3 Angles { get; }
        public override string ToString() => $"TransformOffset(Pos=({Pos.x:f3}, {Pos.y:f3}, {Pos.z:f3}), Angles=({Angles.x:f3}, {Angles.y:f3}, {Angles.z:f3}))";
        internal string SwitchString(string type)
        {
            FormattableString str = $"case {type} _: return new TransformOffset(new Vector3({Pos.x:f3}f, {Pos.y:f3}f, {Pos.z:f3}f), new Vector3({Angles.x:f3}f, {Angles.y:f3}f, {Angles.z:f3}f));";
            return str.ToString(CultureInfo.InvariantCulture);
        }

        public void Apply(Transform tf)
        {
            tf.localPosition = Pos;
            tf.localEulerAngles = Angles;
        }

    }

    // Define all the Offsets
    static class HandOffsets
    {
        // Default Offset when no tool is Equipped
        //public static TransformOffset RightHand = new TransformOffset(new Vector3(0.05f, 0.06f, -0.17f), new Vector3(40.0f, 175.0f, 270.0f));
        //public static TransformOffset LeftHand = new TransformOffset(new Vector3(-0.05f, 0.06f, -0.17f), new Vector3(-40.0f, 0.0f, 90.0f));
        public static TransformOffset RightHand =  new TransformOffset(new Vector3(0.008f, 0.048f, -0.153f), new Vector3(36.736f, 204.358f, 281.731f));
        public static TransformOffset LeftHand =  new TransformOffset(new Vector3(-0.008f, 0.048f, -0.153f), new Vector3(-36.736f, -22.358f, 101.731f));
        public static TransformOffset PDA = new TransformOffset(new Vector3(-0.04f, -0.01f, -0.18f), new Vector3(312.0f, 339.0f, 118.0f));

        internal static TransformOffset GetHandOffset(this PlayerTool tool)
        {
            switch (tool)
            {
                case FireExtinguisher _: return new TransformOffset(new Vector3(0.062f, 0.077f, -0.148f), new Vector3(40.736f, 139.849f, 249.888f));
                case Seaglide _: return new TransformOffset(new Vector3(0.055f, -0.008f, -0.170f), new Vector3(56.208f, 190.815f, 300.073f));
                case Gravsphere _: return new TransformOffset(new Vector3(0.000f, 0.026f, -0.187f), new Vector3(39.548f, 161.521f, 241.912f));
                case DeployableStorage _: return new TransformOffset(new Vector3(0.000f, -0.025f, -0.173f), new Vector3(63.504f, 123.350f, 192.938f));
                case Constructor _: return new TransformOffset(new Vector3(0.011f, 0.016f, -0.175f), new Vector3(46.927f, 159.837f, 244.111f));
                case LEDLight _: return new TransformOffset(new Vector3(-0.014f, 0.052f, -0.174f), new Vector3(25.686f, 159.462f, 230.006f));
                //case Knife _: return new TransformOffset(new Vector3(0.008f, 0.095f, -0.115f), new Vector3(17.193f, 162.033f, 250.308f));
                case HeatBlade _: return new TransformOffset(new Vector3(-0.006f, 0.053f, -0.144f), new Vector3(22.605f, 169.347f, 253.672f));
                case Knife _: return new TransformOffset(new Vector3(-0.006f, 0.053f, -0.144f), new Vector3(22.605f, 169.347f, 253.672f));
                case FlashLight _: return new TransformOffset(new Vector3(-0.001f, 0.056f, -0.205f), new Vector3(42.308f, 176.628f, 250.467f));
                case Beacon _: return new TransformOffset(new Vector3(-0.007f, 0.048f, -0.208f), new Vector3(53.772f, 166.540f, 253.121f));
                case StasisRifle _: return new TransformOffset(new Vector3(0.013f, 0.091f, -0.155f), new Vector3(32.203f, 147.266f, 237.102f));
                case PropulsionCannonWeapon _: return new TransformOffset(new Vector3(-0.009f, 0.064f, -0.150f), new Vector3(13.698f, 168.461f, 244.767f));
                case BuilderTool _: return new TransformOffset(new Vector3(0.025f, 0.012f, -0.198f), new Vector3(53.604f, 163.256f, 247.458f)); 
                case AirBladder _: return new TransformOffset(new Vector3(-0.011f, 0.032f, -0.175f), new Vector3(35.419f, 161.485f, 227.887f));
                case Welder _: return new TransformOffset(new Vector3(0.005f, 0.049f, -0.187f), new Vector3(36.966f, 165.995f, 252.229f));
                case ScannerTool _: return new TransformOffset(new Vector3(0.000f, 0.022f, -0.202f), new Vector3(52.459f, 153.129f, 231.355f));
                //case ScannerTool _: return new TransformOffset(new Vector3(0.003f, 0.030f, -0.212f), new Vector3(46.895f, 169.689f, 261.732f));
                case LaserCutter _: return new TransformOffset(new Vector3(0.006f, 0.051f, -0.178f), new Vector3(37.351f, 166.002f, 252.017f));
                case Flare _: return new TransformOffset(new Vector3(0.005f, 0.035f, -0.176f), new Vector3(30.936f, 159.593f, 233.597f));
                case RepulsionCannon _: return new TransformOffset(new Vector3(-0.002f, 0.088f, -0.166f), new Vector3(33.777f, 149.093f, 232.610f));
                case SpyPenguinRemote _: return new TransformOffset(new Vector3(0.040f, 0.041f, -0.181f), new Vector3(36.275f, 155.571f, 251.696f));
                case SpyPenguinPlayerTool _: return new TransformOffset(new Vector3(0.030f, 0.023f, -0.193f), new Vector3(47.005f, 196.721f, 289.403f));
                case DiveReel _: return new TransformOffset(new Vector3(0.008f, 0.020f, -0.167f), new Vector3(46.825f, 153.870f, 230.479f));
                case TeleportationTool _: return new TransformOffset(new Vector3(0.011f, 0.013f, -0.199f), new Vector3(60.300f, 173.113f, 254.254f));
                case MetalDetector _: return new TransformOffset(new Vector3(0.000f, 0.042f, -0.170f), new Vector3(46.441f, 174.227f, 249.639f));
                case CreatureTool _: return new TransformOffset(new Vector3(0.003f, 0.007f, -0.179f), new Vector3(51.316f, 171.030f, 244.545f));
                case DropTool _: return new TransformOffset(new Vector3(0.020f, 0.038f, -0.172f), new Vector3(32.201f, 167.764f, 268.554f));
                default: return RightHand;
            };
        }

        internal static TransformOffset GetAimOffset(this PlayerTool tool)
        {
            switch (tool)
            {
                //case BuilderTool _: return new TransformOffset(new Vector3(-0.021f, -0.040f, 0.030f), new Vector3(73.312f, 2.306f, 323.270f));
                //case ScannerTool _: return new TransformOffset(new Vector3(0.003f, 0.030f, -0.212f) + new Vector3(-0.014f, -0.031f, 0.008f), new Vector3(1.183f, -13.063f, -28.934f) - new Vector3(46.895f, 169.689f, 261.732f) );
                case FireExtinguisher _: return new TransformOffset(new Vector3(0.003f, -0.101f, -0.054f), new Vector3(64.181f, 10.062f, 346.755f));
                //case Seaglide _: return new TransformOffset(new Vector3(0.003f, -0.101f, -0.054f), new Vector3(64.181f, 10.062f, 346.755f));
                case RepulsionCannon _: return new TransformOffset(new Vector3(-0.014f, -0.083f, 0.024f), new Vector3(13.596f, 352.247f, 315.238f));
                //case PropulsionCannonWeapon _: return new TransformOffset(new Vector3(-0.014f, -0.083f, 0.024f), new Vector3(13.596f, 352.247f, 315.238f));
                case StasisRifle _: return new TransformOffset(new Vector3(-0.037f, -0.073f, 0.039f), new Vector3(44.380f, 349.016f, 309.364f));
                //case FlashLight _: return new TransformOffset(new Vector3(-0.029f, -0.024f, 0.074f), new Vector3(74.684f, 27.358f, 341.475f));
                //case DiveReel _: return new TransformOffset(new Vector3(0.020f, -0.105f, -0.084f), new Vector3(80.062f, 36.462f, 346.389f));
                //case Welder _: return new TransformOffset(new Vector3(-0.028f, -0.017f, 0.056f), new Vector3(59.356f, 350.101f, 303.392f));
                //case LaserCutter _: return new TransformOffset(new Vector3(-0.014f, -0.042f, 0.068f), new Vector3(60.953f, 4.267f, 316.030f));
                //case HeatBlade _: return new TransformOffset(new Vector3(-0.016f, 0.033f, 0.004f), new Vector3(65.781f, 19.633f, 267.986f));
                //case LEDLight _: return new TransformOffset(new Vector3(-0.021f, 0.005f, -0.008f), new Vector3(63.596f, 40.279f, 283.758f));
                default: return VRCameraRig.DefaultTargetTransform;
            };
        }
    }


    class VRHands : MonoBehaviour
    {
        public enum HandSkeletonBone: int
        {
            eBone_Root = 0,
            eBone_Wrist,
            eBone_Thumb0,
            eBone_Thumb1,
            eBone_Thumb2,
            eBone_Thumb3,
            eBone_IndexFinger0,
            eBone_IndexFinger1,
            eBone_IndexFinger2,
            eBone_IndexFinger3,
            eBone_IndexFinger4,
            eBone_MiddleFinger0,
            eBone_MiddleFinger1,
            eBone_MiddleFinger2,
            eBone_MiddleFinger3,
            eBone_MiddleFinger4,
            eBone_RingFinger0,
            eBone_RingFinger1,
            eBone_RingFinger2,
            eBone_RingFinger3,
            eBone_RingFinger4,
            eBone_PinkyFinger0,
            eBone_PinkyFinger1,
            eBone_PinkyFinger2,
            eBone_PinkyFinger3,
            eBone_PinkyFinger4,
            eBone_Aux_Thumb,
            eBone_Aux_IndexFinger,
            eBone_Aux_MiddleFinger,
            eBone_Aux_RingFinger,
            eBone_Aux_PinkyFinger,
            eBone_Count
        };  

        public FullBodyBipedIK ik = null;

        public Transform leftTarget;
        public Transform rightTarget;

        public Transform leftHand;
        public Transform rightHand;
        public Transform origLeftHand;
        public Transform origRightHand;
        public Transform leftElbow;
        public Transform rightElbow;

        public Transform[] leftHandFingers;
        public Transform[] rightHandFingers;
        public Vector3[] minRotation;
        public Vector3[] maxRotation;
        public int currentEditFinger = (int)HandSkeletonBone.eBone_IndexFinger1;

        public static VRHands instance;

        // private OffsetCalibrationTool calibrationTool;

        public void Setup(FullBodyBipedIK ik)
        {
            instance = this;
            this.ik = ik;
 
            Settings.FullBodyChanged -= OnFullBodyChanged;
            Settings.FullBodyChanged += OnFullBodyChanged;

            var camRig = VRCameraRig.instance;
            leftTarget = camRig.leftHandTarget.transform;
            rightTarget = camRig.rightHandTarget.transform;

            leftHand = ik.solver.leftHandEffector.bone;
            rightHand = ik.solver.rightHandEffector.bone;
            origLeftHand = ik.solver.leftHandEffector.bone;
            origRightHand = ik.solver.rightHandEffector.bone;
            leftElbow = leftHand.parent;
            rightElbow = rightHand.parent;
 
            SetHandParents(ik.enabled);

            ResetHandTargets();

            UpdateBody();

            SetupFingers();

            /*
            var calibrationTool = new OffsetCalibrationTool(rightTarget, SteamVR_Actions.subnautica_MoveDown, SteamVR_Actions.subnautica_AltTool);
            calibrationTool.enabled = Settings.IsDebugEnabled;
            Settings.IsDebugChanged += (enabled) =>
            {
                 calibrationTool.enabled = enabled;
            };
            */
        }

        public void SetCurrentEditFinger(int val)
        {
            currentEditFinger = val;
        }

        public static string AdjustPDA(float pX, float pY, float pZ, float aX, float aY, float aZ)
        {
            HandOffsets.PDA = new TransformOffset(new Vector3(HandOffsets.PDA.Pos.x + (pX/1000), HandOffsets.PDA.Pos.y + (pY/1000), HandOffsets.PDA.Pos.z + (pZ/1000)), new Vector3(HandOffsets.PDA.Angles.x + aX,HandOffsets.PDA.Angles.y + aY,HandOffsets.PDA.Angles.z + aZ));
            HandOffsets.PDA.Apply(VRHands.instance.leftTarget);
            return $"AdjustPDA\npX={HandOffsets.PDA.Pos.x.ToString("0.000")}\npY={HandOffsets.PDA.Pos.y.ToString("0.000")}\npZ={HandOffsets.PDA.Pos.z.ToString("0.000")}\naX={HandOffsets.PDA.Angles.x}\naY={HandOffsets.PDA.Angles.y}\naZ={HandOffsets.PDA.Angles.z}";
        }

        public static string AdjustFinger(float x, float y, float z)
        {
            Vector3[] rotation = VRHands.instance.maxRotation;
            rotation[VRHands.instance.currentEditFinger] += new Vector3(x, y, z);
            return $"AdjustFinger\nindex={VRHands.instance.currentEditFinger} x={rotation[VRHands.instance.currentEditFinger].x} y={rotation[VRHands.instance.currentEditFinger].y} z={rotation[VRHands.instance.currentEditFinger].z}";
        }
        public void SetupFingers()
        {     
            string[] boneNamesLeft = new string[(int)HandSkeletonBone.eBone_Count];
            boneNamesLeft[(int)HandSkeletonBone.eBone_Thumb1] = "/hand_L_thumb_base";
            boneNamesLeft[(int)HandSkeletonBone.eBone_Thumb2] = "/hand_L_thumb_base/hand_L_thumb_mid";
            boneNamesLeft[(int)HandSkeletonBone.eBone_Thumb3] = "/hand_L_thumb_base/hand_L_thumb_mid/hand_L_thumb_tip";
            boneNamesLeft[(int)HandSkeletonBone.eBone_IndexFinger1] = "/hand_L_point_base";
            boneNamesLeft[(int)HandSkeletonBone.eBone_IndexFinger2] = "/hand_L_point_base/hand_L_point_mid";
            boneNamesLeft[(int)HandSkeletonBone.eBone_IndexFinger3] = "/hand_L_point_base/hand_L_point_mid/hand_L_point_tip";
            boneNamesLeft[(int)HandSkeletonBone.eBone_MiddleFinger1] = "/hand_L_midl_base";
            boneNamesLeft[(int)HandSkeletonBone.eBone_MiddleFinger2] = "/hand_L_midl_base/hand_L_midl_mid";
            boneNamesLeft[(int)HandSkeletonBone.eBone_MiddleFinger3] = "/hand_L_midl_base/hand_L_midl_mid/hand_L_midl_tip";
            boneNamesLeft[(int)HandSkeletonBone.eBone_RingFinger1] = "/hand_L_ring_base";
            boneNamesLeft[(int)HandSkeletonBone.eBone_RingFinger2] = "/hand_L_ring_base/hand_L_ring_mid";
            boneNamesLeft[(int)HandSkeletonBone.eBone_RingFinger3] = "/hand_L_ring_base/hand_L_ring_mid/hand_L_ring_tip";
            boneNamesLeft[(int)HandSkeletonBone.eBone_PinkyFinger1] = "/hand_L_pinky_base";
            boneNamesLeft[(int)HandSkeletonBone.eBone_PinkyFinger2] = "/hand_L_pinky_base/hand_L_pinky_mid";
            boneNamesLeft[(int)HandSkeletonBone.eBone_PinkyFinger3] = "/hand_L_pinky_base/hand_L_pinky_mid/hand_L_pinky_tip";

            string[] boneNamesRight = new string[(int)HandSkeletonBone.eBone_Count];
            boneNamesRight[(int)HandSkeletonBone.eBone_Thumb1] = "/hand_R_thumb_base";
            boneNamesRight[(int)HandSkeletonBone.eBone_Thumb2] = "/hand_R_thumb_base/hand_R_thumb_mid";
            boneNamesRight[(int)HandSkeletonBone.eBone_Thumb3] = "/hand_R_thumb_base/hand_R_thumb_mid/hand_R_thumb_tip_rig";
            boneNamesRight[(int)HandSkeletonBone.eBone_IndexFinger1] = "/hand_R_point_base";
            boneNamesRight[(int)HandSkeletonBone.eBone_IndexFinger2] = "/hand_R_point_base/hand_R_point_mid";
            boneNamesRight[(int)HandSkeletonBone.eBone_IndexFinger3] = "/hand_R_point_base/hand_R_point_mid/hand_R_point_tip_rig";
            boneNamesRight[(int)HandSkeletonBone.eBone_MiddleFinger1] = "/hand_R_midl_base";
            boneNamesRight[(int)HandSkeletonBone.eBone_MiddleFinger2] = "/hand_R_midl_base/hand_R_midl_mid";
            boneNamesRight[(int)HandSkeletonBone.eBone_MiddleFinger3] = "/hand_R_midl_base/hand_R_midl_mid/hand_R_midl_tip_rig";
            boneNamesRight[(int)HandSkeletonBone.eBone_RingFinger1] = "/hand_R_ring_base";
            boneNamesRight[(int)HandSkeletonBone.eBone_RingFinger2] = "/hand_R_ring_base/hand_R_ring_mid";
            boneNamesRight[(int)HandSkeletonBone.eBone_RingFinger3] = "/hand_R_ring_base/hand_R_ring_mid/hand_R_ring_tip_rig";
            boneNamesRight[(int)HandSkeletonBone.eBone_PinkyFinger1] = "/hand_R_pinky_base";
            boneNamesRight[(int)HandSkeletonBone.eBone_PinkyFinger2] = "/hand_R_pinky_base/hand_R_pinky_mid";
            boneNamesRight[(int)HandSkeletonBone.eBone_PinkyFinger3] = "/hand_R_pinky_base/hand_R_pinky_mid/hand_R_pinky_tip_rig";

            minRotation = new Vector3[(int)HandSkeletonBone.eBone_Count];
            for(int i = 0; i < minRotation.Length; i++)
            {
                minRotation[i] = Vector3.zero;
            }

            minRotation[(int)HandSkeletonBone.eBone_Thumb1] = new Vector3(50.2f, 65.0f, 23.1f);
            minRotation[(int)HandSkeletonBone.eBone_Thumb2] = new Vector3(2.7f, -8f, 10f);
            minRotation[(int)HandSkeletonBone.eBone_Thumb3] = new Vector3(0.0f, 0.0f, 2.2f);            

            maxRotation = new Vector3[(int)HandSkeletonBone.eBone_Count];
            maxRotation[(int)HandSkeletonBone.eBone_Thumb1] = new Vector3(20.2f, 50.2f, 31.6f);
            maxRotation[(int)HandSkeletonBone.eBone_Thumb2] = new Vector3(37.7f, -8f, 34.0f);
            maxRotation[(int)HandSkeletonBone.eBone_Thumb3] = new Vector3(0.0f, 0.0f, 52.9f);
            maxRotation[(int)HandSkeletonBone.eBone_IndexFinger1] = new Vector3(-10f, -16f, 79.1f);
            maxRotation[(int)HandSkeletonBone.eBone_IndexFinger2] = new Vector3(30.0f, 0.0f, 109.8f);
            maxRotation[(int)HandSkeletonBone.eBone_IndexFinger3] = new Vector3(0.0f, 2.7f, 76.5f);
            maxRotation[(int)HandSkeletonBone.eBone_MiddleFinger1] = new Vector3(-9f, -16f, 77.1f);
            maxRotation[(int)HandSkeletonBone.eBone_MiddleFinger2] = new Vector3(20.0f, 0.0f, 96.8f);
            maxRotation[(int)HandSkeletonBone.eBone_MiddleFinger3] = new Vector3(7.0f, 2.7f, 78.5f);
            maxRotation[(int)HandSkeletonBone.eBone_RingFinger1] = new Vector3(-10f, -20f, 74.1f);
            maxRotation[(int)HandSkeletonBone.eBone_RingFinger2] = new Vector3(15.0f, 0.0f, 94.8f);
            maxRotation[(int)HandSkeletonBone.eBone_RingFinger3] = new Vector3(0.0f, 2.7f, 78.5f);
            maxRotation[(int)HandSkeletonBone.eBone_PinkyFinger1] = new Vector3(-7f, -15f, 71.1f);
            maxRotation[(int)HandSkeletonBone.eBone_PinkyFinger2] = new Vector3(6.0f, 0.0f, 101.8f);
            maxRotation[(int)HandSkeletonBone.eBone_PinkyFinger3] = new Vector3(-8f, 2.7f, 78.5f);
            
            leftHandFingers = new Transform[(int)HandSkeletonBone.eBone_Count];
            rightHandFingers = new Transform[(int)HandSkeletonBone.eBone_Count];
            var animator = Player.main?.playerAnimator;
            if (animator is Animator anim)
            {
                for(int i = 0; i < boneNamesLeft.Length; i++)
                {
                    String boneName = boneNamesLeft[i];
                    if(boneName != null)
                    {
                        leftHandFingers[i] = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_L/clav_L_aim/shoulder_L/hand_L" + boneName);
                        if(leftHandFingers[i] == null)
                        {
                            leftHandFingers[i] = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_L/clav_L_aim/shoulder_L/elbow_L/hand_L" + boneName);
                        }
                    }
                }
                for(int i = 0; i < boneNamesRight.Length; i++)
                {
                    String boneName = boneNamesRight[i];
                    if(boneName != null)
                    {
                        rightHandFingers[i] = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_R/clav_R_aim/shoulder_R/hand_R" + boneName);
                        if(rightHandFingers[i] == null)
                        {
                            rightHandFingers[i] = anim.transform.Find("export_skeleton/head_rig/neck/chest/clav_R/clav_R_aim/shoulder_R/elbow_R/hand_R" + boneName);
                        }
                    }
                }
            }

        }


        public void SetHandParents(bool fullbody)
        {
            if( fullbody)
            {
                leftElbow.localScale = Vector3.one;
                rightElbow.localScale = Vector3.one;

                leftHand.parent = leftElbow;
                rightHand.parent = rightElbow;
            
                //leftHand.transform.SetPositionAndRotation(origLeftHand.position, origLeftHand.rotation);
                //rightHand.transform.SetPositionAndRotation(origRightHand.position, origRightHand.rotation);
            }
            else
            {
                transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true).Where(m => m.name.Contains("glove") || m.name.Contains("hands")).ForEach(
                mr =>
                {
                    var newBounds = new Bounds(Vector3.zero, new Vector3(3.0f, 3.0f, 3.0f));
                    mr.localBounds = newBounds;
                    mr.allowOcclusionWhenDynamic = false;
                    // TODO: This actually fixes the culling, but still not sure why the bbox doesn't work
                    mr.updateWhenOffscreen = true;
                });

                leftHand.parent = leftElbow.parent;
                rightHand.parent = rightElbow.parent;
                
                leftElbow.localScale = Vector3.zero;
                rightElbow.localScale =  Vector3.zero;
                
            }
            //Mod.logger.LogInfo($"SetHandParents {ik.enabled} {leftScale} {rightScale} {leftElbow.localScale} {rightElbow.localScale}");

        }

        public static void OnFullBodyChanged(bool val)
        {
            VRHands.instance.ik.enabled = val;
            VRHands.instance.SetHandParents(val);
        }

        public void ResetHandTargets()
        {
            HandOffsets.LeftHand.Apply(leftTarget);
            HandOffsets.RightHand.Apply(rightTarget);

        }
        public void OnOpenPDA()
        {
            HandOffsets.PDA.Apply(leftTarget);
        }
        public void OnClosePDA()
        {
            ResetHandTargets();
        }

        void Update()
        {
            if (ik.enabled && uGUI_SpyPenguin.main.activePenguin == false)
            {
                ik.solver.leftHandEffector.target = leftTarget;
                ik.solver.rightHandEffector.target = rightTarget;
            }
        }

        void LateUpdate()
        {
            if(!ik.enabled && uGUI_SpyPenguin.main.activePenguin == false)
            {
                leftHand.transform.SetPositionAndRotation(leftTarget.position, leftTarget.rotation);
                rightHand.transform.SetPositionAndRotation(rightTarget.position, rightTarget.rotation);

                // Reset Elbows
                leftElbow.transform.SetPositionAndRotation(leftHand.position, leftHand.rotation);
                rightElbow.transform.SetPositionAndRotation(rightHand.position, rightHand.rotation);
            }

            if(Settings.ArticulatedHands)
            {
                SteamVR_Action_Skeleton rightSkeletonAction = SteamVR_Input.GetSkeletonAction("RightHandSkeleton");
                SteamVR_Action_Skeleton leftSkeletonAction = SteamVR_Input.GetSkeletonAction("LeftHandSkeleton");
                //DebugPanel.Show($"GetLookDelta: 0={skeletonAction.boneRotations[11].eulerAngles}\n 1={skeletonAction.boneRotations[12].eulerAngles}\n2={skeletonAction.boneRotations[13].eulerAngles}\n3={skeletonAction.boneRotations[14].eulerAngles}\ncurl={skeletonAction.middleCurl}", true);
                //DebugPanel.Show($"Fingers: 1={rightHandFingers[(int)HandSkeletonBone.eBone_Thumb1].transform.localRotation.eulerAngles}\n2={rightHandFingers[(int)HandSkeletonBone.eBone_Thumb2].transform.localRotation.eulerAngles}\n3={rightHandFingers[(int)HandSkeletonBone.eBone_Thumb3].transform.localRotation.eulerAngles}", true);
                //DebugPanel.Show($"Fingers: {leftSkeletonAction.skeletalTrackingLevel} {leftSkeletonAction.indexCurl} {leftSkeletonAction.pinkyCurl}", true);

                if(!Player.main.pda.isOpen)
                {
                    UpdateFinger(leftHandFingers, (int)HandSkeletonBone.eBone_PinkyFinger1, leftSkeletonAction.pinkyCurl);
                    UpdateFinger(leftHandFingers, (int)HandSkeletonBone.eBone_RingFinger1, leftSkeletonAction.ringCurl);
                    UpdateFinger(leftHandFingers, (int)HandSkeletonBone.eBone_MiddleFinger1, leftSkeletonAction.middleCurl);
                    UpdateFinger(leftHandFingers, (int)HandSkeletonBone.eBone_IndexFinger1, leftSkeletonAction.indexCurl);
                    UpdateFinger(leftHandFingers, (int)HandSkeletonBone.eBone_Thumb1, leftSkeletonAction.thumbCurl);
                }
               
                if(Inventory.main.GetHeld() == null)
                {
                    UpdateFinger(rightHandFingers, (int)HandSkeletonBone.eBone_PinkyFinger1, rightSkeletonAction.pinkyCurl);
                    UpdateFinger(rightHandFingers, (int)HandSkeletonBone.eBone_RingFinger1, rightSkeletonAction.ringCurl);
                    UpdateFinger(rightHandFingers, (int)HandSkeletonBone.eBone_MiddleFinger1, rightSkeletonAction.middleCurl);
                    UpdateFinger(rightHandFingers, (int)HandSkeletonBone.eBone_IndexFinger1, rightSkeletonAction.indexCurl);
                    UpdateFinger(rightHandFingers, (int)HandSkeletonBone.eBone_Thumb1, rightSkeletonAction.thumbCurl);
                    
                    //SN right ring finger is deformed for some reason so this corrects it
                    Vector3 minRotation =  new Vector3(8.0f, -5.0f, -2.2f);
                    Vector3 maxRotation = new Vector3(-5f, -20f, 74.1f); //this.maxRotation[(int)HandSkeletonBone.eBone_RingFinger1]; //
                    rightHandFingers[(int)HandSkeletonBone.eBone_RingFinger1].transform.localRotation = Quaternion.Euler(minRotation.x + ((maxRotation.x - minRotation.x) * rightSkeletonAction.ringCurl), minRotation.y + ((maxRotation.y - minRotation.y) * rightSkeletonAction.ringCurl), minRotation.z + ((maxRotation.z - minRotation.z) * rightSkeletonAction.ringCurl));
                    minRotation = Vector3.zero;
                    maxRotation = new Vector3(28f, 0f, 94.8f); 
                    rightHandFingers[(int)HandSkeletonBone.eBone_RingFinger2].transform.localRotation = Quaternion.Euler(minRotation.x + ((maxRotation.x - minRotation.x) * rightSkeletonAction.ringCurl), minRotation.y + ((maxRotation.y - minRotation.y) * rightSkeletonAction.ringCurl), minRotation.z + ((maxRotation.z - minRotation.z) * rightSkeletonAction.ringCurl));
                }

            }
           

/*
            for(int i = 0; i < leftHandFingers.Length; i++)
            {
                Transform finger = leftHandFingers[i];
                if(finger != null)
                {
                    //finger.transform.localRotation = Quaternion.Euler(-skeletonAction.boneRotations[i].eulerAngles.x, -skeletonAction.boneRotations[i].eulerAngles.y, -skeletonAction.boneRotations[i].eulerAngles.z); //skeletonAction.boneRotations[12] * new Quaternion(-1, 1, -1, 1); //Quaternion.Euler(0f,0f,0f); 
                    finger.transform.localRotation = skeletonAction.boneRotations[i].GetInverse();
                }
            }
*/
            /*
            leftMidBase.transform.localRotation = Quaternion.Euler(-skeletonAction.boneRotations[(int)HandSkeletonBone.eBone_MiddleFinger1].eulerAngles.x, -skeletonAction.boneRotations[12].eulerAngles.y, -skeletonAction.boneRotations[12].eulerAngles.z); //skeletonAction.boneRotations[12] * new Quaternion(-1, 1, -1, 1); //Quaternion.Euler(0f,0f,0f); 
            leftMidMid.transform.localRotation = Quaternion.Euler(-skeletonAction.boneRotations[13].eulerAngles.x, -skeletonAction.boneRotations[13].eulerAngles.y, -skeletonAction.boneRotations[13].eulerAngles.z); //skeletonAction.boneRotations[13] * new Quaternion(-1, 1, -1, 1); //Quaternion.Euler(0f,0f,0f); 
            leftMidTip.transform.localRotation = Quaternion.Euler(-skeletonAction.boneRotations[14].eulerAngles.x, -skeletonAction.boneRotations[14].eulerAngles.y, -skeletonAction.boneRotations[14].eulerAngles.z); //skeletonAction.boneRotations[14] * new Quaternion(-1, 1, -1, 1); //Quaternion.Euler(0f,0f,0f); 
            
            rightMidBase.transform.localRotation = rightSkeletonAction.boneRotations[12].GetInverse();
            rightMidMid.transform.localRotation = rightSkeletonAction.boneRotations[13].GetInverse();
            rightMidTip.transform.localRotation = rightSkeletonAction.boneRotations[14].GetInverse();
            */
        }

        public void UpdateFinger(Transform[] fingers,int fingerID, float percent)
        {
            fingers[fingerID].transform.localRotation = Quaternion.Euler(minRotation[fingerID].x + ((maxRotation[fingerID].x - minRotation[fingerID].x) * percent), minRotation[fingerID].y + ((maxRotation[fingerID].y - minRotation[fingerID].y) * percent), minRotation[fingerID].z + ((maxRotation[fingerID].z - minRotation[fingerID].z) * percent));
            fingers[fingerID+1].transform.localRotation = Quaternion.Euler(minRotation[fingerID+1].x + ((maxRotation[fingerID+1].x - minRotation[fingerID+1].x) * percent), minRotation[fingerID+1].y + ((maxRotation[fingerID+1].y - minRotation[fingerID+1].y) * percent), minRotation[fingerID+1].z + ((maxRotation[fingerID+1].z - minRotation[fingerID+1].z) * percent));
            fingers[fingerID+2].transform.localRotation = Quaternion.Euler(minRotation[fingerID+2].x + ((maxRotation[fingerID+2].x - minRotation[fingerID+2].x) * percent), minRotation[fingerID+2].y + ((maxRotation[fingerID+2].y - minRotation[fingerID+2].y) * percent), minRotation[fingerID+2].z + ((maxRotation[fingerID+2].z - minRotation[fingerID+2].z) * percent));
        }

        public void UpdateBody()
        {            
            //var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>(includeInactive: true);
            //foreach (var bodyRenderer in bodyRenderers)
            //{
            //    Mod.logger.LogInfo($"bodyRenderer {bodyRenderer.name}");
            //}

           StartCoroutine(UpdateBodyRendering());
        }

        public void SetBodyRendering(bool val)
        {
            var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.name.Contains("body") || r.name.Contains("vest"));
            bodyRenderers.ForEach(r => r.enabled = val);
        }

        // Retries until changes have actually been made
        IEnumerator UpdateBodyRendering()
        {
            bool retry = true;
            while (retry)
            {
                //Mod.logger.LogInfo($"UpdateBodyRendering {Settings.FullBody}");

                var bodyRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r.name.Contains("body") || r.name.Contains("vest"));
                foreach (var bodyRenderer in bodyRenderers)
                {
                    if(bodyRenderer.enabled == Settings.FullBody)
                    {
                        retry = false;
                    }
                    bodyRenderer.enabled = Settings.FullBody;
                }

                yield return new WaitForSeconds(0.5f);
            }

        }

        internal void OnToolEquipped(PlayerTool tool)
        {
            var aimOffset = tool.GetAimOffset();
            VRCameraRig.instance.TargetTransform = aimOffset;
            tool.GetHandOffset().Apply(rightTarget.transform);
        }
    }

    #region Patches

    [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Close))]
    class IngameMenu_Update_Body
    {
        public static void Postfix(IngameMenu __instance)
        {
           VRHands.instance.UpdateBody();
        }
    }
 
    // TODO: Move/cleanup this
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Start))]
    public class VRPlayerCreate : MonoBehaviour
    {

        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            Mod.logger.LogInfo($"ArmsController.Start called");
            // Disable IK
            __instance.ik.enabled = Settings.FullBody;
            __instance.leftAim.aimer.enabled = false;
            __instance.rightAim.aimer.enabled = false;

            // Attach
            __instance.gameObject.AddComponent<VRHands>().Setup(__instance.ik);
            // __instance.pda.ui.canvasScaler.vrMode = uGUI_CanvasScaler.Mode.Inversed;
        }
    }

/*
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Update))]
    public static class DisableSwimmingAnimation
    {
        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            //SafeAnimator.SetBool(__instance.animator, "is_underwater", false);
        }
    }
*/
    // Reconfigure the aiming
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Reconfigure))]
    public static class ChangeAimAngleForTools
    {
        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance, PlayerTool tool)
        {
            VRHands.instance?.OnToolEquipped(tool);
            if(!__instance.pda.isInUse)
            {
                VRHands.instance.ik.solver.GetBendConstraint(FullBodyBipedChain.LeftArm).bendGoal =  Player.main.armsController.defaultLeftArmBendGoal;  
            }

        }
    }

    //Dont do the built in tool animations when first picking up a tool
    [HarmonyPatch(typeof(PlayerTool), nameof(PlayerTool.Awake))]
    public static class DisableFirstUseToolAnimations
    {
        [HarmonyPostfix]
        public static void Postfix(PlayerTool __instance)
        {
           __instance.hasFirstUseAnimation = false;

        }
    }

    // This removes the animation that inspects the object/tool when equipped for the first time
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.StartInspectObjectAsync))]
    public static class DontInspectObjectAnimation
    {
        public static bool Prefix(ArmsController __instance)
        {
            return false;
        }
    }

    // This configures the left hands offset for the PDA
    [HarmonyPatch(typeof(PDA), nameof(PDA.Open))]
    public static class SetPDAHandOffsets
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRHands.instance.OnOpenPDA();
        }
    }

    // This resets the PDA configuration for the Hand configuration for the PDA
    [HarmonyPatch(typeof(PDA), nameof(PDA.Close))]
    public static class UnsetPDAHandOffsets
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            VRHands.instance.OnClosePDA();
        }
    }

    // Aiming related fixes are following now
    // TODO: Consider moving them into their own category/class or namespace
    public static class Aiming
    {
        public static Camera GetAimCamera()
        {
            return VRCameraRig.instance?.laserPointer.eventCamera;
        }
        public static Transform GetAimTransform()
        {
            return VRCameraRig.instance?.laserPointer.transform;
        }
    }

    // This makes it so the Builder tool aims with the laser pointer
    [HarmonyPatch(typeof(Builder), nameof(Builder.GetAimTransform))]
    public static class Builder_GetAimTransform__Patch
    {
        [HarmonyPrefix]
        static bool Prefix(ref Transform __result)
        {
            __result = Aiming.GetAimTransform();
            return false;
        }
    }

    // Make the stasis rifle/or sphere shoot with the hands rotation
    // NOTE: This is easier than patching StasisRifle.Fire() but but might break some mods?
    [HarmonyPatch(typeof(StasisSphere), nameof(StasisSphere.Shoot))]
    public static class OverrideStasisRifleAim
    {
        static bool Prefix(ref Quaternion rotation)
        {
            rotation = Aiming.GetAimTransform().rotation;
            return true;
        }
    }

    // Make the Propulsion canon aim with the laser pointer/target transform
    [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.GetObjectPosition))]
    public static class OverridePropulsionObjectPosition
    {
        // Replace the first line/instruction in GetObjectPosition() that is Camera camera = MainCamera.camera, with our own above.
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.Start().SetInstruction(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    // Make the Propulsion canon shoot with the laser pointer/target transform
    [HarmonyPatch(typeof(PropulsionCannon), nameof(PropulsionCannon.OnShoot))]
    public static class OverridePropulsionShootDirection
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstruction(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    // Make the Propulsion canon shoot with the laser pointer/target transform
    [HarmonyPatch(typeof(RepulsionCannon), nameof(RepulsionCannon.OnToolUseAnim))]
    public static class OverrideRePropulsionShootDirection
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstructionAndAdvance(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstruction(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    // Make the Knife, Fire Extinguisher and Exosuit aim with the laserpointer instead of camera
    [HarmonyPatch(typeof(UWE.Utils), nameof(UWE.Utils.TraceFPSTargetPosition))]
    [HarmonyPatch(new Type[] { typeof(GameObject), typeof(float), typeof(GameObject), typeof(Vector3), typeof(Vector3), typeof(bool) },
                  new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Ref, ArgumentType.Out, ArgumentType.Normal })]
    public static class TraceFPSTargetUsingControllers
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var m = new CodeMatcher(instructions);
            m.MatchForward(false, new CodeMatch[] { new CodeMatch(ci => ci.Calls(AccessTools.DeclaredPropertyGetter(typeof(MainCamera), nameof(MainCamera.camera)))) });
            m.SetInstructionAndAdvance(CodeInstruction.Call(typeof(Aiming), nameof(Aiming.GetAimCamera)));
            return m.InstructionEnumeration();
        }
    }

    #endregion
}
