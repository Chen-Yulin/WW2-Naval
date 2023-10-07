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
    public class TacCamera : MonoBehaviour
    {
        bool _active = false;
        public bool IsActive
        {
            get { return _active; }
            set
            {
                _active = value;
                if (value)
                {
                    Camera.main.orthographic = true;
                    Camera.main.orthographicSize = _orthoSize;
                    transform.rotation = Quaternion.Euler(90, 0, 0);

                    Vector3 pos = Base.transform.position;
                    pos.y = 400f;
                    transform.position = pos;
                }
                else
                {
                    Camera.main.orthographic = false;
                }
            }
        }

        public float _orthoSize = 400f;
        public float ViewSensitivity;
        public MKey ResetView;
        public MKey ViewMove;

        public Transform Base;

        public Camera camera;

        public void Start()
        {
            camera = Camera.main;
        }
        public void Update()
        {
            if (IsActive && Base)
            {
                float mouseScroll = Input.mouseScrollDelta.y;
                _orthoSize = Mathf.Clamp(_orthoSize * (mouseScroll > 0 ? 1f / (1f + mouseScroll * 0.2f) : (1f - mouseScroll * 0.2f)), 50, 2000);
                camera.orthographicSize = _orthoSize;
                if (ResetView.IsPressed)
                {
                    transform.position = Base.transform.position;
                }

                // move camera
                if (ViewMove.IsHeld)
                {
                    float mouseX = Input.GetAxis("Mouse X");
                    float mouseY = Input.GetAxis("Mouse Y");
                    Vector3 moveDir = (mouseX * -Vector3.right + mouseY * -Vector3.forward);
                    moveDir.y = 0;
                    transform.position += _orthoSize * moveDir * 0.05f * ViewSensitivity;
                }

                Vector3 pos = transform.position;
                pos.y = 400f;
                transform.position = pos;

            }
            
        }

        public void OnDestroy()
        {
            ModCameraController.Instance.needCamera = true;
        }
    }
}
