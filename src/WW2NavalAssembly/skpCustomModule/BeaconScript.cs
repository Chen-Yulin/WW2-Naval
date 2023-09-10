using System;
using System.Collections.Generic;
using Modding;
using Modding.Common;
using UnityEngine;
using UnityEngine.UI;

namespace skpCustomModule
{
    // Token: 0x02000051 RID: 81
    public class BeaconScript : AddScriptBase
    {
        // Token: 0x1700003F RID: 63
        // (get) Token: 0x060001B3 RID: 435 RVA: 0x00002B62 File Offset: 0x00000D62
        private bool TargetMarkerDisable
        {
            get
            {
                return AdCustomModuleMod.mod4.toggle_TargetMarkerDisable;
            }
        }

        // Token: 0x060001B4 RID: 436 RVA: 0x0001AC80 File Offset: 0x00018E80
        public override void SafeAwake()
        {
            this.markerG_sp = Sprite.Create(this.markerG, new Rect(0f, 0f, (float)this.markerG.width, (float)this.markerG.height), new Vector2(0.5f, 0.5f));
            this.markerB_sp = Sprite.Create(this.markerB, new Rect(0f, 0f, (float)this.markerB.width, (float)this.markerB.height), new Vector2(0.5f, 0.5f));
            this.markerR_sp = Sprite.Create(this.markerR, new Rect(0f, 0f, (float)this.markerR.width, (float)this.markerR.height), new Vector2(0.5f, 0.5f));
            this.markerG_info_sp = Sprite.Create(this.markerG_info, new Rect(0f, 0f, (float)this.markerG_info.width, (float)this.markerG_info.height), new Vector2(0.5f, 0.5f));
            this.markerB_info_sp = Sprite.Create(this.markerB_info, new Rect(0f, 0f, (float)this.markerB_info.width, (float)this.markerB_info.height), new Vector2(0.5f, 0.5f));
            this.markerR_info_sp = Sprite.Create(this.markerR_info, new Rect(0f, 0f, (float)this.markerR_info.width, (float)this.markerR_info.height), new Vector2(0.5f, 0.5f));
            this.marker_entity_sp = Sprite.Create(this.marker_entity, new Rect(0f, 0f, (float)this.marker_entity.width, (float)this.marker_entity.height), new Vector2(0.5f, 0.5f));
            this.marker_progress_BG_sp = Sprite.Create(this.marker_progress_BG, new Rect(0f, 0f, 10f, 10f), new Vector2(0f, 1f));
            this.marker_progress_G_sp = Sprite.Create(this.marker_progress_G, new Rect(0f, 0f, 10f, 10f), new Vector2(0f, 1f));
            this.marker_progress_R_sp = Sprite.Create(this.marker_progress_R, new Rect(0f, 0f, 10f, 10f), new Vector2(0f, 1f));
            this.diamarkerG_sp = Sprite.Create(this.diamarkerG, new Rect(0f, 0f, (float)this.diamarkerG.width, (float)this.diamarkerG.height), new Vector2(0.5f, 0.5f));
            this.hud_direction_mask_sp = Sprite.Create(this.hud_direction_mask, new Rect(0f, 0f, (float)this.hud_direction_mask.width, (float)this.hud_direction_mask.height), new Vector2(0.5f, 0.5f));
            this.hud_direction_sp = Sprite.Create(this.hud_direction, new Rect(0f, 0f, (float)this.hud_direction.width, (float)this.hud_direction.height), new Vector2(0.5f, 0.5f));
            this.hud_horizon_sp = Sprite.Create(this.hud_horizon, new Rect(0f, 0f, (float)this.hud_horizon.width, (float)this.hud_horizon.height), new Vector2(0.5f, 0.5f));
            this.hud_altitude_sp = Sprite.Create(this.hud_altitude, new Rect(0f, 0f, (float)this.hud_altitude.width, (float)this.hud_altitude.height), new Vector2(0.5f, 0.5f));
            this.hud_altitude_mask_sp = Sprite.Create(this.hud_altitude_mask, new Rect(0f, 0f, (float)this.hud_altitude_mask.width, (float)this.hud_altitude_mask.height), new Vector2(0.5f, 0.5f));
            this.hud_speed_sp = Sprite.Create(this.hud_speed, new Rect(0f, 0f, (float)this.hud_speed.width, (float)this.hud_speed.height), new Vector2(0.5f, 0.5f));
            this.hud_speed_mask_sp = Sprite.Create(this.hud_speed_mask, new Rect(0f, 0f, (float)this.hud_speed_mask.width, (float)this.hud_speed_mask.height), new Vector2(0.5f, 0.5f));
            this.team_icon_sp = Sprite.Create(this.team_icon, new Rect(0f, 0f, (float)this.team_icon.width, (float)this.team_icon.height), new Vector2(0.5f, 0.5f));
            this.sq_white_sp = Sprite.Create(this.sq_white, new Rect(0f, 0f, (float)this.sq_white.width, (float)this.sq_white.height), new Vector2(0.5f, 0.5f));
            this.FunctionToggle = this.ObjectBehavior.AddToggle("HUD using", "AdHud", false);
            this.FunctionToggleKey = this.ObjectBehavior.AddKey("HUD enable", "AdHudEnableKey", 0);
            this.FunctionSlider = this.ObjectBehavior.AddSlider("HUD alpha", "AdHudAlpha", 0.5f, 0f, 1f, "", "x");
            this.FunctionSlider2 = this.ObjectBehavior.AddSlider("HUD Text alpha", "AdHudTextAlpha", 0.8f, 0f, 1f, "", "x");
            this.SoundVolumeSlider = this.ObjectBehavior.AddSlider("SE Vlume", "AdSEVolume", 0.5f, 0f, 1f, "", "x");
            this.ColorSlider_normal = this.ObjectBehavior.AddColourSlider("HUD color", "AdHudColor", new Color(0f, 1f, 0.3f, 0f), false, true);
            this.ColorSlider_alert = this.ObjectBehavior.AddColourSlider("HUD alert color", "AdHudAlertColor", new Color(1f, 0.22f, 0.12f, 0f), false, true);
            this.PlayerData = Player.From((ushort)this.OwnerID);
            bool isMP = StatMaster.isMP;
            if (isMP)
            {
                this.ServerMachineData = this.PlayerData.InternalObject.machine;
                this.Labeltransform = GameObject.Find("HUD").transform.Find("MULTIPLAYER").transform.Find("PLAYER_LABELS").gameObject;
            }
            this.TargetId.Clear();
            bool flag = AdCustomModuleMod.mod2.TaregtIdListContainer.ContainsKey(this.OwnerID);
            if (flag)
            {
                AdCustomModuleMod.mod2.TaregtIdListContainer[this.OwnerID].Clear();
            }
            else
            {
                AdCustomModuleMod.mod2.TaregtIdListContainer.Add(this.OwnerID, this.TargetId);
            }
            bool flag2 = AdCustomModuleMod.mod2.PlayerRespawnFlagContainer.ContainsKey(this.OwnerID);
            if (flag2)
            {
                AdCustomModuleMod.mod2.PlayerRespawnFlagContainer[this.OwnerID] = false;
            }
            else
            {
                AdCustomModuleMod.mod2.PlayerRespawnFlagContainer.Add(this.OwnerID, false);
            }
            for (int i = 0; i < 20; i++)
            {
                bool flag3 = !this.LockonTimer.ContainsKey(i);
                if (flag3)
                {
                    this.LockonTimer.Add(i, 0f);
                }
            }
            for (int j = 0; j < 20; j++)
            {
                bool flag4 = !this.LockonTergetDistance.ContainsKey(j);
                if (flag4)
                {
                    this.LockonTergetDistance.Add(j, 0f);
                }
            }
            bool flag5 = AdCustomModuleMod.mod2.AdSeAudioSourceContainer.ContainsKey("alert");

            this.AudioManager = GameObject.Find("AdAudioManager");
            this.SoundController = this.AudioManager.GetComponent<AdSoundController>();
            this.SoundController.SetOneShotClip(0, AdCustomModuleMod.mod2.AdSeAudioSourceContainer["alert"]);
            this.SoundController.SetOneShotClip(1, AdCustomModuleMod.mod2.AdSeAudioSourceContainer["lock"]);
            this.SoundController.SetLoopClip(0, AdCustomModuleMod.mod2.AdLoopAudioSourceContainer["locking"]);
            this.ACMUIcanvas = GameObject.Find("AdUIcanvas");
            this.UItextContainer = AdCustomModuleMod.mod2.UItextContainer;
            this.UIimageContainer = AdCustomModuleMod.mod2.UIimageContainer;
            this.UImaskedimageContainer = AdCustomModuleMod.mod2.UImaskedimageContainer;
            this.UIStyle.font = this.UIfont;
            this.UIStyle.stretchHeight = true;
            this.UIStyle.fixedHeight = 20f;
            this.UIStyle.normal.textColor = Color.green;
            for (int k = 0; k < 10; k++)
            {
                this.coreSpeedArray[k] = Vector3.zero;
                this.IntegrationTimeArray[k] = 0f;
            }
            for (int l = 81; l < 84; l++)
            {
                bool flag8 = !this.UIimageContainer[l].GetComponent<Mask>();
                if (flag8)
                {
                    Mask mask = this.UIimageContainer[l].AddComponent<Mask>();
                    mask.showMaskGraphic = false;
                }
                GameObject gameObject = this.UImaskedimageContainer[l];
                gameObject.transform.SetParent(this.UIimageContainer[l].transform);
                this.UImaskedimageContainer[l].SetActive(true);
                this.UIimageContainer[l].SetActive(false);
            }
        }

