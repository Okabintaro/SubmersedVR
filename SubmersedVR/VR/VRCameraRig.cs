using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UWE;
using System.Linq;
using UnityEngine.EventSystems;

/*
The VRCamera Rig handles the controllers together with their laser pointers to control the UI.
TODO: This class and file does too much at the moment. Need to refactor stuff out and clean things up a bit more.
*/
namespace SubmersedVR
{
    extern alias SteamVRActions;
    extern alias SteamVRRef;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System.Collections.Generic;
    using UWEXR;
    using System.Reflection.Emit;
    using System.CodeDom;

    class VRCameraRig : MonoBehaviour
    {
        // Setup and created in Start()
        public Camera vrCamera;
        public GameObject leftController;
        public GameObject rightController;
        // Those are used for the IK/Hands
        public GameObject leftHandTarget;
        public GameObject rightHandTarget;

        // TODO: Those should not be full laserpointers probably
        // The only thing I need is cameras at different positions for the UI or Worldspace Raycasts
        public LaserPointer laserPointer;
        public LaserPointer laserPointerLeft;

        public GameObject uiRig;
        public GameObject leftControllerUI;
        public GameObject rightControllerUI;
        public LaserPointer laserPointerUI;

        public GameObject modelL;
        public GameObject modelR;

        public static VRCameraRig instance;

        public Camera uiCamera = null;
        public GameObject worldTarget;
        public float worldTargetDistance;
        public Transform rigParentTarget;

        public bool photoRequested = false;

        float yVelocity = 0.0f;

        //SetTarget can be called before the quickslots are set up. For example when a game
        //first loads and you were piloting the SeaTruck in the save. This holds the target
        //reference until the quickslots exist
        IQuickSlots deferredTarget = null;

        public Camera UIControllerCamera
        {
            get
            {
                if (laserPointerUI == null)
                {
                    return null;
                }
                return laserPointerUI.eventCamera;
            }
        }
        public Camera WorldControllerCamera
        {
            get
            {
                if (laserPointer == null)
                {
                    return null;
                }
                return laserPointer.eventCamera;
            }
        }

        private FPSInputModule fpsInput = null;

        // This transfroms forward vector determines where the equiped tool will be aiming
        public static readonly TransformOffset DefaultTargetTransform = new TransformOffset(Vector3.zero, new Vector3(45, 0, 0));
        private TransformOffset _targetTransform;
        public VRQuickSlots VrQuickSlots;

        public TransformOffset TargetTransform
        {
            get
            {
                return _targetTransform;
            }
            set
            {
                _targetTransform = value;
                value.Apply(laserPointerUI.transform);
                value.Apply(laserPointer.transform);
            }
        }

        public static Transform GetTargetTansform()
        {
            return VRCameraRig.instance.laserPointer.transform;
        }

