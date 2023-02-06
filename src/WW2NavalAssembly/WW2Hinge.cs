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
    class WW2Hinge : MonoBehaviour
    {
        BlockBehaviour BB;
        
        public void Start()
        {
            BB = gameObject.GetComponent<BlockBehaviour>();
            if (BB.isSimulating)
            {
                //Debug.Log("Add Hinge:" + BB.BuildingBlock.Guid.GetHashCode());
                GunnerDataBase.Instance.AddHinge(BB.ParentMachine.PlayerID, BB.BuildingBlock.Guid.GetHashCode(),BB);
            }
        }
        public void OnDestroy()
        {
            if (BB.isSimulating)
            {
                //Debug.Log("Remove Hinge:" + BB.BuildingBlock.Guid.GetHashCode());
                GunnerDataBase.Instance.RemoveHinge(BB.ParentMachine.PlayerID, BB.BuildingBlock.Guid.GetHashCode());
            }
            
        }
        

    }
}
