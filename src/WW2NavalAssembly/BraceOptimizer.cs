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
            optimized = true;
            int jointCount = 0;
            foreach (var joints in BB.iJointTo)
            {
                if (isWooden(joints.connectedBody.GetComponent<BlockBehaviour>()))
                {
                    if (jointCount >= 5)
                    {
                        joints.breakForce = 0;
                        joints.breakTorque = 0;
                    }
                    jointCount++;
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
