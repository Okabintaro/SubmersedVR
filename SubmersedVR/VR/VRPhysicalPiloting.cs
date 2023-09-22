using HarmonyLib;
using UnityEngine;

namespace SubmersedVR
{ 
    extern alias SteamVRRef;
    extern alias SteamVRActions;
    using SteamVRRef.Valve.VR;
    using SteamVRActions.Valve.VR;
    using System.Xml.Schema;
    using Yangrc.VolumeCloud;
    using Steamworks;

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
            LookLeft,
            LookRight,
            LookUp,
            LookDown
        }
        public enum PhysicalPilotingHand: int
        {
            Left = 0,
            Right
        }

        static public Transform leftPilotingTarget;  //Where the hands grip when grabbing the steering controls. Moves due to animation
        static public Transform rightPilotingTarget;  
        static public GameObject rightSeaTruckPilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftSeaTruckPilotingGrip;
        static public GameObject rightExosuitPilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftExosuitPilotingGrip;
        static public GameObject rightSnowBikePilotingGrip; //The fixed grip location for steering. Does not move during animation
        static public GameObject leftSnowBikePilotingGrip;
        static public bool leftPilotingLocked = false;
        static public bool rightPilotingLocked = false;
        static public bool leftGripping = false; //is the left hand currently gripping the steering control
        static public bool rightGripping = false; //is the left hand currently gripping the steering control

        static float xCenterLeft_exosuit = 350f;
        static float yCenterLeft_exosuit = 340f;
        static float deadZone_exosuit = 15f;

        static float zCenterRight_snowbike = -5.0f;
        static float deadZoneRight_snowbike = 15f;
        static float zSensitivityRight_snowbike = 0.03f;
        //static float xCenterLeft_snowbike = 0.0f;
        static float deadZoneLeft_snowbike = 0.008f;
        static float xSensitivityLeft_snowbike = 20f;

        static float xCenterRight = 335f;
        static float xDeadZoneRight = 15f;
        static float xSensitivityRight = 0.03f;
        static float yCenterRight = 341f;
        static float yDeadZoneRight = 15f;
        static float ySensitivityRight = 0.03f;
        static float zCenterLeft = 33;
        static float zDeadZoneLeft = 15f;
        static float zSensitivityLeft = 0.01f;
        static float yCenterLeft = 305f;
        static float yDeadZoneLeft = 10f;
        static float ySensitivityLeft = 0.1f;

        static float gripDistance = 0.13f; // The distance away from a target control where the grip will recognize contact

        public static float? GetValue(PhysicalPilotingDirection direction)
        {
            float? value = null;

            Vector3? leftEulers = GetPilotingHandLocalRotation(PhysicalPilotingHand.Left)?.eulerAngles;
            Vector3? rightEulers = GetPilotingHandLocalRotation(PhysicalPilotingHand.Right)?.eulerAngles;
            Vector3? leftPosition = GetPilotingHandLocalPosition(PhysicalPilotingHand.Left);
            Vector3? rightPosition = GetPilotingHandLocalPosition(PhysicalPilotingHand.Right);
            //DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\nleftPos: {leftPosition}\nrightPos: {rightPosition}\nrightHandGrip:{IsGrippingPilotingControl(PhysicalPilotingHand.Right)}\nleftHandLock:{VRHands.instance.leftHandSteeringLocked}\nvalue:{value}");

            if(direction == PhysicalPilotingDirection.MoveForward)
            {   
                if(Player.main?.inHovercraft == true)
                {
                    if(rightEulers != null)
                    {
                        float z = rightEulers?.z ?? 0f;    
                        z = z > 180 ? z - 360 : z;               
                        value = z < (zCenterRight_snowbike-deadZoneRight_snowbike) ? (zCenterRight_snowbike-deadZoneRight_snowbike - z) * zSensitivityRight_snowbike : 0.0f;
                   }
                }            
                else if(leftEulers != null)
                {     
                    if(Player.main?.inSeatruckPilotingChair == true)
                    {                  
                        float z = leftEulers?.z ?? 0f;                   
                        z = z > 180 ? z - 360 : z;               
                        value = z > (zCenterLeft+zDeadZoneLeft) ? (z - (zCenterLeft+zDeadZoneLeft)) * zSensitivityLeft: 0.0f;
                    }
                    else
                    {
                        float y = leftEulers?.y ?? 0f;    
                        y = y < 180 ? 360 + y : y;               
                        value = y < (yCenterLeft_exosuit-deadZone_exosuit) ? (yCenterLeft_exosuit-deadZone_exosuit - y) * ySensitivityLeft : 0.0f;
                    }
                }
                
            }
            else if(direction == PhysicalPilotingDirection.MoveBackward)
            {                
                if(Player.main?.inHovercraft == true)
                {
                    if(rightEulers != null)
                    {
                        float z = rightEulers?.z ?? 0f;                   
                        z = z > 180 ? z - 360 : z;               
                        value = z > (zCenterRight_snowbike+deadZoneRight_snowbike) ? (z - (zCenterRight_snowbike+deadZoneRight_snowbike)) * zSensitivityRight_snowbike: 0.0f;
                   }
                }            
                else if(leftEulers != null)
                {                       
                    if(Player.main?.inSeatruckPilotingChair == true)
                    {                  
                        float z = leftEulers?.z ?? 0f;    
                        z = z > 180 ? z - 360 : z;               
                        value = z < (zCenterLeft-zDeadZoneLeft) ? (zCenterLeft-zDeadZoneLeft - z) * zSensitivityLeft : 0.0f;
                    }
                    else
                    {
                        float y = leftEulers?.y ?? 0f;    
                        y = y < 180 ? 360 + y : y;               
                        value = y > (yCenterLeft_exosuit+deadZone_exosuit) ? (y - yCenterLeft_exosuit-deadZone_exosuit) * ySensitivityLeft : 0.0f;
                    }
                }
               
            }
            else if(direction == PhysicalPilotingDirection.MoveRight)
            {
                if(Player.main?.inHovercraft == true)
                {
                    //Only Move Right is the A button is presed
                    if(SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {     
                        //If using both hands to steering then take the average of both hands
                        if(leftPosition != null)
                        {
                            float x = leftPosition?.x ?? 0f;   
                            value = x > deadZoneLeft_snowbike ? (x - deadZoneLeft_snowbike) * xSensitivityLeft_snowbike : 0f;
                        }
                        if(rightPosition != null)
                        {
                            float x = rightPosition?.x ?? 0f;   
                            float rvalue = x > deadZoneLeft_snowbike ? (x - deadZoneLeft_snowbike) * xSensitivityLeft_snowbike : 0f;
                            value = value != null ? (value + rvalue) /2 : rvalue;
                        }
                    }
                    else
                    {
                        value = 0f;
                    }                       
                }
                else if(leftEulers != null)
                {                       
                    if(Player.main?.inSeatruckPilotingChair == true)
                    {                  
                        float y = leftEulers?.y ?? 0f;                   
                        y = y < 180 ? 360 + y : y;               
                        value = y > (yCenterLeft+yDeadZoneLeft) ? (y - (yCenterLeft+yDeadZoneLeft)) * ySensitivityLeft : 0.0f;
                    }
                    else
                    {
                        float x = leftEulers?.x ?? 0f;    
                        x = x < 180 ? x + 360 : x;               
                        value = x > (xCenterLeft_exosuit+deadZone_exosuit) ? (x - xCenterLeft_exosuit-deadZone_exosuit) * xSensitivityRight : 0.0f;
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveLeft)
            {
                if(Player.main?.inHovercraft == true)
                {
                    if(SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {     
                        //If using both hands to steering then take the average of both hands
                        if(leftPosition != null)
                        {
                            float x = leftPosition?.x ?? 0f;   
                            value = x < -deadZoneLeft_snowbike ? (-deadZoneLeft_snowbike - x) * xSensitivityLeft_snowbike : 0f;
                        }
                        if(rightPosition != null)
                        {
                            float x = rightPosition?.x ?? 0f;   
                            float rvalue = x < -deadZoneLeft_snowbike ? (-deadZoneLeft_snowbike - x) * xSensitivityLeft_snowbike : 0f;
                            value = value != null ? (value + rvalue) /2 : rvalue;
                        }
                    }
                    else
                    {
                        value = 0f;
                    }                       
                }
                else if(leftEulers != null)
                {    
                    if(Player.main?.inSeatruckPilotingChair == true)
                    {                  
                        float y = leftEulers?.y ?? 0f;    
                        y = y < 180 ? 360 + y : y;               
                        value = y < (yCenterLeft-yDeadZoneLeft) ? (yCenterLeft-yDeadZoneLeft - y) * ySensitivityLeft : 0.0f;
                    }
                    else
                    {
                        float x = leftEulers?.x ?? 0f;    
                        x = x < 180 ? x + 360 : x;               
                        value = x < (xCenterLeft_exosuit-deadZone_exosuit) ? (xCenterLeft_exosuit-deadZone_exosuit - x) * xSensitivityRight : 0.0f; //335 center
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveUp)
            {
                if(rightEulers != null)
                {    
                    //Only perform when the A button is pressed                       
                    if(SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {     
                        //if we want jump to be both A button press and up motion for exosuit then remove the if statement  
                        if(Player.main?.inSeatruckPilotingChair == true)
                        {                             
                            float y = rightEulers?.y ?? 0f;    
                            y = y < 180 ? 360 + y : y;               
                            value = y > (yCenterRight+yDeadZoneRight) ? (y - yCenterRight-yDeadZoneRight) * ySensitivityRight : 0.0f;
                        }
                        else 
                        {
                            value = 1.0f;
                        }
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveDown)
            {
                if(rightEulers != null)
                {     
                    //Only perform when the A button is pressed  
                    if(SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {                  
                        float y = rightEulers?.y ?? 0f;    
                        y = y < 180 ? 360 + y : y;               
                        value = y < (yCenterRight-yDeadZoneRight) ? (yCenterRight-yDeadZoneRight - y) * ySensitivityRight : 0.0f;
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
                        float y = rightEulers?.y ?? 0f;    
                        y = y < 180 ? 360 + y : y;               
                        value = y > (yCenterRight+yDeadZoneRight) ? (y - yCenterRight-yDeadZoneRight) * ySensitivityRight : 0.0f;
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
                        float y = rightEulers?.y ?? 0f;    
                        y = y < 180 ? 360 + y : y;               
                        value = y < (yCenterRight-yDeadZoneRight) ? (yCenterRight-yDeadZoneRight - y) * ySensitivityRight : 0.0f;
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.LookRight)
            {
                if(Player.main?.inHovercraft == true)
                {
                    //If A button is pressed then Move Left not Look Left
                    if(!SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {     
                        //If using both hands to steering then take the average of both hands
                        if(leftPosition != null)
                        {
                            float x = leftPosition?.x ?? 0f;   
                            value = x > deadZoneLeft_snowbike ? (x - deadZoneLeft_snowbike) * xSensitivityLeft_snowbike : 0f;
                        }
                        if(rightPosition != null)
                        {
                            float x = rightPosition?.x ?? 0f;   
                            float rvalue = x > deadZoneLeft_snowbike ? (x - deadZoneLeft_snowbike) * xSensitivityLeft_snowbike : 0f;
                            value = value != null ? (value + rvalue) /2 : rvalue;
                        }
                    }
                    else
                    {
                        value = 0f;
                    }
                }                
                else if(rightEulers != null)
                {      
                    float x = rightEulers?.x ?? 0f;    
                    x = x < 180 ? x + 360 : x;               
                    value = x < (xCenterRight-xDeadZoneRight) ? (xCenterRight-xDeadZoneRight - x) * xSensitivityRight : 0.0f; //335 center              
                }
            }
            else if(direction == PhysicalPilotingDirection.LookLeft)
            {
                if(Player.main?.inHovercraft == true)
                {
                    //If A button is pressed then Move Left not Look Left
                    if(!SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {     
                        if(leftPosition != null)
                        {
                            float x = leftPosition?.x ?? 0f;   
                            value = x < -deadZoneLeft_snowbike ? (-deadZoneLeft_snowbike - x) * xSensitivityLeft_snowbike : 0f;
                        }
                        if(rightPosition != null)
                        {
                            float x = rightPosition?.x ?? 0f;   
                            float rvalue = x < -deadZoneLeft_snowbike ? (-deadZoneLeft_snowbike - x) * xSensitivityLeft_snowbike : 0f;
                            value = value != null ? (value + rvalue) /2 : rvalue;
                        }
                    }
                    else
                    {
                        value = 0f;
                    }
                }                
                else if(rightEulers != null)
                {    
                    float x = rightEulers?.x ?? 0f;    
                    x = x < 180 ? x + 360 : x;               
                    value = x > (xCenterRight+xDeadZoneRight) ? (x - xCenterRight-xDeadZoneRight) * xSensitivityRight : 0.0f;      
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
            bool isInExosuit = Player.main?.inExosuit == true;
            bool isOnSnowBike = Player.main?.inHovercraft == true;
            bool isPilotingSeatruck = Player.main?.inSeatruckPilotingChair == true;
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
                    else if(isInExosuit)
                    {
                        leftPilotingTarget = Player.main?.GetVehicle()?.leftHandPlug;
                    }
                    else if(isOnSnowBike)
                    {
                        leftPilotingTarget = Player.main?.GetComponentInParent<Hoverbike>().leftHandIKTarget;
                    }
                    target = leftPilotingTarget;
                }
                else
                {
                    if(isPilotingSeatruck)
                    {
                        rightPilotingTarget = GameObject.Find("ControllerRight_ikTarg").transform;
                    }
                    else if(isInExosuit)
                    {
                        rightPilotingTarget = Player.main?.GetVehicle()?.rightHandPlug;
                    }
                    else if(isOnSnowBike)
                    {
                        rightPilotingTarget = Player.main?.GetComponentInParent<Hoverbike>().rightHandIKTarget;
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

            bool isInExosuit = Player.main?.inExosuit == true;
            bool isOnSnowBike = Player.main?.inHovercraft == true;
            bool isPilotingSeatruck = Player.main?.inSeatruckPilotingChair == true;
            bool isPiloting = isInExosuit || isOnSnowBike || isPilotingSeatruck;

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
            bool isInExosuit = Player.main?.inExosuit == true;
            bool isOnSnowBike = Player.main?.inHovercraft == true;
            bool isPilotingSeatruck = Player.main?.inSeatruckPilotingChair == true;
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
            if(Player.main?.inExosuit == true)
            {
                parent = parent.parent;
            }
            else if(Player.main?.inHovercraft == true)
            {
                parent = Player.main?.GetComponentInParent<Hoverbike>().transform;
            }
            grip.transform.SetParent(parent, true);
            
            //Mod.logger.LogInfo($"CreateGrip localPosition x={grip.transform.localPosition.ToString("F5")}");
            Material newMaterial = new(ShaderManager.preloadedShaders.DebugDisplaySolid);
            newMaterial.SetColor(ShaderPropertyID._Color, Color.cyan);
            grip.GetComponent<Renderer>().material = newMaterial;
            grip.SetActive(false);
            
            return grip;
        }
    }

    #region Patches
 

    #endregion

}
