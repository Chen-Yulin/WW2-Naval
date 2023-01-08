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
    class DefaultArmour : MonoBehaviour
    {
        public int myseed = 0;
        public int myGuid;
        public void Awake()
        {
            myseed = (int)(UnityEngine.Random.value * 39);
        }
        public void Start()
        {
        }
        public void FixedUpdate()
        {
            if (ModController.Instance.state == myseed)
            {
                if (ModController.Instance.showArmour)
                {
                    if (gameObject.name == "Brace")
                    {
                        transform.Find("A").gameObject.GetComponent<MeshRenderer>().enabled = false;
                        transform.Find("B").gameObject.GetComponent<MeshRenderer>().enabled = false;
                        transform.Find("Cylinder").gameObject.GetComponent<MeshRenderer>().enabled = false;
                    }
                    else if (gameObject.name == "RopeWinch")
                    {
                        return;
                    }
                    else if (gameObject.name == "Spring")
                    {
                        return;
                    }
                    else if (gameObject.name == "Balloon")
                    {
                        return;
                    }
                    else if (gameObject.name == "SqrBalloon")
                    {
                        return;
                    }
                    else
                    {
                        transform.Find("Vis").gameObject.SetActive(false);
                    }
                    
                }
                else
                {
                    if (gameObject.name == "Brace")
                    {
                        transform.Find("A").gameObject.GetComponent<MeshRenderer>().enabled = true;
                        transform.Find("B").gameObject.GetComponent<MeshRenderer>().enabled = true;
                        transform.Find("Cylinder").gameObject.GetComponent<MeshRenderer>().enabled = true;
                    }
                    else if (gameObject.name == "RopeWinch" || gameObject.name == "Balloon" || gameObject.name == "Spring" || gameObject.name == "SqrBalloon")
                    {
                        return;
                    }
                    else
                    {
                        transform.Find("Vis").gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}