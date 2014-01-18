﻿
/*
 * Kerbal Science Foundation Advanced Solid Rocket Booster v0.6 for Kerbal Space Program
 * Released September 13, 2013 under a Creative Commons Attribution-NonCommercial-ShareAlike 3.0 Unported License
 * For attribution, please attribute "Kujuman from the official KSP forums"
 * Portions of this work were based on example code posted at the KSP wiki. If you have something to contribute there, please do!
 */

using KSP;
using UnityEngine;
using System;
using System.Linq;

namespace KSF_SolidRocketBooster
{
    public class KSF_SolidBoosterNozzle : PartModule
    {
        #region Character arrays declaration
            KSF_CharArray Spacech = new KSF_CharArray();
            KSF_CharArray Sch = new KSF_CharArray();
            KSF_CharArray Tch = new KSF_CharArray();
            KSF_CharArray Ach = new KSF_CharArray();
            KSF_CharArray Cch = new KSF_CharArray();
            KSF_CharArray ch1 = new KSF_CharArray();
            KSF_CharArray ch2 = new KSF_CharArray();
            KSF_CharArray ch3 = new KSF_CharArray();
            KSF_CharArray ch4 = new KSF_CharArray();
            KSF_CharArray ch5 = new KSF_CharArray();
            KSF_CharArray ch6 = new KSF_CharArray();
            KSF_CharArray ch7 = new KSF_CharArray();
            KSF_CharArray ch8 = new KSF_CharArray();
            KSF_CharArray ch9 = new KSF_CharArray();
            KSF_CharArray ch0 = new KSF_CharArray();
            KSF_CharArray chs = new KSF_CharArray();
            KSF_CharArray chk = new KSF_CharArray();
            KSF_CharArray cht = new KSF_CharArray();
        #endregion

        #region GUI variable declaration
        string simDuration;
        bool EditorGUIvisible;

        private int GUImodeInt = 0;
        private string[] GUImodeStrings = {"Stack", "Segments", "Vessel"};

        private Rect GUImainRect = new Rect(10, 60, 660, 530);

        private Vector2 segListVector = new Vector2(0, 0);
        private int segListLength = 0;
        private Part segCurrentGUI;
        private Part segPriorGUI;

        private Vector2 nodeListVector = new Vector2(0, 0);
        private int nodeNumber = 0;
        private int nodePriorNumber = 0;

        private Vector2 autoListVector = new Vector2(0, 0);
        private System.Collections.Generic.List<KSFAutoSRB> autoTypeList = new System.Collections.Generic.List<KSFAutoSRB>(); 
        private int autoTypeCurrent;

        private string tbTime;
        private string tbValue;
        private string tbInTan;
        private string tbOutTan;

        private bool isAutoNode = false;
        //float UIduration = 60;
        //float UIstart = 1;
        //float UIthrust = 0;
        //float UIgee = 1;


        private string lbMsgBox;

        bool refreshNodeInfo = true;
        bool refreshSegGraph = true;

        private Texture2D segGraph;

        private string klipboard;


        [KSPField]
        public string sResourceType;

        [KSPField]
        public float graphMassColorR;
        [KSPField]
        public float graphMassColorG;
        [KSPField]
        public float graphMassColorB;


        [KSPField]
        public float graphThrustColorR;
        [KSPField]
        public float graphThrustColorG;
        [KSPField]
        public float graphThrustColorB;

        [KSPField]
        public float graphExtraThrustColorR;
        [KSPField]
        public float graphExtraThrustColorG;
        [KSPField]
        public float graphExtraThrustColorB;

        #endregion



        [KSPField]
        public FloatCurve acISP;

        [KSPField]
        private string thrustTransform = "thrustTransform";

        [KSPField]
        public float resourceDensity = .00975f;

        private Transform tThrustTransform;

        protected Rect EditorGUIPos = new Rect(0,0,0,0);

        private Texture2D graphImg = new Texture2D(640,480);

        [KSPField(isPersistant = true)]
        private bool hasFired = false;

        [KSPField(isPersistant = true)]
        private bool notExhausted = true;

        [KSPField(guiName = "Isp", guiActive = true, guiFormat = "F2")]
        public float fCurrentIsp = 0f;

        [KSPField(guiActive = true, guiName = "Force", guiUnits = " kN", guiFormat = "F2")]
        public float fForce = 0f;

