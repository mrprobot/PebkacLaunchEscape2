using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PebkacLaunchEscape2
{
    /// <summary>
    /// the ModulePebkacLesController2 manages all the various abort modes and associated behavior
    /// for the three sub-systems (Escape, Pitch-control and Jettison) of the single-part versions of the PEBKAC LES 
    /// </summary>
    class ModulePebkacLesController2 : PartModule
    {
        private static string _myModTag = "[PEBKAC LES v2]";

        // does this LES have any pitch control components?
        [KSPField(isPersistant = true, guiActive = false)]
        public bool hasPitchControl = false;

        // does the LES wait until the pod is pointed retro before jettisoning itself?
        [KSPField(isPersistant = true, guiActive = false)]
        public bool jettisonsToRetro = false;

        #region Abort status/modes

        // has the abort command been given?
        [KSPField(isPersistant = true, guiActive = false)]
        public bool hasAborted = false;

        // have the canards been deployed?
        [KSPField(isPersistant = true, guiActive = false)]
        public bool hasDeployed = false;

        // delay, in seconds, between abort command and deployment of canards
        [KSPField(isPersistant = true, guiActive = false)]
        public float canardDelaySeconds = 11;

        // time when the canard is scheduled to pop
        [KSPField(isPersistant = true, guiActive = false)]
        public double canardDeployTime = 0;

        // for escape systems without pitch control, the delay
        // in seconds, between abort command and jettison of LES
        [KSPField(isPersistant = true, guiActive = false)]
        public float jettisonDelaySeconds = 10;

        // for escape systems without pitch control, the
        // time when the LES is scheduled to jettison
        [KSPField(isPersistant = true, guiActive = false)]
        public double jettisonTime = 0;
        
        // has the LES been jettisoned?
        [KSPField(isPersistant = true, guiActive = false)]
        public bool hasJettisoned = false;

        #endregion

        #region Engine Modules

        private string _escapeEngineID = "LES_Escape";
        private double _escapeEngineStartTime = 0;

        //// GUI enabled for tuning purposes
        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Escape Motor Runtime")]
        //[UI_FloatRange(minValue = 1, stepIncrement = 0.1f, maxValue = 5)]
        public float escapeEngineRunTime = 3.3f;

        private string _pitchEngineID = "LES_PitchControl";
        private double _pitchEngineStartTime = 0;

        //// GUI enabled for tuning purposes
        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Motor Runtime")]
        //[UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, maxValue = 2)]
        public float pitchEngineRunTime = 0.7f;

        private string _jettisonEngineID = "LES_Jettison";
        private double _jettisonEngineStartTime = 0;

        //// GUI enabled for tuning purposes
        //[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Jettison Motor Runtime")]
        //[UI_FloatRange(minValue = 0.5f, stepIncrement = 0.1f, maxValue = 2)]
        public float jettisonEngineRunTime = 1.2f;

        private ModuleEngines _escapeEngine;
        private ModuleEngines _pitchEngine;
        private ModuleEngines _jettisonEngine;

        #endregion

        #region Decouple

        private ModuleDecouple _lesDecoupler;

        #endregion

        #region Aerodynamics

        private double _maxFuel;
        private Vector3 _origComOffset;
        private Vector3 _origColOffset;

        // GUI enabled for tuning purposes
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Ballast")]
        [UI_FloatRange(minValue = 60, stepIncrement = 1.0f, maxValue = 200)]
        public float comMult = 100.0f;

        //the animation for the canards
        private ModuleAnimateGeneric _deployAnimation;

        // the lifting surface for the deployed canards
        private ModuleLiftingSurface _liftingSurface;

        private ModuleAnimateGeneric GetDeployAnimation()
        {
            Debug.Log(string.Format("{0} GetDeployAnimation", _myModTag));
            ModuleAnimateGeneric myAnimation = null;

            try
            {
                myAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.LogError(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myAnimation)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: {1}", _myModTag, "Didn't find ModuleAnimateGeneric on LES!"));
            }

            return myAnimation;
        }

        private ModuleLiftingSurface GetLiftingSurface()
        {
            Debug.Log(string.Format("{0} GetLiftingSurface", _myModTag));
            ModuleLiftingSurface myLiftingSurface = null;

            try
            {
                myLiftingSurface = part.FindModulesImplementing<ModuleLiftingSurface>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.LogError(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myLiftingSurface)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleLiftingSurface on LES!", _myModTag));
            }

            return myLiftingSurface;
        }

        #endregion

        #region Part lifecycle and other events

        public override void OnStart(StartState state)
        {
            List<ModuleEngines> engines = part.FindModulesImplementing<ModuleEngines>();
            foreach (ModuleEngines e in engines)
            {
                if (e.engineID == _escapeEngineID)
                {
                    _escapeEngine = e;
                }
                else if (e.engineID == _pitchEngineID)
                {
                    _pitchEngine = e;
                }
                else if (e.engineID == _jettisonEngineID)
                {
                    _jettisonEngine = e;
                }
                foreach (BaseAction a in e.Actions)
                {
                    a.active = false;
                }
                foreach (BaseField f in e.Fields)
                {
                    f.guiActive = false;
                    f.guiActiveEditor = false;
                }
                foreach (BaseEvent ev in e.Events)
                {
                    ev.guiActive = false;
                    ev.guiActiveEditor = false;
                }
            }
            
            if (hasPitchControl)
            {
                // set up the variables used to shift aerodynamics
                _maxFuel = part.Resources["SolidFuel"].maxAmount;
                _origComOffset = part.CoMOffset;
                _origColOffset = part.CoLOffset;

                // set up the variables used by code for simming the canards
                _deployAnimation = GetDeployAnimation();
                _liftingSurface = GetLiftingSurface();
            }

            // get the decoupler
            _lesDecoupler = part.FindModuleImplementing<ModuleDecouple>();

            if (_lesDecoupler == null)
            {
                Debug.LogError(string.Format("{0}: {1}: {2}", _myModTag, part.name, "Did not find a decoupler on the LES!"));
            }

        }

        // fires when part is staged
        public override void OnActive()
        {
            Debug.Log(string.Format("{0}: {1}: {2}", _myModTag, part.name, "Calling DoJettison from OnActive()"));
            DoJettison();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight || vessel.HoldPhysics)
            {
                return;
            }

            if (_escapeEngine != null && _escapeEngine.EngineIgnited)
            {
                if (Planetarium.GetUniversalTime() - _escapeEngineStartTime >= escapeEngineRunTime)
                {
                    _escapeEngine.Shutdown();
                }

                if (hasPitchControl)
                {
                    // in real life, the LES had a huge chunk of depleted uranium ballast in its nosecone
                    // in the game, when we model the LES using a single part the center of mass shifts 
                    // too far aft as solid fuel is burned, compared to real life. This makes it 
                    // difficult to maintain stable flight

                    // while the escape engine is running, adjust the center of mass offset
                    var comFactor = (_maxFuel - part.Resources["SolidFuel"].amount) / comMult;
                    var newComY = _origComOffset.y + (float)comFactor;

                    part.CoMOffset.Set(_origComOffset.x, newComY, _origComOffset.z);
                }
            }

            if (_pitchEngine != null && _pitchEngine.EngineIgnited)
            {
                if (Planetarium.GetUniversalTime() - _pitchEngineStartTime >= pitchEngineRunTime)
                {
                    _pitchEngine.Shutdown();
                }
            }

            if (_jettisonEngine != null && _jettisonEngine.EngineIgnited)
            {
                if (Planetarium.GetUniversalTime() - _jettisonEngineStartTime >= jettisonEngineRunTime)
                {
                    _jettisonEngine.Shutdown();
                }
            }

            if (hasAborted && hasPitchControl && !hasDeployed && (Planetarium.GetUniversalTime() >= canardDeployTime))
            {
                DeployCanards();
            }

            if (hasAborted && CheckCanAutoJettison())
            {
                Debug.Log(string.Format("{0}: {1}: {2}", _myModTag, part.name, "Calling DoJettison from FixedUpdate()"));
                DoJettison();
            }
        }

        #endregion

        #region Abort!

        [KSPAction("Abort!", actionGroup = KSPActionGroup.Abort)]
        public void ActivateAbortAG(KSPActionParam param)
        {
            if (hasAborted == false)
            {
                if (vessel.altitude < 3000 && hasPitchControl)
                {
                    ActivateAbortMode1aAction(param);
                }
                else
                {
                    ActivateAbortMode1bAction(param);
                }

                canardDeployTime = hasPitchControl ? _escapeEngineStartTime + canardDelaySeconds : 0;
                jettisonTime = !hasPitchControl ? _escapeEngineStartTime + jettisonDelaySeconds : 0;

                Debug.Log(string.Format("{0}: {1}: Setting Jettison Time, _escapeEngineStartTime = {2}, jettisonTime = {3}", _myModTag, part.name, _escapeEngineStartTime, jettisonTime));
            }
        }

        private void ActivateAbortMode1aAction(KSPActionParam param)
        {
            Debug.Log(string.Format("{0} Abort Mode 1A Activated", _myModTag));
            hasAborted = true;
            ActivateEscapeEngine();
            ActivatePitchEngine();
        }

        private void ActivateAbortMode1bAction(KSPActionParam param)
        {
            Debug.Log(string.Format("{0} Abort Mode 1B Activated", _myModTag));
            hasAborted = true;
            ActivateEscapeEngine();
        }

        private void ActivateEscapeEngine()
        {
            
            if (_escapeEngineStartTime <= 0)
            {
                _escapeEngineStartTime = Planetarium.GetUniversalTime();
                if (_escapeEngine != null) _escapeEngine.Activate();
            }
        }

        private void ActivatePitchEngine()
        {
            if (_pitchEngineStartTime <= 0)
            {
                _pitchEngineStartTime = Planetarium.GetUniversalTime();
                if (_pitchEngine != null) _pitchEngine.Activate();
            }
        }

        private void DeployCanards()
        {
            // start the animation and adjust aero for deployed canards
            Debug.Log(string.Format("{0} Deploying canards", _myModTag));
            if (_deployAnimation != null)
            {
                _deployAnimation.Toggle();
            }

           // Debug.Log(string.Format("{0} re-setting CoMOffset in Y axis from {1} to {2}", _myModTag, part.CoMOffset.y.ToString(), _origComOffset.y.ToString()));

            part.CoMOffset.Set(_origComOffset.x, _origComOffset.y, _origComOffset.z);
            part.CoLOffset.Set(_origColOffset.x, _origColOffset.y + 1.45f, _origColOffset.z);

            if (_liftingSurface != null && part.Modules.Contains<ModuleLiftingSurface>())
            {
                _liftingSurface.useInternalDragModel = true;
                _liftingSurface.deflectionLiftCoeff = 0.35f;
            }

            hasDeployed = true;
        }

        #endregion

        #region Jettison

        private Vector3 _progradev;
        private Vector3 _yawComponent;
        private Vector3 _pitchComponent;
        private double _yaw;
        private double _pitch;

        // fires when AG is triggered
        [KSPAction("Jettison LES")]
        public void ActivateJettisonAG(KSPActionParam param)
        {
            DoJettison();
        }

        [KSPEvent(guiName = "Jettison LES", guiActive = true)]
        public void DoJettison()
        {
            Debug.Log(string.Format("{0}: {1}: {2}", _myModTag, part.name, "Do Jettison!"));

            if (_lesDecoupler != null)
            {
                try
                {
                    _lesDecoupler.Decouple();
                }
                catch (System.Exception x)
                {
                    Debug.LogError(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
                }
            }
            else
            {
                Debug.LogError(string.Format("{0}: {1}: {2}", _myModTag, part.name, "Did not find a decoupler on the LES!"));
            }

            ActivateJettisonEngine();

            Debug.Log(string.Format("{0} LES jettisoned", _myModTag));
            hasJettisoned = true;

        }

        private bool CheckCanAutoJettison()
        {
            if (!hasAborted || hasJettisoned)
            {
                return false;
            }
            else if (hasPitchControl || jettisonsToRetro)
            {
                // is the vessel pointed within 5 degrees of retrograde?
                _progradev = vessel.GetSrfVelocity();
                _yawComponent = Vector3d.Exclude(vessel.GetTransform().forward, _progradev);
                _pitchComponent = Vector3d.Exclude(vessel.GetTransform().right, _progradev);
                _yaw = Vector3d.Angle(_yawComponent, vessel.GetTransform().up);
                _pitch = Vector3d.Angle(_pitchComponent, vessel.GetTransform().up);

                return _yaw >= 175d && _pitch >= 175d;
            }
            else
            {
                // has the scheduled time passed?
                //var jettisonTimePassed = Planetarium.GetUniversalTime() >= jettisonTime;
                //Debug.Log(string.Format("{0}: {1}: {2} {3}", _myModTag, part.name, "In CheckCanAutoJettison(): jettisonTimePassed = ", jettisonTimePassed));
                
                return Planetarium.GetUniversalTime() >= jettisonTime;
            }
        }

        private void ActivateJettisonEngine()
        {
            if (_jettisonEngineStartTime <= 0)
            {
                _jettisonEngineStartTime = Planetarium.GetUniversalTime();
                _jettisonEngine.Activate();
            }
        }

        #endregion

    }
}