        public void SetCameraTrackTarget(Transform target)
        {
            this.rigParentTarget = target;
        }
/*
 -export_skeleton
 --head_rig
 ---Cam
 ----cam_offset
 -----cam_forward
 -----cam_up
 ---neck
 ----chest
 -----clav_L
 ------clav_L_aim
 -------LArm_PoleTarg
 -------shoulder_L
 --------elbow_L
 ---------elbow_L_twist_rig
 ---------hand_L
 ----------attachL
 -----------PlayerPDA
 ------------Mesh
 ------------ScreenAnchor
 -----------spikeytrap_tentacle_attach
 ----------hand_L_midl_base
 -----------hand_L_midl_mid
 ------------hand_L_midl_tip
 ----------hand_L_pinky_base
 -----------hand_L_pinky_mid
 ------------hand_L_pinky_tip
 ----------hand_L_point_base
 -----------hand_L_point_mid
 ------------hand_L_point_tip
 ----------hand_L_ring_base
 -----------hand_L_ring_mid
 ------------hand_L_ring_tip
 ----------hand_L_thumb_base
 -----------hand_L_thumb_mid
 ------------hand_L_thumb_tip
 ----------metacarpal_L
 ----------bleeder_attach
 --------shoulder_L_twist1_rig
 --------shoulder_L_twist2_rig
 -----clav_R
 ------clav_R_aim
 -------RArm_PoleTarg
 -------shoulder_R
 --------elbow_R
 ---------elbow_R_twist_rig
 ---------hand_R
 ----------attach1
 -----------attach1_forward
 -----------attach1_up
 ----------hand_R_midl_base
 -----------hand_R_midl_mid
 ------------hand_R_midl_tip_rig
 ----------hand_R_pinky_base
 -----------hand_R_pinky_mid
 ------------hand_R_pinky_tip_rig
 ----------hand_R_point_base
 -----------hand_R_point_mid
 ------------hand_R_point_tip_rig
 ----------hand_R_ring_base
 -----------hand_R_ring_mid
 ------------hand_R_ring_tip_rig
 ----------hand_R_thumb_base
 -----------hand_R_thumb_mid
 ------------hand_R_thumb_tip_rig
 ----------left_hand_elbowTarget
 ----------left_hand_target
 ----------metacarpal_R
 --------shoulder_R_twist1_rig
 --------shoulder_R_twist2_rig
 -----spine_3
 ------spine_2
 -------spine_1
 --------hips
 ---------thigh_L
 ----------calf_L
 -----------ankle_L
 ------------toe_L_rig
 -------------Flipper1_L
 ---------thigh_R
 ----------calf_R
 -----------ankle_R
 ------------toe_R
 -------------Flipper1_R
 ---ponytail_base
 ----ponytail_1
 -----ponytail_2
 ------ponytail_3
 -------ponytail_4
 ---robin_head_selfies
 ----Robin_Head_geo
 ---FlashlightHelmet
 ----helmet_geo
 -female_geo
 --base
 ---female_base_body_geo
 ---female_base_flipper_geo
 ---female_base_gloves_geo
 ---female_base_hand_geo
 ---female_base_head_geo
 ---female_base_mask_geo
 --coldProtective
 ---female_coldProtectiveSuit_body_geo
 ---female_coldProtectiveSuit_hands_geo
 ---female_coldProtectiveSuit_head_geo
 ---female_coldProtectiveSuit_mask_geo
 --reinforced
 ---female_reinforced_body_geo
 ---female_reinforced_hands_geo
 --stillSuit
 ---female_stillSuit_body_geo
 --bottom

*/
        public void SetupControllers()
        {
            // TODO: Naming is inconsistent, clean this mess up, only need 1/2 pointers?
            leftController = new GameObject(nameof(leftController)).WithParent(transform);
            rightController = new GameObject(nameof(rightController)).WithParent(transform);

            leftController.SetActive(false);
            rightController.SetActive(false);
            var controller = leftController.AddComponent<SteamVRRef.Valve.VR.SteamVR_Behaviour_Pose>();
            controller.inputSource = SteamVRRef.Valve.VR.SteamVR_Input_Sources.LeftHand;
            controller.poseAction = SteamVRActions.Valve.VR.SteamVR_Actions.subnautica_LeftHandPose;
            controller = rightController.AddComponent<SteamVRRef.Valve.VR.SteamVR_Behaviour_Pose>();
            controller.inputSource = SteamVRRef.Valve.VR.SteamVR_Input_Sources.RightHand;
            controller.poseAction = SteamVRActions.Valve.VR.SteamVR_Actions.subnautica_RightHandPose;
            leftController.SetActive(true);
            rightController.SetActive(true);

            leftHandTarget = new GameObject(nameof(leftHandTarget)).WithParent(leftController);
            rightHandTarget = new GameObject(nameof(rightHandTarget)).WithParent(rightController);
            leftHandTarget.transform.localEulerAngles = new Vector3(270, 90, 0);
            Vector3 handOffset = new Vector3(90, 270, 0);
            rightHandTarget.transform.localEulerAngles = handOffset;

            // Laser Pointer Setup
            laserPointer = new GameObject(nameof(laserPointer)).WithParent(rightController.transform).AddComponent<LaserPointer>();
            laserPointerLeft = new GameObject(nameof(laserPointerLeft)).WithParent(leftController.transform).AddComponent<LaserPointer>();
            laserPointerLeft.gameObject.SetActive(false);
            // laserPointer.gameObject.SetActive(false);
            laserPointer.disableAfterCreation = true;

            // NOTE: These laserpointer and controllers is NOT parented to the Rig, since they act in UI space, not world space
            uiRig = new GameObject(nameof(uiRig));
            Object.DontDestroyOnLoad(uiRig);
            leftControllerUI = new GameObject(nameof(leftControllerUI)).WithParent(uiRig.transform);
            rightControllerUI = new GameObject(nameof(rightControllerUI)).WithParent(uiRig.transform);
            laserPointerUI = new GameObject(nameof(laserPointerUI)).WithParent(rightControllerUI.transform).AddComponent<LaserPointer>();
            // TODO: Constructors possible?
            laserPointerUI.doWorldRaycasts = true;
            laserPointerUI.useUILayer = true;

            leftControllerUI.SetActive(false);
            rightControllerUI.SetActive(false);
            controller = leftControllerUI.AddComponent<SteamVRRef.Valve.VR.SteamVR_Behaviour_Pose>();
            controller.inputSource = SteamVRRef.Valve.VR.SteamVR_Input_Sources.LeftHand;
            controller.poseAction = SteamVRActions.Valve.VR.SteamVR_Actions.subnautica_LeftHandPose;
            controller = rightControllerUI.AddComponent<SteamVRRef.Valve.VR.SteamVR_Behaviour_Pose>();
            controller.inputSource = SteamVRRef.Valve.VR.SteamVR_Input_Sources.RightHand;
            controller.poseAction = SteamVRActions.Valve.VR.SteamVR_Actions.subnautica_RightHandPose;
            leftControllerUI.SetActive(true);
            rightControllerUI.SetActive(true);
            TargetTransform = DefaultTargetTransform;

            SetupControllerModels();

            // Connect Input module and layer pointer together
            // TODO: This should be easier using singleton setup
            fpsInput = FindObjectOfType<FPSInputModule>();
            laserPointer.inputModule = fpsInput;
            laserPointerLeft.inputModule = fpsInput;
            laserPointerUI.inputModule = fpsInput;
        }

