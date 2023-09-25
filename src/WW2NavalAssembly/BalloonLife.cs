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
    class BalloonLife : MonoBehaviour
    {
        public float life = 1000;
        public SqrBalloonController sqrBalloonController;
        public void Awake(){
            sqrBalloonController = gameObject.GetComponent<SqrBalloonController>();
        }
        public void Start()
        {
            sqrBalloonController.blockJoint.breakForce *=3f;
            sqrBalloonController.blockJoint.breakTorque *= 3f;
        }
        public bool isAlive()
        {
            return life > 0;
        }

        public void CutLife(float Caliber, bool AP)
        {
            life -= Caliber * (AP?1:3);
        }
        
    }
}
