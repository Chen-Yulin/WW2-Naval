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
            List<BlockBehaviour> duplicatedBlock = new List<BlockBehaviour>();
            optimized = true;
            foreach (var joints in BB.iJointTo)
            {
                BlockBehaviour jointBB = joints.connectedBody.GetComponent<BlockBehaviour>();
                if (isWooden(jointBB))
                {
                    if (duplicatedBlock.Contains(jointBB))
                    {
                        joints.breakForce = 0;
                        joints.breakTorque = 0;
                    }
                    else
                    {
                        foreach (var woodJoint in jointBB.iJointTo)
                        {
                            try
                            {
                                BlockBehaviour woodJointBB = woodJoint.connectedBody.GetComponent<BlockBehaviour>();
                                if (isWooden(woodJointBB))
                                {
                                    duplicatedBlock.Add(woodJointBB);
                                }
                            }
                            catch { 
                                Debug.Log("ijointTo Error");
                            }
                            
                        }
                        foreach (var woodJoint in jointBB.jointsToMe)
                        {
                            if (woodJoint)
                            {
                                try
                                {
                                    BlockBehaviour woodJointBB = woodJoint.gameObject.GetComponent<BlockBehaviour>();
                                    if (isWooden(woodJointBB))
                                    {
                                        duplicatedBlock.Add(woodJointBB);
                                    }
                                }
                                catch
                                {
                                    Debug.Log("jointToMe Error" + woodJoint.name);
                                }
                            }
                        }
                    }
                }
            }
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
            if (frameCount <= 4 && BB.isSimulating)
            {
                frameCount++;
            }
            if (frameCount > 4 && !optimized)
            {
                Optimize();
            }
        }
    }
}