        public void Awake()
        {
            SteamVR.Initialize();
            SteamVR.settings.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
            SteamVrGameInput.IsSteamVrReady = SteamVR.initializedState == SteamVR.InitializedStates.InitializeSuccess;
        }

        public void Start()
        {
            SetupControllers();
            StartCoroutine(DelayedRecenter(1.0f));
        }

        public IEnumerator DelayedRecenter(float delay)
        {
            yield return new WaitForSeconds(delay);
            VRUtil.Recenter();
        }

        private void SetupControllerModels()
        {
            modelL = new GameObject(nameof(modelL)).WithParent(leftControllerUI).ResetTransform();
            modelR = new GameObject(nameof(modelR)).WithParent(rightControllerUI).ResetTransform();

            var model = modelR.AddComponent<SteamVRRef.Valve.VR.SteamVR_RenderModel>();
            model.SetInputSource(SteamVRRef.Valve.VR.SteamVR_Input_Sources.RightHand);
            model = modelL.AddComponent<SteamVRRef.Valve.VR.SteamVR_RenderModel>();
            model.SetInputSource(SteamVRRef.Valve.VR.SteamVR_Input_Sources.LeftHand);
            modelL.layer = LayerID.UI;
            modelR.layer = LayerID.UI;

            Settings.AlwaysShowControllersChanged += (_) => { UpdateShowControllers(); };
        }

        public void UpdateShowControllers()
        {
            var inMainMenu = !uGUI.isMainLevel;
            bool alwaysShow = Settings.AlwaysShowControllers;
            modelL?.SetActive(alwaysShow || inMainMenu);
            modelR?.SetActive(alwaysShow || inMainMenu);
        }

        // This is used to get the camera from the main menu
        // Main issue with making a new camera was the water surface but that should also be fixable
        // TODO: Maybe remove this, so we only have one common camera
        public void StealCamera(Camera camera)
        {
            // Destroy/Delete old camera
            // NOTE: Subnautica renderes the water using specific camera component which also renders when the camera is disabled

            if (camera != vrCamera && vrCamera != null)
            {
                vrCamera.enabled = false;
                Destroy(vrCamera.gameObject);
            }

            vrCamera = camera;
            Vector3 oldPos = camera.transform.position;
            transform.position = oldPos;
            vrCamera.transform.parent = this.transform;
       }

