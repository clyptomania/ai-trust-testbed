﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WSMGameStudio.RailroadSystem
{
    public class GameHandlerArea : MonoBehaviour
    {

        // shuttle relations
        public GameObject PseudoLocomotive;
        public GameObject Shuttle;
        public GameObject ShuttleTransitionCollidor;
        public GameObject OnBoarding;
        public GameObject ShuttleDoorsFront;
        public GameObject ShuttleDoorsBack;

        // public GameObject Glass;
        public Material GlassMaterial;
        public Material GlassRoofMaterial;


        // shuttle zones
        public GameObject SmallZone;
        public GameObject MediumZone;
        public GameObject BigZone;

        // camera relations
        public GameObject FPVCameraFolder;

        // car relations
        public GameObject CarColliderObeject;
        public GameObject ShuttleZoneCollider;
        // car routes
        public GameObject[] CarRoutes;

        // variables
        [SerializeField] private int RouteCounter = 0;
        private int RouteCounterOpponent = 1;
        public bool shuttleReadyToStart = false;
        public bool ShowOnBoaring = false;

        private float SpeedMin = 0;
        private float SpeedMax = 30;
        private float _currentSpeed;
        private float lastSpeed;
        private float acceleration;
        private float SpeedPercentage;
        private bool shuttleFirstBraking = true;
        private bool shuttleDrivingStraight = true;
        private bool shuttleFirstTimeInCurve = true;
        private bool shuttleFirstTimeStanding = true;
        private float timeFreeze;
        private float timeFreezeAccelerationBaviour;
        private float SmallZone_MinSize = 8;
        private float SmallZone_MaxSize = 8;
        private float MediumZone_MinSize = 8;
        private float MediumZone_MaxSize = 18;
        private float BigZone_MinSize = 8;
        private float BigZone_MaxSize = 30;

        private Renderer HMI_brake_v2_MeshRender;

        // HMI relations
        private float timingOfNotification = 0.0f;
        public bool preNotification;
        public GameObject BrakeNotificaitonDisplay;
        public bool brakeSignal;
        public GameObject HMIBrakeDisplay;

        // CSV Route Playlist
        public TextAsset csvFile;
        private string[] CSV_Lines;
        private string[] CSV_LineObjects;
        private string[] CSV_SingleLineObject;
        private string CSV_RouteNumber;
        private string CSV_Collision;
        private string CSV_FeedbackType;
        private string CSV_LastRound;

        // Testbed Events
        private bool Testbed_Collision = false;
        private bool Testbed_LastRound = false;


        // Test Subject
        public int TestSubjectNR = 0;
        private bool askingQuestions = false;

        private Color glassColor;
        private float GlassOpacity = 0.3f;
        private Color roofColor;
        private float RoofGlassOpacity = 0.8f;

        private Transform doorsFrontPosition;
        private Transform doorsBackPosition;


        private bool ReadyToEnterAgain = true;

        // =================================================================================================
        // ============================= Start is called before the first frame update =====================
        // =================================================================================================
        void Start()
        {
            // Correct internal test subject id number
            TestSubjectNR -= TestSubjectNR;

            glassColor = GlassMaterial.color;
            GlassMaterial.color = new Color(glassColor.r, glassColor.g, glassColor.b, GlassOpacity);
            roofColor = GlassRoofMaterial.color;
            GlassRoofMaterial.color = new Color(roofColor.r, roofColor.g, roofColor.b, RoofGlassOpacity);

            BrakeNotificaitonDisplay.GetComponent<MeshRenderer>().enabled = false;
            HMIBrakeDisplay.GetComponent<MeshRenderer>().enabled = false;
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().AccelerationRate = 0f;
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().BrakingDecelerationRate = 6f;
            StartCoroutine(OnBoardingQuery(2));
            timeFreeze = Time.time;
            Debug.Log("starting Time: " + timeFreeze);
            deactivateAllCars();
            CSV_splitLines();
        }
        // =================================================================================================
        // ============================= split CSV file in lines and provide new array =====================
        // =================================================================================================
        void CSV_splitLines()
        {
            CSV_Lines = csvFile.text.Split('\n');
        }
        void deactivateAllCars()
        {
            for (int i = 0; i < CarRoutes.Length; i++)
            {
                CarRoutes[i].SetActive(false);
            }
        }
        // =================================================================================================
        // ============================= Update is called once per frame ===================================
        // =================================================================================================
        async void Update()
        {
            hotKeys();
            doorHandler();
            checkCurrentSpeed();
            FPVcameraAngleCorrection();
            ShuttleAccelerationBaviour();
            ShuttleCurveBehaviour();
            ShuttleZones();
            ShuttleBrakeBehaviour();
            CheckBrakeFinished();
            if (askingQuestions)
            {
                questionnaireBreak();
            }
        }


        // =================================================================================================
        // ==================================== Open and Close Doors =======================================
        // =================================================================================================
        void doorHandler()
        {
            if (RouteCounter == 0)
            {
                StartCoroutine(openDoors(2));
            }
            if (Testbed_LastRound && ReadyToEnterAgain)
            {
                // if(RouteCounter == 2 && ReadyToEnterAgain){
                Debug.Log("Shuttle arrived at the end");
                StartCoroutine(openDoors(2));
            }
        }
        IEnumerator openDoors(float duration)
        {
            float counter = 0;
            doorsFrontPosition = ShuttleDoorsFront.transform;
            float doorsFrontIdealState = doorsFrontPosition.localPosition.x;
            doorsBackPosition = ShuttleDoorsBack.transform;
            float doorsBackIdealState = doorsBackPosition.localPosition.x;
            while (counter < duration)
            {
                counter += Time.deltaTime;
                // calculate Offset of Doors
                float DoorsOffset = Mathf.Lerp(0, 0.7f, counter / duration);
                // manipulate doors position
                ShuttleDoorsFront.transform.localPosition = new Vector3((doorsFrontIdealState + DoorsOffset), doorsFrontPosition.localPosition.y, doorsFrontPosition.localPosition.z);
                ShuttleDoorsBack.transform.localPosition = new Vector3((doorsBackIdealState - DoorsOffset), doorsBackPosition.localPosition.y, doorsBackPosition.localPosition.z);
                //Wait for a frame
                yield return null;
            }
        }
        IEnumerator closeDoors(float duration)
        {
            float counter = 0;
            doorsFrontPosition = ShuttleDoorsFront.transform;
            float doorsFrontIdealState = doorsFrontPosition.localPosition.x;
            doorsBackPosition = ShuttleDoorsBack.transform;
            float doorsBackIdealState = doorsBackPosition.localPosition.x;
            while (counter < duration)
            {
                counter += Time.deltaTime;
                // calculate Offset of Doors
                float DoorsOffset = Mathf.Lerp(0, 0.7f, counter / duration);
                // manipulate doors position
                ShuttleDoorsFront.transform.localPosition = new Vector3((doorsFrontIdealState - DoorsOffset), doorsFrontPosition.localPosition.y, doorsFrontPosition.localPosition.z);
                ShuttleDoorsBack.transform.localPosition = new Vector3((doorsBackIdealState + DoorsOffset), doorsBackPosition.localPosition.y, doorsBackPosition.localPosition.z);
                //Wait for a frame
                yield return null;
            }

            StartCoroutine(sceneFadeOut(3));
            yield return new WaitForSeconds(5);
            StartCoroutine(sceneFadeIn(1));
            // yield return new WaitForSeconds(1);
            // turnOnEngine();

        }
        // =================================================================================================
        // ============================= Break to ask questions ============================================
        // =================================================================================================
        void questionnaireBreak()
        {
            if (Input.GetKeyUp(KeyCode.Space))
            {

                if (RouteCounter == 1)
                {
                    askingQuestions = false;
                    StartCoroutine(closeDoors(2));
                }
                else
                {
                    // Debug.Log("Questionnaire Break is over");
                    askingQuestions = false;
                    StartCoroutine(sceneFadeIn(1));
                }
            }
        }
        IEnumerator sceneFadeIn(float duration)
        {
            // Check CSV File
            CSV_translateLineObjectIntoEvents();

            // yield return new WaitForSeconds(4);
            float counter = 0;
            //Get current color
            while (counter < duration)
            {
                counter += Time.deltaTime;
                //Fade glass from 1 to 0.3 
                float alpha = Mathf.Lerp(1, 0.3f, counter / duration);
                GlassMaterial.color = new Color(glassColor.r, glassColor.g, glassColor.b, alpha);
                //Fade roof glass from 0.3 to 1
                float roofAlpha = Mathf.Lerp(1, 0.8f, counter / duration);
                GlassRoofMaterial.color = new Color(roofColor.r, roofColor.g, roofColor.b, roofAlpha);
                //Wait for a frame
                yield return null;
            }
            turnOnEngine();
        }

        void turnOnEngine()
        {
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().EnginesOn = true;
            shuttleReadyToStart = true;
        }
        // =================================================================================================
        // ============================= Translate CSV lines --> define Testbed Events =====================
        // =================================================================================================
        void CSV_translateLineObjectIntoEvents()
        {
            Debug.Log("CSV Test Subject Nr.: " + (TestSubjectNR + 1));
            Debug.Log("CSV line number: " + TestSubjectNR + "CSV line: " + CSV_Lines[TestSubjectNR]);
            // split line number X into its objects
            CSV_LineObjects = CSV_Lines[TestSubjectNR].Trim().Split(","[0]);
            Debug.Log("Testbed: CSV RouteCounter / Line Object Nr.: " + RouteCounter + "Line Object: " + CSV_LineObjects[RouteCounter - 1]);

            // select Object X current CSV Line
            CSV_SingleLineObject = CSV_LineObjects[RouteCounter - 1].Trim().Split("-"[0]);
            // read Single Values
            CSV_RouteNumber = CSV_SingleLineObject[0];
            CSV_Collision = CSV_SingleLineObject[1];
            CSV_FeedbackType = CSV_SingleLineObject[2];
            CSV_LastRound = CSV_SingleLineObject[3];
            Debug.Log("TestbedEvent details: \n CSV_RouteNumber: " + CSV_RouteNumber + "\n CSV_Collision: " + CSV_Collision + "\n CSV_FeedbackType: " + CSV_FeedbackType + "\n CSV_LastRound: " + CSV_LastRound);

            if (CSV_Collision == "1")
            {
                Testbed_Collision = true;
            }
            else
            {
                Testbed_Collision = false;
            }
            if (CSV_LastRound == "1")
            {
                Testbed_LastRound = true;
            }
            else
            {
                Testbed_LastRound = false;
            }
            defineTestbedEvents();
        }
        // =================================================================================================
        // ============================= define Testbed Events =============================================
        // =================================================================================================
        void defineTestbedEvents()
        {
            // Is a Collision happening?
            if (Testbed_Collision)
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Collision is true");
                if ((RouteCounter) == 1) { CarRoutes[0].SetActive(true); }
                if ((RouteCounter) == 2) { CarRoutes[0].SetActive(true); }
                if ((RouteCounter) == 3) { CarRoutes[1].SetActive(true); }
                if ((RouteCounter) == 4) { CarRoutes[1].SetActive(true); }
                if ((RouteCounter) == 5) { CarRoutes[2].SetActive(true); }
                if ((RouteCounter) == 6) { CarRoutes[2].SetActive(true); }
                if ((RouteCounter) == 7) { CarRoutes[3].SetActive(true); }
                if ((RouteCounter) == 8) { CarRoutes[4].SetActive(true); }
                if ((RouteCounter) == 9) { CarRoutes[5].SetActive(true); }
                if ((RouteCounter) == 10) { CarRoutes[6].SetActive(true); }
                if ((RouteCounter) == 11) { CarRoutes[7].SetActive(true); }
                // if( (RouteCounter) == 12   ){   CarRoutes[8].SetActive(true);  }
                if ((RouteCounter) == 12) { CarRoutes[7].SetActive(true); }

            }
            else
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Collision is false");
            }

            // Is it the last Round? 
            if (Testbed_LastRound)
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Last Round is true");
            }
            else
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Last Round is false");
            }

            // What is the Feedback Type?
            if (CSV_FeedbackType == "0")
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Feedback Type: pre-notification --> false");
                brakeSignal = true;
                preNotification = false;
            }
            if (CSV_FeedbackType == "1")
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Feedback Type: pre-notification --> 0.5 sec before brake");
                brakeSignal = true;
                preNotification = true;
                timingOfNotification = 0.5f;
            }
            if (CSV_FeedbackType == "2")
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Feedback Type: pre-notification --> 1 sec before brake");
                brakeSignal = true;
                preNotification = true;
                timingOfNotification = 0.0f;
            }
            if (CSV_FeedbackType == "3")
            {
                Debug.Log("Route Nr.: " + (RouteCounter) + "TestbedEvents: Feedback Type: pre-notification --> nothing else");
                brakeSignal = false;
                preNotification = true;
                timingOfNotification = 0.0f;
            }
        }
        // =================================================================================================
        // ==================================== Correct FPV Camera Angle =====================================
        // =================================================================================================
        void FPVcameraAngleCorrection()
        {
            if (RouteCounter == RouteCounterOpponent)
            {
                FPVCameraFolder.transform.rotation = Quaternion.Euler(Shuttle.transform.rotation.eulerAngles.x, Shuttle.transform.rotation.eulerAngles.y, Shuttle.transform.rotation.eulerAngles.z);
                RouteCounterOpponent += 1;
            }
        }
        // =================================================================================================
        // ==================================== Check Current Speed =====================================
        // =================================================================================================
        void checkCurrentSpeed()
        {
            // check current speed of shuttle
            _currentSpeed = PseudoLocomotive.GetComponent<ILocomotive>().Speed_MPS;
            acceleration = ((_currentSpeed - lastSpeed) / Time.deltaTime);
            lastSpeed = _currentSpeed;
            // calculate current speed in percentage
            SpeedPercentage = (_currentSpeed - SpeedMin) / (SpeedMax - SpeedMin);
        }
        // =================================================================================================
        // ==================================== Shuttle Zone Behaviour =====================================
        // =================================================================================================
        void CheckBrakeFinished()
        {
            if (acceleration == 0)
            {
                HMIBrakeDisplay.GetComponent<MeshRenderer>().enabled = false;
            }
        }
        // =================================================================================================
        // ==================================== Shuttle Zone Behaviour =====================================
        // =================================================================================================
        void ShuttleZones()
        {
            // scale big zone
            var BigZone_Size = BigZone_MinSize + ((BigZone_MaxSize - BigZone_MinSize) * SpeedPercentage);
            BigZone.transform.localScale = new Vector3(BigZone_Size, 8, BigZone_Size);
            // scale medium zone
            var MediumZone_Size = MediumZone_MinSize + ((MediumZone_MaxSize - MediumZone_MinSize) * SpeedPercentage);
        }
        // =================================================================================================
        // ==================================== Handle Onboarding Events ===================================
        // =================================================================================================
        IEnumerator OnBoardingQuery(float duration)
        {
            if (ShowOnBoaring)
            {
                yield return new WaitForSeconds(6);
                Debug.Log("onboarding...");
                deactivateOnBoarding();
            }
            else
            {
                Debug.Log(" NO onboarding...");
                deactivateOnBoarding();
            }
        }
        void deactivateOnBoarding()
        {
            if (OnBoarding)
            {
                OnBoarding.SetActive(false);    // hide onBoarding displays
            }
        }
        // =================================================================================================
        // ==================================== Shuttle Acceleration Behaviour =============================
        // =================================================================================================
        void ShuttleAccelerationBaviour()
        {
            if (shuttleReadyToStart)
            {
                Debug.Log("shuttle accelerating");
                if (shuttleFirstTimeStanding)
                {
                    timeFreezeAccelerationBaviour = Time.time;
                    PseudoLocomotive.GetComponent<SplineBasedLocomotive>().AccelerationRate = 0;
                }
                shuttleFirstTimeStanding = false;

                var timeLimitAccelerationBaviour = timeFreezeAccelerationBaviour + 6;
                if (Time.time < timeLimitAccelerationBaviour)
                {
                    PseudoLocomotive.GetComponent<SplineBasedLocomotive>().AccelerationRate = ((-6.0f / (6f * 6f)) * (Time.time - timeLimitAccelerationBaviour) * (Time.time - timeLimitAccelerationBaviour)) + 6;
                    // Debug.Log("accelerationRate: " +  PseudoLocomotive.GetComponent<SplineBasedLocomotive>().AccelerationRate);
                }
                else if (Time.time > timeLimitAccelerationBaviour)
                {
                    shuttleFirstTimeStanding = true;
                    // PseudoLocomotive.GetComponent<SplineBasedLocomotive>().AccelerationRate = 0;
                    shuttleReadyToStart = false;
                }
            }
        }
        // =================================================================================================
        // ==================================== Shuttle Curve Behaviour ====================================
        // =================================================================================================
        void ShuttleCurveBehaviour()
        {
            float angle = PseudoLocomotive.transform.rotation.eulerAngles.y;
            // ============================================================== define curve behavior
            if (((angle / 90) % 1) != 0)
            {      // if rotation is not 90 or 180 or 270 and so on > reduce speed
                if (shuttleFirstTimeInCurve)
                {
                    shuttleDrivingStraight = false;
                    timeFreeze = Time.time;
                    Debug.Log("time freeze: " + timeFreeze);
                }
                shuttleFirstTimeInCurve = false;

                // define acceleration at curve start
                var timeLimit = timeFreeze + 2;
                if (Time.time < timeLimit)
                {
                    PseudoLocomotive.GetComponent<SplineBasedLocomotive>().MaxSpeed = 20f;       // Change velocity
                    PseudoLocomotive.GetComponent<SplineBasedLocomotive>().BrakingDecelerationRate = ((-6.0f / (4.0f)) * (Time.time - timeLimit) * (Time.time - timeLimit)) + 6;
                    // Debug.Log("BrakingDecelerationRate: " +  PseudoLocomotive.GetComponent<SplineBasedLocomotive>().BrakingDecelerationRate);
                }
                // define acceleration at curve end
                var secondTimeLimit = timeFreeze + 4;
                if (Time.time < secondTimeLimit && Time.time > timeLimit)
                {
                    PseudoLocomotive.GetComponent<SplineBasedLocomotive>().MaxSpeed = 30f;
                    PseudoLocomotive.GetComponent<SplineBasedLocomotive>().AccelerationRate = ((-6.0f / (4.0f)) * (Time.time - secondTimeLimit) * (Time.time - secondTimeLimit)) + 6;
                    // Debug.Log("BrakingDecelerationRate: " +  PseudoLocomotive.GetComponent<SplineBasedLocomotive>().BrakingDecelerationRate);
                }
            }
            if (((angle / 90) % 1) == 0)
            {     // if rotation is 90, 180, 270, 360,  and so on > max speed
                shuttleFirstTimeInCurve = true;
                shuttleDrivingStraight = true;
                // PseudoLocomotive.GetComponent<SplineBasedLocomotive>().MaxSpeed = 30f;  
            }
        }
        // =================================================================================================
        // ==================================== Shuttle Brake Behaviour ==================================
        // =================================================================================================
        void ShuttleBrakeBehaviour()
        {
            // check if Car Collider is triggered?
            if (ShuttleZoneCollider.GetComponent<ShuttleZoneDetectCar>().TriggerEnter)
            {
                StartCoroutine(activateNotificationSignal());
                StartCoroutine(activeBrake());
                StartCoroutine(activateEngine());
                ShuttleZoneCollider.GetComponent<ShuttleZoneDetectCar>().TriggerEnter = false;
            }
        }
        IEnumerator activateNotificationSignal()
        {
            // reduce speed
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().MaxSpeed = 20f;

            // change color of car
            var myColor = new Color(255, 0, 0, 0.1f);
            // car collider color
            // CarColliderObeject.GetComponent<Renderer>().material.color = myColor;

            yield return new WaitForSeconds(timingOfNotification);
            if (preNotification && shuttleFirstBraking)
            {
                BrakeNotificaitonDisplay.GetComponent<MeshRenderer>().enabled = true;
                Debug.Log("pre activateNotificationSignal signal on? " + BrakeNotificaitonDisplay.GetComponent<MeshRenderer>().enabled);
            }
            shuttleFirstBraking = false;

            // BrakeNotificaitonDisplay.GetComponent<MeshRenderer>().enabled = true;
            Debug.Log("notifictation signal: " + Time.time);
            // yield return new WaitForSeconds(0.5f);
            // BrakeNotificaitonDisplay.GetComponent<MeshRenderer>().enabled = false;
        }
        IEnumerator activeBrake()
        {
            yield return new WaitForSeconds(1f);
            BrakeNotificaitonDisplay.GetComponent<MeshRenderer>().enabled = false;
            HMIBrakeDisplay.GetComponent<MeshRenderer>().enabled = true;
            if (brakeSignal)
            {
                HMIBrakeDisplay.GetComponent<MeshRenderer>().enabled = true;
            }
            Debug.Log("activate brake: " + Time.time);
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().EnginesOn = false;
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().BrakingDecelerationRate = 40f;
        }
        IEnumerator activateEngine()
        {
            // yield on a new YieldInstruction that waits for 3 seconds.
            yield return new WaitForSeconds(3);

            // reset max speed to 30 kmh
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().MaxSpeed = 30f;
            // HMIBrakeDisplay.GetComponent<MeshRenderer>().enabled = false;

            Debug.Log("start engine: " + Time.time);
            shuttleReadyToStart = true;
            Debug.Log("ready to go? :" + shuttleReadyToStart);
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().EnginesOn = true;
            PseudoLocomotive.GetComponent<SplineBasedLocomotive>().BrakingDecelerationRate = 6f;

            shuttleFirstBraking = true;
        }
        // =================================================================================================
        // ==================================== Shuttle Entering GameHandlerArea ===========================
        // =================================================================================================
        void OnTriggerEnter(Collider other)
        {
            // check if Object with tag "TransitionCollidorTriggered" enters Collider
            // object with 'TransitionCollidorTriggered' Tag --> Shuttle --> TransitionCollidor
            if (ReadyToEnterAgain && other.CompareTag("TransitionCollidorTriggered"))
            {
                RouteCounter += 1;
                Debug.Log("add to route number: " + RouteCounter);
                // possible break
                askingQuestions = true;
                ReadyToEnterAgain = false;
            }
        }
        // =================================================================================================
        // ==================================== Shuttle Exiting GameHandlerArea ===========================
        // =================================================================================================
        void OnTriggerExit(Collider other)
        {
            // check if Object with tag "StartTransitionToNextRoute" exits Collider
            // if (other.CompareTag("Shuttle")){
            if (other.CompareTag("TransitionCollidorTriggered"))
            {
                deactivateAllCars();
                //prepare Shuttle for next route
                ReadyToEnterAgain = true;
                // turn off engine
                PseudoLocomotive.GetComponent<SplineBasedLocomotive>().EnginesOn = false;
                // fade in BlackBox around Shuttle to create transition to next route
                StartCoroutine(sceneFadeOut(3));
                // StartCoroutine(placeShuttleOnNextRoute(4));
            }
        }
        IEnumerator sceneFadeOut(float duration)
        {
            float counter = 0;
            while (counter < duration)
            {
                counter += Time.deltaTime;
                //Fade glass from 0 to 1
                float alpha = Mathf.Lerp(0.3f, 1, counter / duration);
                GlassMaterial.color = new Color(glassColor.r, glassColor.g, glassColor.b, alpha);
                //Fade roof glass from 0.8 to 1
                float roofAlpha = Mathf.Lerp(0.8f, 1, counter / duration);
                GlassRoofMaterial.color = new Color(roofColor.r, roofColor.g, roofColor.b, roofAlpha);
                //Wait for a frame
                yield return null;
            }
        }
        // =================================================================================================
        // ==================================== hot keys ===================================================
        // =================================================================================================
        void hotKeys()
        {
            if (Input.GetKeyUp(KeyCode.Alpha1)) { timingOfNotification = 1.0f; }
            if (Input.GetKeyUp(KeyCode.Alpha5)) { timingOfNotification = 0.5f; }
            if (Input.GetKeyUp(KeyCode.Alpha0)) { timingOfNotification = 0.0f; }
        }
        // =================================================================================================
        // =================================================================================================
    }
}

// coding on the dance floor
// armin pushing
// uni pc wsm and post processing already test
// testing script changes
// armins pc -> writing this line