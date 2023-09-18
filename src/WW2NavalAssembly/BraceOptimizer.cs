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
            List<BlockBehaviour> duplicatedBlock = new List<BlockBehaviour>();
            optimized = true;
            foreach (var joints in BB.gameObject.GetComponents<ConfigurableJoint>())
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
                            try
                            {
                                BlockBehaviour woodJointBB = woodJoint.connectedBody.GetComponent<BlockBehaviour>();
                                if (isWooden(woodJointBB))
                                {
                                    redundancyBlock.Add(woodJointBB);
                                }
                            }
                            catch { 
                                Debug.Log("ijointTo Error");
                            }
                            
                        }
                        foreach (var woodJoint in connectedBB.jointsToMe)
                        {
                            if (woodJoint)
                            {
                                try
                                {
                                    BlockBehaviour woodJointBB = woodJoint.gameObject.GetComponent<BlockBehaviour>();
                                    if (isWooden(woodJointBB))
                                    {
                                        redundancyBlock.Add(woodJointBB);
                                    }
                                }
                                catch
                                {
                                    Debug.Log("jointToMe Error" + woodJoint.name);
                                }
                            }
                        }
                    }
                    if (duplicatedBlock.Contains(connectedBB))
                    {
                        //Debug.Log("destroy duplicated joint of " + connectedBB.BuildingBlock.Guid.ToString());
                        joints.breakForce = 0;
                        joints.breakTorque = 0;
                        optimizeCnt++;
                    }
                    else
                    {
                        duplicatedBlock.Add(connectedBB);
                    }
                }

            }
            //Debug.Log("Optimize " + optimizeCnt.ToString() + " joints");
        }

        public void Start()
        {
            frameCount = 0;
            BB = GetComponent<BlockBehaviour>();
        }
        public void FixedUpdate()
        {
            if (StatMaster.isClient)
            {
                return;
            }
            if (frameCount <= 1 && BB.isSimulating)
            {
                frameCount++;
            }
            if (frameCount > 1 && !optimized)
            {
                try
                {
                    Optimize();
                }
                catch { }
            }
        }
    }
}
