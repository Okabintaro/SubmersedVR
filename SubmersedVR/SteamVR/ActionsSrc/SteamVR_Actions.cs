namespace Valve.VR;

public class SteamVR_Actions
{
	private static SteamVR_Input_ActionSet_subnautica p_subnautica;

	private static SteamVR_Action_Boolean p_subnautica_Jump;

	private static SteamVR_Action_Boolean p_subnautica_PDA;

	private static SteamVR_Action_Boolean p_subnautica_Deconstruct;

	private static SteamVR_Action_Boolean p_subnautica_Exit;

	private static SteamVR_Action_Boolean p_subnautica_LeftHand;

	private static SteamVR_Action_Boolean p_subnautica_RightHand;

	private static SteamVR_Action_Boolean p_subnautica_AltTool;

	private static SteamVR_Action_Boolean p_subnautica_TakePicture;

	private static SteamVR_Action_Boolean p_subnautica_Reload;

	private static SteamVR_Action_Boolean p_subnautica_MoveForward;

	private static SteamVR_Action_Boolean p_subnautica_MoveBackward;

	private static SteamVR_Action_Boolean p_subnautica_MoveLeft;

	private static SteamVR_Action_Boolean p_subnautica_MoveRight;

	private static SteamVR_Action_Boolean p_subnautica_Sprint;

	private static SteamVR_Action_Boolean p_subnautica_LookUp;

	private static SteamVR_Action_Boolean p_subnautica_LookDown;

	private static SteamVR_Action_Boolean p_subnautica_LookLeft;

	private static SteamVR_Action_Boolean p_subnautica_LookRight;

	private static SteamVR_Action_Boolean p_subnautica_CycleNext;

	private static SteamVR_Action_Boolean p_subnautica_CyclePrev;

	private static SteamVR_Action_Boolean p_subnautica_CameraCycleNext;

	private static SteamVR_Action_Boolean p_subnautica_CameraCyclePrev;

	private static SteamVR_Action_Boolean p_subnautica_UISubmit;

	private static SteamVR_Action_Boolean p_subnautica_UICancel;

	private static SteamVR_Action_Boolean p_subnautica_UIMenu;

	private static SteamVR_Action_Boolean p_subnautica_UIAssign;

	private static SteamVR_Action_Boolean p_subnautica_UIAdjustLeft;

	private static SteamVR_Action_Boolean p_subnautica_UIAdjustRight;

	private static SteamVR_Action_Boolean p_subnautica_UIPrevTab;

	private static SteamVR_Action_Boolean p_subnautica_UINextTab;

	private static SteamVR_Action_Boolean p_subnautica_UILeft;

	private static SteamVR_Action_Boolean p_subnautica_UIRight;

	private static SteamVR_Action_Boolean p_subnautica_UIUp;

	private static SteamVR_Action_Boolean p_subnautica_UIDown;

	private static SteamVR_Action_Boolean p_subnautica_UIClear;

	private static SteamVR_Action_Vector2 p_subnautica_UIScroll;

	private static SteamVR_Action_Boolean p_subnautica_OpenQuickSlotWheel;

	private static SteamVR_Action_Pose p_subnautica_LeftHandPose;

	private static SteamVR_Action_Pose p_subnautica_RightHandPose;

	private static SteamVR_Action_Skeleton p_subnautica_LeftHandSkeleton;

	private static SteamVR_Action_Skeleton p_subnautica_RightHandSkeleton;

	private static SteamVR_Action_Vector2 p_subnautica_Move;

	private static SteamVR_Action_Vector2 p_subnautica_Look;

	private static SteamVR_Action_Boolean p_subnautica_MoveDown;

	private static SteamVR_Action_Boolean p_subnautica_MoveUp;

	private static SteamVR_Action_Boolean p_subnautica_BuilderRotateRight;

	private static SteamVR_Action_Boolean p_subnautica_BuilderRotateLeft;

	private static SteamVR_Action_Boolean p_subnautica_DebugToggle;

	private static SteamVR_Action_Vibration p_subnautica_HapticsRight;

	private static SteamVR_Action_Vibration p_subnautica_HapticsLeft;

	public static SteamVR_Input_ActionSet_subnautica subnautica => p_subnautica.GetCopy<SteamVR_Input_ActionSet_subnautica>();

