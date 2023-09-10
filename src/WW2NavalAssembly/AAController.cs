using Modding;
using Modding.Blocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static EntityAI;
using static WW2NavalAssembly.Controller;

namespace WW2NavalAssembly
{
    public class AAController : BlockScript
    {
        public int myPlayerID;
        public int myGuid;
        public int mySeed;

        public MKey SwitchTarget;

        public int CurrentTarget = -1;
        public bool hasTarget
        {
            get
            {
                return CurrentTarget >= 0;
            }
        }

        public List<Aircraft> DetectedAircraft = new List<Aircraft>();
        public Dictionary<float, FCResult> FCResults = new Dictionary<float, FCResult>();

        public bool AAFCInitialized = false;

        public Texture LockIcon;
        int iconSize = 30;

        public Vector2 GetForward()
        {
            if (StatMaster.isClient)
            {
                return new Vector2(-transform.up.x, -transform.up.z);
                //return new Vector2(BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].x, BlockPoseReceiver.Instance.forward[myPlayerID][myGuid].z);
            }
            else
            {
                return new Vector2(-transform.up.x, -transform.up.z);
            }
        }
        public Dist2PitchResult CalculateGunPitchFromDist(float dist, float caliber, float targetHeight = 60f)
        {
            float cannonDrag = caliber > 100 ? 5000f / (caliber * caliber) : 1 - caliber / 200f;
            //Debug.Log("Start Iterating");
            float initialSpeed = (130 + 0.08f * (caliber + 50) + ((18000) / (caliber + 100))) * 2;
            float g = 32.4f;
            float vx;
            float vy;
            float sy;// gravity direction positive
            float esT = 0;
            float angle = 0;
            for (int i = 0; i < 6; i++)
            {
                vx = initialSpeed * Mathf.Cos(angle);
                vy = -initialSpeed * Mathf.Sin(angle);
                esT = -1 / cannonDrag * Mathf.Log(1 - dist * cannonDrag / vx);
                if (esT > 4f)
                {
                    return new Dist2PitchResult();
                }
                sy = -Mathf.Exp(-cannonDrag * esT) *
                    ((cannonDrag * Mathf.Exp(cannonDrag * esT) - cannonDrag) * vy + ((cannonDrag * esT - 1) * Mathf.Exp(cannonDrag * esT) + 1) * g)
                    / (cannonDrag * cannonDrag) - targetHeight;

                float pre_sy = dist * Mathf.Tan(angle);
                angle = (float)Math.Atan((pre_sy - sy) / dist);
            }
            return new Dist2PitchResult(esT, angle * 180 / Mathf.PI);
        }
        public FCResult CalculateGunFCPara(Vector3 targetPosition, Vector3 velocity, float caliber)
        {
            float targetHeight = targetPosition.y - 20f;
            Vector3 myPosition = new Vector3(transform.position.x, 21f, transform.position.z);
            float dist2D = MathTool.Get2DDistance(targetPosition, myPosition);
            Dist2PitchResult pitchRes = CalculateGunPitchFromDist(dist2D, caliber, targetHeight);
            if (pitchRes.hasResult)
            {
                targetPosition = targetPosition + velocity * pitchRes.time;
                dist2D = MathTool.Get2DDistance(targetPosition, myPosition);
                pitchRes = CalculateGunPitchFromDist(dist2D, caliber, targetHeight);
                if (pitchRes.hasResult) // valid result
                {
                    float Orien = MathTool.SignedAngle(GetForward(), MathTool.Get2DCoordinate(targetPosition + velocity * pitchRes.time - myPosition));
                    return new FCResult(Orien, pitchRes.pitch, MathTool.Get2DCoordinate(targetPosition + velocity * pitchRes.time), pitchRes.time);
                }
                else
                {
                    return new FCResult(MathTool.SignedAngle(GetForward(), MathTool.Get2DCoordinate(targetPosition - myPosition)));
                }
            }
            else
            {
                return new FCResult(MathTool.SignedAngle(GetForward(), MathTool.Get2DCoordinate(targetPosition - myPosition)));
            }
        }
        public void InitAAFireControl()
        {
            foreach (var calibergroup in FireControlManager.Instance.Guns[myPlayerID])
            {
                {// determine whether the calibergroup is valid
                    if (calibergroup.Value.Count == 0)
                    {
                        continue;
                    }

                    int validGun = 0;
                    foreach (var gun in calibergroup.Value)
                    {
                        try
                        {
                            if (gun.Value.GetComponent<Gun>().myPlayerID == myPlayerID)
                            {
                                validGun++;
                            }
                        }
                        catch { }
                    }
                    if (validGun == 0)
                    {
                        continue;
                    }
                }

                if (!FCResults.ContainsKey(calibergroup.Key))
                {
                    FCResults.Add(calibergroup.Key, new FCResult(0));
                }
            }
        }
        public void UpdateDetectedAircraft()
        {
            foreach (var leader in Grouper.Instance.AircraftLeaders[ModController.Instance.state % 16])
            {
                Aircraft a = leader.Value.Value;
                if (Vector3.Distance(a.transform.position, transform.position) < 500 && a.isFlying)
                {
                    if (!DetectedAircraft.Contains(a))
                    {
                        DetectedAircraft.Add(a);
                    }
                }
                else if (DetectedAircraft.Contains(a))
                {
                    DetectedAircraft.Remove(a);
                }
            }
        }
        public void UpdateAAResult()
        {
            CurrentTarget = Mathf.Clamp(CurrentTarget, -999, DetectedAircraft.Count - 1);
            if (CurrentTarget < 0)
            {

                if (DetectedAircraft.Count != 0)
                {
                    CurrentTarget = 0;
                }
            }
            else
            {
                Aircraft target = DetectedAircraft[CurrentTarget];
                Vector3 targetPos = target.transform.position;
                Vector3 targetVel = target.GetComponent<Rigidbody>().velocity;
                foreach (var fcRes in FCResults)
                {
                    FCResult res = CalculateGunFCPara(targetPos, targetVel, fcRes.Key);
                    fcRes.Value.Set(res.Orien, res.Pitch, res.hasRes, res.predPosition, res.timer);
                    //Debug.Log(fcRes.Key + " " + res.hasRes + " " + res.Pitch);
                }
                ControllerDataManager.Instance.AAControllerFCResult[myPlayerID] = FCResults;

            }
        }