        public void StealUICamera(Camera camera, bool fromGame = false)
        {
            uiRig.transform.SetPositionAndRotation(camera.transform.position, camera.transform.rotation);
            if (uiCamera != null)
            {
                uiCamera.transform.DetachChildren();
                Destroy(uiCamera.gameObject);
            }

            if (fromGame)
            {
                // This fixes a weird issue I had, where the UI Camera from the game would behave like it wasnt moving
                // even though the transform was changing properly.
                // Maybe it is because the tracking was once disabled in the main game, but I am not sure, since I tried enabling it too.
                // Copying the properties from the main camera and setting up the original important properties fixed it.

                uiRig.transform.position = Vector3.zero;
                var oldMask = camera.cullingMask;
                var oldClear = camera.clearFlags;
                var oldDepth = camera.depth;

                camera.CopyFrom(SNCameraRoot.main.mainCamera);
                camera.transform.localPosition = Vector3.zero;
                camera.transform.localRotation = Quaternion.identity;
                camera.renderingPath = RenderingPath.Forward;
                camera.cullingMask = oldMask;
                camera.clearFlags = CameraClearFlags.Depth;
                camera.depth = oldDepth;

                camera.transform.parent = uiRig.transform;
                camera.transform.localPosition = Vector3.zero; //new Vector3(0.0f, 2.0f, 0.0f);
                camera.transform.localRotation = Quaternion.identity;

                // Set all canvas scalers to static, which makes UI better usable
                FindObjectsOfType<uGUI_CanvasScaler>().Where(obj => !obj.name.Contains("PDA")).ForEach(cs => cs.vrMode = uGUI_CanvasScaler.Mode.Static);
                SetupPDA();
                VrQuickSlots = new GameObject("VRQuickSlots").ResetTransform().AddComponent<VRQuickSlots>();
                VrQuickSlots.Setup(SteamVR_Actions.subnautica_OpenQuickSlotWheel);
                if(deferredTarget != null)
                {
                    VrQuickSlots.SetTarget(deferredTarget);
                    deferredTarget = null;
                }
            }
            else
            {
                camera.transform.parent = uiRig.transform;
                camera.transform.localPosition = Vector3.zero; //new Vector3(0.0f, 1.0f, 0.0f);
                camera.transform.localRotation = Quaternion.identity;
            }
            uiCamera = camera;
            VRHud.Setup(uiCamera, rightControllerUI.transform);
        }