        // Token: 0x060001B5 RID: 437 RVA: 0x0001BAB4 File Offset: 0x00019CB4
        public override void SafeEnable()
        {
            this.UIinit();
            this.HUDactive = false;
            AdCustomModuleMod.mod2.PlayerRespawnFlagContainer[this.OwnerID] = true;
            this.RespawnResetTime = 0.2f;
            this.CameraMain = Camera.main;
            this.LabelModeCheck = StatMaster.Mode.hideLabels;
        }

        // Token: 0x060001B6 RID: 438 RVA: 0x0001BB08 File Offset: 0x00019D08
        public void OnDisable()
        {
            this.UIinit();
            this.HUDactive = false;
            this.Init = false;
            this.EntityCount = 0;
            bool hudinit = this.HUDInit;
            if (hudinit)
            {
                this.HUDInit = false;
                bool flag = StatMaster.isMP && this.Labeltransform != null;
                if (flag)
                {
                    bool flag2 = !this.Labeltransform.activeSelf;
                    if (flag2)
                    {
                        this.Labeltransform.SetActive(true);
                    }
                }
            }
        }

        // Token: 0x060001B7 RID: 439 RVA: 0x0001BB84 File Offset: 0x00019D84
        public void Update()
        {
            bool isSimulating = this.ObjectBehavior.isSimulating;
            if (isSimulating)
            {
                bool flag = this.FunctionToggleKey.IsPressed || this.FunctionToggleKey.EmulationPressed();
                if (flag)
                {
                    this.modeflag = !this.modeflag;
                }
                this.BeaconPosi = base.gameObject.transform.position;
                this.BeaconRot = base.gameObject.transform.rotation;
                bool flag2 = !this.Init;
                if (flag2)
                {
                    this.PlayerData = Player.From((ushort)this.OwnerID);
                    float value = this.FunctionSlider.Value;
                    float value2 = this.FunctionSlider2.Value;
                    this.HUDColorNormal = new Color(this.ColorSlider_normal.Value.r, this.ColorSlider_normal.Value.g, this.ColorSlider_normal.Value.b, value);
                    this.HUDColorAlert = new Color(this.ColorSlider_alert.Value.r, this.ColorSlider_alert.Value.g, this.ColorSlider_alert.Value.b, value);
                    this.HUDColorNormaltext = new Color(this.ColorSlider_normal.Value.r, this.ColorSlider_normal.Value.g, this.ColorSlider_normal.Value.b, value2);
                    this.HUDColorAlerttext = new Color(this.ColorSlider_alert.Value.r, this.ColorSlider_alert.Value.g, this.ColorSlider_alert.Value.b, value2);
                    this.HUDColorGrayText = new Color(0.88f, 0.88f, 0.88f, value2);
                    this.HUDColorGray = new Color(0.88f, 0.88f, 0.88f, value);
                    this.HUDColorWhite = new Color(1f, 1f, 1f, value);
                    this.TeamColorGray = new Color(0.65f, 0.65f, 0.65f, value2);
                    this.TeamColorRed = new Color(1f, 0.15f, 0f, value2);
                    this.TeamColorGreen = new Color(0.37f, 1f, 0f, value2);
                    this.TeamColorBlue = new Color(0f, 0.8f, 1f, value2);
                    this.TeamColorOrange = new Color(1f, 0.8f, 0f, value2);
                    bool isMP = StatMaster.isMP;
                    if (isMP)
                    {
                        this.ServerMachineData = this.PlayerData.InternalObject.machine;
                    }
                    this.Init = true;
                }
            }
            try
            {
                bool flag3 = StatMaster.isMP && this.PlayerData.Machine.InternalObject.isRespawning;
                if (flag3)
                {
                }
            }
            catch
            {
                Debug.Log("Left core : " + this.OwnerID.ToString());
            }
            bool flag4 = StatMaster.isMP && this.ObjectBehavior.isSimulating;
            if (flag4)
            {
                bool isClient = StatMaster.isClient;
                if (!isClient)
                {
                    bool isHosting = StatMaster.isHosting;
                    if (isHosting)
                    {
                    }
                }
                bool flag5 = !AdCustomModuleMod.mod2.BeaconContainer.ContainsKey(this.OwnerID);
                if (flag5)
                {
                    Debug.Log("Add core : " + this.OwnerID.ToString());
                    AdShootingModule.BeaconData beaconData = new AdShootingModule.BeaconData();
                    beaconData.Posi = this.BeaconPosi;
                    beaconData.Name = this.PlayerData.Name;
                    beaconData.Team = this.PlayerData.Team;
                    AdCustomModuleMod.mod2.BeaconContainer.Add(this.OwnerID, beaconData);
                }
                else
                {
                    AdCustomModuleMod.mod2.BeaconContainer[this.OwnerID].Posi = this.BeaconPosi;
                    AdCustomModuleMod.mod2.BeaconContainer[this.OwnerID].Name = this.PlayerData.Name;
                    AdCustomModuleMod.mod2.BeaconContainer[this.OwnerID].Team = this.PlayerData.Team;
                }
                float sqrMagnitude = (this.BeaconPosi - Camera.main.transform.position).sqrMagnitude;
                bool flag6 = sqrMagnitude > this.LockonNormalDistance;
                if (flag6)
                {
                    this.LockonTimeRatio = this.LockonNormalDistance / sqrMagnitude;
                }
                else
                {
                    this.LockonTimeRatio = 1f;
                }
            }
        }

