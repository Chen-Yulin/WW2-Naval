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
    public class FPVCamera : MonoBehaviour
    {
        bool _isActive = false;
        public bool IsActive
        {
            set {
                if (_isActive != value)
                {
                    _isActive = value;
                    if (_isActive)
                    {
                        pre_FOV = Camera.main.fieldOfView;
                        pre_PlaneDist = Camera.main.nearClipPlane;
                        Camera.main.fieldOfView = 85f;
                        Camera.main.nearClipPlane = 0.02f;
                        transform.rotation = Base.transform.rotation;
                    }
                    else
                    {
                        Camera.main.fieldOfView = pre_FOV;
                        Camera.main.nearClipPlane = pre_PlaneDist;
                    }
                    cockpit.SetActive(value);
                }
            }
            get { return _isActive; }
        }


        public Transform Base;

        public Vector3 PosOffset;

        public float rotationX;
        public float rotationY;
        public float Sensitivity = 2f;
        public float lerpCoeff = 0.1f;

        public float pre_FOV;
        public float pre_PlaneDist;

        public GameObject cockpit;

        public void InitCockpit()
        {
            if (!transform.Find("Cockpit"))
            {
                cockpit = new GameObject("Cockpit");
                cockpit.transform.SetParent(transform);
                cockpit.transform.localPosition = Vector3.zero;
                cockpit.transform.localRotation = Quaternion.identity;
                cockpit.transform.localScale = Vector3.one * 0.012f;
                
                MeshFilter mf = cockpit.AddComponent<MeshFilter>();
                mf.sharedMesh = ModResource.GetMesh("Cockpit Mesh");
                MeshRenderer mr = cockpit.AddComponent<MeshRenderer>();
                mr.material.mainTexture = ModResource.GetTexture("Cockpit Texture").Texture;
            }
            else
            {
                cockpit = transform.Find("Cockpit").gameObject;
            }
            cockpit.SetActive(false);
        }
        public void Start()
        {
            InitCockpit();
        }

        public void Update()
        {
            if (IsActive && Base)
            {

                if (Input.GetMouseButton(1))
                {
                    rotationX += Input.GetAxis("Mouse X") * Sensitivity;
                    rotationY += Input.GetAxis("Mouse Y") * Sensitivity;
                    rotationX = Mathf.Clamp(rotationX, -170, 170);
                    rotationY = Mathf.Clamp(rotationY, -70, 70);
                }

                transform.rotation = Quaternion.Lerp(transform.rotation, Base.transform.rotation * Quaternion.Euler(-rotationY, rotationX, 0), lerpCoeff);
            }
            
        }
        public void LateUpdate()
        {
            if (IsActive && Base)
            {
                cockpit.transform.rotation = Base.transform.rotation;
                transform.position = Base.transform.TransformPoint(PosOffset);
            }
        }
    }
}
