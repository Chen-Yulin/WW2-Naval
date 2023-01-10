using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.Networking;

namespace WW2NavalAssembly
{
    public class Controller : BlockScript
    {
        public int myPlayerID;

        public MKey TrackCannon;
        public MKey SwitchCannnon;

        public bool TrackOn;
        public Camera _viewCamera;

        public GameObject TargetCannon;

        public Camera MainCamera
        {
            get
            {
                bool flag;
                if (this._viewCamera == null)
                {
                    MouseOrbit instance = SingleInstanceFindOnly<MouseOrbit>.Instance;
                    flag = (((instance != null) ? instance.cam : null) != null);
                }
                else
                {
                    flag = false;
                }
                bool flag2 = flag;
                if (flag2)
                {
                    this._viewCamera = SingleInstanceFindOnly<MouseOrbit>.Instance.cam;
                }
                bool flag3 = this._viewCamera == null;
                if (flag3)
                {
                    this._viewCamera = Camera.main;
                }
                return this._viewCamera;
            }
        }

        public override void SafeAwake()
        {
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            TrackCannon = AddKey("Track Cannon", "TrackCannon", KeyCode.T);
            SwitchCannnon = AddKey("Switch Tracking Cannon", "SwitchTrackingCannon", KeyCode.RightShift);
        }

        public override void OnSimulateStop()
        {
            SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;
        }

        public override void SimulateLateUpdateAlways()
        {
            if (StatMaster.isMP)
            {
                if (PlayerData.localPlayer.networkId != myPlayerID)
                {
                    return;
                }
            }
            if (TrackCannon.IsHeld)
            {
                if (!TrackOn)
                {
                    TargetCannon = CannonTrackManager.Instance.GetTrackCannon(myPlayerID);
                }
                TrackOn = true;
            }
            else
            {
                TrackOn = false;
            }
            if (TrackOn)
            {
                if (SwitchCannnon.IsPressed)
                {
                    TargetCannon = CannonTrackManager.Instance.SwitchTrackCannon(myPlayerID);
                }
                if (TargetCannon && 
                    !(TargetCannon.transform.position.y < 20) &&
                    !TargetCannon.GetComponent<BulletBehaviour>().exploded)
                {
                    SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = false;
                    MainCamera.transform.position = Vector3.Lerp(MainCamera.transform.position, TargetCannon.transform.position - 30f * TargetCannon.transform.forward,0.2f);
                    MainCamera.transform.rotation = Quaternion.LookRotation(TargetCannon.transform.position + TargetCannon.GetComponent<Rigidbody>().velocity);
                    float tmpAngle = Vector3.Angle(TargetCannon.transform.position-MainCamera.transform.position, Vector3.up);
                    tmpAngle = 90 - tmpAngle;
                    MainCamera.transform.rotation = Quaternion.Euler(-tmpAngle, MainCamera.transform.eulerAngles.y, 0);
                }
            }
            else
            {
                SingleInstanceFindOnly<MouseOrbit>.Instance.isActive = true;
            }
        }
    }
}
