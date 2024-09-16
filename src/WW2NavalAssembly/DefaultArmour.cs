using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Collections;

using Modding.Modules;
using Modding;
using Modding.Blocks;
using UnityEngine;
using UnityEngine.Networking;
using Modding.Blocks;

namespace WW2NavalAssembly
{
    class DefaultArmour : MonoBehaviour
    {
        public int myseed = 0;
        public int myGuid;

        public bool AsKeel = false;

        public void UpdateVis(bool show)
        {
            if (GetComponent<Horizon>().Show)
            {
                try
                {
                    if (show)
                    {
                        if (gameObject.name == "Brace")
                        {
                            transform.Find("A").gameObject.GetComponent<MeshRenderer>().enabled = false;
                            if (transform.Find("B"))
                            {
                                transform.Find("B").gameObject.GetComponent<MeshRenderer>().enabled = false;
                            }
                            if (transform.Find("Cylinder"))
                            {
                                transform.Find("Cylinder").gameObject.GetComponent<MeshRenderer>().enabled = false;
                            }
                        }
                        else if (gameObject.name == "RopeWinch")
                        {
                            return;
                        }
                        else if (gameObject.name == "Spring")
                        {
                            return;
                        }
                        else if (gameObject.name == "Balloon")
                        {
                            return;
                        }
                        else if (gameObject.name == "SqrBalloon")
                        {
                            return;
                        }
                        // mod-related
                        else if (gameObject.name == "Aircraft")
                        {
                            return;
                        }
                        else if (gameObject.name == "Gun")
                        {
                            return;
                        }
                        else if (gameObject.name == "Gunner")
                        {
                            return;
                        }
                        else if (gameObject.name == "Captain")
                        {
                            return;
                        }
                        else if (gameObject.name == "Aircraft Captain")
                        {
                            return;
                        }
                        else if (gameObject.name == "AA Captain")
                        {
                            return;
                        }
                        else if (gameObject.name == "AA Block")
                        {
                            return;
                        }
                        else if (gameObject.name == "Engine")
                        {
                            return;
                        }
                        else if (gameObject.name == "FlyingBlock")
                        {
                            transform.Find("Rot").Find("Vis").gameObject.SetActive(false);
                        }
                        else if (gameObject.name == "SmallWheel")
                        {
                            transform.Find("rot").Find("Vis").gameObject.SetActive(false);
                        }
                        else
                        {
                            transform.Find("Vis").gameObject.SetActive(false);
                        }

                    }
                    else
                    {
                        if (gameObject.name == "Brace")
                        {
                            transform.Find("A").gameObject.GetComponent<MeshRenderer>().enabled = true;
                            if (gameObject.GetComponent<BraceCode>().cylinder.localScale.y < 0.001f)
                            {

                            }
                            else
                            {
                                if (transform.Find("B"))
                                {
                                    transform.Find("B").gameObject.GetComponent<MeshRenderer>().enabled = true;
                                }
                                if (transform.Find("Cylinder"))
                                {
                                    transform.Find("Cylinder").gameObject.GetComponent<MeshRenderer>().enabled = true;
                                }
                            }

                        }
                        else if (gameObject.name == "RopeWinch" || gameObject.name == "Balloon" || gameObject.name == "Spring" || gameObject.name == "SqrBalloon")
                        {
                            return;
                        }
                        else if (gameObject.name == "FlyingBlock")
                        {
                            transform.Find("Rot").Find("Vis").gameObject.SetActive(true);
                        }
                        else if (gameObject.name == "SmallWheel")
                        {
                            transform.Find("rot").Find("Vis").gameObject.SetActive(true);
                        }
                        else
                        {
                            transform.Find("Vis").gameObject.SetActive(true);
                        }
                    }
                }
                catch
                {
                    Debug.Log("Error: " + gameObject.name);
                }
            }
            
        }
        IEnumerator ChangeVis()
        {
            yield return new WaitForFixedUpdate();
            ModController.Instance.ShowChanged = false;
            yield return new WaitForSeconds(0.01f * myseed);

            UpdateVis(ModController.Instance.ShowArmour || ModController.Instance.ShowCrew);

            yield break;
        }

        public void Awake()
        {
            myseed = (int)(UnityEngine.Random.value * 40f);
        }
        public void Start()
        {
        }
        public void FixedUpdate()
        {
            if (ModController.Instance.ShowChanged)
            {
                StartCoroutine(ChangeVis());
            }

        }
        public void OnEnable()
        {
            StartCoroutine(ChangeVis());
        }
    }
}