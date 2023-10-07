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
        public bool IsActive;


        public Transform Base;

        public Vector3 PosOffset = new Vector3(0, 0, 1);

        public float rotationX;
        public float rotationY;
        public float Sensitivity = 0.1f;
        public float lerpCoeff = 0.1f;

        public void Update()
        {
            if (IsActive && Base)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    rotationX += Input.GetAxis("Mouse X") * Sensitivity;
                    rotationY += Input.GetAxis("Mouse Y") * Sensitivity;
                    rotationX = Mathf.Clamp(rotationX, -170, 170);
                    rotationY = Mathf.Clamp(rotationY, -70, 70);
                }

                transform.rotation = Quaternion.Lerp(transform.rotation, Base.transform.rotation * new Quaternion(1, 0, 0, 1f) * Quaternion.Euler(-rotationY, rotationX, 0), lerpCoeff);
            }
            
        }
        public void LateUpdate()
        {
            if (IsActive && Base)
            {
                transform.position = Base.transform.position + Base.transform.TransformPoint(PosOffset);
            }
        }
    }
}