        // Token: 0x060001B8 RID: 440 RVA: 0x0001C020 File Offset: 0x0001A220
        public void FixedUpdate()
        {
            bool isMP = StatMaster.isMP;
            if (isMP)
            {
                this.NetworkID = (int)Player.GetLocalPlayer().NetworkId;
                bool flag = StatMaster.isClient && this.TargetChangeFlag && this.OwnerID == this.NetworkID;
                if (flag)
                {
                    int[] array = this.TargetId.ToArray();
                    ModNetworking.SendToHost(AdCustomModuleMod.msgLockOnData.CreateMessage(new object[] { this.OwnerID, array }));
                    this.TargetChangeFlag = false;
                }
                bool flag2 = AdCustomModuleMod.mod2.PlayerRespawnFlagContainer[this.OwnerID];
                if (flag2)
                {
                    bool flag3 = this.RespawnResetTime > 0f;
                    if (flag3)
                    {
                        this.RespawnResetTime -= Time.deltaTime;
                    }
                    else
                    {
                        this.RespawnResetTime = 0f;
                        AdCustomModuleMod.mod2.PlayerRespawnFlagContainer[this.OwnerID] = false;
                    }
                }
                bool flag4 = StatMaster.isHosting && this.TargetChangeFlag && this.OwnerID == this.NetworkID;
                if (flag4)
                {
                    bool flag5 = AdCustomModuleMod.mod2.TaregtIdListContainer.ContainsKey(this.OwnerID);
                    if (flag5)
                    {
                        AdCustomModuleMod.mod2.TaregtIdListContainer[this.OwnerID] = this.TargetId;
                    }
                    else
                    {
                        AdCustomModuleMod.mod2.TaregtIdListContainer.Add(this.OwnerID, this.TargetId);
                    }
                    this.TargetChangeFlag = false;
                }
                bool intercept = this.Intercept;
                if (intercept)
                {
                    bool flag6 = this.WarningTimer == 0f;
                    if (flag6)
                    {
                        this.SoundController.SEPlay(this.seVolume, 0, false);
                    }
                    this.WarningTimer += Time.deltaTime;
                    bool flag7 = this.WarningTimer > this.WarningInterval * Time.timeScale;
                    if (flag7)
                    {
                        this.WarningTimer = 0f;
                    }
                }
            }
            bool flag8 = !StatMaster.isMP || StatMaster.isHosting || StatMaster.isLocalSim || (StatMaster.isClient && StatMaster.InLocalPlayMode);
            bool flag9 = flag8;
            if (flag9)
            {
                GameObject gameObject = base.transform.FindChild("Vis").gameObject;
                for (int i = 0; i < 9; i++)
                {
                    this.coreSpeedArray[9 - i] = this.coreSpeedArray[8 - i];
                    this.IntegrationTimeArray[9 - i] = this.IntegrationTimeArray[8 - i];
                }
                this.coreSpeedArray[0] = gameObject.transform.position;
                this.IntegrationTimeArray[0] = Time.fixedDeltaTime;
                this.SpeedIntegrationTime = 0f;
                float num = 0f;
                for (int j = 0; j < 4; j++)
                {
                    num += (this.coreSpeedArray[j + 1] - this.coreSpeedArray[j]).magnitude;
                    this.SpeedIntegrationTime += this.IntegrationTimeArray[j];
                }
                this.CoreSpeed = num / this.SpeedIntegrationTime * 60f * 60f / 1000f;
                bool flag10 = !AdCustomModuleMod.mod2.CoreSpeedContainer.ContainsKey(this.OwnerID);
                if (flag10)
                {
                    AdCustomModuleMod.mod2.CoreSpeedContainer.Add(this.OwnerID, this.CoreSpeed);
                }
                else
                {
                    AdCustomModuleMod.mod2.CoreSpeedContainer[this.OwnerID] = this.CoreSpeed;
                }
                float num2 = 0f;
                for (int k = 0; k < 4; k++)
                {
                    num2 += (this.coreSpeedArray[k + 2] - this.coreSpeedArray[k + 1]).magnitude - (this.coreSpeedArray[k + 1] - this.coreSpeedArray[k]).magnitude;
                }
                Vector3 vector = (this.coreSpeedArray[3] + this.coreSpeedArray[0]).normalized * num2 * 4f;
                Vector3 vector2;
                vector2 = new Vector3(0f, 32.81f * this.SpeedIntegrationTime * this.SpeedIntegrationTime, 0f);
                this.CoreAccelertion = (vector + vector2).magnitude / this.SpeedIntegrationTime / this.SpeedIntegrationTime / 9.8f;
                bool flag11 = !AdCustomModuleMod.mod2.CoreAccelerationContainer.ContainsKey(this.OwnerID);
                if (flag11)
                {
                    AdCustomModuleMod.mod2.CoreAccelerationContainer.Add(this.OwnerID, this.CoreAccelertion);
                }
                else
                {
                    AdCustomModuleMod.mod2.CoreAccelerationContainer[this.OwnerID] = this.CoreAccelertion;
                }
                float deltaTime = TimeSlider.Instance.deltaTime;
                this.speedCounter += deltaTime;
                float sendRate = NetworkScene.ServerSettings.sendRate;
                bool flag12 = this.speedCounter >= sendRate;
                if (flag12)
                {
                    this.speedCounter = 0f;
                    bool flag13 = this.OwnerID != this.NetworkID;
                    if (flag13)
                    {
                        ModNetworking.SendToAll(AdCustomModuleMod.msgCoreSpeed.CreateMessage(new object[]
                        {
                            this.OwnerID,
                            AdCustomModuleMod.mod2.CoreSpeedContainer[this.OwnerID]
                        }));
                        ModNetworking.SendToAll(AdCustomModuleMod.msgCoreAcceleration.CreateMessage(new object[]
                        {
                            this.OwnerID,
                            AdCustomModuleMod.mod2.CoreAccelerationContainer[this.OwnerID]
                        }));
                    }
                }
            }
        }

