using HarmonyLib;
using UnityEngine;
using System.Collections.Generic; 

namespace SubmersedVR
{ 
    extern alias SteamVRRef;
    extern alias SteamVRActions;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System.Xml.Schema;

    //Use hands to grip vehicle controls and pilot vehicles
    static class PhysicalPilotingVR
    {
        public enum PhysicalPilotingDirection: int
        {
            MoveForward = 0,
            MoveBackward,
            MoveLeft,
            MoveRight,
            MoveUp,
            MoveDown,
            LookUp,
            LookDown,
            LookLeft,
            LookRight,
        }
        public enum PhysicalPilotingHand: int
        {
            Left = 0,
            Right
        }
        public enum PhysicalVehicle: int
        {
            None = -1,
            Exosuit,
            Seamoth,
            Cyclops,
            SeaTruck,
            SnowBike
        }

        static public Transform leftPilotingTarget;  //Where the hands grip when grabbing the steering controls. Moves due to animation
        static public Transform rightPilotingTarget;  
        static public GameObject rightSeaTruckPilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftSeaTruckPilotingGrip;
        static public GameObject rightExosuitPilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftExosuitPilotingGrip;
        static public GameObject rightSnowBikePilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftSnowBikePilotingGrip;
        static public GameObject rightSeamothPilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftSeamothPilotingGrip;
        static public GameObject rightCyclopsPilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftCyclopsPilotingGrip;


        static public bool leftPilotingLocked = false;
        static public bool rightPilotingLocked = false;
        static public bool leftGripping = false; //is the left hand currently gripping the steering control
        static public bool rightGripping = false; //is the left hand currently gripping the steering control