        [KSPField(guiActive = true, guiName = "Fuel Mass Flow", guiUnits = " t/s", guiFormat = "F4")]
        private float fFuelFlowMass = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiName = "T+", guiUnits = " s", guiFormat = "F2")]
        public float fEngRunTime = 0f;

        [KSPField(guiActive = false, isPersistant = true)]
        private bool hasAborted = false;

        [KSPField]
        public Vector3 fxOffset = Vector3.zero;

        [KSPAction("Ignite")]
        private void TryIgnite(KSPActionParam a)
        {
            if (hasFired)
                return;
            else
                this.part.force_activate();
        }

        [KSPAction("Abort")]
        private void Abort(KSPActionParam a)
        {
            hasAborted = true;
        }

        private Part p;
        private bool bEndofFuelSearch;
        private int i;

        private float fMassFlow = 0;

        private FXGroup gRunning;
        private FXGroup gFlameout;

        [KSPField]
        public string topNode = "SRBtop";

        System.Collections.Generic.List<Part> FuelSourcesList = new System.Collections.Generic.List<Part>(); //filled once during OnActivate, is the master list
        System.Collections.Generic.List<Part> CurrentFuelSourcesList = new System.Collections.Generic.List<Part>(); //filled everytime the vehicle part count changes.

        private int iVesselPartCount;

        private bool isExploding;

        public override void OnAwake()
        {
            gRunning = this.part.findFxGroup("running");
            gFlameout = this.part.findFxGroup("flameout");

            tThrustTransform = this.part.FindModelTransform(thrustTransform);

            if (acISP == null)
                acISP = new FloatCurve();
        }

        public override void OnStart(StartState state)
        {
            if ((EditorGUIPos.x == 0) && (EditorGUIPos.y == 0))//windowPos is used to position the GUI window, lets set it in the center of the screen
            {
                EditorGUIPos = new Rect((Screen.width - 680) / 2, (Screen.height - 570) / 2, GUImainRect.width + 2 * GUImainRect.xMin, GUImainRect.height + GUImainRect.yMin + 10);
            }

            for (int y = 0; y < graphImg.height; y++)
            {
                for (int x = 0; x < graphImg.width; x++)
                {
                    graphImg.SetPixel(x, y, Color.black);
                }
            }


            autoTypeList.Clear();
            autoTypeList.Add(new autoSeg_ThrustForDuration());
            autoTypeList.Add(new autoSeg_ThrustForThrust());
            autoTypeList.Add(new autoSeg_ThrustAtTime());
            autoTypeList.Add(new autoSeg_ExtraThrustForDuration());
            autoTypeList.Add(new autoSeg_ExtraThrustForExtraThrustAtGee());

            

            RenderingManager.AddToPostDrawQueue(3, new Callback(drawEditorGUI));//start the GUI
        }


        public override void OnActive()
        {
            if (notExhausted) //the if is here to prevent SRBIgnite() being called when loading a scene (ie, if there are booster nozzles scattered around the KSC, they get forceactivated)
            {
                isExploding = false;
                SRBIgnite();
            }
        }

        public override void OnInactive()
        {
            RenderingManager.RemoveFromPostDrawQueue(3, new Callback(drawEditorGUI)); //close the GUI
        }

        #region SRB code which actually does stuff on the physical side

        public override void OnFixedUpdate()
        {
            HeatSegments();
            if (hasFired && notExhausted)
            {
                if (hasPartCountChanged() && isExploding != true)
                {
                    FuelStackSearcher(CurrentFuelSourcesList);
                    if (FuelSourcesList.Count != CurrentFuelSourcesList.Count)
                    {
                        isExploding = true;
                    }
                }
                RunEngClock();
                CalcCurrentIsp();
                RunFuelFlow();
                DoApplyEngine();

                gRunning.SetPower(Mathf.Min(1.5f, fFuelFlowMass * 10f)); //fx, will move to OnUpdate eventually

                if (notExhausted == false)
                    SRBExtinguish();
            }
        }


        /// <summary>
        /// RunEngClock ticks the engine run clock forward
        /// </summary>
        private void RunEngClock()
        {
            fEngRunTime += TimeWarp.fixedDeltaTime;
        }


        /// <summary>
        /// hasPartCountChanged basically determines if the part count of the vessel has changed.
        /// </summary>
        /// <returns>True or false depending upon if the part count of the vessel has changed</returns>
        private bool hasPartCountChanged()
        {
            if (iVesselPartCount == 0)
            {
                iVesselPartCount = vessel.parts.Count;
                print("Initial Part count = " + iVesselPartCount);
                return false;
            }

            if (vessel.parts.Count != iVesselPartCount)
            {
                iVesselPartCount = vessel.parts.Count;
                return true;
            }
            return false;
        }


        /// <summary>
        /// RunFuelFlow gets the fuel from the connected SRB segments and adds their mass flow to the nozzle. It also determines if there is fuel in the stack.
        /// </summary>
        private void RunFuelFlow()
        {
            notExhausted = false;
            fFuelFlowMass = 0f;
            foreach (Part p in FuelSourcesList)
            {
                KSF_SolidBoosterSegment srb = p.GetComponent<KSF_SolidBoosterSegment>();
                notExhausted = (notExhausted | srb.isFuelRemaining());
                fMassFlow = Mathf.Max(0, srb.CalcMassFlow(fEngRunTime));
                fFuelFlowMass += (p.RequestResource(sResourceType, (fMassFlow / resourceDensity) * TimeWarp.fixedDeltaTime)) * resourceDensity;
            }
        }


        /// <summary>
        /// CalcCurrentIsp() calculates the current Isp of the nozzle, taking into account whether or not the abort sequence has fired
        /// </summary>
        private void CalcCurrentIsp()
        {
            if (hasAborted)
                fCurrentIsp = acISP.Evaluate((float)FlightGlobals.getStaticPressure()) / 15f;
            else
                fCurrentIsp = acISP.Evaluate((float)FlightGlobals.getStaticPressure());
        }


        /// <summary>
        /// DoApplyEngine() applies a force to the nozzle (simulating the thrust)
        /// Also it converts a few variables into a "per second" format versus "per physics frame" for display in the context menu
        /// </summary>
        private void DoApplyEngine()
        {
            fForce = 9.81f * fCurrentIsp * fFuelFlowMass / TimeWarp.fixedDeltaTime;
            part.Rigidbody.AddForceAtPosition(tThrustTransform.forward * fForce * -1, this.part.rigidbody.position);
            fFuelFlowMass = fFuelFlowMass / TimeWarp.fixedDeltaTime;
        }


        /// <summary>
        /// SRBIgnite() does a number of things
        /// 1. It sets some variables to let the nozzle know that the booster has been fired
        /// 2. It sets up the effects, turning them on and positioning them in the proper location
        /// </summary>
        private void SRBIgnite()
        {
            hasFired = true;
            foreach (FXGroup e in this.part.fxGroups)
            {
                foreach (GameObject o in e.fxEmitters)
                {
                    o.transform.SetParent(tThrustTransform);
                    //I might want to flip the following two lines, as effects seem to be offset in directions they should not be. But that's what the next version is for.
                    //flipped the lines since the above line was written
                    o.transform.Rotate(-90, 0, 0);
                    o.transform.position += fxOffset;
                }
            }
            gRunning.setActive(true);
            gRunning.audio.Play();
            FuelStackSearcher(FuelSourcesList);
        }


        /// <summary>
        /// SRBExtinguish() is run when the booster stack is out of fuel, and it only controls effects.
        /// </summary>
        private void SRBExtinguish()
        {
            gRunning.setActive(false);
            gRunning.audio.Stop();
            gFlameout.Burst();
        }


        /// <summary>
        /// HeatSegments is an attempt to have failing boosters break by adding heat to the part rapidly
        /// </summary>
        private void HeatSegments()
        {
            if (isExploding)
            {
                FuelSourcesList.RemoveAll(item => item == null);

                if (FuelSourcesList.Contains(this.part))
                {
                    if (FuelSourcesList.Count == 1 && FuelSourcesList.Contains(this.part) || FuelSourcesList.Count == 0)
                    {
                        this.part.explode();
                    }

                    foreach (Part p in FuelSourcesList)
                    {
                        p.temperature += UnityEngine.Random.Range(20, 200); //I want to refine this so that parts are even still more randomly heated...they tend to explode within a few frames of one another. I may move this to OnUpdate()
                    }
                    this.part.temperature = 100;
                }
                else
                {
                    if (FuelSourcesList.Count == 0)
                    {
                        this.part.explode();
                    }

                    foreach (Part p in FuelSourcesList)
                    {
                        p.temperature += UnityEngine.Random.Range(20, 200); //I want to refine this so that parts are even still more randomly heated...they tend to explode within a few frames of one another. I may move this to OnUpdate()
                    }
                }
            }
        }

        /// <summary>
        /// This is the combined FuelStackSearch and FuelStackReSearch. It should be a bit more reliable as well as preventing me from updating one segment of code and not the other (which would likley lead to autonomus explosions)
        /// So this sub basically finds all the valid fuel sources for the booster segment, it should work fine. I've left debugging prints commented out because I'm sure I'll need them again.
        /// </summary>
        /// <param name="pl"></param>
        private void FuelStackSearcher(System.Collections.Generic.List<Part> pl)
        {
            //print("Begin Stack Search (Generic)");
            pl.Clear();
            i = 0;
            bEndofFuelSearch = false;

            p = this.part;
            AttachNode n = new AttachNode();

            KSF_SolidBoosterSegment SRB = new KSF_SolidBoosterSegment();

            p = this.part;
            n = p.findAttachNode(topNode);

            if (p.Modules.Contains("KSF_SolidBoosterSegment"))
            {
                if (pl.Contains(p) != true)
                    pl.Add(p);

                SRB = p.GetComponent<KSF_SolidBoosterSegment>();

                if (SRB.endOfStack)
                    goto Leave;
            }

            do
            {
                if (n.attachedPart.Modules.Contains("KSF_SolidBoosterSegment"))
                {
                    p = n.attachedPart;

                    if (pl.Contains(p) != true)
                        pl.Add(p);

                    SRB = p.GetComponent<KSF_SolidBoosterSegment>();

                    if (SRB.endOfStack != true)
                    {
                        foreach (AttachNode k in p.attachNodes)
                        {
                            if (k.id == SRB.topNode)
                            {
                                n = k;
                                goto Finish;
                            }
                        }

                    Finish:

                        if (n.attachedPart == null)
                        {
                            bEndofFuelSearch = true;
                        }
                    }
                    else
                    {
                        bEndofFuelSearch = true;
                    }
                }
                else
                    bEndofFuelSearch = true;

                i += 1;

            } while (bEndofFuelSearch == false && i < 250);

            if (i == 250) //I hope to hell no one builds a rocket such that 250 iterations is restricting an otherwise valid design. Because damn.
            {
                print("SolidBoosterNozzle: Forced out of loop");
                print("SolidBoosterNozzle: Loop executed 250 times");
            }

        Leave:
            ;
        }

        #endregion

        #region GUI graph maker
        /// <summary>
        /// This will return an image of the specified size with the thrust graph for this nozzle
        /// Totally jacked the image code from the wiki http://wiki.kerbalspaceprogram.com/wiki/Module_code_examples, with some modifications
        /// 
        /// 
        /// </summary>
        /// <param name="imgH">Hight of the desired output, in pixels</param>
        /// <param name="imgW">Width of the desired output, in pixels</param>
        /// <param name="burnTime">Chart for burn time in seconds</param>
        /// <returns></returns>
        private void stackThrustPredictPic(int imgH, int imgW, int burnTime)
        {
            //First we set up the analyzer
            //Step 1: Figure out fuel sources
            FuelStackSearcher(FuelSourcesList);
            //Step 2: Set up mass variables
            float stackTotalMass = 0f;
            stackTotalMass = CalcStackWetMass(FuelSourcesList, stackTotalMass);

            float[] segmentFuelArray = new float[FuelSourcesList.Count];

            int i = 0;
            foreach (Part p in FuelSourcesList)
            {
                segmentFuelArray[i] = p.GetResourceMass();
                i++;
            }

            //float stackCurrentMass = stackTotalMass;
            //Now we set up the image maker
            Texture2D image = new Texture2D(imgW, imgH);
            int graphXmin = 19;
            int graphXmax = imgW - 20;
            int graphYmin = 19;
            //Step 3: Set Up color variables
            Color brightG = Color.black;
            brightG.r = .3372549f;
            brightG.g = 1;
            Color mediumG = Color.black;
            mediumG.r = 0.16862745f;
            mediumG.g = .5f;
            Color lowG = Color.black;
            lowG.r = 0.042156863f;
            lowG.g = .125f;

            Color MassColor = Color.black;
            Color ThrustColor = Color.black;
            Color ExtraThrustColor = Color.black;

            if (graphExtraThrustColorB == 0 && graphExtraThrustColorG == 0 && graphExtraThrustColorR == 0)
                ExtraThrustColor = brightG;
            else
            {
                ExtraThrustColor.r = graphExtraThrustColorR;
                ExtraThrustColor.g = graphExtraThrustColorG;
                ExtraThrustColor.b = graphExtraThrustColorB;
            }

            if (graphMassColorB == 0 && graphMassColorG == 0 && graphMassColorR == 0)
                MassColor = brightG;
            else
            {
                MassColor.r = graphMassColorR;
                MassColor.g = graphMassColorG;
                MassColor.b = graphMassColorB;
            }

            if (graphThrustColorB == 0 && graphThrustColorG == 0 && graphThrustColorR == 0)
                ThrustColor = brightG;
            else
            {
                ThrustColor.r = graphThrustColorR;
                ThrustColor.g = graphThrustColorG;
                ThrustColor.b = graphThrustColorB;
            }



            //Step 4: Define text arrays
            populateCharArrays();
            //Step 5a: Define time markings (every 10 seconds gets a verticle line)
            System.Collections.Generic.List<int> timeLines = new System.Collections.Generic.List<int>();
            double xScale = (imgW - 40) / (double)burnTime;
            //print("xScale: " + xScale);
            calcTimeLines((float)burnTime, 10,timeLines, (float)xScale, 20);
            //Step 5b: Define vertical line markings (9 total to give 10 sections)
            System.Collections.Generic.List<int> horzLines = new System.Collections.Generic.List<int>();
            calcHorizLines(imgH - graphYmin, horzLines, 9, 20);
            //Step 6: Clear the background

            //Set all the pixels to black. If you don't do this the image contains random junk.
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    image.SetPixel(x, y, Color.black);
                }
            }

            //Step 7a: Draw Time Lines
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    if (timeLines.Contains(x) && y > graphYmin)
                        image.SetPixel(x, y, lowG);
                }
            }

            //Step 7b: Draw Vert Lines
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    if (horzLines.Contains(y) && x < graphXmax && x > graphXmin)
                        image.SetPixel(x, y, lowG);
                }
            }


            //Step 7c: Draw Bounding Lines
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    if ((x == graphXmin | x == graphXmax) && (y > graphYmin | y == graphYmin))
                        image.SetPixel(x, y, mediumG);

                    if (y == graphYmin && graphXmax > x && graphXmin < x)
                        image.SetPixel(x, y, mediumG);
                }
            }

            //Step 8a: Populate graphArray
            double simStep = .2;
            i = 0;
            //double peakThrustTime = 0;
            double peakThrustAmt = 0;

            //set up the array for the graphs
            int graphArraySize = Convert.ToInt16(Convert.ToDouble(simDuration) / simStep);
            double[,] graphArray = new double[graphArraySize, 4];


            //one time setups
            graphArray[i, 0] = stackMassFlow(FuelSourcesList, (float)(i * simStep),segmentFuelArray, simStep);
            graphArray[i, 1] = stackTotalMass;
            graphArray[i, 2] = 9.81 * acISP.Evaluate(1) * graphArray[i,0];
            graphArray[i, 3] = graphArray[i, 2] - (graphArray[i, 1] * 9.81);


            //fForce = 9.81f * fCurrentIsp * fFuelFlowMass / TimeWarp.fixedDeltaTime;
            do
            {
                i++;
                graphArray[i, 0] = stackMassFlow(FuelSourcesList, (float)(i * simStep), segmentFuelArray, simStep);
                graphArray[i, 1] = graphArray[i-1,1]-(graphArray[i-1,0] * simStep);
                graphArray[i, 2] = 9.81 * acISP.Evaluate(1) * graphArray[i, 0];
                graphArray[i, 3] = graphArray[i, 2] - (graphArray[i, 1] * 9.81);

                if (graphArray[i, 2] > peakThrustAmt)
                {
                    peakThrustAmt = graphArray[i, 2];
                    //peakThrustTime = i;
                }
                //print("generating params i=" + i + " peak at " + Convert.ToInt16(Convert.ToDouble(simDuration) / simStep));
            } while (i + 1 < graphArraySize);

            //Step 8b: Make scales for the y axis
            double yScaleMass = 1;
            double yScaleThrust = 1;
            int usableY;
            usableY = imgH - 20;

            yScaleMass = usableY / (Mathf.CeilToInt((float)(graphArray[0, 1] / 10)) * 10);
            float inter;
            inter = (float)peakThrustAmt / 100;
            //print("1: " + inter);
            inter = Mathf.CeilToInt(inter);
            //print("22: " + inter);
            inter = inter * 100;
            //print("3: " + inter);
            inter = usableY / inter;
            //print("4: " + inter);

            yScaleThrust = inter;

            //print(yScaleThrust + ":" + peakThrustAmt + ":" + usableY);

            print("graphed scales");

            //Step 8c: Graph the mass
            int lineWidth = 3;
            for (int x = graphXmin; x < graphXmax; x++)
            {
                int fx = fGraph(xScale,x - 20,yScaleMass,graphArray, simStep, graphArraySize,1, 20);

                for (int y = fx; y < fx + lineWidth; y++)
                {
                    image.SetPixel(x, y, MassColor);
                }
            }

            //Step 8d: Graph the thrust
            lineWidth = 3;
            for (int x = graphXmin; x < graphXmax; x++)
            {
                int fx = fGraph(xScale, x - 20, yScaleThrust, graphArray, simStep, graphArraySize,2, 20);
                for (int y = fx; y < fx + lineWidth; y++)
                {
                    image.SetPixel(x, y, ThrustColor);
                }
            }

            //Step 8e: Graph the thrust extra
            lineWidth = 3;
            for (int x = graphXmin; x < graphXmax; x++)
            {
                int fx = fGraph(xScale, x - 20, yScaleThrust, graphArray, simStep, graphArraySize, 3, 20);
                for (int y = fx; y < fx + lineWidth; y++)
                {
                    image.SetPixel(x, y,ExtraThrustColor);
                }
            }




            //Step 9: Set up boxes for time

            //int i = 0;
            string s;
            i = 0;
            int length = 0;
            int pos = 0;
            int startpos = 0;
            Texture2D tex;

            do
            {   
                s = "";

                pos = timeLines[i];
                i++;
                s = i*10 + " s";

                //print("composite string: " + s);

                length = calcStringPixLength(convertStringToCharArray(s));
                //print("length: " + length);

                startpos = Mathf.FloorToInt((float)pos - 0.5f * (float)length);

                Color[] region = image.GetPixels(startpos, 23, length, 11);

                for (int c = 0; c < region.Length; c++)
                {
                    region[c] = brightG;   
                }

                image.SetPixels(startpos, 23, length, 11, region);

                tex = convertCharArrayToTex(convertStringToCharArray(s), length, brightG);

                Color[] region2 = tex.GetPixels();

                image.SetPixels(startpos + 2, 25, length - 4, 7, region2);

                length = 0;

            } while (i < timeLines.Count);


            //set up boxes for horizontal lines, mass first
            startpos = 0;
            length = 0;
            pos = 0;
            i = 0;
            do
            {
                s = "";
                pos = horzLines[i];
                i++;
                s = ((Mathf.CeilToInt((float)(graphArray[0, 1] / 10)) * i)).ToString() + " t";
                length = calcStringPixLength(convertStringToCharArray(s));
                startpos = Mathf.FloorToInt((float)pos - 5f);
                Color[] region = image.GetPixels(23, startpos, length, 11);
                for (int c = 0; c < region.Length; c++)
                {
                    region[c] = brightG;
                }
                image.SetPixels(23, startpos, length, 11, region);
                tex = convertCharArrayToTex(convertStringToCharArray(s), length, brightG);
                Color[] region2 = tex.GetPixels();
                image.SetPixels(25, startpos + 2, length - 4, 7, region2);
                length = 0;
            } while (i < horzLines.Count);

            //set up boxes for horizontal lines,  thrust
            startpos = 0;
            length = 0;
            pos = 0;
            i = 0;
            do
            {
                s = "";
                pos = horzLines[i];
                i++;
                s = ((Mathf.CeilToInt((float)(peakThrustAmt / 100)) * (i * 10))).ToString() + " k";
                length = calcStringPixLength(convertStringToCharArray(s));
                startpos = Mathf.FloorToInt((float)pos - 5f);
                Color[] region = image.GetPixels(imgW-60, startpos, length, 11);
                for (int c = 0; c < region.Length; c++)
                {
                    region[c] = brightG;
                }
                image.SetPixels(imgW - 60, startpos, length, 11, region);
                tex = convertCharArrayToTex(convertStringToCharArray(s), length, brightG);
                Color[] region2 = tex.GetPixels();
                image.SetPixels(imgW - 58, startpos + 2, length - 4, 7, region2);
                length = 0;
            } while (i < horzLines.Count);

            image.Apply();
            graphImg = image;
        }

        private int fGraph(double xScale, int xPos, double yScale, double[,] array, double step, int arraySize, int column, int yOffset)
        {
            double t = 0;
            int i = 0;
            t = xPos / xScale;
            t = Mathf.Floor((float)(t / step));
            i = Convert.ToInt16(t);
            i = Mathf.Clamp(i, 0, arraySize - 1);
            if (Convert.ToInt16(yScale * array[i, column]) > 0)
                return (Convert.ToInt16(yScale * array[i, column]) + yOffset);
            else
                return yOffset;
        }


        private int calcStringPixLength(System.Collections.Generic.List<KSF_CharArray> list)
        {
            int i = 2;

            foreach (KSF_CharArray c in list)
            {
                i += c.charWidth;
            }
            i++;
            return i;
        }

        private System.Collections.Generic.List<KSF_CharArray> convertStringToCharArray(string s)
        {
            System.Collections.Generic.List<KSF_CharArray> chArray = new System.Collections.Generic.List<KSF_CharArray>();
            chArray.Clear();

            foreach (char c in s)
            {
                if (c == '1')
                {
                    chArray.Add(ch1);
                    continue;
                }

                if (c == '2')
                {
                    chArray.Add(ch2);
                    continue;
                }

                if (c == '3')
                {
                    chArray.Add(ch3);
                    continue;
                }

                if (c == '4')
                {
                    chArray.Add(ch4);
                    continue;
                }

                if (c == '5')
                {
                    chArray.Add(ch5);
                    continue;
                }

                if (c == '6')
                {
                    chArray.Add(ch6);
                    continue;
                }

                if (c == '7')
                {
                    chArray.Add(ch7);
                    continue;
                }

                if (c == '8')
                {
                    chArray.Add(ch8);
                    continue;
                }

                if (c == '9')
                {
                    chArray.Add(ch9);
                    continue;
                }

                if (c == '0')
                {
                    chArray.Add(ch0);
                    continue;
                }

                if (c == 's')
                {
                    chArray.Add(chs);
                    continue;
                }

                if (c == 'A')
                {
                    chArray.Add(Ach);
                    continue;
                }

                if (c == ' ')
                {
                    chArray.Add(Spacech);
                    continue;
                }

                if (c == 'k')
                {
                    chArray.Add(chk);
                    continue;
                }

                if (c == 't')
                {
                    chArray.Add(cht);
                    continue;
                }
            }

            return chArray;
        }

        private Texture2D convertCharArrayToTex(System.Collections.Generic.List<KSF_CharArray> chArray, int length, Color color)
        {
            Texture2D tex = new Texture2D(length - 4, 7);
            int offSet = 0;
            System.Collections.Generic.List<Vector2> pxMap = new System.Collections.Generic.List<Vector2>();
            pxMap.Clear();

            foreach (KSF_CharArray ka in chArray)
            {
                foreach (Vector2 px in ka.pixelList)
                {
                    pxMap.Add(new Vector2(px.x + offSet, px.y));
                }
                offSet += ka.charWidth;
            }

            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    if (pxMap.Contains(new Vector2(x,y)))
                    {
                        tex.SetPixel(x, y, Color.black);
                    }
                    else
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
            return tex;

        }

        private void calcTimeLines(float duration, float spacing, System.Collections.Generic.List<int> list, float xscale, int xOffset)
        {
            list.Clear();
            float f = 0f;
            int result = 0;
            int i = 1;
            do
            {
                f = (i * spacing * xscale);
                result = Mathf.RoundToInt(f) + xOffset;
                list.Add(result);
                i++;
            } while (i < duration/spacing);
        }

        private void calcHorizLines(float yHeight, System.Collections.Generic.List<int> list, int numLines, int yOffset)
        {
            list.Clear();
            float f = 0f;
            int result = 0;
            int i = 1;
            do
            {
                f = (i * yHeight)/(numLines+1);
                result = Mathf.RoundToInt(f)+ yOffset;
                list.Add(result);
                //print("Added horizontal line at: " + result);
                i++;
            } while (i<= numLines);
        }

        private float stackMassFlow(System.Collections.Generic.List<Part> pl, float t, float[] a, double step)
        {
            int i = 0;
            float totalFlow = 0f;
            foreach (Part p in pl)
            {
                KSF_SolidBoosterSegment srb = p.GetComponent<KSF_SolidBoosterSegment>();

                if(a[i] > 0 && srb.CalcMassFlow(t) > 0)
                {
                    totalFlow += srb.CalcMassFlow(t);
                    a[i] -= (float)(srb.CalcMassFlow(t)*step);
                }
                i++;
            }
            return totalFlow;
        }

        private float CalcStackWetMass(System.Collections.Generic.List<Part> pl, float totalMass)
        {
            totalMass = 0f;
            foreach (Part p in pl)
            {
                totalMass += p.mass + p.GetResourceMass();
            }
            totalMass += part.mass;
            return totalMass;
        }

        public void populateCharArrays()
        {
            Spacech.pixelList.Clear();
            Spacech.charName = "Space";
            Spacech.charWidth = 2;

            Sch.pixelList.Clear();
            Sch.charName = "S";
            Sch.charWidth = 5;
            Sch.pixelList.Add(new Vector2(0, 5));
            Sch.pixelList.Add(new Vector2(0, 4));
            Sch.pixelList.Add(new Vector2(0, 1));
            Sch.pixelList.Add(new Vector2(1, 6));
            Sch.pixelList.Add(new Vector2(1, 3));
            Sch.pixelList.Add(new Vector2(1, 0));
            Sch.pixelList.Add(new Vector2(2, 6));
            Sch.pixelList.Add(new Vector2(2, 3));
            Sch.pixelList.Add(new Vector2(2, 0));
            Sch.pixelList.Add(new Vector2(3, 5));
            Sch.pixelList.Add(new Vector2(3, 4));
            Sch.pixelList.Add(new Vector2(3, 1));

            Tch.pixelList.Clear();
            Tch.charName = "T";
            Tch.charWidth = 6;
            Tch.pixelList.Add(new Vector2(0, 6));
            Tch.pixelList.Add(new Vector2(1, 6));
            Tch.pixelList.Add(new Vector2(2, 6));
            Tch.pixelList.Add(new Vector2(3, 6));
            Tch.pixelList.Add(new Vector2(4, 6));
            Tch.pixelList.Add(new Vector2(2, 1));
            Tch.pixelList.Add(new Vector2(2, 2));
            Tch.pixelList.Add(new Vector2(2, 3));
            Tch.pixelList.Add(new Vector2(2, 4));
            Tch.pixelList.Add(new Vector2(2, 5));
            Tch.pixelList.Add(new Vector2(2, 0));

            Ach.pixelList.Clear();
            Ach.charName = "A";
            Ach.charWidth = 6;
            Ach.pixelList.Add(new Vector2(0, 3));
            Ach.pixelList.Add(new Vector2(0, 2));
            Ach.pixelList.Add(new Vector2(0, 1));
            Ach.pixelList.Add(new Vector2(0, 0));
            Ach.pixelList.Add(new Vector2(4, 3));
            Ach.pixelList.Add(new Vector2(4, 2));
            Ach.pixelList.Add(new Vector2(4, 1));
            Ach.pixelList.Add(new Vector2(4, 0));
            Ach.pixelList.Add(new Vector2(1, 5));
            Ach.pixelList.Add(new Vector2(1, 4));
            Ach.pixelList.Add(new Vector2(1, 2));
            Ach.pixelList.Add(new Vector2(3, 5));
            Ach.pixelList.Add(new Vector2(3, 4));
            Ach.pixelList.Add(new Vector2(3, 2));
            Ach.pixelList.Add(new Vector2(2, 6));
            Ach.pixelList.Add(new Vector2(2, 2));

            Cch.pixelList.Clear();
            Cch.charName = "C";
            Cch.charWidth = 6;
            Cch.pixelList.Add(new Vector2(0, 4));
            Cch.pixelList.Add(new Vector2(0, 3));
            Cch.pixelList.Add(new Vector2(0, 2));
            Cch.pixelList.Add(new Vector2(1, 5));
            Cch.pixelList.Add(new Vector2(1, 1));
            Cch.pixelList.Add(new Vector2(2, 6));
            Cch.pixelList.Add(new Vector2(2, 0));
            Cch.pixelList.Add(new Vector2(3, 6));
            Cch.pixelList.Add(new Vector2(3, 0));
            Cch.pixelList.Add(new Vector2(4, 5));
            Cch.pixelList.Add(new Vector2(4, 1));



            ch1.pixelList.Clear();
            ch1.charName = "1";
            ch1.charWidth = 5;
            ch1.pixelList.Add(new Vector2(0, 0));
            ch1.pixelList.Add(new Vector2(0, 4));
            ch1.pixelList.Add(new Vector2(1, 0));
            ch1.pixelList.Add(new Vector2(1, 1));
            ch1.pixelList.Add(new Vector2(1, 2));
            ch1.pixelList.Add(new Vector2(1, 3));
            ch1.pixelList.Add(new Vector2(1, 4));
            ch1.pixelList.Add(new Vector2(1, 5));
            ch1.pixelList.Add(new Vector2(2, 0));
            ch1.pixelList.Add(new Vector2(2, 1));
            ch1.pixelList.Add(new Vector2(2, 2));
            ch1.pixelList.Add(new Vector2(2, 3));
            ch1.pixelList.Add(new Vector2(2, 4));
            ch1.pixelList.Add(new Vector2(2, 5));
            ch1.pixelList.Add(new Vector2(2, 6));
            ch1.pixelList.Add(new Vector2(3, 0));
            
            ch2.pixelList.Clear();
            ch2.charName = "2";
            ch2.charWidth = 6;
            ch2.pixelList.Add(new Vector2(0, 0));
            ch2.pixelList.Add(new Vector2(0, 4));
            ch2.pixelList.Add(new Vector2(0, 5));
            ch2.pixelList.Add(new Vector2(1, 0));
            ch2.pixelList.Add(new Vector2(1, 1));
            ch2.pixelList.Add(new Vector2(1, 5));
            ch2.pixelList.Add(new Vector2(1, 6));
            ch2.pixelList.Add(new Vector2(2, 0));
            ch2.pixelList.Add(new Vector2(2, 1));
            ch2.pixelList.Add(new Vector2(2, 2));
            ch2.pixelList.Add(new Vector2(2, 6));
            ch2.pixelList.Add(new Vector2(3, 0));
            ch2.pixelList.Add(new Vector2(3, 2));
            ch2.pixelList.Add(new Vector2(3, 3));
            ch2.pixelList.Add(new Vector2(3, 5));
            ch2.pixelList.Add(new Vector2(3, 6));
            ch2.pixelList.Add(new Vector2(4, 0));
            ch2.pixelList.Add(new Vector2(4, 3));
            ch2.pixelList.Add(new Vector2(4, 4));
            ch2.pixelList.Add(new Vector2(4, 5));

            ch3.pixelList.Clear();
            ch3.charName = "3";
            ch3.charWidth = 6;
            ch3.pixelList.Add(new Vector2(0, 1));
            ch3.pixelList.Add(new Vector2(0, 2));
            ch3.pixelList.Add(new Vector2(0, 4));
            ch3.pixelList.Add(new Vector2(0, 5));
            ch3.pixelList.Add(new Vector2(1, 0));
            ch3.pixelList.Add(new Vector2(1, 1));
            ch3.pixelList.Add(new Vector2(1, 5));
            ch3.pixelList.Add(new Vector2(1, 6));
            ch3.pixelList.Add(new Vector2(2, 0));
            ch3.pixelList.Add(new Vector2(2, 3));
            ch3.pixelList.Add(new Vector2(2, 6));
            ch3.pixelList.Add(new Vector2(3, 0));
            ch3.pixelList.Add(new Vector2(3, 1));
            ch3.pixelList.Add(new Vector2(3, 2));
            ch3.pixelList.Add(new Vector2(3, 3));
            ch3.pixelList.Add(new Vector2(3, 4));
            ch3.pixelList.Add(new Vector2(3, 5));
            ch3.pixelList.Add(new Vector2(3, 6));
            ch3.pixelList.Add(new Vector2(4, 1));
            ch3.pixelList.Add(new Vector2(4, 2));
            ch3.pixelList.Add(new Vector2(4, 4));
            ch3.pixelList.Add(new Vector2(4, 5));

            ch4.pixelList.Clear();
            ch4.charName = "4";
            ch4.charWidth = 6;
            ch4.pixelList.Add(new Vector2(0, 2));
            ch4.pixelList.Add(new Vector2(0, 3));
            ch4.pixelList.Add(new Vector2(0, 4));
            ch4.pixelList.Add(new Vector2(1, 2));
            ch4.pixelList.Add(new Vector2(1, 4));
            ch4.pixelList.Add(new Vector2(1, 5));
            ch4.pixelList.Add(new Vector2(2, 2));
            ch4.pixelList.Add(new Vector2(3, 0));
            ch4.pixelList.Add(new Vector2(3, 1));
            ch4.pixelList.Add(new Vector2(3, 2));
            ch4.pixelList.Add(new Vector2(3, 3));
            ch4.pixelList.Add(new Vector2(3, 4));
            ch4.pixelList.Add(new Vector2(3, 5));
            ch4.pixelList.Add(new Vector2(3, 6));
            ch4.pixelList.Add(new Vector2(4, 0));
            ch4.pixelList.Add(new Vector2(4, 1));
            ch4.pixelList.Add(new Vector2(4, 2));
            ch4.pixelList.Add(new Vector2(4, 3));
            ch4.pixelList.Add(new Vector2(4, 4));
            ch4.pixelList.Add(new Vector2(4, 5));
            ch4.pixelList.Add(new Vector2(4, 6));

            ch5.pixelList.Clear();
            ch5.charName = "5";
            ch5.charWidth = 5;
            ch5.pixelList.Add(new Vector2(3, 1));
            ch5.pixelList.Add(new Vector2(3, 2));
            ch5.pixelList.Add(new Vector2(3, 6));
            ch5.pixelList.Add(new Vector2(1, 0));
            ch5.pixelList.Add(new Vector2(1, 3));
            ch5.pixelList.Add(new Vector2(1, 6));
            ch5.pixelList.Add(new Vector2(2, 0));
            ch5.pixelList.Add(new Vector2(2, 1));
            ch5.pixelList.Add(new Vector2(2, 2));
            ch5.pixelList.Add(new Vector2(2, 3));
            ch5.pixelList.Add(new Vector2(2, 6));
            ch5.pixelList.Add(new Vector2(0, 0));
            ch5.pixelList.Add(new Vector2(0, 3));
            ch5.pixelList.Add(new Vector2(0, 4));
            ch5.pixelList.Add(new Vector2(0, 5));
            ch5.pixelList.Add(new Vector2(0, 6));

            ch6.pixelList.Clear();
            ch6.charName = "6";
            ch6.charWidth = 6;
            ch6.pixelList.Add(new Vector2(0, 1));
            ch6.pixelList.Add(new Vector2(0, 2));
            ch6.pixelList.Add(new Vector2(0, 3));
            ch6.pixelList.Add(new Vector2(1, 0));
            ch6.pixelList.Add(new Vector2(1, 3));
            ch6.pixelList.Add(new Vector2(1, 4));
            ch6.pixelList.Add(new Vector2(1, 5));
            ch6.pixelList.Add(new Vector2(2, 0));
            ch6.pixelList.Add(new Vector2(2, 3));
            ch6.pixelList.Add(new Vector2(2, 5));
            ch6.pixelList.Add(new Vector2(2, 6));
            ch6.pixelList.Add(new Vector2(3, 0));
            ch6.pixelList.Add(new Vector2(3, 1));
            ch6.pixelList.Add(new Vector2(3, 2));
            ch6.pixelList.Add(new Vector2(3, 3));
            ch6.pixelList.Add(new Vector2(3, 6));
            ch6.pixelList.Add(new Vector2(4, 1));
            ch6.pixelList.Add(new Vector2(4, 2));
            ch6.pixelList.Add(new Vector2(4, 6));

            ch7.pixelList.Clear();
            ch7.charName = "7";
            ch7.charWidth = 5;
            ch7.pixelList.Add(new Vector2(0, 5));
            ch7.pixelList.Add(new Vector2(0, 6));
            ch7.pixelList.Add(new Vector2(1, 0));
            ch7.pixelList.Add(new Vector2(1, 1));
            ch7.pixelList.Add(new Vector2(1, 5));
            ch7.pixelList.Add(new Vector2(1, 6));
            ch7.pixelList.Add(new Vector2(2, 1));
            ch7.pixelList.Add(new Vector2(2, 2));
            ch7.pixelList.Add(new Vector2(2, 3));
            ch7.pixelList.Add(new Vector2(2, 5));
            ch7.pixelList.Add(new Vector2(2, 6));
            ch7.pixelList.Add(new Vector2(3, 3));
            ch7.pixelList.Add(new Vector2(3, 4));
            ch7.pixelList.Add(new Vector2(3, 5));
            ch7.pixelList.Add(new Vector2(3, 6));

            ch8.pixelList.Clear();
            ch8.charName = "8";
            ch8.charWidth = 6;
            ch8.pixelList.Add(new Vector2(0, 1));
            ch8.pixelList.Add(new Vector2(0, 2));
            ch8.pixelList.Add(new Vector2(0, 4));
            ch8.pixelList.Add(new Vector2(0, 5));
            ch8.pixelList.Add(new Vector2(1, 0));
            ch8.pixelList.Add(new Vector2(1, 3));
            ch8.pixelList.Add(new Vector2(1, 6));
            ch8.pixelList.Add(new Vector2(2, 0));
            ch8.pixelList.Add(new Vector2(2, 3));
            ch8.pixelList.Add(new Vector2(2, 6));
            ch8.pixelList.Add(new Vector2(3, 0));
            ch8.pixelList.Add(new Vector2(3, 1));
            ch8.pixelList.Add(new Vector2(3, 2));
            ch8.pixelList.Add(new Vector2(3, 3));
            ch8.pixelList.Add(new Vector2(3, 4));
            ch8.pixelList.Add(new Vector2(3, 5));
            ch8.pixelList.Add(new Vector2(3, 6));
            ch8.pixelList.Add(new Vector2(4, 1));
            ch8.pixelList.Add(new Vector2(4, 2));
            ch8.pixelList.Add(new Vector2(4, 5));
            ch8.pixelList.Add(new Vector2(4, 6));

            ch9.pixelList.Clear();
            ch9.charName = "9";
            ch9.charWidth = 6;
            ch9.pixelList.Add(new Vector2(0, 3));
            ch9.pixelList.Add(new Vector2(0, 4));
            ch9.pixelList.Add(new Vector2(0, 5));
            ch9.pixelList.Add(new Vector2(1, 2));
            ch9.pixelList.Add(new Vector2(1, 3));
            ch9.pixelList.Add(new Vector2(1, 5));
            ch9.pixelList.Add(new Vector2(1, 6));
            ch9.pixelList.Add(new Vector2(2, 2));
            ch9.pixelList.Add(new Vector2(2, 6));
            ch9.pixelList.Add(new Vector2(3, 0));
            ch9.pixelList.Add(new Vector2(3, 1));
            ch9.pixelList.Add(new Vector2(3, 2));
            ch9.pixelList.Add(new Vector2(3, 3));
            ch9.pixelList.Add(new Vector2(3, 4));
            ch9.pixelList.Add(new Vector2(3, 5));
            ch9.pixelList.Add(new Vector2(3, 6));
            ch9.pixelList.Add(new Vector2(4, 0));
            ch9.pixelList.Add(new Vector2(4, 1));
            ch9.pixelList.Add(new Vector2(4, 2));
            ch9.pixelList.Add(new Vector2(4, 3));
            ch9.pixelList.Add(new Vector2(4, 4));
            ch9.pixelList.Add(new Vector2(4, 5));

            ch0.pixelList.Clear();
            ch0.charName = "0";
            ch0.charWidth = 6;
            ch0.pixelList.Add(new Vector2(1, 1));
            ch0.pixelList.Add(new Vector2(1, 1));
            ch0.pixelList.Add(new Vector2(1, 2));
            ch0.pixelList.Add(new Vector2(1, 5));
            ch0.pixelList.Add(new Vector2(1, 6));
            ch0.pixelList.Add(new Vector2(2, 0));
            ch0.pixelList.Add(new Vector2(2, 3));
            ch0.pixelList.Add(new Vector2(2, 6));
            ch0.pixelList.Add(new Vector2(3, 0));
            ch0.pixelList.Add(new Vector2(3, 1));
            ch0.pixelList.Add(new Vector2(3, 4));
            ch0.pixelList.Add(new Vector2(3, 5));
            ch0.pixelList.Add(new Vector2(3, 6));
            ch0.pixelList.Add(new Vector2(4, 1));
            ch0.pixelList.Add(new Vector2(4, 2));
            ch0.pixelList.Add(new Vector2(4, 3));
            ch0.pixelList.Add(new Vector2(4, 4));
            ch0.pixelList.Add(new Vector2(4, 5));
            ch0.pixelList.Add(new Vector2(0, 1));
            ch0.pixelList.Add(new Vector2(0, 2));
            ch0.pixelList.Add(new Vector2(0, 3));
            ch0.pixelList.Add(new Vector2(0, 4));
            ch0.pixelList.Add(new Vector2(0, 5));

            chs.pixelList.Clear();
            chs.charName = "sec";
            chs.charWidth = 4;
            chs.pixelList.Add(new Vector2(0, 0));
            chs.pixelList.Add(new Vector2(0, 2));
            chs.pixelList.Add(new Vector2(0, 3));
            chs.pixelList.Add(new Vector2(0, 4));
            chs.pixelList.Add(new Vector2(2, 0));
            chs.pixelList.Add(new Vector2(2, 2));
            chs.pixelList.Add(new Vector2(2, 1));
            chs.pixelList.Add(new Vector2(2, 4));
            chs.pixelList.Add(new Vector2(1, 0));
            chs.pixelList.Add(new Vector2(1, 2));
            chs.pixelList.Add(new Vector2(1, 4));

            chk.pixelList.Clear();
            chk.charName = "kN";
            chk.charWidth = 10;
            chk.pixelList.Add(new Vector2(0, 0));
            chk.pixelList.Add(new Vector2(0, 1));
            chk.pixelList.Add(new Vector2(0, 2));
            chk.pixelList.Add(new Vector2(0, 3));
            chk.pixelList.Add(new Vector2(0, 4));
            chk.pixelList.Add(new Vector2(1, 1));
            chk.pixelList.Add(new Vector2(2, 0));
            chk.pixelList.Add(new Vector2(2, 2));
            chk.pixelList.Add(new Vector2(4, 0));
            chk.pixelList.Add(new Vector2(4, 1));
            chk.pixelList.Add(new Vector2(4, 2));
            chk.pixelList.Add(new Vector2(4, 3));
            chk.pixelList.Add(new Vector2(4, 4));
            chk.pixelList.Add(new Vector2(4, 5));
            chk.pixelList.Add(new Vector2(5, 4));
            chk.pixelList.Add(new Vector2(5, 5));
            chk.pixelList.Add(new Vector2(6, 2));
            chk.pixelList.Add(new Vector2(6, 3));
            chk.pixelList.Add(new Vector2(7, 1));
            chk.pixelList.Add(new Vector2(7, 0));
            chk.pixelList.Add(new Vector2(8, 0));
            chk.pixelList.Add(new Vector2(8, 1));
            chk.pixelList.Add(new Vector2(8, 2));
            chk.pixelList.Add(new Vector2(8, 3));
            chk.pixelList.Add(new Vector2(8, 4));
            chk.pixelList.Add(new Vector2(8, 5));

            cht.pixelList.Clear();
            cht.charName = "t";
            cht.charWidth = 4;
            cht.pixelList.Add(new Vector2(0, 3));
            cht.pixelList.Add(new Vector2(1, 0));
            cht.pixelList.Add(new Vector2(1, 1));
            cht.pixelList.Add(new Vector2(1, 2));
            cht.pixelList.Add(new Vector2(1, 3));
            cht.pixelList.Add(new Vector2(1, 4));
            cht.pixelList.Add(new Vector2(2, 0));
            cht.pixelList.Add(new Vector2(2, 3));
        }