        // Token: 0x060001B9 RID: 441 RVA: 0x0001C608 File Offset: 0x0001A808
        public void OnGUI()
        {
            bool flag = this.ObjectBehavior != null && this.FunctionToggle.IsActive && this.modeflag;
            if (flag)
            {
                this.seVolume = this.SoundVolumeSlider.Value;
                this.HUDactive = true;
                bool flag2 = StatMaster.levelSimulating && this.OwnerID == this.NetworkID;
                if (flag2)
                {
                    bool flag3 = !this.HUDInit;
                    if (flag3)
                    {
                        bool isMP = StatMaster.isMP;
                        if (isMP)
                        {
                            bool activeSelf = this.Labeltransform.activeSelf;
                            if (activeSelf)
                            {
                                this.Labeltransform.SetActive(false);
                            }
                        }
                        this.HUDInit = true;
                    }
                    Rect rect;
                    rect = new Rect(0f, 0f, (float)Screen.width, (float)Screen.height);
                    Color hudcolorWhite = this.HUDColorWhite;
                    float num = (float)Screen.height / 1080f;
                    GUI.color = hudcolorWhite;
                    bool intercept = this.Intercept;
                    Color color;
                    Color co;
                    if (intercept)
                    {
                        color = this.HUDColorAlert;
                        co = this.HUDColorAlerttext;
                    }
                    else
                    {
                        color = this.HUDColorNormal;
                        co = this.HUDColorNormaltext;
                    }
                    GUI.color = color;
                    GUI.DrawTexture(rect, this.hud_left);
                    GUI.DrawTexture(rect, this.hud_right);
                    GUI.DrawTexture(rect, this.hud_center_out);
                    GUI.color = this.HUDColorGray;
                    GUI.DrawTexture(rect, this.hud_center_in);
                    GUI.color = hudcolorWhite;
                    float num2 = 0f;
                    float num3 = 0f;
                    bool flag4 = AdCustomModuleMod.mod2.CoreSpeedContainer.ContainsKey(this.OwnerID);
                    if (flag4)
                    {
                        num2 = AdCustomModuleMod.mod2.CoreSpeedContainer[this.OwnerID];
                    }
                    bool flag5 = AdCustomModuleMod.mod2.CoreAccelerationContainer.ContainsKey(this.OwnerID);
                    if (flag5)
                    {
                        num3 = AdCustomModuleMod.mod2.CoreAccelerationContainer[this.OwnerID];
                    }
                    bool flag6 = !this.UIimageContainer[81].activeSelf;
                    if (flag6)
                    {
                        this.UIimageContainer[81].SetActive(true);
                    }
                    Image component = this.UIimageContainer[81].GetComponent<Image>();
                    component.sprite = this.hud_direction_mask_sp;
                    component.color = color;
                    RectTransform rectTransform = component.rectTransform;
                    rectTransform.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform.sizeDelta = new Vector3(1920f, 1080f, 1f);
                    rectTransform.localPosition = new Vector3(0f, 0f, 0f);
                    Vector3 eulerAngles = this.CameraMain.transform.eulerAngles;
                    float num4 = 1200f * eulerAngles.y / 360f;
                    Image component2 = this.UImaskedimageContainer[81].GetComponent<Image>();
                    component2.sprite = this.hud_direction_sp;
                    component2.color = color;
                    RectTransform rectTransform2 = component2.rectTransform;
                    rectTransform2.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform2.sizeDelta = new Vector3(1920f, 1080f, 1f);
                    rectTransform2.localPosition = new Vector3(600f - num4, 0f, 0f);
                    bool flag7 = !this.UIimageContainer[80].activeSelf;
                    if (flag7)
                    {
                        this.UIimageContainer[80].SetActive(true);
                    }
                    Image component3 = this.UIimageContainer[80].GetComponent<Image>();
                    component3.sprite = this.hud_horizon_sp;
                    component3.color = color;
                    RectTransform rectTransform3 = component3.rectTransform;
                    rectTransform3.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform3.sizeDelta = new Vector3(1080f, 1080f, 1f);
                    rectTransform3.localPosition = new Vector3(0f, 0f, 0f);
                    Vector3 vector = Vector3.Cross(new Vector3(0f, 1f, 0f), this.CameraMain.transform.forward);
                    float num5 = Vector3.Angle(vector, this.CameraMain.transform.right);
                    bool flag8 = Vector3.Cross(vector, this.CameraMain.transform.right).normalized == this.CameraMain.transform.forward;
                    if (flag8)
                    {
                        rectTransform3.localRotation = Quaternion.Euler(0f, 0f, -num5);
                    }
                    else
                    {
                        rectTransform3.localRotation = Quaternion.Euler(0f, 0f, num5);
                    }
                    bool flag9 = !this.UIimageContainer[82].activeSelf;
                    if (flag9)
                    {
                        this.UIimageContainer[82].SetActive(true);
                    }
                    Image component4 = this.UIimageContainer[82].GetComponent<Image>();
                    component4.sprite = this.hud_altitude_mask_sp;
                    component4.color = color;
                    RectTransform rectTransform4 = component4.rectTransform;
                    rectTransform4.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform4.sizeDelta = new Vector3(1920f, 1080f, 1f);
                    rectTransform4.localPosition = new Vector3(0f, 0f, 0f);
                    float num6 = this.BeaconPosi.y - 100f * (float)Math.Floor((double)(this.BeaconPosi.y / 100f));
                    Image component5 = this.UImaskedimageContainer[82].GetComponent<Image>();
                    component5.sprite = this.hud_altitude_sp;
                    component5.color = color;
                    RectTransform rectTransform5 = component5.rectTransform;
                    rectTransform5.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform5.sizeDelta = new Vector3(1103f, 1103f, 1f);
                    rectTransform5.localPosition = new Vector3(0f, 0f, 0f);
                    rectTransform5.localRotation = Quaternion.Euler(0f, 0f, -num6);
                    Vector3 posi;
                    posi = new Vector3(600f, -10f, 0f);
                    Vector3 size;
                    size = new Vector3(0.25f, 0.25f, 1f);
                    Vector3 sc;
                    sc = new Vector3(800f, 200f, 1f);
                    this.TextCreate(this.UItextContainer[22], string.Format("{0,5:F1}", this.BeaconPosi.y) + "M", co, size, sc, posi, 0);
                    bool flag10 = !this.UIimageContainer[83].activeSelf;
                    if (flag10)
                    {
                        this.UIimageContainer[83].SetActive(true);
                    }
                    Image component6 = this.UIimageContainer[83].GetComponent<Image>();
                    component6.sprite = this.hud_speed_mask_sp;
                    component6.color = color;
                    RectTransform rectTransform6 = component6.rectTransform;
                    rectTransform6.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform6.sizeDelta = new Vector3(1920f, 1080f, 1f);
                    rectTransform6.localPosition = new Vector3(0f, 0f, 0f);
                    float num7 = (num2 - 1000f * (float)Math.Floor((double)(num2 / 1000f))) / 5f;
                    Image component7 = this.UImaskedimageContainer[83].GetComponent<Image>();
                    component7.sprite = this.hud_speed_sp;
                    component7.color = color;
                    RectTransform rectTransform7 = component7.rectTransform;
                    rectTransform7.localScale = new Vector3(1f, 1f, 1f);
                    rectTransform7.sizeDelta = new Vector3(1103f, 1103f, 1f);
                    rectTransform7.localPosition = new Vector3(0f, 0f, 0f);
                    rectTransform7.localRotation = Quaternion.Euler(0f, 0f, num7);
                    Vector3 posi2;
                    posi2 = new Vector3(-800f, -10f, 0f);
                    Vector3 size2;
                    size2 = new Vector3(0.25f, 0.25f, 1f);
                    Vector3 sc2;
                    sc2 = new Vector3(800f, 200f, 1f);
                    this.TextCreate(this.UItextContainer[21], string.Format("{0,5:F1}", num2) + "KMPH", co, size2, sc2, posi2, (TextAnchor)2);
                    Vector3 posi3;
                    posi3 = new Vector3(-800f, -55f, 0f);
                    Vector3 size3;
                    size3 = new Vector3(0.25f, 0.25f, 1f);
                    Vector3 sc3;
                    sc3 = new Vector3(800f, 200f, 1f);
                    this.TextCreate(this.UItextContainer[20], string.Format("{0,5:F1}", num3) + "G", co, size3, sc3, posi3, (TextAnchor)2);
                    bool isMP2 = StatMaster.isMP;
                    if (isMP2)
                    {
                        bool registerDamage = this.ServerMachineData.registerDamage;
                        if (registerDamage)
                        {
                            bool flag11 = !this.UIimageContainer[84].activeSelf;
                            if (flag11)
                            {
                                this.UIimageContainer[84].SetActive(true);
                            }
                            bool flag12 = !this.UIimageContainer[85].activeSelf;
                            if (flag12)
                            {
                                this.UIimageContainer[85].SetActive(true);
                            }
                            bool registerDamage2 = this.ServerMachineData.registerDamage;
                            float num8 = this.ServerMachineData.Health;
                            bool flag13 = !registerDamage2;
                            if (flag13)
                            {
                                num8 = 0f;
                            }
                            Vector3 posi4;
                            posi4 = new Vector3(-100f, -450f, 0f);
                            Vector3 sc4;
                            sc4 = new Vector3(num8 * 200f, 6f, 1f);
                            this.MakerCreateLU(this.UIimageContainer[84], this.sq_white_sp, co, sc4, posi4);
                            Vector3 posi5;
                            posi5 = new Vector3(-100f + num8 * 200f, -450f, 0f);
                            Vector3 sc5;
                            sc5 = new Vector3(200f - num8 * 200f, 6f, 1f);
                            this.MakerCreateLU(this.UIimageContainer[85], this.sq_white_sp, this.HUDColorGrayText, sc5, posi5);
                        }
                    }
                    this.EntityCount = AdCustomModuleMod.mod2.EntityCount;
                    int num9 = 0;
                    bool flag14 = this.EntityCount != 0;
                    if (flag14)
                    {
                        foreach (KeyValuePair<long, AdShootingModule.EntityData> keyValuePair in AdCustomModuleMod.mod2.EntityMarkerContainer)
                        {
                            num9++;
                            Vector3 position = keyValuePair.Value.transform.position;
                            Vector3 vector2 = Camera.main.WorldToScreenPoint(position);
                            float magnitude = (this.BeaconPosi - position).magnitude;
                            bool flag15 = vector2.z > 0f;
                            if (flag15)
                            {
                                Vector3 posi6;
                                posi6 = new Vector3((vector2.x - (float)(Screen.width / 2)) / num + 55f, (vector2.y - (float)(Screen.height / 2)) / num + 30f, 0f);
                                Vector3 posi7;
                                posi7 = new Vector3((vector2.x - (float)(Screen.width / 2)) / num, (vector2.y - (float)(Screen.height / 2)) / num, 0f);
                                Vector3 size4;
                                size4 = new Vector3(1f, 1f, 1f);
                                Vector3 sc6;
                                sc6 = new Vector3(800f, 200f, 1f);
                                Vector3 posi8;
                                posi8 = new Vector3((vector2.x - (float)(Screen.width / 2)) / num + 55f, (vector2.y - (float)(Screen.height / 2)) / num, 0f);
                                Vector3 size5;
                                size5 = new Vector3(0.2f, 0.2f, 1f);
                                Vector3 sc7;
                                sc7 = new Vector3(1000f, 200f, 1f);
                                bool flag16 = !AdCustomModuleMod.mod2.UIEntitytextContainer[num9].activeSelf;
                                if (flag16)
                                {
                                    AdCustomModuleMod.mod2.UIEntitytextContainer[num9].SetActive(true);
                                }
                                this.TextCreate(AdCustomModuleMod.mod2.UIEntitytextContainer[num9], keyValuePair.Value.Name, co, size4, sc6, posi6, 0);
                                this.TextCreate(AdCustomModuleMod.mod2.UIEntityDistanceContainer[num9], string.Format("{0,4:F1}", magnitude) + "M", co, size5, sc7, posi8, 0);
                                this.MakerCreateHH(AdCustomModuleMod.mod2.UIEntityimageContainer[num9], this.marker_entity_sp, color, posi7);
                            }
                        }
                        int num10 = this.EntityCount - num9;
                        bool flag17 = num10 > 0;
                        if (flag17)
                        {
                            for (int num11 = num9; num11 == this.EntityCount; num11++)
                            {
                                bool activeSelf2 = AdCustomModuleMod.mod2.UIEntitytextContainer[num11].activeSelf;
                                if (activeSelf2)
                                {
                                    AdCustomModuleMod.mod2.UIEntitytextContainer[num11].SetActive(false);
                                }
                            }
                        }
                    }
                    GUI.color = hudcolorWhite;
                    bool flag18 = false;
                    for (int i = 0; i < 20; i++)
                    {
                        bool flag19 = i != this.OwnerID;
                        if (flag19)
                        {
                            Player player = Player.From((ushort)i);
                            bool flag20 = player != null && (int)player.InternalObject.PlayMode == 2;
                            if (flag20)
                            {
                                float num12 = 0f;
                                MPTeam mpteam = 0;
                                bool isMP3 = StatMaster.isMP;
                                if (isMP3)
                                {
                                    ServerMachine machine = player.InternalObject.machine;
                                    bool registerDamage3 = machine.registerDamage;
                                    bool flag21 = registerDamage3;
                                    if (flag21)
                                    {
                                        num12 = machine.Health;
                                    }
                                    mpteam = player.Team;
                                }
                                bool flag22 = AdCustomModuleMod.mod2.BeaconContainer.ContainsKey(i);
                                if (flag22)
                                {
                                    Vector3 posi9 = AdCustomModuleMod.mod2.BeaconContainer[i].Posi;
                                    Vector3 vector3 = Camera.main.WorldToScreenPoint(posi9);
                                    float magnitude2 = (this.BeaconPosi - posi9).magnitude;
                                    this.LockonTergetDistance[i] = magnitude2;
                                    Rect rect2;
                                    rect2 = new Rect((float)(Screen.width / 2 - Screen.height / 2), 0f, (float)Screen.height, (float)Screen.height);
                                    Vector3 vector4;
                                    vector4 = new Vector3(vector3.x - (float)(Screen.width / 2), vector3.y - (float)(Screen.height / 2), 0f);
                                    float num13 = Vector3.Angle(vector4, new Vector3(1f, 0f, 0f));
                                    bool flag23 = false;
                                    bool flag24 = false;
                                    float num14 = 0f;
                                    bool flag25 = vector3.z > 0f;
                                    if (flag25)
                                    {
                                        bool flag26 = vector3.y > (float)(Screen.height / 2);
                                        if (flag26)
                                        {
                                            num13 = -num13;
                                        }
                                    }
                                    else
                                    {
                                        num13 = 180f - num13;
                                        bool flag27 = vector3.y < (float)(Screen.height / 2);
                                        if (flag27)
                                        {
                                            num13 = -num13;
                                        }
                                    }
                                    bool flag28 = (double)vector4.magnitude < (double)Screen.height * 0.33 && vector3.z > 0f && !this.TargetMarkerDisable && !this.LabelModeCheck;
                                    if (flag28)
                                    {
                                        flag23 = true;
                                        bool flag29 = mpteam != this.ObjectBehavior.Team && magnitude2 < 1500f && AdCustomModuleMod.mod2.MissileNum > 0;
                                        if (flag29)
                                        {
                                            bool flag30 = this.LockonTimer[i] > this.LockonTime;
                                            if (flag30)
                                            {
                                                flag24 = true;
                                                bool flag31 = !this.TargetId.Contains(i);
                                                if (flag31)
                                                {
                                                    this.TargetId.Add(i);
                                                    this.TargetChangeFlag = true;
                                                    this.SoundController.SEPlay(this.seVolume, 1, false);
                                                }
                                                num14 = 100f;
                                            }
                                            else
                                            {
                                                Dictionary<int, float> lockonTimer = this.LockonTimer;
                                                int key = i;
                                                lockonTimer[key] += Time.deltaTime * this.LockonTimeRatio;
                                                num14 = this.LockonTimer[i] / this.LockonTime * 100f;
                                                flag18 = true;
                                            }
                                        }
                                        else
                                        {
                                            this.LockonTimer[i] = 0f;
                                            bool flag32 = this.TargetId.Contains(i);
                                            if (flag32)
                                            {
                                                this.TargetId.Remove(i);
                                                this.TargetChangeFlag = true;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        this.LockonTimer[i] = 0f;
                                        bool flag33 = this.TargetId.Contains(i);
                                        if (flag33)
                                        {
                                            this.TargetId.Remove(i);
                                            this.TargetChangeFlag = true;
                                        }
                                    }
                                    bool flag34 = mpteam == this.ObjectBehavior.Team;
                                    if (flag34)
                                    {
                                        GUIUtility.RotateAroundPivot(num13, rect2.center);
                                        bool flag35 = flag23;
                                        if (flag35)
                                        {
                                            GUI.DrawTexture(rect2, this.hud_blue_in);
                                        }
                                        else
                                        {
                                            GUI.DrawTexture(rect2, this.hud_blue_out);
                                        }
                                        GUI.matrix = Matrix4x4.identity;
                                    }
                                    else
                                    {
                                        bool flag36 = mpteam != this.ObjectBehavior.Team && !this.TargetMarkerDisable && !this.LabelModeCheck;
                                        if (flag36)
                                        {
                                            GUIUtility.RotateAroundPivot(num13, rect2.center);
                                            bool flag37 = flag23;
                                            if (flag37)
                                            {
                                                GUI.DrawTexture(rect2, this.hud_green_in);
                                            }
                                            else
                                            {
                                                GUI.DrawTexture(rect2, this.hud_green_out);
                                            }
                                            GUI.matrix = Matrix4x4.identity;
                                        }
                                    }
                                    bool flag38 = vector3.z > 0f;
                                    if (flag38)
                                    {
                                        Vector3 posi10;
                                        posi10 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num + 65f, (vector3.y - (float)(Screen.height / 2)) / num + 10f, 0f);
                                        Vector3 size6;
                                        size6 = new Vector3(0.2f, 0.2f, 1f);
                                        Vector3 sc8;
                                        sc8 = new Vector3(1000f, 200f, 1f);
                                        Vector3 posi11;
                                        posi11 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num, (vector3.y - (float)(Screen.height / 2)) / num, 0f);
                                        Vector3 posi12;
                                        posi12 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num + 65f, (vector3.y - (float)(Screen.height / 2)) / num + 64f, 0f);
                                        Vector3 sc9;
                                        sc9 = new Vector3(75f * num14 * 0.01f, 8f, 1f);
                                        Vector3 posi13;
                                        posi13 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num + (65f + 75f * num14 * 0.01f), (vector3.y - (float)(Screen.height / 2)) / num + 64f, 0f);
                                        Vector3 sc10;
                                        sc10 = new Vector3(75f * (100f - num14) * 0.01f, 8f, 1f);
                                        Vector3 posi14;
                                        posi14 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num + 85f, (vector3.y - (float)(Screen.height / 2)) / num + 45f, 0f);
                                        Vector3 size7;
                                        size7 = new Vector3(1f, 1f, 1f);
                                        Vector3 posi15;
                                        posi15 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num + 65f, (vector3.y - (float)(Screen.height / 2)) / num + 38f, 0f);
                                        Vector3 sc11;
                                        sc11 = new Vector3(16f, 16f, 1f);
                                        Vector3 posi16;
                                        posi16 = new Vector3((vector3.x - (float)(Screen.width / 2)) / num + 65f, (vector3.y - (float)(Screen.height / 2)) / num + 18f, 0f);
                                        Vector3 sc12;
                                        sc12 = new Vector3(num12 * 100f, 4f, 1f);
                                        bool flag39 = mpteam == this.ObjectBehavior.Team;
                                        if (flag39)
                                        {
                                            switch (AdCustomModuleMod.mod2.BeaconContainer[i].Team)
                                            {
                                                case 0:
                                                    this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorGray, sc11, posi15);
                                                    this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorGray, sc12, posi16);
                                                    this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorGray, size7, sc8, posi14, 0);
                                                    break;
                                                case (MPTeam)1:
                                                    this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorRed, sc11, posi15);
                                                    this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorRed, sc12, posi16);
                                                    this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorRed, size7, sc8, posi14, 0);
                                                    break;
                                                case (MPTeam)2:
                                                    this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorGreen, sc11, posi15);
                                                    this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorGreen, sc12, posi16);
                                                    this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorGreen, size7, sc8, posi14, 0);
                                                    break;
                                                case (MPTeam)3:
                                                    this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorOrange, sc11, posi15);
                                                    this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorOrange, sc12, posi16);
                                                    this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorOrange, size7, sc8, posi14, 0);
                                                    break;
                                                case (MPTeam)4:
                                                    this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorBlue, sc11, posi15);
                                                    this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorBlue, sc12, posi16);
                                                    this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorBlue, size7, sc8, posi14, 0);
                                                    break;
                                            }
                                            this.MakerCreateHH(this.UIimageContainer[i], this.markerB_sp, hudcolorWhite, posi11);
                                            this.TextCreate(this.UItextContainer[i], string.Format("{0,4:F1}", this.LockonTergetDistance[i]) + "M", co, size6, sc8, posi10, 0);
                                        }
                                        else
                                        {
                                            bool flag40 = !this.TargetMarkerDisable && !this.LabelModeCheck;
                                            if (flag40)
                                            {
                                                switch (AdCustomModuleMod.mod2.BeaconContainer[i].Team)
                                                {
                                                    case (MPTeam)0:
                                                        this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorGray, sc11, posi15);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorGray, sc12, posi16);
                                                        this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorGray, size7, sc8, posi14, 0);
                                                        break;
                                                    case (MPTeam)1:
                                                        this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorRed, sc11, posi15);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorRed, sc12, posi16);
                                                        this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorRed, size7, sc8, posi14, 0);
                                                        break;
                                                    case (MPTeam)2:
                                                        this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorGreen, sc11, posi15);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorGreen, sc12, posi16);
                                                        this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorGreen, size7, sc8, posi14, 0);
                                                        break;
                                                    case (MPTeam)3:
                                                        this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorOrange, sc11, posi15);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorOrange, sc12, posi16);
                                                        this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorOrange, size7, sc8, posi14, 0);
                                                        break;
                                                    case (MPTeam)4:
                                                        this.MakerCreateLU(this.UIimageContainer[i + 100], this.team_icon_sp, this.TeamColorBlue, sc11, posi15);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 120], this.sq_white_sp, this.TeamColorBlue, sc12, posi16);
                                                        this.TextCreate(this.UItextContainer[i + 40], AdCustomModuleMod.mod2.BeaconContainer[i].Name, this.TeamColorBlue, size7, sc8, posi14, 0);
                                                        break;
                                                }
                                                this.TextCreate(this.UItextContainer[i], string.Format("{0,4:F1}", this.LockonTergetDistance[i]) + "M", co, size6, sc8, posi10, 0);
                                            }
                                            bool flag41 = flag23 && !this.TargetMarkerDisable && !this.LabelModeCheck;
                                            if (flag41)
                                            {
                                                bool flag42 = flag24;
                                                if (flag42)
                                                {
                                                    bool activeSelf3 = this.UIimageContainer[i + 60].activeSelf;
                                                    if (activeSelf3)
                                                    {
                                                        this.UIimageContainer[i + 60].SetActive(false);
                                                    }
                                                    bool flag43 = AdCustomModuleMod.mod2.MissileNum > 0;
                                                    if (flag43)
                                                    {
                                                        this.MakerCreateLU(this.UIimageContainer[i + 20], this.marker_progress_BG_sp, hudcolorWhite, sc10, posi13);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 60], this.marker_progress_R_sp, hudcolorWhite, sc9, posi12);
                                                    }
                                                    this.MakerCreateHH(this.UIimageContainer[i], this.markerR_info_sp, hudcolorWhite, posi11);
                                                }
                                                else
                                                {
                                                    bool activeSelf4 = this.UIimageContainer[i + 40].activeSelf;
                                                    if (activeSelf4)
                                                    {
                                                        this.UIimageContainer[i + 40].SetActive(false);
                                                    }
                                                    bool flag44 = AdCustomModuleMod.mod2.MissileNum > 0;
                                                    if (flag44)
                                                    {
                                                        this.MakerCreateLU(this.UIimageContainer[i + 20], this.marker_progress_BG_sp, hudcolorWhite, sc10, posi13);
                                                        this.MakerCreateLU(this.UIimageContainer[i + 60], this.marker_progress_G_sp, hudcolorWhite, sc9, posi12);
                                                    }
                                                    this.MakerCreateHH(this.UIimageContainer[i], this.markerG_info_sp, hudcolorWhite, posi11);
                                                }
                                            }
                                            else
                                            {
                                                bool activeSelf5 = this.UIimageContainer[i + 20].activeSelf;
                                                if (activeSelf5)
                                                {
                                                    this.UIimageContainer[i + 20].SetActive(false);
                                                }
                                                bool activeSelf6 = this.UIimageContainer[i + 40].activeSelf;
                                                if (activeSelf6)
                                                {
                                                    this.UIimageContainer[i + 40].SetActive(false);
                                                }
                                                bool activeSelf7 = this.UIimageContainer[i + 60].activeSelf;
                                                if (activeSelf7)
                                                {
                                                    this.UIimageContainer[i + 60].SetActive(false);
                                                }
                                                bool flag45 = !this.TargetMarkerDisable && !this.LabelModeCheck;
                                                if (flag45)
                                                {
                                                    this.MakerCreateHH(this.UIimageContainer[i], this.markerG_sp, hudcolorWhite, posi11);
                                                }
                                                else
                                                {
                                                    bool activeSelf8 = this.UIimageContainer[i].activeSelf;
                                                    if (activeSelf8)
                                                    {
                                                        this.UIimageContainer[i].SetActive(false);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        bool activeSelf9 = this.UIimageContainer[i].activeSelf;
                                        if (activeSelf9)
                                        {
                                            this.UIimageContainer[i].SetActive(false);
                                        }
                                        bool activeSelf10 = this.UIimageContainer[i + 20].activeSelf;
                                        if (activeSelf10)
                                        {
                                            this.UIimageContainer[i + 20].SetActive(false);
                                        }
                                        bool activeSelf11 = this.UIimageContainer[i + 40].activeSelf;
                                        if (activeSelf11)
                                        {
                                            this.UIimageContainer[i + 40].SetActive(false);
                                        }
                                        bool activeSelf12 = this.UIimageContainer[i + 60].activeSelf;
                                        if (activeSelf12)
                                        {
                                            this.UIimageContainer[i + 60].SetActive(false);
                                        }
                                        bool activeSelf13 = this.UIimageContainer[i + 100].activeSelf;
                                        if (activeSelf13)
                                        {
                                            this.UIimageContainer[i + 100].SetActive(false);
                                        }
                                        bool activeSelf14 = this.UIimageContainer[i + 120].activeSelf;
                                        if (activeSelf14)
                                        {
                                            this.UIimageContainer[i + 120].SetActive(false);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    bool flag46 = flag18;
                    if (flag46)
                    {
                        this.SoundController.LoopPlay(this.seVolume, 0, false);
                    }
                    else
                    {
                        this.SoundController.LoopStop(0);
                    }
                    bool flag47 = AdCustomModuleMod.mod2.MissileToTargetListContainer[this.OwnerID].Count > 0;
                    if (flag47)
                    {
                        this.Intercept = true;
                    }
                    else
                    {
                        this.Intercept = false;
                    }
                    this.InterceptDistance = 1000000f;
                    foreach (int key2 in AdCustomModuleMod.mod2.MissileToTargetListContainer[this.OwnerID])
                    {
                        Transform transform = AdCustomModuleMod.mod2.MissilePositionContainer[key2];
                        Vector3 vector5 = Camera.main.WorldToScreenPoint(transform.position);
                        Rect rect3;
                        rect3 = new Rect(vector5.x - (float)(Screen.height / 2), (float)(Screen.height / 2) - vector5.y, (float)Screen.height, (float)Screen.height);
                        Rect rect4;
                        rect4 = new Rect((float)(Screen.width / 2 - Screen.height / 2), 0f, (float)Screen.height, (float)Screen.height);
                        Vector3 vector6;
                        vector6 = new Vector3(vector5.x - (float)(Screen.width / 2), vector5.y - (float)(Screen.height / 2), 0f);
                        float num15 = Vector3.Angle(vector6, new Vector3(1f, 0f, 0f));
                        bool flag48 = false;
                        bool flag49 = (double)vector6.magnitude < (double)Screen.height * 0.33 && vector5.z > 0f;
                        if (flag49)
                        {
                            flag48 = true;
                        }
                        bool flag50 = vector5.z > 0f;
                        if (flag50)
                        {
                            bool flag51 = vector5.y > (float)(Screen.height / 2);
                            if (flag51)
                            {
                                num15 = -num15;
                            }
                        }
                        else
                        {
                            num15 = 180f - num15;
                            bool flag52 = vector5.y < (float)(Screen.height / 2);
                            if (flag52)
                            {
                                num15 = -num15;
                            }
                        }
                        GUIUtility.RotateAroundPivot(num15, rect4.center);
                        bool flag53 = flag48;
                        if (flag53)
                        {
                            GUI.DrawTexture(rect4, this.hud_red_in);
                        }
                        else
                        {
                            GUI.DrawTexture(rect4, this.hud_red_out);
                        }
                        GUI.matrix = Matrix4x4.identity;
                        bool flag54 = vector5.z > 0f;
                        if (flag54)
                        {
                            GUI.DrawTexture(rect3, this.diamarkerG);
                        }
                        float sqrMagnitude = (this.BeaconPosi - transform.position).sqrMagnitude;
                        bool flag55 = this.InterceptDistance > sqrMagnitude;
                        if (flag55)
                        {
                            this.InterceptDistance = sqrMagnitude;
                        }
                    }
                    bool intercept2 = this.Intercept;
                    if (intercept2)
                    {
                        bool flag56 = this.InterceptDistance < 40000f;
                        if (flag56)
                        {
                            this.WarningInterval = 0.2f;
                        }
                        else
                        {
                            bool flag57 = this.InterceptDistance < 250000f;
                            if (flag57)
                            {
                                this.WarningInterval = 0.4f;
                            }
                            else
                            {
                                this.WarningInterval = 1.2f;
                            }
                        }
                    }
                    GUI.color = new Color(1f, 1f, 1f, 1f);
                }
            }
            else
            {
                bool hudactive = this.HUDactive;
                if (hudactive)
                {
                    this.UIinit();
                    this.HUDactive = false;
                }
                bool hudinit = this.HUDInit;
                if (hudinit)
                {
                    bool isMP4 = StatMaster.isMP;
                    if (isMP4)
                    {
                        bool flag58 = !this.Labeltransform.activeSelf;
                        if (flag58)
                        {
                            this.Labeltransform.SetActive(true);
                        }
                    }
                    this.HUDInit = false;
                }
            }
        }

        // Token: 0x060001BA RID: 442 RVA: 0x0001E950 File Offset: 0x0001CB50
        public void UIinit()
        {
            bool flag = this.SoundController != null;
            if (flag)
            {
                this.SoundController.LoopStop(0);
                this.WarningTimer = 0f;
                this.Intercept = false;
            }
            for (int i = 0; i < 20; i++)
            {
                bool activeSelf = this.UItextContainer[i].activeSelf;
                if (activeSelf)
                {
                    this.UItextContainer[i].SetActive(false);
                }
                bool activeSelf2 = this.UItextContainer[i + 20].activeSelf;
                if (activeSelf2)
                {
                    this.UItextContainer[i + 20].SetActive(false);
                }
                bool activeSelf3 = this.UItextContainer[i + 40].activeSelf;
                if (activeSelf3)
                {
                    this.UItextContainer[i + 40].SetActive(false);
                }
                bool activeSelf4 = this.UIimageContainer[i].activeSelf;
                if (activeSelf4)
                {
                    this.UIimageContainer[i].SetActive(false);
                }
                bool activeSelf5 = this.UIimageContainer[i + 20].activeSelf;
                if (activeSelf5)
                {
                    this.UIimageContainer[i + 20].SetActive(false);
                }
                bool activeSelf6 = this.UIimageContainer[i + 40].activeSelf;
                if (activeSelf6)
                {
                    this.UIimageContainer[i + 40].SetActive(false);
                }
                bool activeSelf7 = this.UIimageContainer[i + 60].activeSelf;
                if (activeSelf7)
                {
                    this.UIimageContainer[i + 60].SetActive(false);
                }
                bool activeSelf8 = this.UIimageContainer[i + 100].activeSelf;
                if (activeSelf8)
                {
                    this.UIimageContainer[i + 100].SetActive(false);
                }
                bool activeSelf9 = this.UIimageContainer[i + 120].activeSelf;
                if (activeSelf9)
                {
                    this.UIimageContainer[i + 120].SetActive(false);
                }
                this.LockonTimer[i] = 0f;
                bool flag2 = this.TargetId.Contains(i);
                if (flag2)
                {
                    this.TargetId.Remove(i);
                    this.TargetChangeFlag = true;
                }
            }
            for (int j = 0; j < 6; j++)
            {
                bool activeSelf10 = this.UIimageContainer[80 + j].activeSelf;
                if (activeSelf10)
                {
                    this.UIimageContainer[80 + j].SetActive(false);
                }
            }
            for (int k = 0; k < this.EntityCount + 1; k++)
            {
                bool activeSelf11 = AdCustomModuleMod.mod2.UIEntitytextContainer[k].activeSelf;
                if (activeSelf11)
                {
                    AdCustomModuleMod.mod2.UIEntitytextContainer[k].SetActive(false);
                }
                bool activeSelf12 = AdCustomModuleMod.mod2.UIEntityimageContainer[k].activeSelf;
                if (activeSelf12)
                {
                    AdCustomModuleMod.mod2.UIEntityimageContainer[k].SetActive(false);
                }
                bool activeSelf13 = AdCustomModuleMod.mod2.UIEntityDistanceContainer[k].activeSelf;
                if (activeSelf13)
                {
                    AdCustomModuleMod.mod2.UIEntityDistanceContainer[k].SetActive(false);
                }
            }
        }

        // Token: 0x060001BB RID: 443 RVA: 0x0001ECAC File Offset: 0x0001CEAC
        public void MakerCreateHH(GameObject go, Sprite sp, Color co, Vector3 posi)
        {
            bool flag = !go.activeSelf;
            if (flag)
            {
                go.SetActive(true);
            }
            Image component = go.GetComponent<Image>();
            component.sprite = sp;
            component.color = co;
            RectTransform component2 = go.GetComponent<RectTransform>();
            component2.localScale = new Vector3(1f, 1f, 1f);
            component2.sizeDelta = new Vector3(1080f, 1080f, 1f);
            component2.localPosition = posi;
        }

        // Token: 0x060001BC RID: 444 RVA: 0x0001ED34 File Offset: 0x0001CF34
        public void MakerCreateLU(GameObject go, Sprite sp, Color co, Vector3 sc, Vector3 posi)
        {
            bool flag = !go.activeSelf;
            if (flag)
            {
                go.SetActive(true);
            }
            Image component = go.GetComponent<Image>();
            component.sprite = sp;
            component.color = co;
            RectTransform component2 = go.GetComponent<RectTransform>();
            component2.pivot = new Vector2(0f, 1f);
            component2.sizeDelta = sc;
            component2.localPosition = posi;
        }

        // Token: 0x060001BD RID: 445 RVA: 0x0001EDA4 File Offset: 0x0001CFA4
        public void TextCreate(GameObject go, string st, Color co, Vector3 size, Vector3 sc, Vector3 posi, TextAnchor anchor = 0)
        {
            bool flag = !go.activeSelf;
            if (flag)
            {
                go.SetActive(true);
            }
            Text component = go.GetComponent<Text>();
            component.color = co;
            component.alignment = anchor;
            RectTransform component2 = go.GetComponent<RectTransform>();
            component2.localScale = size;
            component2.localPosition = posi;
            component2.sizeDelta = sc;
            component.text = st;
        }

        // Token: 0x0400037E RID: 894
        private Player PlayerData;

        // Token: 0x0400037F RID: 895
        private ServerMachine ServerMachineData;

        // Token: 0x04000380 RID: 896
        private GameObject Labeltransform;

        // Token: 0x04000381 RID: 897
        private Vector3 BeaconPosi;

        // Token: 0x04000382 RID: 898
        private Quaternion BeaconRot;

        // Token: 0x04000383 RID: 899
        private MToggle FunctionToggle;

        // Token: 0x04000384 RID: 900
        private MKey FunctionToggleKey;

        // Token: 0x04000385 RID: 901
        private MSlider FunctionSlider;

        // Token: 0x04000386 RID: 902
        private MSlider FunctionSlider2;

        // Token: 0x04000387 RID: 903
        private MSlider SoundVolumeSlider;

        // Token: 0x04000388 RID: 904
        private MColourSlider ColorSlider_normal;

        // Token: 0x04000389 RID: 905
        private MColourSlider ColorSlider_alert;

        // Token: 0x0400038A RID: 906
        private Color HUDColorNormal;

        // Token: 0x0400038B RID: 907
        private Color HUDColorAlert;

        // Token: 0x0400038C RID: 908
        private Color HUDColorNormaltext;

        // Token: 0x0400038D RID: 909
        private Color HUDColorAlerttext;

        // Token: 0x0400038E RID: 910
        private Color HUDColorGrayText;

        // Token: 0x0400038F RID: 911
        private Color HUDColorGray;

        // Token: 0x04000390 RID: 912
        private Color HUDColorWhite;

        // Token: 0x04000391 RID: 913
        private Color TeamColorGray;

        // Token: 0x04000392 RID: 914
        private Color TeamColorRed;

        // Token: 0x04000393 RID: 915
        private Color TeamColorGreen;

        // Token: 0x04000394 RID: 916
        private Color TeamColorBlue;

        // Token: 0x04000395 RID: 917
        private Color TeamColorOrange;

        // Token: 0x04000396 RID: 918
        private bool useHUD = false;

        // Token: 0x04000397 RID: 919
        private bool modeflag = false;

        // Token: 0x04000398 RID: 920
        private MPTeam myTeam;

        // Token: 0x04000399 RID: 921
        private GameObject AudioManager;

        // Token: 0x0400039A RID: 922
        private AdSoundController SoundController;

        // Token: 0x0400039B RID: 923
        private float seVolume;

        // Token: 0x0400039C RID: 924
        private float InterceptDistance;

        // Token: 0x0400039D RID: 925
        private float WarningInterval;

        // Token: 0x0400039E RID: 926
        private float WarningTimer = 0f;

        // Token: 0x0400039F RID: 927
        private Vector3[] coreSpeedArray = new Vector3[10];

        // Token: 0x040003A0 RID: 928
        private float[] IntegrationTimeArray = new float[10];

        // Token: 0x040003A1 RID: 929
        private float SpeedIntegrationTime = 0f;

        // Token: 0x040003A2 RID: 930
        private float speedCounter;

        // Token: 0x040003A3 RID: 931
        private float CoreSpeed = 0f;

        // Token: 0x040003A4 RID: 932
        private float CoreAccelertion = 0f;

        // Token: 0x040003A5 RID: 933
        private GUIStyle UIStyle = new GUIStyle();

        // Token: 0x040003A6 RID: 934
        private Font UIfont;

        // Token: 0x040003A7 RID: 935
        private Canvas UIcanvas;

        // Token: 0x040003A8 RID: 936
        private GameObject BeaconUI;

        // Token: 0x040003A9 RID: 937
        private Camera CameraMain;

        // Token: 0x040003AA RID: 938
        private bool LabelModeCheck;

        // Token: 0x040003AB RID: 939
        private bool Init = false;

        // Token: 0x040003AC RID: 940
        private bool HUDInit = false;

        // Token: 0x040003AD RID: 941
        private int EntityCount = 0;

        // Token: 0x040003AE RID: 942
        public Dictionary<int, GameObject> UItextContainer;

        // Token: 0x040003AF RID: 943
        public Dictionary<int, GameObject> UIimageContainer;

        // Token: 0x040003B0 RID: 944
        public GameObject DirectionimageObject;

        // Token: 0x040003B1 RID: 945
        public Dictionary<int, GameObject> UImaskedimageContainer;

        // Token: 0x040003B2 RID: 946
        private GameObject ACMUIcanvas;

        // Token: 0x040003B3 RID: 947
        public Texture2D markerG;

        // Token: 0x040003B4 RID: 948
        public Texture2D markerB;

        // Token: 0x040003B5 RID: 949
        public Texture2D markerR;

        // Token: 0x040003B6 RID: 950
        public Texture2D markerG_info;

        // Token: 0x040003B7 RID: 951
        public Texture2D markerB_info;

        // Token: 0x040003B8 RID: 952
        public Texture2D markerR_info;

        // Token: 0x040003B9 RID: 953
        public Texture2D marker_progress_BG;

        // Token: 0x040003BA RID: 954
        public Texture2D marker_progress_G;

        // Token: 0x040003BB RID: 955
        public Texture2D marker_progress_R;

        // Token: 0x040003BC RID: 956
        public Texture2D marker_entity;

        // Token: 0x040003BD RID: 957
        public Texture2D diamarkerG;

        // Token: 0x040003BE RID: 958
        public Texture2D hud_left;

        // Token: 0x040003BF RID: 959
        public Texture2D hud_right;

        // Token: 0x040003C0 RID: 960
        public Texture2D hud_center;

        // Token: 0x040003C1 RID: 961
        public Texture2D hud_center_in;

        // Token: 0x040003C2 RID: 962
        public Texture2D hud_center_out;

        // Token: 0x040003C3 RID: 963
        public Texture2D hud_red_left;

        // Token: 0x040003C4 RID: 964
        public Texture2D hud_red_right;

        // Token: 0x040003C5 RID: 965
        public Texture2D hud_red_center;

        // Token: 0x040003C6 RID: 966
        public Texture2D hud_green_out;

        // Token: 0x040003C7 RID: 967
        public Texture2D hud_green_in;

        // Token: 0x040003C8 RID: 968
        public Texture2D hud_blue_out;

        // Token: 0x040003C9 RID: 969
        public Texture2D hud_blue_in;

        // Token: 0x040003CA RID: 970
        public Texture2D hud_red_out;

        // Token: 0x040003CB RID: 971
        public Texture2D hud_red_in;

        // Token: 0x040003CC RID: 972
        public Texture2D hud_direction_mask;

        // Token: 0x040003CD RID: 973
        public Texture2D hud_direction;

        // Token: 0x040003CE RID: 974
        public Texture2D hud_horizon;

        // Token: 0x040003CF RID: 975
        public Texture2D hud_altitude;

        // Token: 0x040003D0 RID: 976
        public Texture2D hud_altitude_mask;

        // Token: 0x040003D1 RID: 977
        public Texture2D hud_speed;

        // Token: 0x040003D2 RID: 978
        public Texture2D hud_speed_mask;

        // Token: 0x040003D3 RID: 979
        public Texture2D team_icon;

        // Token: 0x040003D4 RID: 980
        public Texture2D sq_white;

        // Token: 0x040003D5 RID: 981
        public Sprite markerG_sp;

        // Token: 0x040003D6 RID: 982
        public Sprite markerB_sp;

        // Token: 0x040003D7 RID: 983
        public Sprite markerR_sp;

        // Token: 0x040003D8 RID: 984
        public Sprite markerG_info_sp;

        // Token: 0x040003D9 RID: 985
        public Sprite markerB_info_sp;

        // Token: 0x040003DA RID: 986
        public Sprite markerR_info_sp;

        // Token: 0x040003DB RID: 987
        public Sprite marker_progress_BG_sp;

        // Token: 0x040003DC RID: 988
        public Sprite marker_progress_G_sp;

        // Token: 0x040003DD RID: 989
        public Sprite marker_progress_R_sp;

        // Token: 0x040003DE RID: 990
        public Sprite marker_entity_sp;

        // Token: 0x040003DF RID: 991
        public Sprite diamarkerG_sp;

        // Token: 0x040003E0 RID: 992
        public Sprite hud_direction_mask_sp;

        // Token: 0x040003E1 RID: 993
        public Sprite hud_direction_sp;

        // Token: 0x040003E2 RID: 994
        public Sprite hud_horizon_sp;

        // Token: 0x040003E3 RID: 995
        public Sprite hud_altitude_sp;

        // Token: 0x040003E4 RID: 996
        public Sprite hud_altitude_mask_sp;

        // Token: 0x040003E5 RID: 997
        public Sprite hud_speed_sp;

        // Token: 0x040003E6 RID: 998
        public Sprite hud_speed_mask_sp;

        // Token: 0x040003E7 RID: 999
        public Sprite team_icon_sp;

        // Token: 0x040003E8 RID: 1000
        public Sprite sq_white_sp;

        // Token: 0x040003E9 RID: 1001
        public List<int> TargetId = new List<int>();

        // Token: 0x040003EA RID: 1002
        public Dictionary<int, float> LockonTimer = new Dictionary<int, float>();

        // Token: 0x040003EB RID: 1003
        public Dictionary<int, float> LockonTergetDistance = new Dictionary<int, float>();

        // Token: 0x040003EC RID: 1004
        public float LockonTime = 2f;

        // Token: 0x040003ED RID: 1005
        public float LockonTimeRatio = 1f;

        // Token: 0x040003EE RID: 1006
        public float LockonNormalDistance = 2500f;

        // Token: 0x040003EF RID: 1007
        public bool TargetChangeFlag = false;

        // Token: 0x040003F0 RID: 1008
        public bool Intercept = false;

        // Token: 0x040003F1 RID: 1009
        public int NetworkID = 0;

        // Token: 0x040003F2 RID: 1010
        public float RespawnResetTime = 0f;

        // Token: 0x040003F3 RID: 1011
        private bool HUDactive = false;
    }
}