	public static SteamVR_Action_Boolean subnautica_Jump => p_subnautica_Jump.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_PDA => p_subnautica_PDA.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_Deconstruct => p_subnautica_Deconstruct.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_Exit => p_subnautica_Exit.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_LeftHand => p_subnautica_LeftHand.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_RightHand => p_subnautica_RightHand.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_AltTool => p_subnautica_AltTool.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_TakePicture => p_subnautica_TakePicture.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_Reload => p_subnautica_Reload.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_MoveForward => p_subnautica_MoveForward.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_MoveBackward => p_subnautica_MoveBackward.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_MoveLeft => p_subnautica_MoveLeft.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_MoveRight => p_subnautica_MoveRight.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_Sprint => p_subnautica_Sprint.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_LookUp => p_subnautica_LookUp.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_LookDown => p_subnautica_LookDown.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_LookLeft => p_subnautica_LookLeft.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_LookRight => p_subnautica_LookRight.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_CycleNext => p_subnautica_CycleNext.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_CyclePrev => p_subnautica_CyclePrev.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_CameraCycleNext => p_subnautica_CameraCycleNext.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_CameraCyclePrev => p_subnautica_CameraCyclePrev.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UISubmit => p_subnautica_UISubmit.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UICancel => p_subnautica_UICancel.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIMenu => p_subnautica_UIMenu.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIAssign => p_subnautica_UIAssign.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIAdjustLeft => p_subnautica_UIAdjustLeft.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIAdjustRight => p_subnautica_UIAdjustRight.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIPrevTab => p_subnautica_UIPrevTab.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UINextTab => p_subnautica_UINextTab.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UILeft => p_subnautica_UILeft.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIRight => p_subnautica_UIRight.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIUp => p_subnautica_UIUp.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIDown => p_subnautica_UIDown.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_UIClear => p_subnautica_UIClear.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Vector2 subnautica_UIScroll => p_subnautica_UIScroll.GetCopy<SteamVR_Action_Vector2>();

	public static SteamVR_Action_Boolean subnautica_OpenQuickSlotWheel => p_subnautica_OpenQuickSlotWheel.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Pose subnautica_LeftHandPose => p_subnautica_LeftHandPose.GetCopy<SteamVR_Action_Pose>();

	public static SteamVR_Action_Pose subnautica_RightHandPose => p_subnautica_RightHandPose.GetCopy<SteamVR_Action_Pose>();

	public static SteamVR_Action_Skeleton subnautica_LeftHandSkeleton => p_subnautica_LeftHandSkeleton.GetCopy<SteamVR_Action_Skeleton>();

	public static SteamVR_Action_Skeleton subnautica_RightHandSkeleton => p_subnautica_RightHandSkeleton.GetCopy<SteamVR_Action_Skeleton>();

	public static SteamVR_Action_Vector2 subnautica_Move => p_subnautica_Move.GetCopy<SteamVR_Action_Vector2>();

	public static SteamVR_Action_Vector2 subnautica_Look => p_subnautica_Look.GetCopy<SteamVR_Action_Vector2>();

	public static SteamVR_Action_Boolean subnautica_MoveDown => p_subnautica_MoveDown.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_MoveUp => p_subnautica_MoveUp.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_BuilderRotateRight => p_subnautica_BuilderRotateRight.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_BuilderRotateLeft => p_subnautica_BuilderRotateLeft.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Boolean subnautica_DebugToggle => p_subnautica_DebugToggle.GetCopy<SteamVR_Action_Boolean>();

	public static SteamVR_Action_Vibration subnautica_HapticsRight => p_subnautica_HapticsRight.GetCopy<SteamVR_Action_Vibration>();

	public static SteamVR_Action_Vibration subnautica_HapticsLeft => p_subnautica_HapticsLeft.GetCopy<SteamVR_Action_Vibration>();

	private static void StartPreInitActionSets()
	{
		p_subnautica = SteamVR_ActionSet.Create<SteamVR_Input_ActionSet_subnautica>("/actions/subnautica");
		SteamVR_Input.actionSets = new SteamVR_ActionSet[1] { subnautica };
	}

