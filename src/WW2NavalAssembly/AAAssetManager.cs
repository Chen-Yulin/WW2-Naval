using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using Modding.Common;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Linq;

namespace WW2NavalAssembly
{
    class AAAssetManager :SingleInstance<AAAssetManager>
    {
        public override string Name { get; } = "AA Asset Manager";

        public bool Loaded = false;

        public string[] AssetName =
        {
            "AA-20",
            "AA-25",
            "AA-40-x2",
            "AA-40-x4",
            "AA-100",
            "AA-127",
        };

        public Mesh[][] AA_mesh = new Mesh[6][];
        public Texture[][] AA_texture = new Texture[6][];

        public void LoadAsset()
        {
            for (int i = 0; i < 6; i++)
            {
                AA_mesh[i] = new Mesh[2];
                AA_texture[i] = new Texture[2];
                for (int j = 0; j < 2; j++)
                {
                    AA_mesh[i][j] = ModResource.GetMesh(AssetName[i] +"-" + j.ToString() + " Mesh");
                    AA_texture[i][j] = ModResource.GetTexture(AssetName[i] + "-" + j.ToString() + " Texture");
                }
            }
        }

        public Mesh GetMesh(int Type, int part)
        {
            return AA_mesh[Type][part];
        }
        public Texture GetTexture(int Type, int part)
        {
            return AA_texture[Type][part];
        }

        public void Update()
        {
            if (!Loaded && !StatMaster.isMainMenu)
            {
                LoadAsset();
                Loaded = true;
                Debug.Log("WW2 AA Asset Loaded");
            }
        }

    }
}
