﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Modding;
using System.Collections;

namespace WW2NavalAssembly
{
    public class CatapultMsgReceiver : SingleInstance<CatapultMsgReceiver>
    {
        public override string Name { get; } = "Catapult Msg Receiver";
        public static MessageType LaunchMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public static MessageType ReadyMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public static MessageType JYMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public static MessageType MMsg = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer);
        public List<int>[] Launch = new List<int>[16];
        public List<int>[] Ready = new List<int>[16];
        public List<int>[] JY = new List<int>[16];
        public List<int>[] M = new List<int>[16];

        public CatapultMsgReceiver()
        {
            for (int i = 0; i < 16; i++)
            {
                Launch[i] = new List<int>();
                Ready[i] = new List<int>();
                JY[i] = new List<int>();
                M[i] = new List<int>();
            }
        }
        public void LaunchMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            Launch[playerid].Add(guid);
        }
        public void ReadyMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            Ready[playerid].Add(guid);
        }
        public void JYMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            JY[playerid].Add(guid);
        }
        public void MMsgReceiver(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            M[playerid].Add(guid);
        }
    }
    public class Catapult : BlockScript
    {
        public int myPlayerID;
        public int myGuid;

        public float energy = 100;
        public float maxEnergy = 100;

        public bool operating = false;

        public bool firstFrame = true;

        public Transform Hook;
        public GameObject HookJY;
        public GameObject HookM;
        public Transform HookPos;

        public ParticleSystem Smoke;

        public bool Ready
        {
            get
            {
                return energy >= maxEnergy && !operating;
            }
        }

        IEnumerator ClientLaunchCoroutine()
        {
            operating = true;
            float speed = -35f;
            while (energy > 0)
            {
                yield return new WaitForFixedUpdate();
                energy -= Mathf.Clamp(speed, 0f, 15f);
                speed += 1f;
            }
            energy = 0;
            operating = false;
            EmitSmoke();
            yield break;
        }

        public void SwitchHook(Aircraft a)
        {
            if (a.isSeaplane && a.SeaplaneType.Selection == "SC-1")
            {
                HookM.SetActive(true);
                HookJY.SetActive(false);
                ModNetworking.SendToAll(CatapultMsgReceiver.MMsg.CreateMessage(myPlayerID, myGuid));
            }
            else
            {
                HookM.SetActive(false);
                HookJY.SetActive(true);
                ModNetworking.SendToAll(CatapultMsgReceiver.JYMsg.CreateMessage(myPlayerID, myGuid));
            }
        }
        public void SwitchHookClient(bool M)
        {
            if (M)
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
        public void InitSmoke()
        {
            if (!transform.Find("Smoke"))
            {
                Smoke = Instantiate(AssetManager.Instance.Catapult.CatapultSmoke).GetComponent<ParticleSystem>();
                Smoke.name = "Smoke";
                Smoke.Stop();
                Smoke.transform.parent = transform;
                Smoke.transform.localPosition = new Vector3(0, 1.1f, 0.4f);
                Smoke.transform.transform.localEulerAngles = new Vector3(90, 0, 0);
                Smoke.transform.localScale = Vector3.one;
            }
        }
        public void EmitSmoke()
        {
            if (Smoke)
            {
                Smoke.Stop();
                Smoke.Play();
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
                myGuid = BlockBehaviour.BuildingBlock.Guid.GetHashCode();
                InitHook();
                InitSmoke();
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
            if (!Ready && !operating)
            {
                energy += Time.deltaTime * 2;
            }
        }

        public override void SimulateUpdateClient()
        {
            if (CatapultMsgReceiver.Instance.Launch[myPlayerID].Contains(myGuid))
            {
                CatapultMsgReceiver.Instance.Launch[myPlayerID].Remove(myGuid);
                StartCoroutine(ClientLaunchCoroutine());
            }
            if (CatapultMsgReceiver.Instance.Ready[myPlayerID].Contains(myGuid))
            {
                CatapultMsgReceiver.Instance.Ready[myPlayerID].Remove(myGuid);
                operating = false;
                energy = maxEnergy;
            }
            if (CatapultMsgReceiver.Instance.M[myPlayerID].Contains(myGuid))
            {
                CatapultMsgReceiver.Instance.M[myPlayerID].Remove(myGuid);
                SwitchHookClient(true);
            }
            if (CatapultMsgReceiver.Instance.JY[myPlayerID].Contains(myGuid))
            {
                CatapultMsgReceiver.Instance.JY[myPlayerID].Remove(myGuid);
                SwitchHookClient(false);
            }
        }
    }
}