	private static void InitializeActionArrays()
	{
		SteamVR_Input.actions = new SteamVR_Action[50]
		{
			subnautica_Jump, subnautica_PDA, subnautica_Deconstruct, subnautica_Exit, subnautica_LeftHand, subnautica_RightHand, subnautica_AltTool, subnautica_TakePicture, subnautica_Reload, subnautica_MoveForward,
			subnautica_MoveBackward, subnautica_MoveLeft, subnautica_MoveRight, subnautica_Sprint, subnautica_LookUp, subnautica_LookDown, subnautica_LookLeft, subnautica_LookRight, subnautica_CycleNext, subnautica_CyclePrev,
			subnautica_CameraCycleNext, subnautica_CameraCyclePrev, subnautica_UISubmit, subnautica_UICancel, subnautica_UIMenu, subnautica_UIAssign, subnautica_UIAdjustLeft, subnautica_UIAdjustRight, subnautica_UIPrevTab, subnautica_UINextTab,
			subnautica_UILeft, subnautica_UIRight, subnautica_UIUp, subnautica_UIDown, subnautica_UIClear, subnautica_UIScroll, subnautica_OpenQuickSlotWheel, subnautica_LeftHandPose, subnautica_RightHandPose, subnautica_LeftHandSkeleton,
			subnautica_RightHandSkeleton, subnautica_Move, subnautica_Look, subnautica_MoveDown, subnautica_MoveUp, subnautica_BuilderRotateRight, subnautica_BuilderRotateLeft, subnautica_DebugToggle, subnautica_HapticsRight, subnautica_HapticsLeft
		};
		SteamVR_Input.actionsIn = new ISteamVR_Action_In[48]
		{
			subnautica_Jump, subnautica_PDA, subnautica_Deconstruct, subnautica_Exit, subnautica_LeftHand, subnautica_RightHand, subnautica_AltTool, subnautica_TakePicture, subnautica_Reload, subnautica_MoveForward,
			subnautica_MoveBackward, subnautica_MoveLeft, subnautica_MoveRight, subnautica_Sprint, subnautica_LookUp, subnautica_LookDown, subnautica_LookLeft, subnautica_LookRight, subnautica_CycleNext, subnautica_CyclePrev,
			subnautica_CameraCycleNext, subnautica_CameraCyclePrev, subnautica_UISubmit, subnautica_UICancel, subnautica_UIMenu, subnautica_UIAssign, subnautica_UIAdjustLeft, subnautica_UIAdjustRight, subnautica_UIPrevTab, subnautica_UINextTab,
			subnautica_UILeft, subnautica_UIRight, subnautica_UIUp, subnautica_UIDown, subnautica_UIClear, subnautica_UIScroll, subnautica_OpenQuickSlotWheel, subnautica_LeftHandPose, subnautica_RightHandPose, subnautica_LeftHandSkeleton,
			subnautica_RightHandSkeleton, subnautica_Move, subnautica_Look, subnautica_MoveDown, subnautica_MoveUp, subnautica_BuilderRotateRight, subnautica_BuilderRotateLeft, subnautica_DebugToggle
		};
		SteamVR_Input.actionsOut = new ISteamVR_Action_Out[2] { subnautica_HapticsRight, subnautica_HapticsLeft };
		SteamVR_Input.actionsVibration = new SteamVR_Action_Vibration[2] { subnautica_HapticsRight, subnautica_HapticsLeft };
		SteamVR_Input.actionsPose = new SteamVR_Action_Pose[2] { subnautica_LeftHandPose, subnautica_RightHandPose };
		SteamVR_Input.actionsBoolean = new SteamVR_Action_Boolean[41]
		{
			subnautica_Jump, subnautica_PDA, subnautica_Deconstruct, subnautica_Exit, subnautica_LeftHand, subnautica_RightHand, subnautica_AltTool, subnautica_TakePicture, subnautica_Reload, subnautica_MoveForward,
			subnautica_MoveBackward, subnautica_MoveLeft, subnautica_MoveRight, subnautica_Sprint, subnautica_LookUp, subnautica_LookDown, subnautica_LookLeft, subnautica_LookRight, subnautica_CycleNext, subnautica_CyclePrev,
			subnautica_CameraCycleNext, subnautica_CameraCyclePrev, subnautica_UISubmit, subnautica_UICancel, subnautica_UIMenu, subnautica_UIAssign, subnautica_UIAdjustLeft, subnautica_UIAdjustRight, subnautica_UIPrevTab, subnautica_UINextTab,
			subnautica_UILeft, subnautica_UIRight, subnautica_UIUp, subnautica_UIDown, subnautica_UIClear, subnautica_OpenQuickSlotWheel, subnautica_MoveDown, subnautica_MoveUp, subnautica_BuilderRotateRight, subnautica_BuilderRotateLeft,
			subnautica_DebugToggle
		};
		SteamVR_Input.actionsSingle = new SteamVR_Action_Single[0];
		SteamVR_Input.actionsVector2 = new SteamVR_Action_Vector2[3] { subnautica_UIScroll, subnautica_Move, subnautica_Look };
		SteamVR_Input.actionsVector3 = new SteamVR_Action_Vector3[0];
		SteamVR_Input.actionsSkeleton = new SteamVR_Action_Skeleton[2] { subnautica_LeftHandSkeleton, subnautica_RightHandSkeleton };
		SteamVR_Input.actionsNonPoseNonSkeletonIn = new ISteamVR_Action_In[44]
		{
			subnautica_Jump, subnautica_PDA, subnautica_Deconstruct, subnautica_Exit, subnautica_LeftHand, subnautica_RightHand, subnautica_AltTool, subnautica_TakePicture, subnautica_Reload, subnautica_MoveForward,
			subnautica_MoveBackward, subnautica_MoveLeft, subnautica_MoveRight, subnautica_Sprint, subnautica_LookUp, subnautica_LookDown, subnautica_LookLeft, subnautica_LookRight, subnautica_CycleNext, subnautica_CyclePrev,
			subnautica_CameraCycleNext, subnautica_CameraCyclePrev, subnautica_UISubmit, subnautica_UICancel, subnautica_UIMenu, subnautica_UIAssign, subnautica_UIAdjustLeft, subnautica_UIAdjustRight, subnautica_UIPrevTab, subnautica_UINextTab,
			subnautica_UILeft, subnautica_UIRight, subnautica_UIUp, subnautica_UIDown, subnautica_UIClear, subnautica_UIScroll, subnautica_OpenQuickSlotWheel, subnautica_Move, subnautica_Look, subnautica_MoveDown,
			subnautica_MoveUp, subnautica_BuilderRotateRight, subnautica_BuilderRotateLeft, subnautica_DebugToggle
		};
	}