        public void SetQuickSlotsTarget(IQuickSlots target)
        {
            if(VrQuickSlots == null)
            {
                deferredTarget = target;
            }
            else
            {
                VrQuickSlots.SetTarget(target);
            }
        }
        void SetupPDA()
        {
            // Move the quickslots to bottom of PDA bottom left and make it bigger
            var pda = uGUI_PDA.main;
            var targetParent = pda.tabInventory.transform;
            var qs = FindObjectOfType<uGUI_QuickSlots>();
            var qstf = qs.transform;

            qstf.parent = targetParent;
            qstf.localPosition = new Vector3(-250, -455, 4f);
            qstf.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            qstf.localRotation = Quaternion.identity;

            // Add Pasuse Menu Button to PDA to PDA
            var dialog = pda.GetComponentInChildren<uGUI_Dialog>(true);
            var buttonPrefab = dialog.buttonPrefab;
            var button = Object.Instantiate(buttonPrefab, targetParent).GetComponent<uGUI_DialogButton>();
            button.button.transform.parent = targetParent;
            button.button.gameObject.gameObject.name = "PauseMenuButton";
            button.text.text = "Pause Menu";
            button.button.onClick.RemoveAllListeners();
            button.button.onClick.AddListener(() =>
            {
                IngameMenu.main.Open();
            });
            // Move it to the bottom right
            button.rectTransform.anchoredPosition = new Vector2(1100, 50);
            button.rectTransform.pivot = new Vector2(1, 0);
            button.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
            button.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
            button.rectTransform.ForceUpdateRectTransforms();
            button.rectTransform.GetComponentsInChildren<RectTransform>().ForEach(rt => rt.ForceUpdateRectTransforms());

            buttonPrefab = dialog.buttonPrefab;
            button = Object.Instantiate(buttonPrefab, targetParent).GetComponent<uGUI_DialogButton>();
            button.button.transform.parent = targetParent;
            button.button.gameObject.gameObject.name = "ScreenshotButton";
            button.text.text = "Photo";
            button.button.onClick.RemoveAllListeners();
            button.button.onClick.AddListener(() =>
            {
                photoRequested = true;
            });
            // Move it to the bottom right
            button.rectTransform.anchoredPosition = new Vector2(780, 50);
            button.rectTransform.pivot = new Vector2(1, 0);
            button.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 150);
            button.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100);
            button.rectTransform.ForceUpdateRectTransforms();
            button.rectTransform.GetComponentsInChildren<RectTransform>().ForEach(rt => rt.ForceUpdateRectTransforms());

        }

        public IEnumerator SetupGameCameras()
        {
            var rig = VRCameraRig.instance;
            rig.StealCamera(SNCameraRoot.main.mainCamera);
            yield return new WaitForSeconds(1.0f);
            rig.StealUICamera(SNCameraRoot.main.guiCamera, true);
            yield return new WaitForSeconds(0.1f);

            FindObjectsOfType<uGUI_CanvasScaler>().ForEach(cs => cs.SetDirty());
        }

        public void LateUpdate()
        {
            // Move the camera rig to the player each frame and rotate the uiRig accordingly
            if (rigParentTarget != null)
            {
                this.transform.SetPositionAndRotation(rigParentTarget.position, rigParentTarget.rotation);
                uiRig.transform.rotation = transform.rotation;
                //Move the body to whereever the head is
                if(uGUI_SpyPenguin.main.activePenguin == false)
                {
                    RecenterBodyOnCameraOrientation(35f, 0.3f, 3.0f, 1.5f);  

                    float zOffset = Player.main?.inSeatruckPilotingChair == true || Player.main?.inExosuit == true ? -0.2f : -0.08f;                 
                    float yOffset = Player.main?.inSeatruckPilotingChair == true || Player.main?.inExosuit == true ? -0.2f : -0.1f;    
                    if(Player.main._cinematicModeActive == false)  
                    {
                        Player.main.armsController.transform.position = SNCameraRoot.main.mainCamera.transform.position + (SNCameraRoot.main.mainCamera.transform.forward * zOffset) + new Vector3(0f, yOffset, 0f);
                        //Player.main.armsController.transform.position = MainCameraControl.main.transform.position + (MainCameraControl.main.transform.forward * zOffset) + new Vector3(0f, yOffset, 0f);
                    }           
                }
                //Experimental player scaling
                Player.main.armsController.transform.localScale = new Vector3(Settings.PlayerScale, Settings.PlayerScale, Settings.PlayerScale);        
                Player.main.playerController.standheight = 1.5f * Settings.PlayerScale; 
            }
        }

        //If the camera turns more than a certain degree (more than the head can normally turn on a human body without rotating the shoulders) then
        //rotate the body quickly to prevent the "Exorcist" head rotation
        //If the camera is slightly rotated away from the body direction then very slowly turn the body to center with the head
        public void RecenterBodyOnCameraOrientation(float leewayAngle, float duration, float secondaryLeewayAngle, float secondaryDuration)
        {
            float cameraYRot = SNCameraRoot.main.mainCamera.transform.rotation.eulerAngles.y;
            //float cameraYRot = MainCameraControl.main.transform.rotation.eulerAngles.y;
            float bodyYRot = Player.main.armsController.transform.rotation.eulerAngles.y;
            float diff = Mathf.DeltaAngle(cameraYRot, bodyYRot);
            if(Mathf.Abs(diff) > leewayAngle)
            {
                float yAngle = Mathf.SmoothDampAngle(bodyYRot, cameraYRot + (leewayAngle * Mathf.Sign(diff)), ref yVelocity, duration);
                Player.main.armsController.transform.rotation = Quaternion.Euler( new Vector3(0f, yAngle, 0f));
            }
            else if(Mathf.Abs(diff) > secondaryLeewayAngle)
            {
                float yAngle = Mathf.SmoothDampAngle(bodyYRot, cameraYRot + (secondaryLeewayAngle * Mathf.Sign(diff)), ref yVelocity, secondaryDuration);
                Player.main.armsController.transform.rotation = Quaternion.Euler( new Vector3(0f, yAngle, 0f));
            }
        }

        void DebugRaycasts()
        {
            if (false && Settings.IsDebugEnabled)
            {
                RaycastResult? uiTarget = fpsInput?.lastRaycastResult;
                DebugPanel.Show($"World Target: {worldTarget?.name}({worldTargetDistance})\nUI Target:{uiTarget?.gameObject?.name}({uiTarget?.distance})\nFocused: {EventSystem.current.isFocused}");
            }
        }

        // Gets set by GUIHand Patch, which already does world raycasting so we dont have to do it ourselves
        public void SetWorldTarget(GameObject activeTarget, float activeHitDistance)
        {
            this.worldTarget = activeTarget;
            this.worldTargetDistance = activeHitDistance;
            this.laserPointerUI.SetWorldTarget(worldTarget, worldTargetDistance);
        }
    }

    #region Patches

    //Head based vs Hand based movement
    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.forwardReference), MethodType.Getter)]
    public static class MoveDirectionOverride
    {
        public static Transform controllerTransform;
        static bool Prefix(PlayerController __instance, ref Transform __result)
        {
            if(Settings.HandBasedTurning)
            {
                //Use the Camera's position and the laser pointer's rotation
                //Use a dummy object to hold the transform
                if(controllerTransform == null)
                {
                    controllerTransform = new GameObject().transform;
                }
                controllerTransform.position = MainCamera.camera.transform.position;
                controllerTransform.rotation = VRCameraRig.GetTargetTansform().rotation; //the laser pointer transform
                __result = controllerTransform;
            }
            else
            {
                __result = MainCamera.camera.transform;
            }
            return false;
        }
    }

