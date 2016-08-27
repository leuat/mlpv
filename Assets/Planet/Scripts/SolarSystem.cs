﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace LemonSpawn
{


    public class SpaceAtmosphere
    {
        Material mat;
        GameObject sun;
        public Color color;
        public float m_g = -0.990f;             // The Mie phase asymmetry factor, must be between 0.999 to -0.999
        public float hdr = 0.1f;
        public SpaceAtmosphere(Material m, GameObject s, Color col, float h)
        {
            mat = m;
            sun = s;
            color = col;
            hdr = h;
        }


        public void Update()
        {
            mat.SetVector("v3LightPos", sun.transform.forward * -1.0f);
            mat.SetColor("sunColor", color);
            mat.SetFloat("fHdrExposure", hdr * Atmosphere.sunScale);
            mat.SetFloat("g", m_g);
            mat.SetFloat("g2", m_g * m_g);

        }


    }


    public class SolarSystem
    {

        static public Material spaceMaterial;
        static public Material groundMaterial;
        public Transform transform;
        private GameObject sun;
        private Mesh sphere;
        public SpaceAtmosphere space;
        public List<Planet> planets = new List<Planet>();
        public List<GameObject> cameraObjects = new List<GameObject>();

        // Closest active planet
        public static Planet planet = null;

        // Use this for initialization

        private void InitializeSurfaces()
        {
            if (!RenderSettings.GPUSurface)
                groundMaterial = (Material)Resources.Load("GroundMaterial");
            else
                groundMaterial = (Material)Resources.Load("GroundMaterialGPU");

        }

        public SolarSystem(GameObject pSun, Mesh s, Transform t, int skybox)
        {
            sun = pSun;
            sphere = s;
            transform = t;
            spaceMaterial = (Material)Resources.Load("SpaceMaterial");
            InitializeSurfaces();

            space = new SpaceAtmosphere(spaceMaterial, sun, Color.white, 0.1f);


            SetSkybox(skybox);

        }

        void setSun()
        {
            //		if (World.WorldCamera
            if (sun == null)
                return;
            sun.transform.rotation = Quaternion.FromToRotation(Vector3.forward, World.WorldCamera.toVectorf().normalized);
            sun.GetComponent<Light>().color = space.color;
        }


        public void ReplaceMaterial(GameObject g, Material mat, PlanetSettings ps)
        {
            Renderer r = g.GetComponent<Renderer>();
            if (r != null)
                r.material = mat;

            ps.atmosphere.InitAtmosphereMaterial(mat);
            ps.atmosphere.initGroundMaterial(true,mat);


            foreach (Transform child in g.transform)
            {
                ReplaceMaterial(child.gameObject, mat, ps);
            }
        }

        public void toggleGPUSurface()
        {
            RenderSettings.GPUSurface = !RenderSettings.GPUSurface;
            InitializeSurfaces();
            foreach (Planet p in planets)
            {
                p.pSettings.atmosphere.ReinitializeGroundMaterial(groundMaterial);
//                ReplaceMaterial(p.pSettings.properties.terrainObject, groundMaterial, p.pSettings);
                GameObject.DestroyImmediate(p.pSettings.properties.terrainObject);
            }


        }

        public void findClosestPlanet()
        {
            if (planets.Count > 0)
                planet = planets[0];

            float min = 1E10f;
            foreach (Planet p in planets)
            {
                float l = (p.pSettings.gameObject.transform.position).magnitude - p.pSettings.radius;
                if (l < min)
                {
                    planet = p;
                    min = l;
                }
            }

        }




        public void InitializeFromScene()
        {

            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if (go.activeSelf)
                {
                    PlanetSettings ps = go.GetComponent<PlanetSettings>();
                    if (ps == null)
                    {
                        cameraObjects.Add(go);
                        continue;
                    }
                    Planet p;
                    if (ps.planetTypeName=="star")
                        p = new Star(ps);
                    else
                        p = new Planet(ps);


                    p.pSettings.properties.orgPos.Set(go.transform.position);
                    p.pSettings.properties.pos.Set(go.transform.position);
                    go.transform.parent = transform;
                    p.pSettings.properties.parent = go;
					//p.pSettings.planetType = PlanetSettings.planetTypes.planetTypes[p.pSettings.planetTypeIndex];
 //                   if (RenderSettings.GPUSurface)
///                        p.pSettings.planetType = PlanetSettings.planetTypes.planetTypes[0];
                    //				p.pSettings.planetType = PlanetType.planetTypes[1];
                    if (ps.planetTypeName != "star")
                        p.Initialize(sun, groundMaterial, (Material)Resources.Load("SkyMaterial"), sphere);
                    else
                        p.Initialize(sun, groundMaterial, (Material)Resources.Load("Sun"), sphere);

                    planets.Add(p);

                }
            }

            RenderSettings.ResolutionScale = World.SzWorld.resolutionScale;

            space.color = new Color(World.SzWorld.sun_col_r, World.SzWorld.sun_col_g, World.SzWorld.sun_col_b);
            space.hdr = World.SzWorld.sun_intensity;
        }




        public void Update()
        {
            setSun();
            if (space != null)
                space.Update();

            findClosestPlanet();

            if (planet != null)
                planet.ConstrainCameraExterior();

            foreach (Planet p in planets)
                p.Update();

            // Set closest clippping plane
            if (planet != null)
            {
                if (planet.pSettings.atmosphere != null)
                    planet.pSettings.atmosphere.setClippingPlanes();

                UnityEngine.RenderSettings.reflectionIntensity = UnityEngine.RenderSettings.reflectionIntensity * 0.99f + planet.pSettings.m_reflectionIntensity * 0.01f;

            }

        }


        public void Reset()
        {
            foreach (Planet p in planets)
                p.Reset();
        }


        public void LoadWorld(string data, bool isFile, bool ExitOnSave, World world, bool randomizeSeeds = false)
        {
            ClearStarSystem();
            SerializedWorld sz;
            if (isFile)
            {
                //			RenderSettings.extraText = data;

                if (!System.IO.File.Exists(data))
                {
                    //RenderSettings.extraText = ("ERROR: Could not find file :'" + data + "'");
                    World.FatalError("Could not load file: " + data);
                    return;
                }
                sz = SerializedWorld.DeSerialize(data);
            }
            else
                sz = SerializedWorld.DeSerializeString(data);


            RenderSettings.ExitSaveOnRendered = ExitOnSave;
            RenderSettings.extraText = "";
            SetSkybox((int)sz.skybox);
            if (RenderSettings.ignoreXMLResolution) {
				sz.resolutionScale = World.SzWorld.resolutionScale;
				sz.resolution = World.SzWorld.resolution;

			}
			else {
				RenderSettings.sizeVBO = Mathf.Clamp(sz.resolution, 32, 128);
				RenderSettings.ResolutionScale = sz.resolutionScale;

			}
			World.SzWorld = sz;

            RenderSettings.ScreenshotX = sz.screenshot_height;
            RenderSettings.ScreenshotY = sz.screenshot_width;
            int cnt = 0;
            World.hasScene = true;
            RenderSettings.isVideo = sz.isVideo();
            if (RenderSettings.isVideo == true)
                RenderSettings.ExitSaveOnRendered = false;


            //		RenderSettings.isVideo = false;
            if (WorldMC.Slider!=null)	
	            WorldMC.Slider.SetActive(RenderSettings.isVideo);

            foreach (SerializedPlanet sp in sz.Planets)
            {
                GameObject go = new GameObject(sp.name);
                go.transform.parent = transform;
				PlanetSettings ps = sp.DeSerialize(go, cnt++, sz.global_radius_scale);
				if (randomizeSeeds) {
					ps.seed = (int)(Random.value * 10000f);
					ps.Randomize(0, sp.planetType);
				}

                Planet p;
                if (ps.planetTypeName =="star" && World.CurrentApp==Verification.MCAstName)
                    continue;

                if (ps.planetTypeName=="star")
                        p = new Star(ps);
                    else
                        p = new Planet(ps);
				p.pSettings.properties.parent = go;


                if (ps.planetTypeName != "star")
                        p.Initialize(sun, groundMaterial, (Material)Resources.Load("SkyMaterial"), sphere);
                    else
                        p.Initialize(sun, groundMaterial, (Material)Resources.Load("Sun"), sphere);

//                p.Initialize(sun, groundMaterial, (Material)Resources.Load("SkyMaterial"), sphere);
                planets.Add(p);
            }
			world.setWorld(sz);
        }
        public void ClearStarSystem()
        {
            planets.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                GameObject go = transform.GetChild(i).gameObject;
                if(go.GetComponent<PlanetSettings>()!=null)
                    GameObject.Destroy(go);
                //	Debug.Log ("Destroying " + go.name);
            }



        }
       
        public static void SetSkybox(int s)
        {
            string skybox = "Skybox3";
            s = s % 7;

            if (s == 1) skybox = "Skybox4";
            if (s == 2) skybox = "Skybox5";
            if (s == 3) skybox = "Skybox2";
            if (s == 4) skybox = "Skybox7";
            if (s == 5) skybox = "Skybox8";
            if (s == 6) skybox = "Skybox9";

            UnityEngine.RenderSettings.skybox = (Material)Resources.Load(skybox);

        }


    }
}