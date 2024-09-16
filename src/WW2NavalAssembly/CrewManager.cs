using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Modding;
using UnityEngine;


namespace WW2NavalAssembly
{
    public class ShipSizeManager : SingleInstance<ShipSizeManager>
    {
        public override string Name { get; } = "Ship Size Manager";
        public class ShipSize
        {
            public Vector3 sizePos;
            public Vector3 sizeNeg;

            public float Volumn
            {
                get
                {
                    return forward == Vector3.zero? 0 : Mathf.Clamp(sizePos.x - sizeNeg.x, 2f, 999f) * Mathf.Clamp(Mathf.Sqrt(sizePos.y - sizeNeg.y), 2f, 999f) * Mathf.Pow(Mathf.Clamp(sizePos.z - sizeNeg.z, 1f, 999f), 1.5f);
                }
            }

            public Vector3 origin;
            public Vector3 forward;
            float DistThreshold = 100f;
            public ShipSize(Vector3 o)
            {
                origin = o;
                sizePos = Vector3.zero;
                sizeNeg = Vector3.zero;
            }
            public ShipSize()
            {
                origin = Vector3.zero;
                sizePos = Vector3.zero;
                sizeNeg = Vector3.zero;
            }

            public void AddDot(Vector3 o)
            {
                if(forward == Vector3.zero)
                {
                    //Debug.LogError("No captain");
                    return;
                }
                Vector3 pointer = o - origin;
                if (pointer.magnitude < DistThreshold)
                {
                    float halfLen = Vector3.Dot(forward, pointer);
                    float halfHeight = Vector3.Dot(Vector3.up, pointer);
                    float halfWidth = Vector3.Dot(Vector3.Cross(forward, Vector3.up).normalized, pointer);
                    sizePos.x = Mathf.Max(halfWidth, sizePos.x);
                    sizePos.y = Mathf.Max(halfHeight, sizePos.y);
                    sizePos.z = Mathf.Max(halfLen, sizePos.z);
                    sizeNeg.x = Mathf.Min(halfWidth, sizeNeg.x);
                    sizeNeg.y = Mathf.Min(halfHeight, sizeNeg.y);
                    sizeNeg.z = Mathf.Min(halfLen, sizeNeg.z);
                }
                //Debug.LogError(sizePos.ToString() + sizeNeg.ToString());
            }

            public void Reset()
            {
                origin = Vector3.zero;
                forward = Vector3.zero;
                sizePos = Vector3.zero;
                sizeNeg = Vector3.zero;
            }
        }
        public ShipSize[] size = new ShipSize[16];

        public ShipSizeManager()
        {
            for (int i = 0; i < 16; i++)
            {
                size[i] = new ShipSize();
            }
        }



    }

    public class CrewManager : SingleInstance<CrewManager>
    {
        public override string Name { get; } = "Crew Manager";

        public static MessageType CrewUpdateMessage = ModNetworking.CreateMessageType(DataType.Integer, DataType.IntegerArray, DataType.SingleArray, DataType.Single);
        public static MessageType OnFireMessage = ModNetworking.CreateMessageType(DataType.Integer, DataType.Integer, DataType.Boolean);

        public float[] CrewNum = new float[16];
        public float[] OriginCrewNum = new float[16];

        public float[] VirtualCrew = new float[16];
        public float[] CrewResize  = new float[16];

        // for send
        public Dictionary<int, float>[] CrewUpdateSend = new Dictionary<int, float>[16];

        // for receive
        public Dictionary<int, float>[] CrewUpdateReceive = new Dictionary<int, float>[16];
        public Dictionary<int, bool>[] OnFire = new Dictionary<int, bool>[16];

        public float SendCycle = 0.5f;
        float time = 0;

        public CrewManager()
        {
            for(int i = 0;i < 16;i++)
            {
                CrewUpdateSend[i] = new Dictionary<int, float>();
                CrewUpdateReceive[i] = new Dictionary<int, float>();
                OnFire[i] = new Dictionary<int, bool>();
            }
        }

        public float GetEfficiency(int playerID)
        {
            if (OriginCrewNum[playerID] != 0)
            {
                return CrewNum[playerID] / OriginCrewNum[playerID];
            }
            else
            {
                return 0;
            }
            
        }

        public void SetCrewNumOnStart(int playerID)
        {
            CrewNum[playerID] = ShipSizeManager.Instance.size[playerID].Volumn / 4f;
            OriginCrewNum[playerID] = CrewNum[playerID];
        }

        public void AddVirtualCrew(int playerID, float crew)
        {
            VirtualCrew[playerID] += crew;
        }

        public void GetResize(int playerID)
        {
            CrewResize[playerID] = OriginCrewNum[playerID] / VirtualCrew[playerID];
        }

        public void SendOnFire(int playerID, int guid, bool fire)
        {
            ModNetworking.SendToAll(CrewManager.OnFireMessage.CreateMessage(playerID, guid, fire));
        }

        public void ReceiveOnfire(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int guid = (int)msg.GetData(1);
            bool fire = (bool)msg.GetData(2);
            if (OnFire[playerid].ContainsKey(guid))
            {
                OnFire[playerid][guid] = fire;
            }
            else
            {
                OnFire[playerid].Add(guid, fire);
            }
        }

        public void SendCrewRate(int playerID, int guid, float crewrate)
        {
            if (CrewUpdateSend[playerID].ContainsKey(guid))
            {
                CrewUpdateSend[playerID][guid] = crewrate;
            }
            else
            {
                CrewUpdateSend[playerID].Add(guid, crewrate);
            }
        }

        public void ReceiveCrewRate(Message msg)
        {
            int playerid = (int)msg.GetData(0);
            int[] guids = (int[])msg.GetData(1);
            float[] crews = (float[])msg.GetData(2);
            float total = (float)msg.GetData(3);

            if (StatMaster.isClient)
            {
                CrewNum[playerid] = total;
            }

            if (guids.Length == crews.Length)
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    if (CrewUpdateReceive[playerid].ContainsKey(guids[i]))
                    {
                        CrewUpdateReceive[playerid][guids[i]] = crews[i];
                    }
                    else
                    {
                        CrewUpdateReceive[playerid].Add(guids[i], crews[i]);
                    }
                }
            }
            else
            {
                Debug.Log("WTF");
            }

        }

        public void Update()
        {
            if (time > SendCycle)
            {
                time = 0f;
                int playerid = 0;
                foreach (var update in CrewUpdateSend)
                {
                    if (update.Count > 0)
                    {
                        int[] guids = new int[update.Count];
                        float[] rates = new float[update.Count];
                        int i = 0;
                        foreach (var record in update)
                        {
                            guids[i] = record.Key;
                            rates[i] = record.Value;
                            i++;
                        }
                        update.Clear();
                        ModNetworking.SendToAll(CrewManager.CrewUpdateMessage.CreateMessage(playerid, guids, rates, CrewNum[playerid]));
                    }
                    playerid++;
                }
            }
            else
            {
                time += Time.unscaledDeltaTime;
            }
        }

    }
}
