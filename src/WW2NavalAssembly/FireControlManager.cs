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

        public FireControlManager()
        {
            for (int i = 0; i < 16; i++)
            {
                Guns[i] = new Dictionary<float, List<KeyValuePair<int, GameObject>>>();
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
                    }
                    break;
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

        

    }
}