/*
    [HarmonyPatch(typeof(UnderwaterMotor), nameof(UnderwaterMotor.UpdateMove))]
    public static class MovementDirectionTest
    {
        static bool Prefix(UnderwaterMotor __instance)
        {
            //__instance.playerController.forwardReference.rotation = Quaternion.Euler(new Vector3(0f,Player.main.playerController.forwardReference.rotation.eulerAngles.y, Player.main.playerController.forwardReference.rotation.eulerAngles.z));
            return true;
        }
    }
*/

    //Adjust the player position while piloting vehicles with vr offset positions and user overrides
    [HarmonyPatch(typeof(MainCameraControl), nameof(MainCameraControl.OnLateUpdate))]
    public static class PlayerPositionFixer
    {
        static bool Prefix(MainCameraControl __instance)
        {
            float zOffset = 0.0f;
            float yOffset = 0.0f;
            if(Player.main?.inSeatruckPilotingChair == true)
            {
                zOffset += 0.2f + Settings.SeaTruckZOffset;
                yOffset += 0.2f + Settings.SeaTruckYOffset;
            }
            else if(Player.main?.inHovercraft == true)
            {
                zOffset += 0.07f + Settings.SnowBikeZOffset;
                yOffset += 0.2f + Settings.SnowBikeYOffset;
            }
            else if(Player.main?.inExosuit == true)
            {
                zOffset += 0.1f + Settings.ExosuitZOffset;
                yOffset += 0.2f + Settings.ExosuitYOffset;
            }

            __instance.cameraUPTransform.localPosition = new Vector3(__instance.cameraUPTransform.localPosition.x, yOffset, zOffset);
            //__instance.cameraUPTransform.localRotation = Quaternion.Euler( new Vector3(0.0f, 0.0f, 0.0f));
            return false;
        }
    }

    // Create the Rig together with the uGUI Prefab
    [HarmonyPatch(typeof(uGUI), nameof(uGUI.Awake))]
    public static class uGUI_AwakeSetupRig
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            // TODO: Should use proper singleton pattern?
            var rig = new GameObject(nameof(VRCameraRig)).AddComponent<VRCameraRig>();
            VRCameraRig.instance = rig;
            Object.DontDestroyOnLoad(rig);
        }
    }

    // Make the uGUI_GraphicRaycaster take the LaserPointers EventCamera when possible
    // Have to switch between guiCameraSpace and Worldspace for e.g. Scanner Room and Cyclops UI
    [HarmonyPatch(typeof(uGUI_GraphicRaycaster))]
    [HarmonyPatch(nameof(uGUI_GraphicRaycaster.eventCamera), MethodType.Getter)]
    class uGUI_GraphicRaycaster_VREventCamera_Patch
    {
        public static bool Prefix(uGUI_GraphicRaycaster __instance, ref Camera __result)
        {
            if (VRCameraRig.instance == null)
            {
                return true;
            }
            if (!(SNCameraRoot.main != null))
            {
                __result = VRCameraRig.instance.UIControllerCamera;
            }
            else
            {
                if (__instance.guiCameraSpace)
                {
                    __result = VRCameraRig.instance.UIControllerCamera;
                }
                else
                {
                    __result = VRCameraRig.instance.WorldControllerCamera;
                }
            }
            return false;
        }
    }

    // Same Patch as above but for the UnityEngine GraphicRaycaster
    // Turns out some canvases like the left panel on the cyclops don't use the uGUI_GraphicRaycaster
    // TODO: They seem to be in world space only though, have to double check.
    [HarmonyPatch(typeof(GraphicRaycaster))]
    [HarmonyPatch(nameof(GraphicRaycaster.eventCamera), MethodType.Getter)]
    class Unity_GraphicRaycaster_VREventCamera_Patch
    {
        public static bool Prefix(GraphicRaycaster __instance, ref Camera __result)
        {
            // TODO: Clean this up
            var canvas = __instance.GetComponent<Canvas>();
            if (canvas == null)
            {
                return true;
            }
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay || (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null))
            {
                return true;
            }
            if (VRCameraRig.instance == null)
            {
                return true;
            }
            __result = VRCameraRig.instance.WorldControllerCamera;
            return false;
        }
    }

    [HarmonyPatch(typeof(uGUI_CanvasScaler), nameof(uGUI_CanvasScaler.UpdateTransform))]
    static class uGUI_CanvasScalerPDA_Attach
    {
        public static void Postfix(uGUI_CanvasScaler __instance)
        {
             // TODO: There gotta be a better way to attach this only to the PDA, maybe custom behaviour, disabling the Scalar?
            if (__instance.gameObject.GetComponent<uGUI_PDA>() == null)
            {
                return;
            }
            if (VRCameraRig.instance == null)
                return;
            var rigWorldPos = SNCameraRoot.main.transform;

            var worldPos = __instance._anchor.transform.position;
            var worldRot = __instance._anchor.transform.rotation;
            var uiSpacePos = worldPos - rigWorldPos.position;
            var uiSpaceRotation = worldRot;
            // DebugPanel.Show($"PDA Pos/Rot: {worldPos}/{worldRot.eulerAngles}\n -> {uiSpacePos}/{uiSpaceRotation.eulerAngles}\nrigPos/rot: {rigWorldPos.position}, {rigWorldPos.eulerAngles}");
            __instance.rectTransform.position = uiSpacePos;
            __instance.rectTransform.rotation = uiSpaceRotation;
        }
    }

     // Makes the ingame menu spawn infront of you in vr
    [HarmonyPatch(typeof(IngameMenu), nameof(IngameMenu.Awake))]
    class MakeIngameMenuStatic
    {
        public static void Postfix(IngameMenu __instance)
        {
            var scalar = __instance.GetComponent<uGUI_CanvasScaler>();
            scalar.vrMode = uGUI_CanvasScaler.Mode.Static;
        }
    }

    // Makes the builder menu spawn infront of you in vr
    // TODO: Could make those more general patches?
    [HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Awake))]
    class MakeBuilderMenuStatic
    {
        public static void Postfix(uGUI_BuilderMenu __instance)
        {
            var scalar = __instance.GetComponent<uGUI_CanvasScaler>();
            scalar.vrMode = uGUI_CanvasScaler.Mode.Static;
        }
    }

    // Makes the builder menu spawn infront of you in vr
    [HarmonyPatch(typeof(uGUI_BuilderMenu), nameof(uGUI_BuilderMenu.Open))]
    class MakeBuilderMenuStatic2
    {
        public static void Postfix(uGUI_BuilderMenu __instance)
        {
            var scalar = __instance.GetComponent<uGUI_CanvasScaler>();
            scalar.SetDirty();
            scalar.UpdateTransform(SNCameraRoot.main.guiCamera);
        }
    }

    // Create the VRCameraRig when ArmsController is started
    [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Start))]
    public static class ArmsController_Start_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(ArmsController __instance)
        {
            Camera mainCamera = SNCameraRoot.main.mainCam;
            VRCameraRig.instance.SetCameraTrackTarget(mainCamera.transform.parent);
            CoroutineHost.StartCoroutine(VRCameraRig.instance.SetupGameCameras());
        }
    }

    // // Don't disable the the automatic camera tracking of the UI Camera in the Main Game
    // [HarmonyPatch(typeof(ManagedCanvasUpdate), nameof(ManagedCanvasUpdate.GetUICamera))]
    // public static class PatchCameraTrackingDisabled
    // {
    //     static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    //     {
    //         return new CodeMatcher(instructions).MatchForward(false, new CodeMatch[] {
    //             new CodeMatch(ci => ci.Calls(typeof(XRDevice).GetMethod(nameof(XRDevice.DisableAutoXRCameraTracking))))
    //         }).ThrowIfNotMatch("Could not find XRDevice Deactivation").Advance(-2).RemoveInstructions(3).InstructionEnumeration();
    //     }
    // }

    [HarmonyPatch(typeof(uGUI), nameof(uGUI.UpdateLevelIdentifier))]
    static class OnMainLevelChanged
    {
        public static void Postfix(uGUI __instance)
        {
            VRCameraRig.instance?.UpdateShowControllers();
        }
    }


    // Disable the last XRSettings.enabled branch by replacing it with false/0
    [HarmonyPatch(typeof(MainCameraControl), nameof(MainCameraControl.OnUpdate))]
    public static class DisableVRLockMechanic
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).End().MatchBack(false, new CodeMatch[] {
                new CodeMatch(ci => ci.Calls(typeof(XRSettings).GetProperty(nameof(XRSettings.enabled)).GetGetMethod()))
            }).ThrowIfNotMatch("Could not find last thingy").SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_0)).InstructionEnumeration();
        }
    }

    // Disable the XRSettings.enabled to disable HandReticle Patch
    [HarmonyPatch(typeof(HandReticle), nameof(HandReticle.LateUpdate))]
    public static class DisableHandReticleVR
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return new CodeMatcher(instructions).End().MatchBack(false, new CodeMatch[] {
                new CodeMatch(ci => ci.Calls(typeof(XRSettings).GetProperty(nameof(XRSettings.enabled)).GetGetMethod()))
            }).ThrowIfNotMatch("Could not find last thingy").SetInstruction(new CodeInstruction(OpCodes.Ldc_I4_0)).InstructionEnumeration();
        }
    }

    // Disable LightBar stuff
    [HarmonyPatch(typeof(PlatformUtils), nameof(PlatformUtils.DimLightBar))]
    public static class NoDimLightBar {
        public static bool Prefix() {
            return false;
        }
    }
    [HarmonyPatch(typeof(PlatformUtils), nameof(PlatformUtils.SetLightBarColor))]
    public static class NoSetLightBarColor {
        public static bool Prefix() {
            return false;
        }
    }

    [HarmonyPatch(typeof(PlatformUtils), nameof(PlatformUtils.ResetLightBarColor))]
    public static class NoResetLightBarColor {
        public static bool Prefix() {
            return false;
        }
    }

    #endregion
}