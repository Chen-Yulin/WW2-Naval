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
using System.Linq;

namespace WW2NavalAssembly
{
    class Grouper : SingleInstance<Grouper>
    {
        public override string Name { get; } = "Grouper";

        public Dictionary<string, Dictionary<int,GameObject>>[] GunGroups = new Dictionary<string, Dictionary<int,GameObject>>[16];
        public Dictionary<string, Dictionary<int, Aircraft>>[] AircraftGroups = new Dictionary<string, Dictionary<int, Aircraft>>[16];
        public Dictionary<string, KeyValuePair<int, Aircraft>>[] AircraftLeaders = new Dictionary<string, KeyValuePair<int, Aircraft>>[16];

        public Grouper()
        {
            for (int i = 0; i < 16; i++)
            {
                GunGroups[i] = new Dictionary<string, Dictionary<int,GameObject>>();
                AircraftGroups[i] = new Dictionary<string, Dictionary<int, Aircraft>>();
                AircraftLeaders[i] = new Dictionary<string, KeyValuePair<int, Aircraft>>();
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

        public void AddAircraft(int playerID, string key, int guid, Aircraft ac)
        {
            // AircraftGroups
            foreach (var groups in AircraftGroups[playerID])
            {
                if (groups.Value.ContainsKey(guid))
                {
                    groups.Value.Remove(guid);
                }
            }

            if (!AircraftGroups[playerID].ContainsKey(key))
            {
                AircraftGroups[playerID].Add(key, new Dictionary<int, Aircraft>());
            }
            if (AircraftGroups[playerID][key].ContainsKey(guid))
            {
                AircraftGroups[playerID][key][guid] = ac;
            }
            else
            {
                AircraftGroups[playerID][key].Add(guid, ac);
            }

            // AircraftLeaders
            if (ac.Rank.Value == 1)
            {
                foreach (var leader in AircraftLeaders[playerID])
                {

                    if (leader.Value.Key == guid)
                    {
                        AircraftLeaders[playerID].Remove(leader.Key);
                        break;
                    }

                }

                if (!AircraftLeaders[playerID].ContainsKey(key))
                {
                    AircraftLeaders[playerID].Add(key, new KeyValuePair<int, Aircraft>(guid, ac));
                }
                else
                {
                    AircraftLeaders[playerID][key] = new KeyValuePair<int, Aircraft>(guid, ac);
                }
            }
            else
            {
                foreach (var leader in AircraftLeaders[playerID])
                {

                    if (leader.Value.Key == guid)
                    {
                        AircraftLeaders[playerID].Remove(leader.Key);
                        break;
                    }

                }
            }

            var sortedDict = AircraftGroups[playerID][key].OrderBy(entry => entry.Value.Rank.Value != 1).ToDictionary(pair => pair.Key, pair => pair.Value);
            AircraftGroups[playerID][key] = sortedDict;
        }

        public Dictionary<int, Aircraft> GetAircraft(int playerID, string key)
        {
            if (!AircraftGroups[playerID].ContainsKey(key))
            {
                return new Dictionary<int, Aircraft>();
            }
            else
            {
                return AircraftGroups[playerID][key];
            }
            

            
        }

        public Aircraft GetLeader(int playerID, string key)
        {
            if (AircraftLeaders[playerID].ContainsKey(key))
            {
                return AircraftLeaders[playerID][key].Value;
            }
            else
            {
                return null;
            }
        }

        public Dictionary<string, KeyValuePair<int, Aircraft>> GetLeaders(int playerID)
        {
            return AircraftLeaders[playerID];
        }
    }
}
