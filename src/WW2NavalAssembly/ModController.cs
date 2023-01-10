using System;
using System.Collections.Generic;
using System.Text;

using Modding;

using Modding.Common;
using UnityEngine;


namespace WW2NavalAssembly
{
    class ModController : SingleInstance<ModController>
    {
        public override string Name { get; } = "WW2NavalModController";

        private Rect windowRect = new Rect(15f, 100f, 280f, 50f);
        private readonly int windowID = ModUtility.GetWindowId();
        public bool windowHidden = false;
        public bool showArmour = false;

        public int state;


        private void Awake()
        {

        }

        public void Start()
        {

        }

        public void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.W))
                {
                    windowHidden = !windowHidden;
                }

            }
        }
        public void FixedUpdate()
        {
            if (state == 40)
            {
                state = 0;
            }
            else
            {
                state++;
            }

        }

        private void MACWindow(int windoID)
        {
            showArmour = GUILayout.Toggle(showArmour, "Show Armour Layout");
            GUILayout.Label("Press Ctrl+W to hide");

            GUI.DragWindow();

        }

        private void OnGUI()
        {
            //GUI.Box(new Rect(100, 200, 200, 50), BoundaryOff.ToString());

            if (!windowHidden && !StatMaster.hudHidden)
            {
                windowRect = GUILayout.Window(windowID, windowRect, new GUI.WindowFunction(MACWindow), "WW2-Naval Mod Setting");
            }
        }
    }
}
