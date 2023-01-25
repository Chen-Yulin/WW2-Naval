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
    public class FireControlManager : SingleInstance<FireControlManager>
    {
        public override string Name { get; } = "FireControlManager";

        public Dictionary<float, List<KeyValuePair<int,GameObject>>>[] Guns = new Dictionary<float, List<KeyValuePair<int, GameObject>>>[16];
        public Dictionary<int, List<KeyValuePair<int, GameObject>>>[] Torpedos = new Dictionary<int, List<KeyValuePair<int, GameObject>>>[16];

        public FireControlManager()
        {
            for (int i = 0; i < 16; i++)
            {
                Guns[i] = new Dictionary<float, List<KeyValuePair<int, GameObject>>>();
                Torpedos[i] = new Dictionary<int, List<KeyValuePair<int, GameObject>>>();
            }
        }

        public void AddGun(int playerID, float caliber, int guid, GameObject gameObject)
        {
            RemoveGun(playerID, guid);
            if (!Guns[playerID].ContainsKey(caliber))
            {
                Guns[playerID].Add(caliber, new List<KeyValuePair<int, GameObject>>());
            }
            Guns[playerID][caliber].Add(new KeyValuePair<int, GameObject>(guid, gameObject));
        }
        
        //remove the already exist gun gameobject
        public void RemoveGun(int playerID, int guid)
        {
            foreach (var caliberGroup in Guns[playerID])
            {
                for (int i = 0; i < caliberGroup.Value.Count; i++)
                {
                    if (caliberGroup.Value[i].Key == guid)
                    {
                        caliberGroup.Value.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void AddTorpedo(int playerID, int type, int guid, GameObject gameObject)
        {
            RemoveTorpedo(playerID, guid);
            if (!Torpedos[playerID].ContainsKey(type))
            {
                Torpedos[playerID].Add(type, new List<KeyValuePair<int, GameObject>>());
            }
            Torpedos[playerID][type].Add(new KeyValuePair<int, GameObject>(guid, gameObject));
        }

        //remove the already exist gun gameobject
        public void RemoveTorpedo(int playerID, int guid)
        {
            foreach (var typeGroup in Torpedos[playerID])
            {
                for (int i = 0; i < typeGroup.Value.Count; i++)
                {
                    if (typeGroup.Value[i].Key == guid)
                    {
                        typeGroup.Value.RemoveAt(i);
                        break;
                    }
                    
                }
            }
        }

        public GameObject GetGun(int playerID, int guid)
        {
            foreach (var caliberGroup in Guns[playerID])
            {
                for (int i = 0; i < caliberGroup.Value.Count; i++)
                {
                    if (caliberGroup.Value[i].Key == guid)
                    {
                        return caliberGroup.Value[i].Value;
                    }
                }
            }
            return null;
        }

        public GameObject GetTorpedo(int playerID, int guid)
        {
            foreach (var typeGroup in Torpedos[playerID])
            {
                for (int i = 0; i < typeGroup.Value.Count; i++)
                {
                    if (typeGroup.Value[i].Key == guid)
                    {
                        return typeGroup.Value[i].Value;
                    }
                }
            }
            return null;
        }



    }
}
