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

        public Vector3[] Base_Offset = new Vector3[6];
        public Vector3[] Gun_Offset = new Vector3[6];
        public Vector3[] GunBase_Offset = new Vector3[6];

        public void LoadAsset()
        {
            for (int i = 0; i < 6; i++)
            {
                AA_mesh[i] = new Mesh[2];
                AA_texture[i] = new Texture[2];
                for (int j = 0; j < 2; j++)
                {
                    AA_mesh[i][j] = ModResource.GetMesh(AssetName[i] +"-" + (j + 1).ToString() + " Mesh");
                    AA_texture[i][j] = ModResource.GetTexture(AssetName[i] + "-" + (j + 1).ToString() + " Texture");
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
        public Vector3 GetOffset(int Type, int part)
        {
            if (part == 0) return Base_Offset[Type];
            else if (part == 1) return Gun_Offset[Type];
            else return GunBase_Offset[Type];
        }

        public void Start()
        {
            Base_Offset[0] = new Vector3 (0.0f, -0.59f, -0.3f);
            GunBase_Offset[0] = new Vector3(0f, 0.95f, -0.3f);
            Gun_Offset[0] = new Vector3(0f, -1.5f, 0f);

            Base_Offset[1] = new Vector3(0.0f, -0f, -0f);
            GunBase_Offset[1] = new Vector3(0f, 1.05f, -0.3f);
            Gun_Offset[1] = new Vector3(0f, -1.05f, 0.3f);

            Base_Offset[2] = new Vector3(0.0f, -1.3f, 2.55f);
            GunBase_Offset[2] = new Vector3(0f, 1.55f, 0f);
            Gun_Offset[2] = new Vector3(0f, -2.8f, 2.6f);

            Base_Offset[3] = new Vector3(0.8f, -1.3f, 2.55f);
            GunBase_Offset[3] = new Vector3(0.8f, 1.55f, 0f);
            Gun_Offset[3] = new Vector3(0f, -2.8f, 2.6f);
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
