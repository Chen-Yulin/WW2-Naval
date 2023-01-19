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
    class Grouper : SingleInstance<Grouper>
    {
        public override string Name { get; } = "Grouper";

        public Dictionary<string, Dictionary<int,GameObject>>[] GunGroups = new Dictionary<string, Dictionary<int,GameObject>>[16];

        public Grouper()
        {
            for (int i = 0; i < 16; i++)
            {
                GunGroups[i] = new Dictionary<string, Dictionary<int,GameObject>>();
            }
        }

        public void AddGun(int playerID, string key, int guid, GameObject Gun)
        {
            foreach (var groups in GunGroups[playerID])
            {
                if (groups.Value.ContainsKey(guid))
                {
                    groups.Value.Remove(guid);
                }
            }
            if (!GunGroups[playerID].ContainsKey(key))
            {
                GunGroups[playerID].Add(key, new Dictionary<int, GameObject>());
            }
            if (GunGroups[playerID][key].ContainsKey(guid))
            {
                GunGroups[playerID][key][guid] = Gun;
            }
            else
            {
                GunGroups[playerID][key].Add(guid, Gun);
            }
            
        }
        public Dictionary<int,GameObject> GetGun(int playerID, string key)
        {
            if (!GunGroups[playerID].ContainsKey(key))
            {
                GunGroups[playerID].Add(key, new Dictionary<int, GameObject>());
            }

            return GunGroups[playerID][key];
        }

    }
}
