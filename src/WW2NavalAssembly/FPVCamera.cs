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
                        Camera.main.fieldOfView = 80f;
                        Camera.main.nearClipPlane = 0.1f;
                    }
                    else
                    {
                        Camera.main.fieldOfView = pre_FOV;
                        Camera.main.nearClipPlane = pre_PlaneDist;
                    }
                    
                }
            }
            get { return _isActive; }
        }


        public Transform Base;

        public Vector3 PosOffset = new Vector3(0, 3.9f, 3.7f);

        public float rotationX;
        public float rotationY;
        public float Sensitivity = 2f;
        public float lerpCoeff = 0.1f;

        public float pre_FOV;
        public float pre_PlaneDist;

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
                transform.position = Base.transform.TransformPoint(PosOffset);
            }
        }
    }
}
