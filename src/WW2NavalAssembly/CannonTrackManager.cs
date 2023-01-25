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
    public class CannonTrackManager : SingleInstance<CannonTrackManager>
    {
        public override string Name { get; } = "CannonTrackManager";

        public Queue<GameObject>[] Cannons = new Queue<GameObject>[16];

        public CannonTrackManager()
        {
            for (int i = 0; i < 16; i++)
            {
                Cannons[i] = new Queue<GameObject>();
            }
        }

        public void AddTrackedCannon(int playerID, GameObject cannon)
        {
            Cannons[playerID].Enqueue(cannon);
            while (Cannons[playerID].Peek() == null || 
                    (Cannons[playerID].Peek().transform.position.y < 20) || 
                    Cannons[playerID].Peek().GetComponent<BulletBehaviour>().exploded)
            {
                Cannons[playerID].Dequeue();
            }
        }

        public GameObject GetTrackCannon(int playerID)
        {
            if (Cannons[playerID].Count != 0)
            {
                while (Cannons[playerID].Peek() == null ||
                    (Cannons[playerID].Peek().transform.position.y < 20) ||
                    Cannons[playerID].Peek().GetComponent<BulletBehaviour>().exploded)
                {
                    Cannons[playerID].Dequeue();
                    if (Cannons[playerID].Count == 0)
                    {
                        break;
                    }
                }
                if (Cannons[playerID].Count == 0)
                {
                    return null;
                }
                else
                {
                    return Cannons[playerID].Peek();
                }
                
            }
            else
            {
                return null;
            }
        }
        
        public GameObject SwitchTrackCannon(int playerID)
        {
            if (Cannons[playerID].Count != 0)
            {
                while (Cannons[playerID].Peek() == null ||
                    (Cannons[playerID].Peek().transform.position.y < 20) ||
                    Cannons[playerID].Peek().GetComponent<BulletBehaviour>().exploded)
                {
                    Cannons[playerID].Dequeue();
                    if (Cannons[playerID].Count == 0)
                    {
                        break;
                    }
                }
                if (Cannons[playerID].Count > 1)
                {
                    Cannons[playerID].Dequeue();
                }
                if (Cannons[playerID].Count == 0)
                {
                    return null;
                }
                else
                {
                    return Cannons[playerID].Peek();
                }
            }
            else
            {
                return null;
            }
            
        }

    }
}
