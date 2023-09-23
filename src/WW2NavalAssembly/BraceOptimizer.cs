using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.Networking;
using Modding.Blocks;

namespace WW2NavalAssembly
{
    class BraceOptimizer:MonoBehaviour
    {
        BlockBehaviour BB;
        public int frameCount = 0;
        public bool optimized = false;

        public bool isWooden(BlockBehaviour bb)
        {
            if (!bb)
            {
                return false;
            }
            int blockID = bb.BlockID;
            switch (blockID)
            {
                case (int)BlockType.SingleWoodenBlock:
                    return true;
                case (int)BlockType.DoubleWoodenBlock:
                    return true;
                case (int)BlockType.Log:
                    return true;
                default: 
                    return false;
            }
        }
        public void Optimize()
        {
            int optimizeCnt = 0;
            List<BlockBehaviour> redundancyBlock = new List<BlockBehaviour>();
            optimized = true;
            foreach (var joints in BB.gameObject.GetComponents<ConfigurableJoint>())
            {
                try
                {
                    BlockBehaviour connectedBB = joints.connectedBody.GetComponent<BlockBehaviour>();
                    if (isWooden(connectedBB))
                    {
                        //Debug.Log("joint to " + connectedBB.BuildingBlock.Guid.ToString());
                        if (redundancyBlock.Contains(connectedBB))
                        {
                            //Debug.Log("destroy redundancy joint of " + connectedBB.BuildingBlock.Guid.ToString());
                            joints.breakForce = 0;
                            joints.breakTorque = 0;
                            optimizeCnt++;
                        }
                        else
                        {
                            foreach (var woodJoint in connectedBB.iJointTo)
                            {
                                BlockBehaviour woodJointBB = woodJoint.connectedBody.GetComponent<BlockBehaviour>();
                                if (woodJointBB)
                                {
                                    if (isWooden(woodJointBB))
                                    {
                                        redundancyBlock.Add(woodJointBB);
                                    }
                                }
                            }
                            foreach (var woodJoint in connectedBB.jointsToMe)
                            {
                                BlockBehaviour woodJointBB = woodJoint.GetComponent<BlockBehaviour>();
                                if (woodJointBB)
                                {
                                    if (isWooden(woodJointBB))
                                    {
                                        redundancyBlock.Add(woodJointBB);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //Debug.Log("joint Error");
                }
            }
            //Debug.Log("Optimize " + optimizeCnt.ToString() + " joints");
        }

        public void Start()
        {
            frameCount = 0;
            BB = GetComponent<BlockBehaviour>();
        }
        public void Update()
        {
            if (StatMaster.isClient)
            {
                return;
            }
            
        }
        public void FixedUpdate()
        {
            if (StatMaster.isClient)
            {
                return;
            }
            if (frameCount <= 2 && BB.isSimulating)
            {
                frameCount++;
            }
            if (frameCount > 2 && !optimized)
            {
                try
                {
                    optimized = true;
                    Optimize();
                }
                catch { }
            }
        }
    }
}
