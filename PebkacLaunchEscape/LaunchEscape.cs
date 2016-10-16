using System.Linq;
using UnityEngine;

namespace PebkacLaunchEscape
{
    /// <summary>
    /// the ModulePebkacLesController manages all the various abort modes and associated behavior
    /// for the three sub-systems (Escape, Pitch-control and Jettison) of the legacy three-part version of the PEBKAC LES 
    /// </summary>
    public class ModulePebkacLesController : PartModule
    {
        private static string _myModTag = "[PEBKAC LES]";
        
        #region PartModules associated with the PEBKAC LES

        private ModulePebkacLesEscape _escape;
        private ModulePebkacLesJettison _jettison;
        private ModulePebkacLesPitchControl _pitchControl;

        #region Helpers

        private ModulePebkacLesEscape GetEscapePart()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesController.GetEscapePart", _myModTag));

            ModulePebkacLesEscape module = null;

            try
            {
                if (vessel != null)
                    module = vessel.FindPartModulesImplementing<ModulePebkacLesEscape>().FirstOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!module)
            {
                Debug.LogError(string.Format("{0} ERROR: Didn't find part on vessel with ModulePebkacLesEscape", _myModTag));
            }

            return module;

        }

        private ModulePebkacLesJettison GetJettisonPart()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesController.GetJettisonPart", _myModTag));
            ModulePebkacLesJettison module = null;

            try
            {
                if (vessel != null)
                    module = vessel.FindPartModulesImplementing<ModulePebkacLesJettison>().FirstOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!module)
            { 
                Debug.LogError(string.Format("{0} ERROR: Didn't find part on vessel with ModulePebkacLesJettison", _myModTag));
            }

            return module;

        }

        private ModulePebkacLesPitchControl GetPitchControlPart()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesController.GetPitchControlPart", _myModTag));
            ModulePebkacLesPitchControl module = null;

            try
            {
                if (vessel != null)
                    module = vessel.FindPartModulesImplementing<ModulePebkacLesPitchControl>().FirstOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!module)
            {
                Debug.LogError(string.Format("{0} ERROR: Didn't find part on vessel with ModulePebkacLesPitchControl", _myModTag));
            }

            return module;
        }
        
        #endregion 

        #endregion

        #region Abort status/modes

        // has the abort command been given?
        [KSPField(isPersistant = true, guiActive = false)]
        bool hasAborted = false;
        
        // have the canards been deployed?
        [KSPField(isPersistant = true, guiActive = false)]
        bool hasDeployed = false;

        // time when the abort command was given
        [KSPField(isPersistant = true, guiActive = false)]
        double abortTime = 0;

        // delay, in seconds, between abort command and deployment of canards
        [KSPField(isPersistant = true, guiActive = false)]
        public float canardDelaySeconds = 11;
        
        // time when the canard is scheduled to pop
        [KSPField(isPersistant = true, guiActive = false)]
        double deployTime = 0;

        // can the LES be jettisoned?
        private bool _canJettison = false;

        // has the LES been jettisoned?
        private bool _hasJettisoned = false;

        #endregion

        #region Vessel vector 

        private Vector3 _progradev;
        private Vector3 _yawComponent;
        private Vector3 _pitchComponent;
        private double _yaw;
        private double _pitch;

        #endregion
        
        public override void OnStart(StartState state)
        {
            Debug.Log(string.Format("{0} ModulePebkacLesController.OnStart", _myModTag));

            _escape = GetEscapePart();
            _jettison = GetJettisonPart();
            _pitchControl = GetPitchControlPart();

            base.OnStart(state);
        }

        // ...WAIT FOR IT!!!
        [KSPAction("Abort!", actionGroup = KSPActionGroup.Abort)]
        public void activateAbortAG(KSPActionParam param)
        {
            Debug.Log(string.Format("{0} User pressed abort key", _myModTag));

            if (hasAborted == false)
            {
                hasAborted = true;

                // set current game time to _abortTime variable:
                abortTime = Planetarium.GetUniversalTime();

                // set the game time when the canards are to pop:
                deployTime = abortTime + canardDelaySeconds;

                if (_escape != null) _escape.Abort();
                if (_pitchControl != null) _pitchControl.Abort();

            }
        }

        [KSPAction("Jettison")]
        public void activateJettison(KSPActionParam param)
        {
            Debug.Log(string.Format("{0} User pressed AG for jettison", _myModTag));

            if (_hasJettisoned == false)
            {
                Debug.Log(string.Format("{0} DoJettison", _myModTag));
                DoJettison();
            }
        }
        
        public override void OnUpdate()
        {
            // if we are in an abort status
            if (hasAborted && !hasDeployed)
            {
                if (Planetarium.GetUniversalTime() > deployTime)
                {
                    //deploy the canards if we have them
                    if (_pitchControl != null) _pitchControl.DeployCanards();
                    hasDeployed = true;
                }
            }

            if (hasAborted && hasDeployed && !_canJettison)
            {
                SetCanJettison();
            }

            if (hasAborted && hasDeployed && _canJettison && !_hasJettisoned)
            {
                DoJettison();
            }

            base.OnUpdate();
        }

        private void SetCanJettison()
        {

            // is the vessel pointed within +- 10 deg of retrograde?
            _progradev = vessel.GetSrfVelocity();
            _yawComponent = Vector3d.Exclude(vessel.GetTransform().forward, _progradev);
            _pitchComponent = Vector3d.Exclude(vessel.GetTransform().right, _progradev);
            _yaw = Vector3d.Angle(_yawComponent, vessel.GetTransform().up);
            _pitch = Vector3d.Angle(_pitchComponent, vessel.GetTransform().up);

            // if we have canards, wait until pointed retro, otherwise we're ready to jettison
            _canJettison = (!_pitchControl) || (_yaw > 175d && _pitch > 175d);
        }

        private void DoJettison()
        {
            if (_escape != null) _escape.Decouple();
            if (_jettison != null) _jettison.Jettison();

            _hasJettisoned = true;
        }

    }
}
