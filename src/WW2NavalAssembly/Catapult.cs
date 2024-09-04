using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Modding;

namespace WW2NavalAssembly
{
    public class Catapult : BlockScript
    {
        public int myPlayerID;

        public float energy = 100;
        public float maxEnergy = 100;

        public bool operating = false;

        public bool firstFrame = true;

        public Transform Hook;
        public GameObject HookJY;
        public GameObject HookM;
        public Transform HookPos;

        public bool Ready
        {
            get
            {
                return energy >= maxEnergy && !operating;
            }
        }

        public void SwitchHook(Aircraft a)
        {
            if (a.isSeaplane && a.SeaplaneType.Selection == "SC-1")
            {
                HookM.SetActive(true);
                HookJY.SetActive(false);
            }
            else
            {
                HookM.SetActive(false);
                HookJY.SetActive(true);
            }
        }

        public void InitHook()
        {
            if (!transform.Find("Hook"))
            {
                Hook = (new GameObject("Hook")).transform;
                Hook.parent = transform.Find("Vis");
                Hook.localPosition = Vector3.zero;
                Hook.localRotation = Quaternion.identity;
                Hook.localScale = Vector3.one;

                HookPos = (new GameObject("HookPos")).transform;
                HookPos.parent = Hook;
                HookPos.localPosition = new Vector3(0, 2f, -5.5f);
                HookPos.localRotation = Quaternion.identity;
                HookPos.localScale = Vector3.one;

                HookJY = new GameObject("Hook_JY");
                HookJY.transform.parent = Hook;
                HookJY.transform.localPosition = Vector3.zero;
                HookJY.transform.localRotation = Quaternion.identity;
                HookJY.transform.localScale = Vector3.one;
                MeshFilter JY_MF = HookJY.AddComponent<MeshFilter>();
                JY_MF.sharedMesh = ModResource.GetMesh("Catapult JY Mesh").Mesh;
                MeshRenderer JY_MR = HookJY.AddComponent<MeshRenderer>();
                JY_MR.material.mainTexture = ModResource.GetTexture("Catapult Texture").Texture;
                HookJY.SetActive(false);

                HookM = new GameObject("Hook_M");
                HookM.transform.parent = Hook;
                HookM.transform.localPosition = Vector3.zero;
                HookM.transform.localRotation = Quaternion.identity;
                HookM.transform.localScale = Vector3.one;
                MeshFilter M_MF = HookM.AddComponent<MeshFilter>();
                M_MF.sharedMesh = ModResource.GetMesh("Catapult M Mesh").Mesh;
                MeshRenderer M_MR = HookM.AddComponent<MeshRenderer>();
                M_MR.material.mainTexture = ModResource.GetTexture("Catapult Texture").Texture;
                HookM.SetActive(false);
            }
        }

        public void Start()
        {
            name = "Catapult";
            myPlayerID = BlockBehaviour.ParentMachine.PlayerID;
            if (BlockBehaviour.isSimulating)
            {
                InitHook();
            }
        }

        public override void SimulateFixedUpdateHost()
        {
            if (firstFrame)
            {
                firstFrame = false;
                FlightDataBase.Instance.aircraftController[myPlayerID].Catapults.AddCatapult(this);
            }
            else
            {

            }
        }

        public override void SimulateUpdateAlways()
        {
            Hook.localPosition = new Vector3(0, 0, 12.5f-(energy / maxEnergy) * 12.5f);
        }



    }
}
