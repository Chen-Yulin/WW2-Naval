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
    class WW2Hinge : BlockBehaviour
    {
        BlockBehaviour BB;
        SteeringWheel SW;
        public float angleToBe;
        public bool gunnerControlling;
        public bool originMode;
        public float originSpeed;
        
        public void Start()
        {
            gunnerControlling = false;
            BB = gameObject.GetComponent<BlockBehaviour>();
            if (BB.isSimulating && !StatMaster.isClient)
            {
                //Debug.Log("Add Hinge:" + BB.BuildingBlock.Guid.GetHashCode());
                SW = gameObject.GetComponent<SteeringWheel>();
                originMode = SW.ReturnToCenterToggle.isDefaultValue;
                originSpeed = SW.SpeedSlider.Value;
                GunnerDataBase.Instance.AddHinge(BB.ParentMachine.PlayerID, BB.BuildingBlock.Guid.GetHashCode(),BB);
            }
        }
        public void OnDestroy()
        {
            if (BB.isSimulating && !StatMaster.isClient)
            {
                SW.ReturnToCenterToggle.SetValue(originMode);
                SW.SpeedSlider.SetValue(originSpeed);
                //Debug.Log("Remove Hinge:" + BB.BuildingBlock.Guid.GetHashCode());
                GunnerDataBase.Instance.RemoveHinge(BB.ParentMachine.PlayerID, BB.BuildingBlock.Guid.GetHashCode());
            }
            
        }
        public void FixedUpdate()
        {
            if (BB.isSimulating && !StatMaster.isClient)
            {
                angleToBe = SW.AngleToBe;
                if (gunnerControlling)
                {
                    
                    if (ModController.Instance.state % 10 == 0)
                    {
                        SW.ReturnToCenterToggle.SetValue(true);
                        SW.SpeedSlider.SetValue(0.01f);
                    }
                    else
                    {
                        SW.ReturnToCenterToggle.SetValue(false);
                        SW.SpeedSlider.SetValue(originSpeed);
                    }
                }
                else
                {
                    SW.ReturnToCenterToggle.SetValue(originMode);
                }
            }

            
        }
        

    }
}
