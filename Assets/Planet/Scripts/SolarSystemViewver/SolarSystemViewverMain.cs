﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace LemonSpawn {

	public class SSVSettings {
		public static float SolarSystemScale = 500.0f;
		public static float PlanetSizeScale = 1.0f / 200.0f;
		public static int OrbitalLineSegments = 100;
		public static Vector2 OrbitalLineWidth = new Vector2 (3.03f, 3.03f);
        public static float currentFrame = 0;
	}

	public class DisplayPlanet {
		public Planet planet;
        public SerializedPlanet serializedPlanet;
		public GameObject go;
		public List<GameObject> orbitLines = new List<GameObject>();

/*		private void CreateOrbitCircles() {
			float radius = (float)planet.pSettings.properties.pos.Length () * SSVSettings.SolarSystemScale;
			for (int i = 0; i < SSVSettings.OrbitalLineSegments; i++) {
				float t0 = 2 * Mathf.PI / (float)SSVSettings.OrbitalLineSegments * (float)i;
				float t1 = 2 * Mathf.PI / (float)SSVSettings.OrbitalLineSegments * (float)(i+1);
				Vector3 from = new Vector3 (Mathf.Cos (t0), 0, Mathf.Sin (t0)) * radius;
				Vector3 to = new Vector3 (Mathf.Cos (t1), 0, Mathf.Sin (t1)) * radius;
			
				GameObject g = new GameObject ();
				g.transform.parent = SolarSystemViewverMain.linesObject.transform;
				LineRenderer lr = g.AddComponent<LineRenderer> ();
				lr.material = (Material)Resources.Load ("LineMaterial");
				lr.SetWidth (SSVSettings.OrbitalLineWidth.x, SSVSettings.OrbitalLineWidth.y);
				lr.SetPosition (0, from);
				lr.SetPosition (1, to);
				orbitLines.Add (g);
			}
		}
*/

        public void MaintainOrbits() {
            int maxFrames = serializedPlanet.Frames.Count;
            int currentFrame = (int)(SSVSettings.currentFrame*maxFrames);
            Color c = new Color(0.3f, 0.7f, 1.0f,1.0f);
            for (int i=0;i<orbitLines.Count;i++) {
                int f = Mathf.Clamp(i - orbitLines.Count/2 + currentFrame,0,maxFrames);
                if (f+1 >= serializedPlanet.Frames.Count)
                    break;
                LineRenderer lr = orbitLines[i].GetComponent<LineRenderer>();
                Frame sp = serializedPlanet.Frames[f];
                Frame sp2 = serializedPlanet.Frames[f+1];
                DVector from = new DVector (sp.pos_x, sp.pos_y,sp.pos_z) * SSVSettings.SolarSystemScale;
                DVector to = new DVector (sp2.pos_x, sp2.pos_y,sp2.pos_z) * SSVSettings.SolarSystemScale;


                lr.SetPosition (0, from.toVectorf());
                lr.SetPosition (1, to.toVectorf());

                float colorScale = Mathf.Abs(i-orbitLines.Count/2)/(float)orbitLines.Count*2;
                Color col = c*(1-colorScale);
                col.a = 1;
                lr.SetColors(col,col);
            }
        }

        public void CreateOrbitFromFrames(int maxLines) {
            foreach (GameObject go in orbitLines)
                GameObject.Destroy(go);

            orbitLines.Clear();
            if (serializedPlanet.Frames.Count<=2)
                return;             
            for (int i = 0; i < maxLines; i++) {
                Frame sp = serializedPlanet.Frames[i];
                Frame sp2 = serializedPlanet.Frames[i+1];
                DVector from = new DVector (sp.pos_x, sp.pos_y,sp.pos_z) * SSVSettings.SolarSystemScale;
                DVector to = new DVector (sp2.pos_x, sp2.pos_y,sp2.pos_z) * SSVSettings.SolarSystemScale;
            
                GameObject g = new GameObject ();
                g.transform.parent = SolarSystemViewverMain.linesObject.transform;
                LineRenderer lr = g.AddComponent<LineRenderer> ();
                lr.material = new Material(Shader.Find("Particles/Additive"));//(Material)Resources.Load ("LineMaterial");
                lr.SetWidth (SSVSettings.OrbitalLineWidth.x, SSVSettings.OrbitalLineWidth.y);
                lr.SetPosition (0, from.toVectorf());
                lr.SetPosition (1, to.toVectorf());
                orbitLines.Add (g);
            }
        }

		public DisplayPlanet(GameObject g, Planet p, SerializedPlanet sp) {
			go = g;
			planet = p;
            serializedPlanet = sp;

			//CreateOrbitFromFrames ();
		}

        public void UpdatePosition() {
            planet.pSettings.properties.pos*=SSVSettings.SolarSystemScale;
            planet.pSettings.transform.position = planet.pSettings.properties.pos.toVectorf();
            go.transform.position = planet.pSettings.properties.pos.toVectorf();
            MaintainOrbits();
        }

	}

	public class SolarSystemViewverMain : WorldMC {
		private List<DisplayPlanet> dPlanets = new List<DisplayPlanet>();
		private Vector3 mouseAccel = new Vector3();
		private Vector3 focusPoint = Vector3.zero;
		private Vector3 focusPointCur = Vector3.zero;
        private float scrollWheel, scrollWheelAccel;
		private DisplayPlanet selected = null;
        public static GameObject linesObject = null;
        private GameObject pnlInfo = null;
		private void SelectPlanet(DisplayPlanet dp) {
			selected = dp;
			focusPoint = dp.go.transform.position;
            pnlInfo.SetActive(true);
            setText("txtPlanetType",dp.planet.pSettings.planetType.name);

            string infoText = "";
            int radius = (int)(dp.planet.pSettings.radius/(float)SSVSettings.PlanetSizeScale);
            float orbit = (dp.planet.pSettings.properties.pos.toVectorf().magnitude/(float)SSVSettings.SolarSystemScale);
            infoText += "Radius           : " +radius+ "km\n";
            infoText += "Temperature      : " +(int)dp.planet.pSettings.temperature+ "K\n";
            infoText += "Orbital distance : " +orbit+ "Au\n\n";
            infoText += dp.planet.pSettings.planetType.PlanetInfo;
            setText("txtPlanetInfo", infoText);


		}


        public void FocusOnPlanetClick() {
            int idx = GameObject.Find("Overview").GetComponent<UnityEngine.UI.Dropdown>().value;
            string name = GameObject.Find("Overview").GetComponent<UnityEngine.UI.Dropdown>().options[idx].text;
            foreach (DisplayPlanet dp in dPlanets)
                if (dp.planet.pSettings.name == name)
                    SelectPlanet(dp);
   
        }

        private void DeFocus() {
            selected = null;
            focusPoint = Vector3.zero;
            pnlInfo.SetActive(false);
         
        }


		private void UpdateFocus() {
			if (Input.GetMouseButtonDown (0)) {
				RaycastHit hit;
				Ray ray = MainCamera.ScreenPointToRay (Input.mousePosition);
				if (Physics.Raycast (ray, out hit)) {
					foreach (DisplayPlanet dp in dPlanets) {
						if (dp.go == hit.transform.gameObject)
							SelectPlanet(dp);
					}
				}
                else {
                    if (!EventSystem.current.IsPointerOverGameObject() )
                        DeFocus();
                }
			}
		}

		private void UpdateCamera () {
			float s = 1.0f;
			float theta = 0.0f;
			float phi = 0.0f;

			if (Input.GetMouseButton (1)) {
				theta = s * Input.GetAxis ("Mouse X");
				phi = s * Input.GetAxis ("Mouse Y") * -1.0f;
			}
			mouseAccel += new Vector3 (theta, phi, 0);
			focusPointCur += (focusPoint - focusPointCur) * 0.1f;
			mainCamera.transform.RotateAround (focusPointCur, Vector3.up, mouseAccel.x);
			mainCamera.transform.RotateAround (focusPointCur, mainCamera.transform.right, mouseAccel.y);
			mainCamera.transform.LookAt (focusPointCur);
			mouseAccel *= 0.9f;
		}


		private void PopulateWorld() {
			DestroyAllGameObjects();
			dPlanets.Clear ();

            //solarSystem.InitializeFromScene();


            int i=0;
			foreach (Planet p in solarSystem.planets) {

                GameObject go = p.pSettings.gameObject;

				Vector3 coolpos = new Vector3 ((float)p.pSettings.properties.pos.x, (float)p.pSettings.properties.pos.y, (float)p.pSettings.properties.pos.z);
				go.transform.position = coolpos * SSVSettings.SolarSystemScale;
                p.pSettings.properties.pos = new DVector(coolpos);
				//go.transform.localScale = Vector3.one * SSVSettings.PlanetSizeScale * p.pSettings.radius;
               //p.pSettings.atmoDensity = 0;

                GameObject hidden = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                hidden.transform.position = coolpos * SSVSettings.SolarSystemScale;
                hidden.transform.localScale = Vector3.one * p.pSettings.radius*2f;

                if (p.pSettings.planetTypeName=="star")
                    hidden.SetActive(false);

                hidden.GetComponent<MeshRenderer>().material = (Material)Resources.Load("HiddenMaterial");

				dPlanets.Add (new DisplayPlanet (hidden, p,szWorld.Planets[i++]));
			}
		}


        public void CreateFakeOrbitsFromMenu() {
            CreateFakeOrbits(2000, 0.05f);

            foreach (DisplayPlanet dp in dPlanets)
                dp.CreateOrbitFromFrames(100);

            Slide();
        }

        public void CreateFakeOrbits(int steps, float stepLength) {
            foreach (SerializedPlanet sp in szWorld.Planets) {
                int frame = 0;

                float t0 = Random.value*2*Mathf.PI;
                float radius = new Vector3((float)sp.pos_x,(float)sp.pos_y, (float)sp.pos_z).magnitude;
                float modifiedStepLength = stepLength / Mathf.Sqrt(radius);
                float rot = Random.value*30f + 10f;
                sp.Frames.Clear();
                    for (int i = 0;i<steps;i++) {
                        float perturb = Mathf.Cos(i/(float)steps*30.234f);
                        float rad = radius*(0.2f*perturb +1);
                        Vector3 pos = new Vector3 (Mathf.Cos (t0), 0, Mathf.Sin (t0)) * rad;
                        Frame f = new Frame();
                        f.pos_x = pos.x;
                        f.pos_y = pos.y;
                        f.pos_z = pos.z;
                        f.rotation = frame/rot;
                        f.id = frame;
                        sp.Frames.Add(f);
                        frame++;
                        t0+=modifiedStepLength;
                   }
            }
        }


        private void CreateLine(Vector3 f, Vector3 t, float c1, float c2, float w) {

            Color c = new Color(0.3f, 0.4f, 1.0f,1.0f);
            GameObject g = new GameObject ();
            LineRenderer lr = g.AddComponent<LineRenderer> ();
            lr.material = new Material(Shader.Find("Particles/Additive"));//(Material)Resources.Load ("LineMaterial");
            lr.SetWidth (w, w);
            lr.SetPosition (0, f);
            lr.SetPosition (1, t);
            Color cc1 = c*c1;
            Color cc2 = c*c2;
            cc1.a = 0.4f;
            cc2.a = 0.4f;
            lr.SetColors(cc1,cc2);
        }

        private void CreateAxis() {
            float w = 10000;

            CreateLine(Vector3.zero, Vector3.up*w, 1, 0.2f, 5);
            CreateLine(Vector3.zero, Vector3.up*w*-1, 1, 0.2f, 5);
            CreateLine(Vector3.zero, Vector3.right*w, 1, 0.2f, 5);
            CreateLine(Vector3.zero, Vector3.right*w*-1, 1, 0.2f, 5);
            CreateLine(Vector3.zero, Vector3.forward*w, 1, 0.2f, 5);
            CreateLine(Vector3.zero, Vector3.forward*w*-1, 1, 0.2f, 5);

        }

        public static GameObject satellite = null;

		public override void Start () { 
			CurrentApp = Verification.SolarSystemViewerName;
            RenderSettings.path = Application.dataPath + "/../";

            RenderSettings.UseThreading = true;
            RenderSettings.reCalculateQuads = false;
            RenderSettings.GlobalRadiusScale = SSVSettings.PlanetSizeScale;
            RenderSettings.maxQuadNodeLevel = m_maxQuadNodeLevel;
            RenderSettings.sizeVBO = szWorld.resolution;
            RenderSettings.minQuadNodeLevel = m_minQuadNodeLevel;
            RenderSettings.MoveCam = false;
            RenderSettings.ResolutionScale = szWorld.resolutionScale;
            RenderSettings.usePointLightSource = true;



            satellite = GameObject.Find("Satellite");
            if (satellite!=null)
                satellite.SetActive(false);

            pnlInfo = GameObject.Find("pnlInfo");
            pnlInfo.SetActive(false);
            solarSystem = new SolarSystem(sun, sphere, transform, (int)szWorld.skybox);
			PlanetTypes.Initialize ();
            SetupCloseCamera();
			MainCamera = mainCamera.GetComponent<Camera> ();
			PopulateFileCombobox("ComboBoxLoadFile","xml");
			SzWorld = szWorld;
            slider = GameObject.Find ("Slider");

            setText("TextVersion", "Version: " + RenderSettings.version.ToString("0.00"));


            linesObject = new GameObject("Lines");
            CreateAxis();
//			LoadData ();
		}


        private void UpdateZoom() {
            scrollWheelAccel = Input.GetAxis("Mouse ScrollWheel")*0.5f;
            scrollWheel = scrollWheel * 0.9f + scrollWheelAccel*0.1f;
//            Debug.Log(ScrollWheel);

            Vector3 pos = MainCamera.transform.position;
            if (selected!=null) {
                pos-=selected.go.transform.position;
                MainCamera.transform.position = pos*(1+scrollWheel) + selected.go.transform.position;
            }
            else
                MainCamera.transform.position = pos*(1+scrollWheel);

        }


		public override void Update () {
			UpdateFocus ();
			UpdateCamera ();
            UpdateZoom();
            solarSystem.Update();
            if (RenderSettings.UseThreading) 
                ThreadQueue.MaintainThreadQueue();

            // Always point to selected planet
            if (selected!=null)
                SelectPlanet(selected);

            UpdatePlay();

            if (Input.GetKey(KeyCode.Escape))
            {
                Application.Quit();
            }


        }

        protected void OnGUI() {
		}

		public void LoadFileFromMenu()
        {
            DeFocus();

            int idx = GameObject.Find("ComboBoxLoadFile").GetComponent<UnityEngine.UI.Dropdown>().value;
            string name = GameObject.Find("ComboBoxLoadFile").GetComponent<Dropdown>().options[idx].text;
           	if (name=="-")
           		return;
			name = RenderSettings.dataDir + name + ".xml";

            LoadFromXMLFile(name);


            szWorld.useSpaceCamera = false;
	        PopulateOverviewList("Overview");
			PopulateWorld ();
            foreach (DisplayPlanet dp in dPlanets)
                dp.CreateOrbitFromFrames(100);

            Slide();
        }

        private void DestroyAllGameObjects() {
        	foreach (DisplayPlanet dp in dPlanets)
        		GameObject.Destroy(dp.go);
        }


        public void Slide()
        {
//            if (szWorld.getMaxFrames()<=2)
  //              return;
            float v = slider.GetComponent<Slider>().value;
            SSVSettings.currentFrame = v;
            szWorld.InterpolatePlanetFrames(v, solarSystem.planets);
            foreach (DisplayPlanet dp in dPlanets) {
                dp.UpdatePosition();
            }

        }


        private void setPlaySpeed(float v)
        {
            if (m_playSpeed == v)
            {
                m_playSpeed = 0;
            }
            else
            {
                m_playSpeed = v;
            }

        }

        public void playNormal()
        {
            setPlaySpeed(0.000025f);

        }

        public void playFast()
        {
            setPlaySpeed(0.0001f);
        }


        protected void UpdatePlay()
        {
  //          Debug.Log(Time.time + " " + m_playSpeed);
            if (m_playSpeed > 0 && solarSystem.planets.Count!=0)
            {
                float v = slider.GetComponent<Slider>().value;
                v += m_playSpeed;
                if (v >= 1)
                {
                    m_playSpeed = 0;
                    v = 0;
                }
//                Debug.Log("Playspeed after: " + m_playSpeed + " " + Time.time);
                slider.GetComponent<Slider>().value = v;

                Slide();
            }

        }
	}

}