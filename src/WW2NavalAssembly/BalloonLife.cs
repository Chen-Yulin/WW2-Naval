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
        
        public bool isAlive()
        {
            return life > 0;
        }

        public void CutLife(float Caliber)
        {
            life -= Caliber;
        }
        
    }
}
