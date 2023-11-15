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
using System.Diagnostics.Eventing.Reader;

namespace WW2NavalAssembly
{
    public class ModCameraController : SingleInstance<ModCameraController>
    {
        public override string Name { get; } = "ModCameraController";

        public bool valid = false;
        public GameObject Caller = null;

        public Transform anchor = null;

        public bool localRotation = false;

        public float pitch;
        public float yaw;

        public bool needCamera;

        public enum Mode
        {
            MO,
            FPV,
            TAC
        }
        private Mode _mode = Mode.MO;
        public Mode mode
        {
            set {
                _mode = value;
                if (!MO)
                {
                    MO = SingleInstanceFindOnly<MouseOrbit>.Instance;
                }
                if (value == Mode.MO)
                {
                    MO.isActive = true;
                    FPV.IsActive = false;
                    TAC.IsActive = false;

                }
                else if (value == Mode.FPV)
                {
                    MO.isActive = false;
                    FPV.IsActive = true;
                    TAC.IsActive = false;
                }
                else
                {
                    MO.isActive = false;
                    FPV.IsActive = false;
                    TAC.IsActive = true;
                }
            }
            get { return mode; }
        }

        public Camera camera;
        

        // mouse orbit
        public MouseOrbit MO;
        public BlockBehaviour MO_pre_block = null;
        

        // FPV
        public FPVCamera FPV;

        // TAC
        public TacCamera TAC;

        public bool FindCamera()
        {
            try
            {
                if (!camera)
                {
                    MO = SingleInstanceFindOnly<MouseOrbit>.Instance;
                    if (!MO)
                    {
                        return false;
                    }
                    camera = MO.GetComponent<Camera>();
                    FPV = camera.gameObject.AddComponent<FPVCamera>();
                    TAC = camera.gameObject.AddComponent<TacCamera>();
                    mode = Mode.MO;
                }
                return true;
            }
            catch { return false; }
        }
        public void Awake()
        {
            FindCamera();
        }

        public void EnableModCameraMO(GameObject caller, Transform target, Machine parentMachine)
        {
            mode = Mode.MO;
            BlockBehaviour target_bb = target.GetComponent<BlockBehaviour>();
            if (!target_bb)
            {
                target_bb = target.gameObject.AddComponent<BlockBehaviour>();
                target_bb.SetParentMachine(parentMachine);
            }
            target_bb.hasOffset = true;

            if (Caller == null)
            {
                if (MO.target)
                {
                    MO_pre_block = MO.target.transform.GetComponent<BlockBehaviour>();
                }
                else
                {
                    MO_pre_block = null;
                }
            }
            Caller = caller;
            
            MO.SetTarget(target_bb);
        }
        public void DisableModCameraMO(GameObject caller)
        {
            if (Caller == caller)
            {
                Caller = null;
                if (MO_pre_block)
                {
                    MO.SetTarget(MO_pre_block);
                }
                else
                {
                    MO.target = null;
                    MO.targetType = MouseOrbit.TargetType.Machine;
                }
                
            }
        }

        public void EnableModCameraFPV(Transform target)
        {

            if (target)
            {
                FPV.Base = target;
                FPV.rotationX = 0;
                FPV.rotationY = 0;
                FPV.PosOffset = AircraftAssetManager.Instance.GetCockpitOffset(target.parent.GetComponent<Aircraft>().preAppearance);
                mode = Mode.FPV;
            }
        }
        public void DisableModCamerFPV()
        {
            mode = Mode.MO;
            FPV.Base = null;
        }

        public void EnableModCameraTAC(Transform target, float Sensitivity, MKey reset, MKey move, AircraftController ac)
        {
            if (target)
            {
                TAC.Base = target;
                TAC.ViewSensitivity = Sensitivity;
                TAC.ViewMove = move;
                TAC.ResetView = reset;
                TAC.AC = ac;
                mode = Mode.TAC;
            }
        }

        public void DisableModCameraTAC()
        {
            _mode = Mode.MO;
            mode = Mode.MO;
        }

        public void Update()
        {
            if (needCamera)
            {
                needCamera = ! FindCamera();
                //Debug.Log("Find Camera " + !needCamera);
            }
        }

        private void OnGUI()
        {
            //GUI.Box(new Rect(100, 200, 200, 30), MO.targetType.ToString());
            //GUI.Box(new Rect(100, 200, 200, 30), (MO_pre_block != null).ToString());
        }



    }
}