        public override void SafeAwake()
        {
            gameObject.name = "Captain";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            mySeed = (int)(UnityEngine.Random.value * 16);

            SwitchTarget = AddKey("Switch Target", "Switch Target", KeyCode.T);
            LockIcon = ModResource.GetTexture("AA Lock Icon").Texture;
        }
        public void Start()
        {
            gameObject.name = "AA Captain";
        }
        public override void OnSimulateStart()
        {

            myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
            ControllerDataManager.Instance.aaController[myPlayerID] = this;
        }
        public override void OnSimulateStop()
        {
            ControllerDataManager.Instance.aaController[myPlayerID] = null;
        }
        public void OnDestroy()
        {
        }
        public override void SimulateUpdateHost()
        {
            if (SwitchTarget.IsPressed)
            {
                CurrentTarget++;
                if (CurrentTarget > DetectedAircraft.Count - 1)
                {
                    CurrentTarget = 0;
                }
            }
        }
        public override void SimulateFixedUpdateAlways()
        {
            if (!AAFCInitialized)
            {
                AAFCInitialized = true;
                InitAAFireControl();
            }
            if (!StatMaster.isMP ||(StatMaster.isMP && PlayerData.localPlayer.networkId == myPlayerID))
            {
                // update detected aircraft
                UpdateDetectedAircraft();
                // update AA FC results
                UpdateAAResult();
            }
        }

        public void OnGUI()
        {
            if (StatMaster.isMP)
            {
                if (myPlayerID != PlayerData.localPlayer.networkId)
                {
                    return;
                }
            }
            AircraftController ac = FlightDataBase.Instance.aircraftController[myPlayerID];


            foreach (var a in DetectedAircraft)
            {
                try
                {
                    Aircraft target = a;
                    if (Vector3.Distance(target.transform.position, transform.position) < (target.myGroup.Count * 150) + 400 &&
                        target.isFlying)
                    {
                        // if is self aircraft and inTacticalView, skip
                        if (ac)
                        {
                            if (ac.inTacticalView && target.myPlayerID == myPlayerID)
                            {
                                continue;
                            }
                        }

                        // draw target
                        Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(target.transform.position);
                        if (target.BlockBehaviour.Team.Equals(BlockBehaviour.Team))
                        {
                            GUI.contentColor = Color.yellow;
                        }
                        else
                        {
                            GUI.contentColor = Color.red;
                        }
                        if (onScreenPosition.z >= 0)
                        {
                            GUI.Box(new Rect(onScreenPosition.x - 50, Camera.main.pixelHeight - onScreenPosition.y - 20, 100, 25), target.Type.Selection.ToString() + " *" + target.myGroup.Count.ToString() + "*");
                        }
                    }
                }
                catch { }
            }

            try
            {
                if (CurrentTarget >= 0)
                {
                    Vector3 onScreenPosition = Camera.main.WorldToScreenPoint(DetectedAircraft[CurrentTarget].transform.position);
                    if (onScreenPosition.z >= 0)
                    {
                        GUI.DrawTexture(new Rect(onScreenPosition.x - iconSize / 2, Camera.main.pixelHeight - onScreenPosition.y - iconSize / 2, iconSize, iconSize), LockIcon);
                    }
                }
            }
            catch { }
        }
    }
}