#endregion

        #region Editor GUI


        private void EditorWindowGUI(int windowID)
        {
            GUIStyle mySty = new GUIStyle(GUI.skin.button);
            mySty.normal.textColor = mySty.focused.textColor = Color.white;
            mySty.hover.textColor = mySty.active.textColor = Color.yellow;
            mySty.onNormal.textColor = mySty.onFocused.textColor = mySty.onHover.textColor = mySty.onActive.textColor = Color.green;
            mySty.padding = new RectOffset(8, 8, 8, 8);
            GUI.DragWindow(new Rect(0, 0, 10000, 20));

            if (simDuration == null)
                simDuration = "135";

            //top layer of the window GUI
            GUImodeInt = GUI.Toolbar(new Rect(10, 25, 660, 30), GUImodeInt, GUImodeStrings);
            GUI.Box(GUImainRect, GUImodeStrings[GUImodeInt]);

            //this is the "Stack" mode of the GUI, as used in the first iteration of the GUI
            if (GUImodeInt == 0)
            {
                simDuration = GUI.TextField(new Rect(170 + GUImainRect.xMin, 10 + GUImainRect.yMin, 40, 20), simDuration);
                GUI.Label(new Rect(GUImainRect.xMin + 10, GUImainRect.yMin + 10, 150, 20), "Simulation Run Time (s)");

                if (GUI.Button(new Rect(GUImainRect.xMin + 440, GUImainRect.yMin + 10, 200, 20), "Generate Thrust Graph"))
                {
                    stackThrustPredictPic(480, 640, Convert.ToInt16(simDuration));
                }
                GUI.Box(new Rect(GUImainRect.xMin + 10, GUImainRect.yMin + 40, graphImg.width, graphImg.height), graphImg);
            }

            //this is the "Segment" mode of the GUI, which allows one to customize burn times for each segment
            if (GUImodeInt == 1)
            {
                string segGUIname = "";
                KSF_SolidBoosterSegment SRB;

                //must populate the buttons with the most current fuel sources
                FuelStackSearcher(FuelSourcesList);

                //set length of internal text box to be 50 pix per segment
                segListLength = FuelSourcesList.Count * 50;

                //automatically select first segment as current GUI
                if (FuelSourcesList.Count > 0 && segCurrentGUI == null)
                {
                    segCurrentGUI = FuelSourcesList[0];
                }

                segListVector = GUI.BeginScrollView(new Rect(GUImainRect.xMin + 10, GUImainRect.yMin + 10, 120, 450), segListVector, new Rect(0, 0, 100, segListLength + 30));

                GUI.Label(new Rect(15, 0, 100, 15), "Nozzle End");
                for (int i = 0; i < FuelSourcesList.Count; i++)
                {
                    SRB = FuelSourcesList[i].GetComponent<KSF_SolidBoosterSegment>();
                    if (SRB.GUIshortName != "")
                        segGUIname = SRB.GUIshortName;
                    else
                        segGUIname = "Unknown";

                    if (GUI.Button(new Rect(5, i * 50 + 20, 95, 40), segGUIname))
                    {
                        if (FuelSourcesList[i] != segCurrentGUI)
                        {
                            segPriorGUI = segCurrentGUI;
                            segCurrentGUI = FuelSourcesList[i];
                            refreshSegGraph = true;
                        }
                    }
                }
                GUI.Label(new Rect(15, segListLength + 15, 100, 10), "Top o' Stack");
                GUI.EndScrollView();

                if (isAutoNode)
                {
                    if (GUI.Button(new Rect(GUImainRect.xMin + 10, GUImainRect.yMin + 470, 120, 20), "Go to Manual Node"))
                    {
                        isAutoNode = !isAutoNode;

                        refreshNodeInfo = true;
                    }
                }
                else
                {
                    if (GUI.Button(new Rect(GUImainRect.xMin + 10, GUImainRect.yMin + 470, 120, 20), "Go to Auto Nodes"))
                    {
                        isAutoNode = !isAutoNode;

                        refreshNodeInfo = true;
                    }
                }

                //copy/paste functionality
                if (segCurrentGUI != null)
                {
                    SRB = segCurrentGUI.GetComponent<KSF_SolidBoosterSegment>();

                    if (GUI.Button(new Rect(GUImainRect.xMin + 10, GUImainRect.yMin + 500, 55, 20), "Copy"))
                    {
                        klipboard = SRB.AnimationCurveToString(SRB.MassFlow);
                    }

                    if (GUI.Button(new Rect(GUImainRect.xMin + 75, GUImainRect.yMin + 500, 55, 20), "Paste"))
                    {
                        SRB.MassFlow = SRB.AnimationCurveFromString(klipboard);
                        SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);

                        refreshNodeInfo = true;
                        refreshSegGraph = true;
                    }
                }

                int width = 0;
                if (segCurrentGUI != null)
                {
                    SRB = segCurrentGUI.GetComponent<KSF_SolidBoosterSegment>();
                    width = SRB.MassFlow.length * 60;
                    if (refreshSegGraph)
                    {
                        System.Collections.Generic.List<Part> fSL = new System.Collections.Generic.List<Part>(); //filled once during OnActivate, is the master list
                        fSL.Add(segCurrentGUI);
                        segGraph = segThrustPredictPic(310, 490, Convert.ToInt16(simDuration), segCurrentGUI.GetComponent<KSF_SolidBoosterSegment>(), segCurrentGUI.GetResourceMass(), segCurrentGUI.mass, 5, 5, fSL);
                        refreshSegGraph = false; ;
                    }
                    GUI.Box(new Rect(GUImainRect.xMin + 140, GUImainRect.yMin + 190, 510, 330), segGraph);



                    if (isAutoNode)
                    {
                        //populate the list box
                        int listLength = 0;

                        listLength = 30 * (autoTypeList.Count + 1);

                        //draw the selection box to select which auto mode to use
                        autoListVector = GUI.BeginScrollView(new Rect(GUImainRect.xMin + 140, GUImainRect.yMin + 10, 200, 170), autoListVector, new Rect(0, 0, 180, listLength));

                        for (int i = 0; i < autoTypeList.Count; i++)
                        {
                            if (GUI.Button(new Rect(5, i * 30 + 5, 175, 20), autoTypeList[i].shortName()))
                            {
                                autoTypeCurrent = i;
                            }
                        }
                        GUI.EndScrollView();

                        GUI.Label(new Rect(GUImainRect.xMin + 350, GUImainRect.yMin + 10, 300, 40), autoTypeList[autoTypeCurrent].description());

                        autoTypeList[autoTypeCurrent].drawGUI(GUImainRect);


                        //this is the button to compute the segment values
                        if (GUI.Button(new Rect(GUImainRect.xMin + 500, GUImainRect.yMin + 160, 150, 20), "Evaluate"))
                        {
                            SRB.MassFlow = autoTypeList[autoTypeCurrent].computeCurve(this.acISP, segCurrentGUI);

                            SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);

                            refreshSegGraph = true;
                        }

                    }
                    else
                    {
                        nodeListVector = GUI.BeginScrollView(new Rect(GUImainRect.xMin + 140, GUImainRect.yMin + 10, 510, 40), nodeListVector, new Rect(0, 0, width + 70, 22));
                        if (segCurrentGUI != null)
                        {
                            SRB = segCurrentGUI.GetComponent<KSF_SolidBoosterSegment>();

                            for (int i = 0; i < SRB.MassFlow.length + 1; i++)
                            {
                                if (i < SRB.MassFlow.length)
                                {
                                    if (GUI.Button(new Rect(3 + i * 60, 4, 54, 18), "Node " + (i + 1)))
                                    {
                                        nodePriorNumber = nodeNumber;
                                        nodeNumber = i;
                                        refreshNodeInfo = true;
                                    }
                                }
                                else
                                {
                                    if (GUI.Button(new Rect(3 + i * 60, 4, 64, 18), "Add Node"))
                                    {
                                        nodePriorNumber = nodeNumber;
                                        nodeNumber = i;
                                        SRB.MassFlow.AddKey(SRB.MassFlow.keys[i - 1].time + 10, 0);
                                        refreshNodeInfo = true;
                                    }
                                }
                            }
                        }
                        GUI.EndScrollView();

                        //set up node editing tools

                        if (segCurrentGUI != null && refreshNodeInfo)
                        {
                            SRB = segCurrentGUI.GetComponent<KSF_SolidBoosterSegment>();
                            lbMsgBox = "The currently selected segment is " + SRB.GUIshortName + " and the current node is Node " + (nodeNumber + 1);

                            tbTime = SRB.MassFlow.keys[nodeNumber].time.ToString();
                            tbValue = SRB.MassFlow.keys[nodeNumber].value.ToString();
                            tbInTan = SRB.MassFlow.keys[nodeNumber].inTangent.ToString();
                            tbOutTan = SRB.MassFlow.keys[nodeNumber].outTangent.ToString();

                            refreshNodeInfo = false;
                        }
                        else
                        {
                            if (segCurrentGUI == null)
                            {
                                lbMsgBox = "Please select a segment on the left to begin editing";

                                tbTime = "";
                                tbValue = "";
                                tbInTan = "";
                                tbOutTan = "";
                            }
                        }

                        GUI.Label(new Rect(GUImainRect.xMin + 140, GUImainRect.yMin + 60, 510, 15), lbMsgBox);

                        GUI.Label(new Rect(GUImainRect.xMin + 162, GUImainRect.yMin + 80, 78, 15), "Node Time");
                        GUI.Label(new Rect(GUImainRect.xMin + 292, GUImainRect.yMin + 80, 78, 15), "Node Value");
                        GUI.Label(new Rect(GUImainRect.xMin + 422, GUImainRect.yMin + 80, 78, 15), "In Tangent");
                        GUI.Label(new Rect(GUImainRect.xMin + 552, GUImainRect.yMin + 80, 78, 15), "Out Tangent");

                        tbTime = GUI.TextField(new Rect(140 + GUImainRect.xMin, 100 + GUImainRect.yMin, 120, 20), tbTime);
                        tbValue = GUI.TextField(new Rect(270 + GUImainRect.xMin, 100 + GUImainRect.yMin, 120, 20), tbValue);
                        tbInTan = GUI.TextField(new Rect(400 + GUImainRect.xMin, 100 + GUImainRect.yMin, 120, 20), tbInTan);
                        tbOutTan = GUI.TextField(new Rect(530 + GUImainRect.xMin, 100 + GUImainRect.yMin, 120, 20), tbOutTan);

                        simDuration = GUI.TextField(new Rect(140 + GUImainRect.xMin, 160 + GUImainRect.yMin, 40, 20), simDuration);
                        GUI.Label(new Rect(GUImainRect.xMin + 190, GUImainRect.yMin + 160, 150, 20), "Simulation Run Time (s)");

                        //save node changes
                        if (segCurrentGUI != null)
                        {
                            SRB = segCurrentGUI.GetComponent<KSF_SolidBoosterSegment>();
                            if (GUI.Button(new Rect(GUImainRect.xMin + 490, GUImainRect.yMin + 160, 160, 20), "Save Changes to Node"))
                            {
                                Keyframe k = new Keyframe();
                                k.time = Convert.ToSingle(tbTime);
                                k.value = Convert.ToSingle(tbValue);
                                k.inTangent = Convert.ToSingle(tbInTan);
                                k.outTangent = Convert.ToSingle(tbOutTan);

                                SRB.MassFlow.RemoveKey(nodeNumber);
                                SRB.MassFlow.AddKey(k);

                                SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);
                                refreshNodeInfo = true;

                                refreshSegGraph = true;
                            }

                            if (GUI.Button(new Rect(GUImainRect.xMin + 140, GUImainRect.yMin + 130, 160, 20), "Smooth Tangents"))
                            {
                                SRB.MassFlow.SmoothTangents(nodeNumber, 1);

                                SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);
                            }

                            if (GUI.Button(new Rect(GUImainRect.xMin + 320, GUImainRect.yMin + 130, 160, 20), "Flat In Tan"))
                            {
                                float deltaY;
                                float deltaX;

                                deltaY = SRB.MassFlow.keys[nodeNumber].value - SRB.MassFlow.keys[nodeNumber - 1].value;
                                deltaX = SRB.MassFlow.keys[nodeNumber].time - SRB.MassFlow.keys[nodeNumber - 1].time;

                                tbInTan = (deltaY / deltaX).ToString();

                                SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);
                            }

                            if (GUI.Button(new Rect(GUImainRect.xMin + 490, GUImainRect.yMin + 130, 160, 20), "Flat Out Tan"))
                            {
                                float deltaY;
                                float deltaX;

                                deltaY = SRB.MassFlow.keys[nodeNumber + 1].value - SRB.MassFlow.keys[nodeNumber].value;
                                deltaX = SRB.MassFlow.keys[nodeNumber + 1].time - SRB.MassFlow.keys[nodeNumber].time;

                                tbOutTan = (deltaY / deltaX).ToString();

                                SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);
                            }

                            if (nodeNumber != 0)
                            {
                                if (GUI.Button(new Rect(GUImainRect.xMin + 380, GUImainRect.yMin + 160, 100, 20), "Remove Node"))
                                {
                                    SRB.MassFlow.RemoveKey(nodeNumber);

                                    SRB.BurnProfile = SRB.AnimationCurveToString(SRB.MassFlow);
                                    refreshNodeInfo = true;

                                    refreshSegGraph = true;
                                }
                            }



                            //highlight the selected booster segment in the editor

                            //segCurrentGUI.SetHighlight(true);
                        }
                    }
                }
            }
        }
        

        private void drawEditorGUI()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (Input.GetKeyDown("home") && this.part.stackIcon.highlightIcon)
                {
                    EditorGUIvisible = true;
                }

                if (Input.GetKeyDown("end") && this.part.stackIcon.highlightIcon)
                {
                    EditorGUIvisible = false;
                }

                if (EditorGUIvisible)
                {
                    GUI.skin = HighLogic.Skin;
                    EditorGUIPos = GUI.Window(1, EditorGUIPos, EditorWindowGUI, "AdvSRB Analyzer");
                }
            }
        }

        //private AnimationCurve autoSeg_ThrustForDuration()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_ThrustAtThrust()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_ExtraThrustForDuration()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_ExtraThrustAtExtraThrust()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_ExtraThrustForDurationAtGee()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_ExtraThrustForExtraThrustAtGee()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_SeperationBoostAtTimeAtThrust()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}

        //private AnimationCurve autoSeg_SeperationBoostAtTimeAtStackGee()
        //{
        //    AnimationCurve ac;
        //    return ac;
        //}




        /// <summary>
        /// This is a modified version of stackThrustPredictPic to take more arguments and to fit a single segment
        /// </summary>
        /// <param name="imgH">Desired hieght of the image, in pixels</param>
        /// <param name="imgW">Desired width of the image, in pixels</param>
        /// <param name="burnTime">Length of time to simulate burn, in seconds</param>
        /// <param name="AdvSRB_Module">the partmodule KSF_SolidBoosterSegment</param>
        /// <param name="fuelMass">mass of the SRB fuel on this segment, in KSP units</param>
        /// <param name="xOffset">margin on x axis (beginning and end)</param>
        /// <param name="yOffset">margin on y axis (bottom)</param>
        private Texture2D segThrustPredictPic(int imgH, int imgW, int burnTime, KSF_SolidBoosterSegment AdvSRB_Module, float fuelMass,float dryMass, int xOffset, int yOffset, System.Collections.Generic.List<Part> fSL)
        {
            //Now we set up the image maker
            Texture2D image = new Texture2D(imgW, imgH);
            int graphXmin = xOffset - 1;
            int graphXmax = imgW - xOffset;
            int graphYmin = yOffset - 1;
            //Set Up color variables

            Color brightG = Color.black;
            brightG.r = .3372549f;
            brightG.g = 1;

            Color lowG = Color.black;
            lowG.r = 0.042156863f;
            lowG.g = .125f;

            Color mediumG = Color.black;
            mediumG.r = 0.16862745f;
            mediumG.g = .5f;
           
            Color MassColor = Color.black;
            Color ThrustColor = Color.black;
            Color ExtraThrustColor = Color.black;

            if (graphExtraThrustColorB == 0 && graphExtraThrustColorG == 0 && graphExtraThrustColorR == 0)
                ExtraThrustColor = brightG;
            else
            {
                ExtraThrustColor.r = graphExtraThrustColorR;
                ExtraThrustColor.g = graphExtraThrustColorG;
                ExtraThrustColor.b = graphExtraThrustColorB;
            }

            if (graphMassColorB == 0 && graphMassColorG == 0 && graphMassColorR == 0)
                MassColor = brightG;
            else
            {
                MassColor.r = graphMassColorR;
                MassColor.g = graphMassColorG;
                MassColor.b = graphMassColorB;
            }

            if (graphThrustColorB == 0 && graphThrustColorG == 0 && graphThrustColorR == 0)
                ThrustColor = brightG;
            else
            {
                ThrustColor.r = graphThrustColorR;
                ThrustColor.g = graphThrustColorG;
                ThrustColor.b = graphThrustColorB;
            }


            //fuel arrays
            float[] segmentFuelArray = new float[1];
            segmentFuelArray[0] = fSL[0].GetResourceMass();


            //Step 4: Define text arrays
            populateCharArrays();

            //Step 5a: Define time markings (every 10 seconds gets a verticle line)
            System.Collections.Generic.List<int> timeLines = new System.Collections.Generic.List<int>();
            double xScale = (imgW - (2 * xOffset)) / (double)burnTime;
            calcTimeLines((float)burnTime, 10, timeLines, (float)xScale, xOffset);

            //Step 5b: Define vertical line markings (9 total to give 10 sections)
            System.Collections.Generic.List<int> horzLines = new System.Collections.Generic.List<int>();
            calcHorizLines(imgH - graphYmin, horzLines, 9, yOffset);

            //Step 6: Clear the background
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    image.SetPixel(x, y, Color.black);
                }
            }

            //Step 7a: Draw Time Lines
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    if (timeLines.Contains(x) && y > graphYmin)
                        image.SetPixel(x, y, lowG);
                }
            }

            //Step 7b: Draw Vert Lines
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    if (horzLines.Contains(y) && x < graphXmax && x > graphXmin)
                        image.SetPixel(x, y, lowG);
                }
            }


            //Step 7c: Draw Bounding Lines
            for (int y = 0; y < image.height; y++)
            {
                for (int x = 0; x < image.width; x++)
                {
                    if ((x == graphXmin | x == graphXmax) && (y > graphYmin | y == graphYmin))
                        image.SetPixel(x, y, mediumG);

                    if (y == graphYmin && graphXmax > x && graphXmin < x)
                        image.SetPixel(x, y, mediumG);
                }
            }

            //Step 8a: Populate graphArray
            double simStep = .1;
            i = 0;
            //double peakThrustTime = 0;
            double peakThrustAmt = 0;

            //set up the array for the graphs
            int graphArraySize = Convert.ToInt16(Convert.ToDouble(simDuration) / simStep);
            double[,] graphArray = new double[graphArraySize, 4];

            //one time setups
            graphArray[i, 0] = graphArray[i, 0] = stackMassFlow(fSL, (float)(i * simStep), segmentFuelArray, simStep);
            graphArray[i, 1] = fuelMass + dryMass;
            graphArray[i, 2] = 9.81 * acISP.Evaluate(1) * graphArray[i, 0];
            graphArray[i, 3] = graphArray[i, 2] - (graphArray[i, 1] * 9.81);
            
            //run the full simulation to generate the lookup table for the graph
            do
            {
                i++;
                graphArray[i, 0] = graphArray[i, 0] = stackMassFlow(fSL, (float)(i * simStep), segmentFuelArray, simStep);
                graphArray[i, 1] = graphArray[i - 1, 1] - (graphArray[i - 1, 0] * simStep);
                graphArray[i, 2] = 9.81 * acISP.Evaluate(1) * graphArray[i, 0];
                graphArray[i, 3] = graphArray[i, 2] - (graphArray[i, 1] * 9.81);

                if (graphArray[i, 2] > peakThrustAmt)
                {
                    peakThrustAmt = graphArray[i, 2];
                }
            } while (i + 1 < graphArraySize);

            //Step 8b: Make scales for the y axis
            double yScaleMass = 1;
            double yScaleThrust = 1;
            int usableY;
            usableY = imgH - yOffset;

            yScaleMass = usableY / (Mathf.CeilToInt((float)(graphArray[0, 1] / 10)) * 10);
            float inter;
            inter = (float)peakThrustAmt / 100;
            inter = Mathf.CeilToInt(inter);
            inter = inter * 100;
            inter = usableY / inter;
            yScaleThrust = inter;

            //Step 8c: Graph the mass
            int lineWidth = 3;
            for (int x = graphXmin; x < graphXmax; x++)
            {
                int fx = fGraph(xScale, x - xOffset, yScaleMass, graphArray, simStep, graphArraySize, 1, yOffset);

                for (int y = fx; y < fx + lineWidth; y++)
                {
                    image.SetPixel(x, y, MassColor);
                }
            }

            //Step 8d: Graph the thrust
            lineWidth = 3;
            for (int x = graphXmin; x < graphXmax; x++)
            {
                int fx = fGraph(xScale, x - xOffset, yScaleThrust, graphArray, simStep, graphArraySize, 2, yOffset);
                for (int y = fx; y < fx + lineWidth; y++)
                {
                    image.SetPixel(x, y, ThrustColor);
                }
            }

            //Step 8e: Graph the thrust extra
            lineWidth = 3;
            for (int x = graphXmin; x < graphXmax; x++)
            {
                int fx = fGraph(xScale, x - xOffset, yScaleThrust, graphArray, simStep, graphArraySize, 3, yOffset);
                for (int y = fx; y < fx + lineWidth; y++)
                {
                    image.SetPixel(x, y, ExtraThrustColor);
                }
            }

            //Step 9: Set up boxes for time
            string s;
            i = 0;
            int length = 0;
            int pos = 0;
            int startpos = 0;
            Texture2D tex;

            do
            {
                s = "";
                pos = timeLines[i];
                i++;
                s = i * 10 + " s";
                length = calcStringPixLength(convertStringToCharArray(s));
                startpos = Mathf.FloorToInt((float)pos - 0.5f * (float)length);
                Color[] region = image.GetPixels(startpos, 3 + yOffset, length, 11);
                for (int c = 0; c < region.Length; c++)
                {
                    region[c] = brightG;
                }
                image.SetPixels(startpos, 3 + yOffset, length, 11, region);
                tex = convertCharArrayToTex(convertStringToCharArray(s), length, brightG);
                Color[] region2 = tex.GetPixels();
                image.SetPixels(startpos + 2, 5 + yOffset, length - 4, 7, region2);
                length = 0;
            } while (i < timeLines.Count);


            //set up boxes for horizontal lines, mass first
            startpos = 0;
            length = 0;
            pos = 0;
            i = 0;
            do
            {
                s = "";
                pos = horzLines[i];
                i++;
                s = ((Mathf.CeilToInt((float)(graphArray[0, 1] / 10)) * i)).ToString() + " t";
                length = calcStringPixLength(convertStringToCharArray(s));
                startpos = Mathf.FloorToInt((float)pos - 5f);
                Color[] region = image.GetPixels(3 + xOffset, startpos, length, 11);
                for (int c = 0; c < region.Length; c++)
                {
                    region[c] = brightG;
                }
                image.SetPixels(3 + xOffset, startpos, length, 11, region);
                tex = convertCharArrayToTex(convertStringToCharArray(s), length, brightG);
                Color[] region2 = tex.GetPixels();
                image.SetPixels(5 + xOffset, startpos + 2, length - 4, 7, region2);
                length = 0;
            } while (i < horzLines.Count);

            //set up boxes for horizontal lines,  thrust
            startpos = 0;
            length = 0;
            pos = 0;
            i = 0;
            do
            {
                s = "";
                pos = horzLines[i];
                i++;
                s = ((Mathf.CeilToInt((float)(peakThrustAmt / 100)) * (i * 10))).ToString() + " k";
                length = calcStringPixLength(convertStringToCharArray(s));
                startpos = Mathf.FloorToInt((float)pos - 5f);
                Color[] region = image.GetPixels(imgW - (40 + xOffset), startpos, length, 11);
                for (int c = 0; c < region.Length; c++)
                {
                    region[c] = brightG;
                }
                image.SetPixels(imgW - (40 + xOffset), startpos, length, 11, region);
                tex = convertCharArrayToTex(convertStringToCharArray(s), length, brightG);
                Color[] region2 = tex.GetPixels();
                image.SetPixels(imgW - (38 + xOffset), startpos + 2, length - 4, 7, region2);
                length = 0;
            } while (i < horzLines.Count);

            image.Apply();
            return image;
        }

        #endregion
    }


}
