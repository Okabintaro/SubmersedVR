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
        public enum MovementAxis: int
        {
            X = 0,
            Y,
            Z
        }
        public enum MovementMagnitude: int
        {
            Greater = 0,
            LessThan
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
                new float[3]{340f, 15f, 1.0f}, //exosuit - center angle, deadzone, sensitivity
                new float[3]{20f, 8f, 1.0f}, //seamoth
                new float[3]{15f, 8f, 1.0f}, //cyclops
                new float[3]{33f, 15f, 1.0f}, //seatruck
                new float[3]{355f, 20f, 0.001f}, //snowbike
            },
            new float[5][]  //backward
            {
                new float[3]{340f, 15f, 1.0f}, //exosuit
                new float[3]{20f, 8f, 1.0f}, //seamoth
                new float[3]{15f, 8f, 1.0f}, //cyclops
                new float[3]{33f, 15f, 1.0f}, //seatruck
                new float[3]{355f, 20f, 0.001f}, //snowbike
            },
            new float[5][]  //move left
            {
                new float[3]{15f, 15f, 1f}, //exosuit
                new float[3]{345f, 10f, 1f}, //seamoth
                new float[3]{335f, 15f, 0.1f}, //cyclops
                new float[3]{305f, 15f, 1.0f}, //seatruck
                new float[3]{0f, 0.008f, 0.2f}, //snowbike
            },
            new float[5][]  //move right
            {
                new float[3]{15f, 15f, 1f}, //exosuit
                new float[3]{345f, 10f, 1f}, //seamoth
                new float[3]{335f, 15f, 0.1f}, //cyclops
                new float[3]{305f, 15f, 1.0f}, //seatruck
                new float[3]{0f, 0.008f, 0.2f}, //snowbike
            },
            new float[5][]  //move up
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{20f, 10f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{340f, 15f, 0.03f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike not used
            },
            new float[5][]  //move down
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{20f, 10f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{340f, 15f, 0.03f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike not used
            },
            new float[5][]  //lookup up
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{20f, 10f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{340f, 15f, 0.03f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike not used
            },
            new float[5][]  //look down
            {
                new float[3]{341f, 15f, 0.03f}, //exosuit
                new float[3]{20f, 10f, 0.03f}, //seamoth
                new float[3]{355f, 15f, 0.03f}, //cyclops
                new float[3]{340f, 15f, 0.03f}, //seatruck
                new float[3]{0f, 0f, 0.0f}, //snowbike not used
            },
            new float[5][]  //lookup left
            {
                new float[3]{355f, 15f, 0.03f}, //exosuit
                new float[3]{345f, 15f, 0.07f}, //seamoth
                new float[3]{340f, 8f, 0.07f}, //cyclops
                new float[3]{335f, 15f, 0.03f}, //seatruck
                new float[3]{0f, 0.004f, 10.0f}, //snowbike
            },
            new float[5][]  //look right
            {
                new float[3]{355f, 15f, 0.03f}, //exosuit
                new float[3]{345f, 15f, 0.07f}, //seamoth
                new float[3]{340f, 8f, 0.07f}, //cyclops
                new float[3]{335f, 15f, 0.03f}, //seatruck
                new float[3]{0f, 0.004f, 10.0f}, //snowbike
            },

        };
        static float gripDistance = 0.13f; // The distance away from a target control where the grip will recognize contact

#if BZ
        public static  bool inHovercraft{get {return Player.main?.inHovercraft == true;} }
        public static  bool inSeaTruck{get {return Player.main?.inSeatruckPilotingChair == true;} }
#else
        public static  bool inHovercraft{get {return false;} }
        public static  bool inSeaTruck{get {return false;} }
#endif

        public static  bool inSeamoth{get {return Player.main?.inSeamoth == true;} }
        public static  bool inExosuit{get {return Player.main?.inExosuit == true;} }
        public static  bool inCyclops{get {return Player.main?.currentSub?.isCyclops == true && Player.main?.isPiloting == true;} }

        public static float GetCenterAngle(PhysicalPilotingDirection direction, int vehicleType, PhysicalPilotingHand? hand = null)
        {
            float offset = 0.0f;
            if(vehicleType == (int)PhysicalVehicle.Seamoth)
            {
                if(direction == PhysicalPilotingDirection.MoveForward || direction == PhysicalPilotingDirection.MoveBackward)
                {
                    offset = 10f * Settings.SeamothLeftVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookUp || direction == PhysicalPilotingDirection.LookDown || direction == PhysicalPilotingDirection.MoveUp || direction == PhysicalPilotingDirection.MoveDown)
                {
                    offset = 10f * Settings.SeamothRightVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.MoveLeft || direction == PhysicalPilotingDirection.MoveRight)
                {
                    offset = 20f * Settings.SeamothRightHorizontalCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookLeft || direction == PhysicalPilotingDirection.LookRight)
                {
                    offset = 20f * (hand == PhysicalPilotingHand.Right ? Settings.SeamothRightHorizontalCenterAngle : Settings.SeamothLeftHorizontalCenterAngle) / 10.0f;
                }
            }
            else if(vehicleType == (int)PhysicalVehicle.Exosuit)
            {
                if(direction == PhysicalPilotingDirection.MoveForward || direction == PhysicalPilotingDirection.MoveBackward)
                {
                    offset = 10f * Settings.ExosuitLeftVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookUp || direction == PhysicalPilotingDirection.LookDown || direction == PhysicalPilotingDirection.MoveUp || direction == PhysicalPilotingDirection.MoveDown)
                {
                    offset = 10f * Settings.ExosuitRightVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.MoveLeft || direction == PhysicalPilotingDirection.MoveRight)
                {
                    offset = 20f * Settings.ExosuitLeftHorizontalCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookLeft || direction == PhysicalPilotingDirection.LookRight)
                {
                    offset = 20f * Settings.ExosuitRightHorizontalCenterAngle / 10.0f;
                }
            }
            else if(vehicleType == (int)PhysicalVehicle.Cyclops)
            {
                if(direction == PhysicalPilotingDirection.MoveForward || direction == PhysicalPilotingDirection.MoveBackward)
                {
                    offset = 10f * Settings.CyclopsLeftVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookUp || direction == PhysicalPilotingDirection.LookDown || direction == PhysicalPilotingDirection.MoveUp || direction == PhysicalPilotingDirection.MoveDown || direction == PhysicalPilotingDirection.MoveLeft || direction == PhysicalPilotingDirection.MoveRight)
                {
                    offset = 10f * Settings.CyclopsRightVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookLeft || direction == PhysicalPilotingDirection.LookRight)
                {
                    offset = 20f * (hand == PhysicalPilotingHand.Right ? Settings.CyclopsRightHorizontalCenterAngle : Settings.CyclopsLeftHorizontalCenterAngle) / 10.0f;
                }
            }
            else if(vehicleType == (int)PhysicalVehicle.SeaTruck)
            {
                gripArray[(int)PhysicalPilotingDirection.MoveLeft][vehicleType][0] = Settings.SeatruckAltLeftGrip ? 220f : 305f;
                gripArray[(int)PhysicalPilotingDirection.MoveRight][vehicleType][0] = Settings.SeatruckAltLeftGrip ? 220f : 305f;
                gripArray[(int)PhysicalPilotingDirection.MoveForward][vehicleType][0] = Settings.SeatruckAltLeftGrip ? 315f : 15f;
                gripArray[(int)PhysicalPilotingDirection.MoveBackward][vehicleType][0] = Settings.SeatruckAltLeftGrip ? 315f: 15f;
                if(direction == PhysicalPilotingDirection.MoveForward || direction == PhysicalPilotingDirection.MoveBackward)
                {
                    offset = 15f * Settings.SeatruckLeftVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookUp || direction == PhysicalPilotingDirection.LookDown || direction == PhysicalPilotingDirection.MoveUp || direction == PhysicalPilotingDirection.MoveDown)
                {
                    offset = 10f * Settings.SeatruckRightVerticleCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.MoveLeft || direction == PhysicalPilotingDirection.MoveRight)
                {
                    offset = 10f * Settings.SeatruckLeftHorizontalCenterAngle / 10.0f;
                }
                else if(direction == PhysicalPilotingDirection.LookLeft || direction == PhysicalPilotingDirection.LookRight)
                {
                    offset = 20f * Settings.SeatruckRightHorizontalCenterAngle / 10.0f;
                }
            }
            else if(vehicleType == (int)PhysicalVehicle.SnowBike)
            {
                if(direction == PhysicalPilotingDirection.MoveForward || direction == PhysicalPilotingDirection.MoveBackward)
                {
                    offset = 20f * Settings.SnowbikeRightVerticleCenterAngle / 10.0f;
                }
            }
            return gripArray[(int)direction][vehicleType][0] + offset;
        }

        public static float GetDeadZone(PhysicalPilotingDirection direction, int vehicleType, PhysicalPilotingHand? hand = null)
        {
            float offset = 0.0f;
            if(vehicleType == (int)PhysicalVehicle.Seamoth)
            {
                offset = gripArray[(int)direction][vehicleType][1] * (10f - ((hand == PhysicalPilotingHand.Right ? Settings.SeamothRightDeadZone : Settings.SeamothLeftDeadZone) * 2f)) / 10.0f;               
            }
            else if(vehicleType == (int)PhysicalVehicle.Exosuit)
            {
                offset = gripArray[(int)direction][vehicleType][1] * (10f - ((hand == PhysicalPilotingHand.Right ? Settings.ExosuitRightDeadZone : Settings.ExosuitLeftDeadZone) * 2f)) / 10.0f;               
            }
            else if(vehicleType == (int)PhysicalVehicle.Cyclops)
            {
                offset = gripArray[(int)direction][vehicleType][1] * (10f - ((hand == PhysicalPilotingHand.Right ? Settings.CyclopsRightDeadZone : Settings.CyclopsLeftDeadZone) * 2f)) / 10.0f;               
            }
            else if(vehicleType == (int)PhysicalVehicle.SeaTruck)
            {
                offset = gripArray[(int)direction][vehicleType][1] * (10f - ((hand == PhysicalPilotingHand.Right ? Settings.SeatruckRightDeadZone : Settings.SeatruckLeftDeadZone) * 2f)) / 10.0f;               
            }
            else if(vehicleType == (int)PhysicalVehicle.SnowBike)
            {
                //Not using deadzone for snowbike since adjusting the "Comfort" setting for the snowbike is the same as adjsting the deadzone
            }
            return gripArray[(int)direction][vehicleType][1] + offset;
        }

        public static float GetSensitivity(PhysicalPilotingDirection direction, int vehicleType, PhysicalPilotingHand? hand = null)
        {
            float offset = 0.0f;
            if(vehicleType == (int)PhysicalVehicle.SnowBike && (direction == PhysicalPilotingDirection.LookLeft || direction == PhysicalPilotingDirection.LookRight))
            {
                offset = gripArray[(int)direction][vehicleType][2] * -(10f - ((hand == PhysicalPilotingHand.Right ? Settings.SnowbikeRightDeadZone : Settings.SnowbikeLeftDeadZone) * 2f)) / 10.0f;       //8/10 - -8/10        
            }
            return gripArray[(int)direction][vehicleType][2] + offset;
        }

        public static float? GetValue(PhysicalPilotingDirection direction, Vector3? eulers, int vehicleType, MovementAxis axis, MovementMagnitude magnitude, PhysicalPilotingHand? hand = null)
        {
            float? value = null;
            if(eulers != null)
            {    
                float angle = axis == MovementAxis.X ? eulers?.x ?? 0f : (axis == MovementAxis.Y ? eulers?.y ?? 0f : eulers?.z ?? 0f);
                float delta = (magnitude == MovementMagnitude.LessThan ? -1 : 1) * Mathf.DeltaAngle(angle, GetCenterAngle(direction, vehicleType, hand));
                //if the grip angle exceeds the deadzone then return the sensitivity scaled movement
                float deadzone = GetDeadZone(direction,  vehicleType, hand);
                value = delta > deadzone ? ((delta - deadzone) * GetSensitivity(direction, vehicleType, hand)) : 0.0f;
            } 
            return value;               
        }
        public static float? GetPositionValue(PhysicalPilotingDirection direction, Vector3? position, int vehicleType, MovementAxis axis, MovementMagnitude magnitude, PhysicalPilotingHand? hand = null)
        {
            float? value = null;
            if(position != null)
            {    
                float pos = axis == MovementAxis.X ? position?.x ?? 0f : (axis == MovementAxis.Y ? position?.y ?? 0f : position?.z ?? 0f);
                float delta = (magnitude == MovementMagnitude.LessThan ? -1 : 1) * pos;
                float deadzone = GetDeadZone(direction,  vehicleType, hand);
                value = delta > deadzone ? ((delta - deadzone) * GetSensitivity(direction, vehicleType, hand)) : 0.0f;
            } 
            return value;               
        }

        public static float? GetValue(PhysicalPilotingDirection direction)
        {
            float? value = null;

            Vector3? leftEulers = GetPilotingHandLocalRotation(PhysicalPilotingHand.Left)?.eulerAngles;
            Vector3? rightEulers = GetPilotingHandLocalRotation(PhysicalPilotingHand.Right)?.eulerAngles;
            Vector3? leftPosition = GetPilotingHandLocalPosition(PhysicalPilotingHand.Left);
            Vector3? rightPosition = GetPilotingHandLocalPosition(PhysicalPilotingHand.Right);
            //DebugPanel.Show($"inHovercraft: {inHovercraft}\nleft: {leftEulers}\nright: {rightEulers}\nleftPos: {leftPosition?.ToString("F3")}\nrightPos: {rightPosition?.ToString("F3")}\nrightHandGrip:{IsGrippingPilotingControl(PhysicalPilotingHand.Right)}");
            int vehicleType = (int)(inExosuit ? PhysicalVehicle.Exosuit : (inSeamoth ? PhysicalVehicle.Seamoth : (inCyclops ? PhysicalVehicle.Cyclops : (inSeaTruck ? PhysicalVehicle.SeaTruck : (inHovercraft ? PhysicalVehicle.SnowBike : PhysicalVehicle.None)))));

            if(direction == PhysicalPilotingDirection.MoveForward)
            {   
                if(inHovercraft)
                {
                    value = GetValue(direction, rightEulers, vehicleType, MovementAxis.Z, Settings.SnowbikeAltAccelerator ? MovementMagnitude.LessThan : MovementMagnitude.Greater);
                }
                else if(inSeaTruck)
                {
                    value = GetValue(direction, leftEulers, vehicleType, Settings.SeatruckAltLeftGrip ? MovementAxis.X : MovementAxis.Z, MovementMagnitude.LessThan);
                }
                else
                {
                    value = GetValue(direction, leftEulers, vehicleType, MovementAxis.Y, MovementMagnitude.Greater);
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveBackward)
            {   
                if(inHovercraft)
                {
                    value = GetValue(direction, rightEulers, vehicleType, MovementAxis.Z, Settings.SnowbikeAltAccelerator ? MovementMagnitude.Greater : MovementMagnitude.LessThan);
                }
                else if(inSeaTruck)
                {
                    value = GetValue(direction, leftEulers, vehicleType, Settings.SeatruckAltLeftGrip ? MovementAxis.X : MovementAxis.Z, MovementMagnitude.Greater);
                }
                else
                {
                    value = GetValue(direction, leftEulers, vehicleType, MovementAxis.Y, MovementMagnitude.LessThan);
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveLeft)
            {
                if(inHovercraft)
                {
                    if(SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {  
                        value = GetPositionValue(direction, rightPosition, vehicleType, MovementAxis.X, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);
                        float? lvalue = GetPositionValue(direction, leftPosition, vehicleType, MovementAxis.X, MovementMagnitude.LessThan);
                        value = (value != null && lvalue != null) ? (value + lvalue) /2 : value ?? lvalue;                   
                    }
                    else
                    {
                        value = 0f;
                    }                       
                }
                else if(inExosuit) 
                {
                    value = GetValue(direction, leftEulers, vehicleType, MovementAxis.X, MovementMagnitude.Greater);
                }
                else if(inCyclops) 
                {
                    value = GetValue(direction, rightEulers, vehicleType, MovementAxis.X, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);
                    if(leftEulers != null)
                    {
                        //we already checked for null so we can cast to non-null
                        float lvalue = (float)GetValue(direction, leftEulers, vehicleType, MovementAxis.X, MovementMagnitude.Greater);
                        value = value != null ? (value + lvalue) /2 : lvalue;
                    }
                }
                else if(inSeaTruck) 
                {
                    value = GetValue(direction, leftEulers, vehicleType, MovementAxis.Y, MovementMagnitude.Greater);
                }
                else
                {   
                    if(rightEulers!= null && SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {    
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.X, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.MoveRight)
            {
                if(inHovercraft)
                {
                    if(SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {  
                        value = GetPositionValue(direction, rightPosition, vehicleType, MovementAxis.X, MovementMagnitude.Greater, PhysicalPilotingHand.Right);
                        float? lvalue = GetPositionValue(direction, leftPosition, vehicleType, MovementAxis.X, MovementMagnitude.Greater);
                        value = (value != null && lvalue != null) ? (value + lvalue) /2 : value ?? lvalue;                                          
                    }
                    else
                    {
                        value = 0f;
                    }                       
                }
                else if(inExosuit) 
                {
                    value = GetValue(direction, leftEulers, vehicleType, MovementAxis.X, MovementMagnitude.LessThan);
                }                  
                else if(inCyclops) 
                {
                    value = GetValue(direction, rightEulers, vehicleType, MovementAxis.X, MovementMagnitude.Greater, PhysicalPilotingHand.Right);
                    if(leftEulers!= null)
                    {      
                        //we already checked for null so we can cast to non-null
                        float lvalue = (float)GetValue(direction, leftEulers, vehicleType, MovementAxis.X, MovementMagnitude.LessThan);
                        value = value != null ? (value + lvalue) /2 : lvalue;
                    }
                }
                else if(inSeaTruck) 
                {
                    value = GetValue(direction, leftEulers, vehicleType, MovementAxis.Y, MovementMagnitude.LessThan);
                }
                else
                {    
                    if(rightEulers!= null && SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {      
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.X, MovementMagnitude.Greater, PhysicalPilotingHand.Right);
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
                        if(inCyclops || inSeamoth || inSeaTruck)
                        {      
                            value = GetValue(direction, rightEulers, vehicleType, MovementAxis.Y, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);                       
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
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.Y, MovementMagnitude.Greater, PhysicalPilotingHand.Right);                       
                        //DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\ndelta: {delta}\nspeed: {value}");
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
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.Y, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);                       
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
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.Y, MovementMagnitude.Greater, PhysicalPilotingHand.Right);                       
                    }
                }
            }
            else if(direction == PhysicalPilotingDirection.LookLeft)
            {
                if(inHovercraft)
                {
                    //Only turn if A button not pressed
                    if(!SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {
                        value = GetPositionValue(direction, rightPosition, vehicleType, MovementAxis.X, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);
                        float? lvalue = GetPositionValue(direction, leftPosition, vehicleType, MovementAxis.X, MovementMagnitude.LessThan);
                        value = (value != null && lvalue != null) ? (value + lvalue) /2 : value ?? lvalue;                   
                    }
                }
                else
                {
                    if(rightEulers != null && (!(inCyclops || inSeamoth) || ((inCyclops || inSeamoth) && !SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))))
                    {    
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.X, MovementMagnitude.LessThan, PhysicalPilotingHand.Right);                       
                    }   
                    if(leftEulers != null && (inCyclops || inSeamoth))
                    {    
                        float lvalue = (float)GetValue(direction, leftEulers, vehicleType, MovementAxis.X, MovementMagnitude.Greater);                       
                        value = value != null && !SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand) ? (value + lvalue) /2 : lvalue;
                    }                  
                }
            }
            else if(direction == PhysicalPilotingDirection.LookRight)
            {
                if(inHovercraft)
                {
                    if(!SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))
                    {
                        value = GetPositionValue(direction, rightPosition, vehicleType, MovementAxis.X, MovementMagnitude.Greater, PhysicalPilotingHand.Right);
                        float? lvalue = GetPositionValue(direction, leftPosition, vehicleType, MovementAxis.X, MovementMagnitude.Greater);
                        value = (value != null && lvalue != null) ? (value + lvalue) /2 : value ?? lvalue;   
                    }                                       
                }
                else
                {
                    if(rightEulers != null && (!(inCyclops || inSeamoth) || ((inCyclops || inSeamoth) && !SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand))))
                    {      
                        value = GetValue(direction, rightEulers, vehicleType, MovementAxis.X, MovementMagnitude.Greater, PhysicalPilotingHand.Right);                       
                    }   
                    if(leftEulers != null && (inCyclops || inSeamoth))
                    {    
                        float lvalue = (float)GetValue(direction, leftEulers, vehicleType, MovementAxis.X, MovementMagnitude.LessThan);                       
                        value = value != null && !SteamVR_Actions.subnautica.LeftHand.GetState(SteamVR_Input_Sources.RightHand) ? (value + lvalue) /2 : lvalue;
                        //DebugPanel.Show($"left: {leftEulers}\nright: {rightEulers}\ndelta: {delta}\nlvalue = {lvalue}\nspeed: {value}\nmode = {Player.main?.currentSub.GetComponent<SubControl>().controlMode}");
                    }   
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
            bool isInSeamoth = inSeamoth;
            bool isInExosuit = inExosuit;
            bool isOnSnowBike = inHovercraft;
            bool isInCyclops = inCyclops;
            bool isPilotingSeatruck = inSeaTruck;
            Transform target = hand == PhysicalPilotingHand.Left ? leftPilotingTarget : rightPilotingTarget;
            if(target == null)
            {
                if(hand == PhysicalPilotingHand.Left)
                {
                    if(isPilotingSeatruck)
                    {
                        leftPilotingTarget = GameObject.Find("ControllerLeft_ikTarg").transform;
                        //leftPilotingTarget.eulerAngles += new Vector3(-90f, 0f, 0f);
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
#if BZ
                        leftPilotingTarget = Player.main?.GetComponentInParent<Hoverbike>().leftHandIKTarget;
#else
                        leftPilotingTarget = null; 
#endif
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
#if BZ
                        rightPilotingTarget = Player.main?.GetComponentInParent<Hoverbike>().rightHandIKTarget;
#else
                        rightPilotingTarget = null; 
#endif
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

            bool isPiloting = inExosuit || inHovercraft || inSeaTruck || inSeamoth || inCyclops;

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
            bool isInSeamoth = inSeamoth;
            bool isInExosuit = inExosuit;
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
            //Mod.logger.LogInfo($"Creating Grip");
            GameObject grip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            GameObject.Destroy(grip.GetComponent<SphereCollider>());
            grip.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            grip.transform.position = target.position;
            grip.transform.rotation = target.rotation;
            Transform parent = target.parent;
            if(inExosuit || inSeamoth || inCyclops)
            {
                parent = parent.parent;
            }
            else if(inHovercraft)
            {
#if BZ
                parent = Player.main?.GetComponentInParent<Hoverbike>().transform;
#endif
            }
            grip.transform.SetParent(parent, true);
            
            Material newMaterial = new(ShaderManager.preloadedShaders.DebugDisplaySolid);
            newMaterial.SetColor(ShaderPropertyID._Color, Color.cyan);
            grip.GetComponent<Renderer>().material = newMaterial;
            grip.SetActive(false); //Set to true to see the grip models
            
            return grip;
        }
    }

    #region Patches
 
    //Make the Cyclops steering move less so that physical driving feels better
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



