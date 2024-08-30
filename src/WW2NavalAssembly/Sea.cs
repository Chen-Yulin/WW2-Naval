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
    public class Water : MonoBehaviour
    {
        public enum WaterMode
        {
            Simple = 0,
            Reflective = 1,
            Refractive = 2,
        };


        public WaterMode waterMode = WaterMode.Refractive;
        public bool disablePixelLights = true;
        public int textureSize = 256;
        public float clipPlaneOffset = 0.07f;
        public LayerMask reflectLayers = ~((1<<20)|(1<<5)|(1<<23));
        public LayerMask refractLayers = ~((1 << 20) | (1 << 5)| (1 << 23));


        private Dictionary<Camera, Camera> m_ReflectionCameras = new Dictionary<Camera, Camera>(); // Camera -> Camera table
        private Dictionary<Camera, Camera> m_RefractionCameras = new Dictionary<Camera, Camera>(); // Camera -> Camera table
        private RenderTexture m_ReflectionTexture;
        private RenderTexture m_RefractionTexture;
        private WaterMode m_HardwareWaterSupport = WaterMode.Refractive;
        private int m_OldReflectionTextureSize;
        private int m_OldRefractionTextureSize;
        private static bool s_InsideWater;


        // This is called when it's known that the object will be rendered by some
        // camera. We render reflections / refractions and do other updates here.
        // Because the script executes in edit mode, reflections for the scene view
        // camera will just work!
        public void OnWillRenderObject()
        {
            if (!enabled || !GetComponent<Renderer>() || !GetComponent<Renderer>().sharedMaterial ||
                !GetComponent<Renderer>().enabled)
            {
                return;
            }

            Camera cam = Camera.current;
            if (!cam)
            {
                return;
            }

            // Safeguard from recursive water reflections.
            if (s_InsideWater)
            {
                return;
            }
            s_InsideWater = true;

            // Actual water rendering mode depends on both the current setting AND
            // the hardware support. There's no point in rendering refraction textures
            // if they won't be visible in the end.
            m_HardwareWaterSupport = FindHardwareWaterSupport();
            WaterMode mode = GetWaterMode();

            Camera reflectionCamera, refractionCamera;
            CreateWaterObjects(cam, out reflectionCamera, out refractionCamera);

            // find out the reflection plane: position and normal in world space
            Vector3 pos = transform.position;
            Vector3 normal = transform.up;

            // Optionally disable pixel lights for reflection/refraction
            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (disablePixelLights)
            {
                QualitySettings.pixelLightCount = 0;
            }

            UpdateCameraModes(cam, reflectionCamera);
            UpdateCameraModes(cam, refractionCamera);

            // Render reflection if needed
            if (mode >= WaterMode.Reflective)
            {
                // Reflect camera around reflection plane
                float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
                Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

                Matrix4x4 reflection = Matrix4x4.zero;
                CalculateReflectionMatrix(ref reflection, reflectionPlane);
                Vector3 oldpos = cam.transform.position;
                Vector3 newpos = reflection.MultiplyPoint(oldpos);
                reflectionCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

                // Setup oblique projection matrix so that near plane is our reflection
                // plane. This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(reflectionCamera, pos, normal, 1.0f);
                reflectionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);

                // Set custom culling matrix from the current camera
                reflectionCamera.cullingMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;

                reflectionCamera.cullingMask = ~(1 << 4) & reflectLayers.value; // never render water layer
                reflectionCamera.targetTexture = m_ReflectionTexture;
                bool oldCulling = GL.invertCulling;
                GL.invertCulling = !oldCulling;
                reflectionCamera.transform.position = newpos;
                Vector3 euler = cam.transform.eulerAngles;
                reflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
                reflectionCamera.Render();
                reflectionCamera.transform.position = oldpos;
                GL.invertCulling = oldCulling;
                GetComponent<Renderer>().sharedMaterial.SetTexture("_ReflectionTex", m_ReflectionTexture);
            }

            // Render refraction
            if (mode >= WaterMode.Refractive)
            {
                refractionCamera.worldToCameraMatrix = cam.worldToCameraMatrix;

                // Setup oblique projection matrix so that near plane is our reflection
                // plane. This way we clip everything below/above it for free.
                Vector4 clipPlane = CameraSpacePlane(refractionCamera, pos, normal, -1.0f);
                refractionCamera.projectionMatrix = cam.CalculateObliqueMatrix(clipPlane);

                // Set custom culling matrix from the current camera
                refractionCamera.cullingMatrix = cam.projectionMatrix * cam.worldToCameraMatrix;

                refractionCamera.cullingMask = ~(1 << 4) & refractLayers.value; // never render water layer
                refractionCamera.targetTexture = m_RefractionTexture;
                refractionCamera.transform.position = cam.transform.position;
                refractionCamera.transform.rotation = cam.transform.rotation;
                refractionCamera.Render();
                GetComponent<Renderer>().sharedMaterial.SetTexture("_RefractionTex", m_RefractionTexture);
            }

            // Restore pixel light count
            if (disablePixelLights)
            {
                QualitySettings.pixelLightCount = oldPixelLightCount;
            }

            // Setup shader keywords based on water mode
            switch (mode)
            {
                case WaterMode.Simple:
                    Shader.EnableKeyword("WATER_SIMPLE");
                    Shader.DisableKeyword("WATER_REFLECTIVE");
                    Shader.DisableKeyword("WATER_REFRACTIVE");
                    break;
                case WaterMode.Reflective:
                    Shader.DisableKeyword("WATER_SIMPLE");
                    Shader.EnableKeyword("WATER_REFLECTIVE");
                    Shader.DisableKeyword("WATER_REFRACTIVE");
                    break;
                case WaterMode.Refractive:
                    Shader.DisableKeyword("WATER_SIMPLE");
                    Shader.DisableKeyword("WATER_REFLECTIVE");
                    Shader.EnableKeyword("WATER_REFRACTIVE");
                    break;
            }

            s_InsideWater = false;
        }


        // Cleanup all the objects we possibly have created
        void OnDisable()
        {
            if (m_ReflectionTexture)
            {
                DestroyImmediate(m_ReflectionTexture);
                m_ReflectionTexture = null;
            }
            if (m_RefractionTexture)
            {
                DestroyImmediate(m_RefractionTexture);
                m_RefractionTexture = null;
            }
            foreach (var kvp in m_ReflectionCameras)
            {
                DestroyImmediate((kvp.Value).gameObject);
            }
            m_ReflectionCameras.Clear();
            foreach (var kvp in m_RefractionCameras)
            {
                DestroyImmediate((kvp.Value).gameObject);
            }
            m_RefractionCameras.Clear();
        }


        // This just sets up some matrices in the material; for really
        // old cards to make water texture scroll.
        void Update()
        {
            if (!GetComponent<Renderer>())
            {
                return;
            }
            Material mat = GetComponent<Renderer>().sharedMaterial;
            if (!mat)
            {
                return;
            }

            Vector4 waveSpeed = mat.GetVector("WaveSpeed");
            float waveScale = mat.GetFloat("_WaveScale");
            Vector4 waveScale4 = new Vector4(waveScale, waveScale, waveScale * 0.4f, waveScale * 0.45f);

            // Time since level load, and do intermediate calculations with doubles
            double t = Time.timeSinceLevelLoad / 20.0;
            Vector4 offsetClamped = new Vector4(
                (float)Math.IEEERemainder(waveSpeed.x * waveScale4.x * t, 1.0),
                (float)Math.IEEERemainder(waveSpeed.y * waveScale4.y * t, 1.0),
                (float)Math.IEEERemainder(waveSpeed.z * waveScale4.z * t, 1.0),
                (float)Math.IEEERemainder(waveSpeed.w * waveScale4.w * t, 1.0)
                );

            mat.SetVector("_WaveOffset", offsetClamped);
            mat.SetVector("_WaveScale4", waveScale4);
        }

        void UpdateCameraModes(Camera src, Camera dest)
        {
            if (dest == null)
            {
                return;
            }
            // set water camera to clear the same way as current camera
            dest.clearFlags = src.clearFlags;
            dest.backgroundColor = src.backgroundColor;
            if (src.clearFlags == CameraClearFlags.Skybox)
            {
                Skybox sky = src.GetComponent<Skybox>();
                Skybox mysky = dest.GetComponent<Skybox>();
                if (!sky || !sky.material)
                {
                    mysky.enabled = false;
                }
                else
                {
                    mysky.enabled = true;
                    mysky.material = sky.material;
                }
            }
            // update other values to match current camera.
            // even if we are supplying custom camera&projection matrices,
            // some of values are used elsewhere (e.g. skybox uses far plane)
            dest.farClipPlane = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic = src.orthographic;
            dest.fieldOfView = src.fieldOfView;
            dest.aspect = src.aspect;
            dest.orthographicSize = src.orthographicSize;
        }


        // On-demand create any objects we need for water
        void CreateWaterObjects(Camera currentCamera, out Camera reflectionCamera, out Camera refractionCamera)
        {
            WaterMode mode = GetWaterMode();

            reflectionCamera = null;
            refractionCamera = null;

            if (mode >= WaterMode.Reflective)
            {
                // Reflection render texture
                if (!m_ReflectionTexture || m_OldReflectionTextureSize != textureSize)
                {
                    if (m_ReflectionTexture)
                    {
                        DestroyImmediate(m_ReflectionTexture);
                    }
                    m_ReflectionTexture = new RenderTexture(textureSize, textureSize, 16);
                    m_ReflectionTexture.name = "__WaterReflection" + GetInstanceID();
                    m_ReflectionTexture.isPowerOfTwo = true;
                    m_ReflectionTexture.hideFlags = HideFlags.DontSave;
                    m_OldReflectionTextureSize = textureSize;
                }

                // Camera for reflection
                m_ReflectionCameras.TryGetValue(currentCamera, out reflectionCamera);
                if (!reflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
                {
                    GameObject go = new GameObject("Water Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
                    reflectionCamera = go.GetComponent<Camera>();
                    reflectionCamera.enabled = false;
                    reflectionCamera.transform.position = transform.position;
                    reflectionCamera.transform.rotation = transform.rotation;
                    reflectionCamera.gameObject.AddComponent<FlareLayer>();
                    go.hideFlags = HideFlags.HideAndDontSave;
                    m_ReflectionCameras[currentCamera] = reflectionCamera;
                }
            }

            if (mode >= WaterMode.Refractive)
            {
                // Refraction render texture
                if (!m_RefractionTexture || m_OldRefractionTextureSize != textureSize)
                {
                    if (m_RefractionTexture)
                    {
                        DestroyImmediate(m_RefractionTexture);
                    }
                    m_RefractionTexture = new RenderTexture(textureSize, textureSize, 16);
                    m_RefractionTexture.name = "__WaterRefraction" + GetInstanceID();
                    m_RefractionTexture.isPowerOfTwo = true;
                    m_RefractionTexture.hideFlags = HideFlags.DontSave;
                    m_OldRefractionTextureSize = textureSize;
                }

                // Camera for refraction
                m_RefractionCameras.TryGetValue(currentCamera, out refractionCamera);
                if (!refractionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
                {
                    GameObject go =
                        new GameObject("Water Refr Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(),
                            typeof(Camera), typeof(Skybox));
                    refractionCamera = go.GetComponent<Camera>();
                    refractionCamera.enabled = false;
                    refractionCamera.transform.position = transform.position;
                    refractionCamera.transform.rotation = transform.rotation;
                    refractionCamera.gameObject.AddComponent<FlareLayer>();
                    go.hideFlags = HideFlags.HideAndDontSave;
                    m_RefractionCameras[currentCamera] = refractionCamera;
                }
            }
        }

        WaterMode GetWaterMode()
        {
            if (m_HardwareWaterSupport < waterMode)
            {
                return m_HardwareWaterSupport;
            }
            return waterMode;
        }

        WaterMode FindHardwareWaterSupport()
        {
            if (!SystemInfo.supportsRenderTextures || !GetComponent<Renderer>())
            {
                return WaterMode.Simple;
            }

            Material mat = GetComponent<Renderer>().sharedMaterial;
            if (!mat)
            {
                return WaterMode.Simple;
            }

            string mode = mat.GetTag("WATERMODE", false);
            if (mode == "Refractive")
            {
                return WaterMode.Refractive;
            }
            if (mode == "Reflective")
            {
                return WaterMode.Reflective;
            }

            return WaterMode.Simple;
        }

        // Given position/normal of the plane, calculates plane in camera space.
        Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
        {
            Vector3 offsetPos = pos + normal * clipPlaneOffset;
            Matrix4x4 m = cam.worldToCameraMatrix;
            Vector3 cpos = m.MultiplyPoint(offsetPos);
            Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        // Calculates reflection matrix around the given plane
        static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
        {
            reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
            reflectionMat.m01 = (-2F * plane[0] * plane[1]);
            reflectionMat.m02 = (-2F * plane[0] * plane[2]);
            reflectionMat.m03 = (-2F * plane[3] * plane[0]);

            reflectionMat.m10 = (-2F * plane[1] * plane[0]);
            reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
            reflectionMat.m12 = (-2F * plane[1] * plane[2]);
            reflectionMat.m13 = (-2F * plane[3] * plane[1]);

            reflectionMat.m20 = (-2F * plane[2] * plane[0]);
            reflectionMat.m21 = (-2F * plane[2] * plane[1]);
            reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
            reflectionMat.m23 = (-2F * plane[3] * plane[2]);

            reflectionMat.m30 = 0F;
            reflectionMat.m31 = 0F;
            reflectionMat.m32 = 0F;
            reflectionMat.m33 = 1F;
        }
    }
    public class Sea : SingleInstance<Sea>
    {
        public override string Name { get; } = "Sea";
        public GameObject SeaPlane;
        public bool preShowSea = false;
        public GameObject waterplane;

        public GameObject Underwater;

        public void Awake()
        {
            waterplane = ModResource.GetAssetBundle("waternew").LoadAsset<GameObject>("Ocean");
            UnityEngine.Object.DontDestroyOnLoad(waterplane);
            seaeffect = new GameObject();
            Underwater = Instantiate(AssetManager.Instance.Sea.UnderWater);
            Underwater.transform.parent = Camera.main.transform;
            Underwater.SetActive(false);
            Underwater.transform.localPosition = Vector3.zero;
            Underwater.transform.localScale = Vector3.one * 10f;
            //seaeffect.AddComponent<SeaSurfacer>();
            //DontDestroyOnLoad(seaeffect);

        }
        public float Getseahigh(Vector3 pos)//可以获取点的海高
        {
            try
            {
                if (ModController.Instance.newseaEffect)
                {
                    Vector2 posOrigin = new Vector2(pos.x, pos.z);
                    float result = waveCalculateAll(posOrigin / 15).y * 15 + ModController.Instance.seahigh;
                    return result;
                }
                else
                {
                    return ModController.Instance.seahigh;
                }
            }
            catch
            {
                return 0;
            }
        }
        public float GetOriginseahigh(Vector3 pos)//可以获取点的原始海高偏移(相对20高海平面)
        {
            if (ModController.Instance.newseaEffect)
            {
                Vector2 posOrigin = new Vector2(pos.x, pos.z);
                float result = waveCalculateAll(posOrigin / 15).y * 15;
                return result;
            }
            else
            {
                return 0;
            }
        }
        // Token: 0x06000143 RID: 323 RVA: 0x000135E0 File Offset: 0x000117E0
        public void FixedUpdate()
        {
        }
        public void Update()
        {
            timeseed += Time.deltaTime * timedelta;
            if (timeseed >= 60000)
            {
                timeseed = 0;
            }

            


            if (ModController.Instance.showSea && this.preShowSea && ModController.Instance.newseaEffect)
            {
                foreach (MeshRenderer meshRenderer in seachanges)
                {
                    meshRenderer.material.SetFloat("_SeedIn", timeseed);
                }
                if (seaStrenght != ModController.Instance.seaStrenght)
                {
                    seaStrenght = ModController.Instance.seaStrenght;

                    UnityEngine.Object.Destroy(SeaPlaneFather);
                    SpawnSea(1);

                }

                Vector3 pos = GetCameraPos();
                if (lastPos != pos)
                {
                    SetSeaPos();
                    lastPos = pos;
                }
            }
            if (this.preShowSea && !ModController.Instance.showSea) // switch off
            {
                this.preShowSea = false;
                try
                {
                    try
                    {
                        UnityEngine.Object.Destroy(SeaPlaneFather);
                        UnityEngine.Object.Destroy(SeaPlane);
                    }
                    catch
                    {

                    }
                }
                catch
                {
                }
            }
            else if (!preShowSea && ModController.Instance.showSea) // switch on
            {
                preShowSea = true;
                if (ModController.Instance.newseaEffect)
                {
                    SpawnSea(1);
                }
                else
                {
                    SeaPlane = (GameObject)Instantiate(AssetManager.Instance.Sea.Sea);
                    SeaPlane.transform.position = new Vector3(0, 20, 0);
                    SeaPlane.transform.localScale = new Vector3(8000, 1, 8000);
                    SeaPlane.AddComponent<Water>().waterMode = Water.WaterMode.Refractive;
                    SeaPlane.SetActive(true);
                }
            }

            if (preShowSea)
            {
                if (Camera.main.transform.position.y < 20)
                {
                    if (SeaPlane)
                    {
                        SeaPlane.transform.localScale = new Vector3(SeaPlane.transform.localScale.x, -1, SeaPlane.transform.localScale.z);
                    }
                    try
                    {
                        Underwater.SetActive(true);
                    }
                    catch {
                        Underwater = Instantiate(AssetManager.Instance.Sea.UnderWater);
                        Underwater.transform.parent = Camera.main.transform;
                        Underwater.transform.localPosition = Vector3.zero;
                        Underwater.transform.localScale = Vector3.one * 10f;
                    }
                }
                else
                {
                    if (SeaPlane)
                    {
                        SeaPlane.transform.localScale = new Vector3(SeaPlane.transform.localScale.x, 1, SeaPlane.transform.localScale.z);
                    }
                    try
                    {
                        Underwater.SetActive(false);
                    }
                    catch { }
                    
                }
            }
            else
            {
                if (Underwater)
                {
                    Underwater.SetActive(false);
                }
            }
            

        }
        public Vector3 GetCameraPos()
        {
            float delta = 5000;
            Vector3 pos = new Vector3(Mathf.Round(Camera.main.transform.position.x / delta), 0, Mathf.Round(Camera.main.transform.position.z / delta)) * delta;
            return pos;
        }

        public void SpawnSea(int size)//2=1km
        {
            SeaPlaneFather = new GameObject();
            SeaPlaneFather.transform.position = new Vector3(0, ModController.Instance.seahigh, 0);
            Vector3 startPos = new Vector3(-size * seedelta, 0, -size * seedelta);
            seachanges = new MeshRenderer[size * size];
            for (int n = 0; n < size; n++)
            {
                for (int m = 0; m < size; m++)
                {
                    GameObject seaPlane = UnityEngine.Object.Instantiate<GameObject>(waterplane);
                    seaPlane.SetActive(true);
                    seachanges[n * size + m] = seaPlane.GetComponentInChildren<MeshRenderer>();
                    try
                    {
                        seachanges[n * size + m].material.SetFloat("_WaveStrength", seaStrenght);
                        seachanges[n * size + m].material.SetFloat("_Scale", 100f);
                        seachanges[n * size + m].material.SetFloat("_TScount", 100f);
                        seachanges[n * size + m].material.SetFloat("_WaveScale", 0.2f);
                        seachanges[n * size + m].material.SetFloat("_FoamDepth", 4f);
                    }
                    catch
                    {

                    }
                    seaPlane.transform.parent = SeaPlaneFather.transform;
                    seaPlane.transform.localScale = new Vector3(1500f, 15f, 1500f);

                    seaPlane.transform.localPosition = new Vector3((n - size / 2) * seedelta, 0, (m - size / 2) * seedelta);
                }
            }
            SetSeaPos();
        }
        public void SetSeaPos()
        {
            Vector3 pos = GetCameraPos();
            SeaPlaneFather.transform.position = new Vector3(pos.x, ModController.Instance.seahigh, pos.z);
        }
        public Vector3 GerstnerWave(Vector2 position, float amplitude, float frequency, float speed, float phase, float Dir, float seed)
        {
            float wavePhase = phase + speed * seed;
            Vector3 waveDirection = (new Vector2(Mathf.Sin(Dir), Mathf.Cos(Dir))).normalized;
            float waveFactor = amplitude * frequency * Mathf.Sin(frequency * Vector3.Dot(waveDirection, position) + wavePhase);

            Vector3 displacement;
            displacement.x = position.x + waveFactor * waveDirection.x;
            displacement.z = position.y + waveFactor * waveDirection.y;
            displacement.y = amplitude * Mathf.Cos(frequency * Vector3.Dot(waveDirection, position) + wavePhase);
            return displacement;
        }
        public Vector3 waveCalculateAll(Vector2 pos)
        {
            Vector3 wave1 = GerstnerWave(pos, seaStrenght, _WaveSpeeds[0], _WaveLengths[0], _WaveOffsets[0], _WaveDir[0], timeseed);
            Vector3 wave2 = GerstnerWave(pos, seaStrenght, _WaveSpeeds[1], _WaveLengths[1], _WaveOffsets[1], _WaveDir[1], timeseed);
            Vector3 wave3 = GerstnerWave(pos, seaStrenght, _WaveSpeeds[2], _WaveLengths[2], _WaveOffsets[2], _WaveDir[2], timeseed);
            Vector3 wave4 = GerstnerWave(pos, seaStrenght, _WaveSpeeds[3], _WaveLengths[3], _WaveOffsets[3], _WaveDir[3], timeseed);
            return (wave1 + wave2 + wave3 + wave4) / 20;

        }
        public float timedelta = 0.7f;
        float seedelta = 984f;
        public float[] _WaveSpeeds = new float[4] { 1.42f, -0.89f, -0.4f, 1.68f };
        public float[] _WaveLengths = new float[4] { 0.33f, 3.78f, 0.4f, 1.24f };
        public float[] _WaveOffsets = new float[4] { 0.18f, 5.57f, 8.8f, 3.29f };
        public float[] _WaveDir = new float[4] { 14.32f, 28.2f, -30.68f, -52.4f };
        public float timeseed;
        public float realtimeseed;
        public MeshRenderer[] seachanges;
        // Token: 0x0400015D RID: 349
        public GameObject SeaPlaneFather;
        public GameObject seaeffect;
        public Vector3 lastPos;
        // Token: 0x0400015E RID: 350
        public float seaStrenght = 0.32f;
    }
}