        public static float[][][] gripArray = new float[10][][]  //direction, vehicle, values
        {
            new float[5][]  //forward
            {
                new float[3]{340f, 15f, 0.1f}, //exosuit - center angle, deadzone, sensitivity
                new float[3]{15f, 8f, 0f}, //seamoth
                new float[3]{15f, 8f, 0f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //backward
            {
                new float[3]{340f, 15f, 0.1f}, //exosuit
                new float[3]{15f, 8f, 0f}, //seamoth
                new float[3]{15f, 8f, 0f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //move left
            {
                new float[3]{350f, 15f, 1f}, //exosuit
                new float[3]{350f, 8f, 1f}, //seamoth
                new float[3]{335f, 15f, 0.1f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //move right
            {
                new float[3]{350f, 15f, 1f}, //exosuit
                new float[3]{350f, 8f, 1f}, //seamoth
                new float[3]{335f, 15f, 0.1f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //move up
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{10f, 10f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //move down
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{10f, 10f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //lookup up
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{10f, 8f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //look down
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{10f, 8f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //lookup left
            {
                new float[3]{355f, 15f, 0.03f}, //exosuit
                new float[3]{340f, 8f, 0.07f}, //seamoth
                new float[3]{340f, 8f, 0.07f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },
            new float[5][]  //look right
            {
                new float[3]{355f, 15f, 0.03f}, //exosuit
                new float[3]{340f, 8f, 0.07f}, //seamoth
                new float[3]{340f, 8f, 0.07f}, //cyclops
                new float[3]{33f, 15f, 0.01f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike
            },

        };
        static float gripDistance = 0.13f; // The distance away from a target control where the grip will recognize contact

        public static  bool inHovercraft{get {return false; /*Player.main?.inHovercraft == true;*/} }
        public static  bool inSeaTruck{get {return false; /*Player.main?.inSeatruckPilotingChair == true;*/} }

        public static  bool inSeamoth{get {return Player.main?.inSeamoth == true;} }
        public static  bool inExosuit{get {return Player.main?.inExosuit == true;} }
        public static  bool inCyclops{get {return Player.main?.currentSub?.isCyclops == true && Player.main?.isPiloting == true;} }

        public static float? GetValue(PhysicalPilotingDirection direction)
        {
            float? value = null;

            Vector3? leftEulers = GetPilotingHandLocalRotation(PhysicalPilotingHand.Left)?.eulerAngles;
            Vector3? rightEulers = GetPilotingHandLocalRotation(PhysicalPilotingHand.Right)?.eulerAngles;
            Vector3? leftPosition = GetPilotingHandLocalPosition(PhysicalPilotingHand.Left);
            Vector3? rightPosition = GetPilotingHandLocalPosition(PhysicalPilotingHand.Right);
            //DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\nleftPos: {leftPosition}\nrightPos: {rightPosition}\nrightHandGrip:{IsGrippingPilotingControl(PhysicalPilotingHand.Right)}");
            int vehicleType = (int)(inExosuit ? PhysicalVehicle.Exosuit : (inSeamoth ? PhysicalVehicle.Seamoth : (inCyclops ? PhysicalVehicle.Cyclops : (PhysicalVehicle.None))));

            if(direction == PhysicalPilotingDirection.MoveForward)
            {   
                if(leftEulers != null)
                {     
                    float delta = Mathf.DeltaAngle(leftEulers?.y ?? 0f, gripArray[(int)direction][vehicleType][0]);
                    value = delta > gripArray[(int)direction][vehicleType][1] ? 1.0f : 0.0f;
                }                
            }
            else if(direction == PhysicalPilotingDirection.MoveBackward)
            {                
                if(leftEulers != null)
                {                       
                    float delta = -Mathf.DeltaAngle(leftEulers?.y ?? 0f, gripArray[(int)direction][vehicleType][0]);
                    value = delta > gripArray[(int)direction][vehicleType][1] ? 1.0f : 0.0f;
                }
               
            }
            else if(direction == PhysicalPilotingDirection.MoveLeft)
            {
                if(inExosuit) 
                {
                    if(leftEulers != null)
                    {
                        float delta = Mathf.DeltaAngle(leftEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    }
                }
                else if(inCyclops) 
                {
                    if(rightEulers != null)
                    {
                        float delta = -Mathf.DeltaAngle(rightEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    }
                    if(leftEulers != null)
                    {
                        float delta = Mathf.DeltaAngle(leftEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        float lvalue = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                        value = value != null ? (value + lvalue) /2 : lvalue;
                    }
                }
                else
                {   
                    if(rightEulers!= null && SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {      
                        float delta = -Mathf.DeltaAngle(rightEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? 1.0f : 0.0f;
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveRight)
            {
                if(inExosuit) 
                {
                    if(leftEulers != null)
                    {
                        float delta = -Mathf.DeltaAngle(leftEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    }
                }                  
                else if(inCyclops) 
                {
                    if(rightEulers!= null)
                    {      
                        float delta = Mathf.DeltaAngle(rightEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;             
                        DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\ndelta: {delta}\nspeed: {((float)value).ToString("0.00")}");
                    }
                    if(leftEulers!= null)
                    {      
                        float delta = -Mathf.DeltaAngle(leftEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        float lvalue = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;             
                        value = value != null ? (value + lvalue) /2 : lvalue;
                    }
                }
                else
                {    
                    if(rightEulers!= null && SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {      
                        float delta = Mathf.DeltaAngle(rightEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? 1.0f : 0.0f;
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveUp)
            {
                if(rightEulers != null)
                {    
                    //Only perform when the A button is pressed                       
                    if(inCyclops || SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {     
                        //if we want jump to be both A button press and up motion for exosuit then remove the if statement  
                        if(inCyclops || inSeamoth)
                        {                             
                            float delta = -Mathf.DeltaAngle(rightEulers?.y ?? 0f, gripArray[(int)direction][vehicleType][0]);
                            value = delta > gripArray[(int)direction][vehicleType][1] ? 1.0f : 0.0f;
                        }
                        else 
                        {
                            value = 1.0f; //exosuit
                        }
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveDown)
            {
                if(rightEulers != null)
                {     
                    //Only perform when the A button is pressed  
                    if(inCyclops || SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {                  
                        float delta = Mathf.DeltaAngle(rightEulers?.y ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? 1.0f : 0.0f;
                        DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\ndelta: {delta}\nspeed: {value}");
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.LookUp)
            {
                if(rightEulers != null)
                {    
                    //Only perform when the A button is not pressed
                    if(!SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {                  
                        float delta = -Mathf.DeltaAngle(rightEulers?.y ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.LookDown)
            {
                if(rightEulers != null)
                {       
                    //Only perform when the A button is not pressed
                    if(!SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {                  
                        float delta = Mathf.DeltaAngle(rightEulers?.y ?? 0f, gripArray[(int)direction][vehicleType][0]);
                        value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.LookLeft)
            {
                if(rightEulers != null && (!(inCyclops || inSeamoth) || ((inCyclops || inSeamoth) && !SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))))
                {    
                    float delta = -Mathf.DeltaAngle(rightEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                    value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                }   
                if(leftEulers != null && (inCyclops || inSeamoth))
                {    
                    float delta = Mathf.DeltaAngle(leftEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                    float lvalue = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    value = value != null ? (value + lvalue) /2 : lvalue;
                }                  
            }
            else if(direction == PhysicalPilotingDirection.LookRight)
            {
                if(rightEulers != null && (!(inCyclops || inSeamoth) || ((inCyclops || inSeamoth) && !SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))))
                {      
                    float delta = Mathf.DeltaAngle(rightEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                    value = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                }   
                if(leftEulers != null && (inCyclops || inSeamoth))
                {    
                    float delta = -Mathf.DeltaAngle(leftEulers?.x ?? 0f, gripArray[(int)direction][vehicleType][0]);
                    float lvalue = delta > gripArray[(int)direction][vehicleType][1] ? (delta - gripArray[(int)direction][vehicleType][1]) * gripArray[(int)direction][vehicleType][2] : 0.0f;
                    value = value != null ? (value + lvalue) /2 : lvalue;
                    //DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\ndelta: {delta}\nlvalue = {lvalue}\nspeed: {value}\nmode = {Player.main?.currentSub.GetComponent<SubControl>().controlMode}");
                }   
                
            }
           return value;
        }

        public static Quaternion? GetPilotingHandLocalRotation(PhysicalPilotingHand hand)
        {
            if(IsGrippingPilotingControl(hand))
            {
                //Get hands local rotation with respect to vehicle grip
                //VRhands.instance confirmed not null in IsGrippingPilotingControl
                return Quaternion.Inverse(GetPilotingGrip(hand).transform.rotation) * (hand == PhysicalPilotingHand.Left ? VRHands.instance.leftTarget.rotation : VRHands.instance.rightTarget.rotation); 
            }
            return null;
        }
        
        public static Vector3? GetPilotingHandLocalPosition(PhysicalPilotingHand hand)
        {
            if(IsGrippingPilotingControl(hand))
            {
                Transform gripTransform = GetPilotingGrip(hand).transform;
                Vector3 gripLocal = gripTransform.InverseTransformDirection(gripTransform.position);
                Vector3 handLocal = gripTransform.InverseTransformDirection(hand == PhysicalPilotingHand.Left ? VRHands.instance.leftTarget.position : VRHands.instance.rightTarget.position);
                //DebugPanel.Show($"leftGrip: {GetPilotingGrip(hand).transform.position}\nleftHand: { VRHands.instance.leftTarget.position}\nleftGripLocal: {gripLocal}\nleftHandLocal: {handLocal}\ndelta:{gripLocal - handLocal}");

                return gripLocal - handLocal; //Vector3.zero;//  GetPilotingGrip(hand).transform.InverseTransformVector(GetPilotingGrip(hand).transform.position) - (hand == PhysicalPilotingHand.Left ? VRHands.instance.leftTarget.position : VRHands.instance.rightTarget.position); //Get hands right target local position with respect to seatruck right target
            }
            return null;
        }
        
        public static Transform GetCurrentPilotingTarget(PhysicalPilotingHand hand)
        {
            if(!IsGrippingPilotingControl(hand))
            {
                leftPilotingTarget = hand == PhysicalPilotingHand.Left ? leftPilotingTarget : null;
                rightPilotingTarget = hand == PhysicalPilotingHand.Right ? rightPilotingTarget : null;
                //leftPilotingLocked = hand == PhysicalPilotingHand.Left ? leftPilotingLocked : false;
                //rightPilotingLocked = hand == PhysicalPilotingHand.Right ? rightPilotingLocked : false;
                return null;
            }
            return GetPilotingTarget(hand);
        }

        private static Transform GetPilotingTarget(PhysicalPilotingHand hand)
        {
            bool isInSeamoth = Player.main?.inSeamoth == true;
            bool isInExosuit = Player.main?.inExosuit == true;
            bool isOnSnowBike = inHovercraft;
            bool isInCyclops = inCyclops;
            bool isPilotingSeatruck = inSeaTruck;
            //bool isPiloting = isInExosuit || isOnSnowBike || isPilotingSeatruck;
            Transform target = hand == PhysicalPilotingHand.Left ? leftPilotingTarget : rightPilotingTarget;
            if(target == null)
            {
                if(hand == PhysicalPilotingHand.Left)
                {
                    if(isPilotingSeatruck)
                    {
                        leftPilotingTarget = GameObject.Find("ControllerLeft_ikTarg").transform;
                    }
                    else if(isInExosuit || isInSeamoth)
                    {
                        leftPilotingTarget = Player.main?.GetVehicle()?.leftHandPlug;
                    }
                    else if(isInCyclops)
                    {
                        leftPilotingTarget = Player.main?.GetPilotingChair().leftHandPlug;
                    }
                    else if(isOnSnowBike)
                    {
                        leftPilotingTarget = null;//Player.main?.GetComponentInParent<Hoverbike>().leftHandIKTarget;
                    }
                    target = leftPilotingTarget;
                }
                else
                {
                    if(isPilotingSeatruck)
                    {
                        rightPilotingTarget = GameObject.Find("ControllerRight_ikTarg").transform;
                    }
                    else if(isInExosuit || isInSeamoth)
                    {
                        rightPilotingTarget = Player.main?.GetVehicle()?.rightHandPlug;
                    }
                    else if(isInCyclops)
                    {
                        rightPilotingTarget = Player.main?.GetPilotingChair().rightHandPlug;
                    }
                    else if(isOnSnowBike)
                    {
                        rightPilotingTarget = null; //Player.main?.GetComponentInParent<Hoverbike>().rightHandIKTarget;
                    }
                    target = rightPilotingTarget;
                }
            }
            return target;
        }

        //Clean this up. Locked piloting shouldnt need to be a whole separate section
        public static bool IsGrippingPilotingControl(PhysicalPilotingHand hand)
        {
            bool result = false;
            if(VRHands.instance == null)
            {
                return result;
            } 

            bool isInSeamoth = Player.main?.inSeamoth == true;
            bool isInExosuit = Player.main?.inExosuit == true;
            bool isInCyclops = inCyclops;
            bool isOnSnowBike = inHovercraft;
            bool isPilotingSeatruck = inSeaTruck;
            bool isPiloting = isInExosuit || isOnSnowBike || isPilotingSeatruck || isInSeamoth || isInCyclops;

            if(Settings.PhysicalDriving && isPiloting)
            {
                if(Settings.PhysicalLockedGrips)
                {
                    if((hand == PhysicalPilotingHand.Left && leftPilotingLocked) || (hand == PhysicalPilotingHand.Right && rightPilotingLocked))
                    {
                        if(hand == PhysicalPilotingHand.Left && SteamVR_Actions.subnautica.MoveDown.GetStateDown(SteamVR_Input_Sources.LeftHand))
                        {
                            leftPilotingLocked = false;
                            result = false;
                        }
                        else if(hand == PhysicalPilotingHand.Right && SteamVR_Actions.subnautica.MoveUp.GetStateDown(SteamVR_Input_Sources.RightHand))
                        {
                            rightPilotingLocked = false;
                            result = false;
                        }
                        else
                        {
                            result = true;
                        }
                    }
                    else if(hand == PhysicalPilotingHand.Left && SteamVR_Actions.subnautica.MoveDown.GetStateDown(SteamVR_Input_Sources.LeftHand) && Vector3.Distance(GetPilotingGrip(hand).transform.position, VRHands.instance.leftTarget.position ) < gripDistance)
                    {
                        leftPilotingLocked = true;
                        result = true;
                    }
                    else if(hand == PhysicalPilotingHand.Right && SteamVR_Actions.subnautica.MoveUp.GetStateDown(SteamVR_Input_Sources.RightHand) && Vector3.Distance(GetPilotingGrip(hand).transform.position, VRHands.instance.rightTarget.position ) < gripDistance)
                    {
                        rightPilotingLocked = true;
                        result = true;
                    }
                    else
                    {
                        result = false;
                    }                   
                }
                else
                {
                    if(hand == PhysicalPilotingHand.Left)
                    {
                        bool controlGripped = SteamVR_Actions.subnautica.MoveDown.GetState(SteamVR_Input_Sources.LeftHand);
                        if(!leftGripping)
                        {
                            if(controlGripped && Vector3.Distance(GetPilotingGrip(hand).transform.position, VRHands.instance.leftTarget.position ) < gripDistance )     
                            {
                                leftGripping = true;
                            }                        
                        }
                        else if(!controlGripped)
                        {
                            leftGripping = false;
                        }
                        result = leftGripping;
                    }
                    if(hand == PhysicalPilotingHand.Right)
                    {
                        bool controlGripped = SteamVR_Actions.subnautica.MoveUp.GetState(SteamVR_Input_Sources.RightHand);
                        if(!rightGripping)
                        {
                            if(controlGripped && Vector3.Distance(GetPilotingGrip(hand).transform.position, VRHands.instance.rightTarget.position ) < gripDistance )     
                            {
                                rightGripping = true;
                            }                        
                        }
                        else if(!controlGripped)
                        {
                            rightGripping = false;
                        }
                        result = rightGripping;
                    }
                    //result = (hand == PhysicalPilotingHand.Left ? SteamVR_Actions.subnautica.MoveDown.GetState(SteamVR_Input_Sources.LeftHand) : SteamVR_Actions.subnautica.MoveUp.GetState(SteamVR_Input_Sources.RightHand)) && ( Vector3.Distance(GetPilotingGrip(hand).transform.position, hand == PhysicalPilotingHand.Left ? VRHands.instance.leftTarget.position : VRHands.instance.rightTarget.position ) < gripDistance);
                }
            }
            return result;
        }

        //Use a new GameObject instead of the actual vehicle ikTarget because the iKTarget moves
        //when the steering console is animated. This causes visual stuttering as the animation
        //fights with the manual adjustment.
        //The new GameObject remains in a fixed position separate from the animation. 
        public static GameObject GetPilotingGrip(PhysicalPilotingHand hand)
        {
            GameObject grip = null;
            bool isInSeamoth = Player.main?.inSeamoth == true;
            bool isInExosuit = Player.main?.inExosuit == true;
            bool isInCyclops = inCyclops;
            bool isOnSnowBike = inHovercraft;
            bool isPilotingSeatruck = inSeaTruck;
            if(isPilotingSeatruck)
            {
                grip = hand == PhysicalPilotingHand.Left ? leftSeaTruckPilotingGrip : rightSeaTruckPilotingGrip;
            }
            else if(isInExosuit)
            {
                grip = hand == PhysicalPilotingHand.Left ? leftExosuitPilotingGrip : rightExosuitPilotingGrip;
            }
            else if(isOnSnowBike)
            {
                grip = hand == PhysicalPilotingHand.Left ? leftSnowBikePilotingGrip : rightSnowBikePilotingGrip;
            }
            else if(isInSeamoth)
            {
                grip = hand == PhysicalPilotingHand.Left ? leftSeamothPilotingGrip : rightSeamothPilotingGrip;
            }
            else if(isInCyclops)
            {
                grip = hand == PhysicalPilotingHand.Left ? leftCyclopsPilotingGrip : rightCyclopsPilotingGrip;
            }

            if(grip == null)
            {
                Transform target = GetPilotingTarget(hand);
                if(target != null)
                {
                    grip = CreateGrip(target);
                    if(hand == PhysicalPilotingHand.Left)
                    {
                        if(isPilotingSeatruck)
                        {
                            leftSeaTruckPilotingGrip = grip;
                        }
                        else if(isInExosuit)
                        {
                            leftExosuitPilotingGrip = grip;
                        }
                        else if(isInSeamoth)
                        {
                            leftSeamothPilotingGrip = grip;
                        }
                        else if(isInCyclops)
                        {
                            leftCyclopsPilotingGrip = grip;
                        }
                        else if(isOnSnowBike)
                        {
                            //override with fixed positions since we dont know the orientation of the handlebars when they were first grabbed
                            grip.transform.localPosition = new Vector3(-0.25237f, 0.25228f, 0.01859f);
                            leftSnowBikePilotingGrip = grip;
                        }
                    }
                    else
                    {
                        if(isPilotingSeatruck)
                        {
                            rightSeaTruckPilotingGrip = grip;
                        }
                        else if(isInExosuit)
                        {
                            rightExosuitPilotingGrip = grip;
                        }
                        else if(isInSeamoth)
                        {
                            rightSeamothPilotingGrip = grip;
                        }
                        else if(isInCyclops)
                        {
                            rightCyclopsPilotingGrip = grip;
                        }
                        else if(isOnSnowBike)
                        {
                            //override with fixed positions since we dont know the orientation of the handlebars when they were first grabbed
                            grip.transform.localPosition = new Vector3(0.25237f, 0.25228f, 0.01859f);
                            rightSnowBikePilotingGrip = grip;
                        }
                    }
                }
            }
            return grip;
        }

        public static GameObject CreateGrip(Transform target)
        {
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject.Destroy(grip.GetComponent<SphereCollider>());
            grip.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            grip.transform.position = target.position;
            grip.transform.rotation = target.rotation;
            Transform parent = target.parent;
            if(Player.main?.inExosuit == true || inSeamoth || inCyclops)
            {
                parent = parent.parent;
            }
            grip.transform.SetParent(parent, true);
            
            //Mod.logger.LogInfo($"CreateGrip localPosition x={grip.transform.localPosition.ToString("F5")}");
            Material newMaterial = new(ShaderManager.preloadedShaders.DebugDisplaySolid);
            newMaterial.SetColor(ShaderPropertyID._Color, Color.cyan);
            grip.GetComponent<Renderer>().material = newMaterial;
            grip.SetActive(false); //Set to true to see the grip models
            
            return grip;
        }
    }

    #region Patches
 
    // Moves the camera/attach point of the seamoth 10 cm in front
    [HarmonyPatch(typeof(SubControl), nameof(SubControl.UpdateAnimation))]
    static class SubControlUpdateAnimation
    {
        public static bool Prefix(SubControl __instance)
        {
            float b = 0f;
            float b2 = 0f;
            if ((double)Mathf.Abs(__instance.throttle.x) > 0.0001)
            {
                ShipSide useShipSide;
                if (__instance.throttle.x > 0f)
                {
                    useShipSide = ShipSide.Port;
                    b = 45f;
                }
                else
                {
                    useShipSide = ShipSide.Starboard;
                    b = -45f;
                }
                if (__instance.throttle.x < -0.1f || __instance.throttle.x > 0.1f)
                {
                    for (int i = 0; i < __instance.turnHandlers.Length; i++)
                    {
                        __instance.turnHandlers[i].OnSubTurn(useShipSide);
                    }
                }
            }
            if ((double)Mathf.Abs(__instance.throttle.y) > 0.0001)
            {
                if (__instance.throttle.y > 0f)
                {
                    b2 = 90f;
                }
                else
                {
                    b2 = -90f;
                }
            }
            __instance.steeringWheelYaw = Mathf.Lerp(__instance.steeringWheelYaw, b, Time.deltaTime * __instance.steeringReponsiveness);
            __instance.steeringWheelPitch = Mathf.Lerp(__instance.steeringWheelPitch, b2, Time.deltaTime * __instance.steeringReponsiveness);
            if (__instance.mainAnimator)
            {
                __instance.mainAnimator.SetFloat("view_yaw", __instance.steeringWheelYaw);
                __instance.mainAnimator.SetFloat("view_pitch", __instance.steeringWheelPitch);
                Player.main.playerAnimator.SetFloat("cyclops_yaw", __instance.steeringWheelYaw);
                Player.main.playerAnimator.SetFloat("cyclops_pitch", __instance.steeringWheelPitch);
            }
            return false;
        }
    }

    #endregion

}