	private static void PreInitActions()
	{
		p_subnautica_Jump = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/Jump");
		p_subnautica_PDA = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/PDA");
		p_subnautica_Deconstruct = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/Deconstruct");
		p_subnautica_Exit = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/Exit");
		p_subnautica_LeftHand = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/LeftHand");
		p_subnautica_RightHand = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/RightHand");
		p_subnautica_AltTool = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/AltTool");
		p_subnautica_TakePicture = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/TakePicture");
		p_subnautica_Reload = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/Reload");
		p_subnautica_MoveForward = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/MoveForward");
		p_subnautica_MoveBackward = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/MoveBackward");
		p_subnautica_MoveLeft = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/MoveLeft");
		p_subnautica_MoveRight = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/MoveRight");
		p_subnautica_Sprint = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/Sprint");
		p_subnautica_LookUp = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/LookUp");
		p_subnautica_LookDown = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/LookDown");
		p_subnautica_LookLeft = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/LookLeft");
		p_subnautica_LookRight = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/LookRight");
		p_subnautica_CycleNext = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/CycleNext");
		p_subnautica_CyclePrev = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/CyclePrev");
		p_subnautica_CameraCycleNext = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/CameraCycleNext");
		p_subnautica_CameraCyclePrev = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/CameraCyclePrev");
		p_subnautica_UISubmit = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UISubmit");
		p_subnautica_UICancel = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UICancel");
		p_subnautica_UIMenu = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIMenu");
		p_subnautica_UIAssign = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIAssign");
		p_subnautica_UIAdjustLeft = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIAdjustLeft");
		p_subnautica_UIAdjustRight = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIAdjustRight");
		p_subnautica_UIPrevTab = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIPrevTab");
		p_subnautica_UINextTab = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UINextTab");
		p_subnautica_UILeft = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UILeft");
		p_subnautica_UIRight = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIRight");
		p_subnautica_UIUp = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIUp");
		p_subnautica_UIDown = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIDown");
		p_subnautica_UIClear = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/UIClear");
		p_subnautica_UIScroll = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/subnautica/in/UIScroll");
		p_subnautica_OpenQuickSlotWheel = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/OpenQuickSlotWheel");
		p_subnautica_LeftHandPose = SteamVR_Action.Create<SteamVR_Action_Pose>("/actions/subnautica/in/LeftHandPose");
		p_subnautica_RightHandPose = SteamVR_Action.Create<SteamVR_Action_Pose>("/actions/subnautica/in/RightHandPose");
		p_subnautica_LeftHandSkeleton = SteamVR_Action.Create<SteamVR_Action_Skeleton>("/actions/subnautica/in/LeftHandSkeleton");
		p_subnautica_RightHandSkeleton = SteamVR_Action.Create<SteamVR_Action_Skeleton>("/actions/subnautica/in/RightHandSkeleton");
		p_subnautica_Move = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/subnautica/in/Move");
		p_subnautica_Look = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/subnautica/in/Look");
		p_subnautica_MoveDown = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/MoveDown");
		p_subnautica_MoveUp = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/MoveUp");
		p_subnautica_BuilderRotateRight = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/BuilderRotateRight");
		p_subnautica_BuilderRotateLeft = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/BuilderRotateLeft");
		p_subnautica_DebugToggle = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/subnautica/in/DebugToggle");
		p_subnautica_HapticsRight = SteamVR_Action.Create<SteamVR_Action_Vibration>("/actions/subnautica/out/HapticsRight");
		p_subnautica_HapticsLeft = SteamVR_Action.Create<SteamVR_Action_Vibration>("/actions/subnautica/out/HapticsLeft");
	}

	public static void PreInitialize()
	{
		StartPreInitActionSets();
		SteamVR_Input.PreinitializeActionSetDictionaries();
		PreInitActions();
		InitializeActionArrays();
		SteamVR_Input.PreinitializeActionDictionaries();
		SteamVR_Input.PreinitializeFinishActionSets();
	}
}
