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
                mode = value; 
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

        // mouse orbit
        public MouseOrbit MO;
        public BlockBehaviour MO_pre_block = null;
        

        // FPV
        public FPVCamera FPV;

        // TAC
        public TacCamera TAC;


        public void Awake()
        {
            MO = GetComponent<MouseOrbit>();
            FPV = gameObject.AddComponent<FPVCamera>();
            TAC = gameObject.AddComponent<TacCamera>();
            mode = Mode.MO;
        }

        public void EnableModCameraMO(GameObject caller, Transform target)
        {
            mode = Mode.MO;
            BlockBehaviour target_bb = target.GetComponent<BlockBehaviour>();
            if (!target_bb)
            {
                target_bb = target.gameObject.AddComponent<BlockBehaviour>();
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
                mode = Mode.FPV;
                FPV.Base = target;
            }
        }
        public void DisableModCamerFPV()
        {
            mode = Mode.MO;
        }

        public void EnableModCameraTAC(Transform target, float Sensitivity, MKey reset, MKey move)
        {
            if (target)
            {
                //mode = Mode.TAC;
                //TAC.Base = target;
                //TAC.ViewSensitivity = Sensitivity;
                //TAC.ViewMove = move;
                //TAC.ResetView = reset;
            }
        }

        public void DisableModCameraTAC()
        {
            mode = Mode.MO;
        }

        private void OnGUI()
        {
            //GUI.Box(new Rect(100, 200, 200, 30), MO.targetType.ToString());
            //GUI.Box(new Rect(100, 200, 200, 30), (MO_pre_block != null).ToString());
        }



    }
}
