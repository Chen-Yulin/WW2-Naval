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
        public const int typeNum = 8;

        public string[] AssetName =
        {
            "AA-20",
            "AA-25",
            "AA-40-x2",
            "AA-40-x4",
            "AA-76",
            "AA-100",
            "AA-105",
            "AA-127",
        };

        public Mesh[][] AA_mesh = new Mesh[typeNum][];
        public Texture[][] AA_texture = new Texture[typeNum][];

        public Vector3[] Base_Offset = new Vector3[typeNum];
        public Vector3[] Gun_Offset = new Vector3[typeNum];
        public Vector3[] GunBase_Offset = new Vector3[typeNum];
        public float[] GunWidth = new float[typeNum];
        public float[] GunSpeed = new float[typeNum];

        public void LoadAsset()
        {
            for (int i = 0; i < typeNum; i++)
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
        public float GetWidth(int Type)
        {
            return GunWidth[Type];
        }
        public float GetSpeed(int Type)
        {
            return GunSpeed[Type];
        }

        public void Start()
        {
            Base_Offset[0] = new Vector3 (0.0f, -0.59f, -0.3f);
            GunBase_Offset[0] = new Vector3(0f, 0.95f, -0.3f);
            Gun_Offset[0] = new Vector3(0f, -1.5f, 0f);
            GunWidth[0] = 0.1f;
            GunSpeed[0] = 15f;

            Base_Offset[1] = new Vector3(0.0f, -0f, -0f);
            GunBase_Offset[1] = new Vector3(0f, 1.05f, -0.3f);
            Gun_Offset[1] = new Vector3(0f, -1.05f, 0.3f);
            GunWidth[1] = 2f;
            GunSpeed[1] = 45f;

            Base_Offset[2] = new Vector3(0.0f, -1.3f, 2.55f);
            GunBase_Offset[2] = new Vector3(0f, 1.55f, 0f);
            Gun_Offset[2] = new Vector3(0f, -2.8f, 2.6f);
            GunWidth[2] = 1.2f;
            GunSpeed[2] = 26f;

            Base_Offset[3] = new Vector3(0.8f, -1.3f, 2.55f);
            GunBase_Offset[3] = new Vector3(0, 1.55f, 0f);
            Gun_Offset[3] = new Vector3(0f, -2.8f, 2.6f);
            GunWidth[3] = 3f;
            GunSpeed[3] = 52f;

            Base_Offset[4] = new Vector3(0f, -1f, 0.75f);
            GunBase_Offset[4] = new Vector3(0, 1.5f, 0f);
            Gun_Offset[4] = new Vector3(0f, -2.5f, 0.75f);
            GunWidth[4] = 3f;
            GunSpeed[4] = 9f;

            Base_Offset[5] = new Vector3(0f, -0.4f, 0.7f);
            GunBase_Offset[5] = new Vector3(0, 1.6f, 0f);
            Gun_Offset[5] = new Vector3(0f, -2f, 0.75f);
            GunWidth[5] = 0.07f;

            Base_Offset[6] = new Vector3(0f, -0f, 0.3f);
            GunBase_Offset[6] = new Vector3(0, 1.6f, -0.7f);
            Gun_Offset[6] = new Vector3(0f, -1.6f, 1f);
            GunWidth[6] = 0.08f;

            Base_Offset[7] = new Vector3(0f, -1.1f, -0.2f);
            GunBase_Offset[7] = new Vector3(0, 1.7f, 0.5f);
            Gun_Offset[7] = new Vector3(0f, -2.8f, -0.5f);
            GunWidth[7] = 0.25f;
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
