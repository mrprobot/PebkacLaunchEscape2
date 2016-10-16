using System.Linq;
using UnityEngine;

namespace PebkacLaunchEscape
{
    class ModulePebkacLesPitchControl : PartModule
    {
        private static string _myModTag = "[PEBKAC LES]";

        #region PartModules that should be contained on this part

        // the pitch control engine
        private ModuleEnginesFX _pitchEngine;

        // the animation for the canards
        private ModuleAnimateGeneric _deployAnimation;

        // the lifting surface for the deployed canards
        private ModuleLiftingSurface _liftingSurface;

        #region Helpers

        private ModuleEnginesFX GetPitchEngine()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesPitchControl.GetPitchEngine", _myModTag));
            ModuleEnginesFX myEngine = null;

            try
            {
                myEngine = part.FindModulesImplementing<ModuleEnginesFX>().Where(e => e.engineID == "LES_Nose").FirstOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myEngine)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleEnginesFX on LES nosecone!", _myModTag));
            }

            return myEngine;
        }

        private ModuleAnimateGeneric GetDeployAnimation()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesPitchControl.GetDeployAnimation", _myModTag));
            ModuleAnimateGeneric myAnimation = null;

            try
            {
                myAnimation = part.FindModulesImplementing<ModuleAnimateGeneric>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myAnimation)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleAnimateGeneric on LES nosecone!", _myModTag));
            }

            return myAnimation;
        }

        private ModuleLiftingSurface GetLiftingSurface()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesPitchControl.GetLiftingSurface", _myModTag));
            ModuleLiftingSurface myLiftingSurface = null;

            try
            {
                myLiftingSurface = part.FindModulesImplementing<ModuleLiftingSurface>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myLiftingSurface)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleLiftingSurface on LES nosecone!", _myModTag));
            }

            return myLiftingSurface;
        }

        #endregion

        #endregion

        public override void OnStart(StartState state)
        {
            Debug.Log(string.Format("{0} ModulePebkacLesPitchControl.OnStart", _myModTag));
            _pitchEngine = GetPitchEngine();
            _deployAnimation = GetDeployAnimation();
            _liftingSurface = GetLiftingSurface();

            base.OnStart(state);
        }

        internal void Abort()
        {
            if (_pitchEngine != null)
            {
                _pitchEngine.Activate();
                Debug.Log(string.Format("{0} Pitch control engine fired!", _myModTag));
            }
        }
        
        internal void DeployCanards()
        {
            // start the animation and adjust aero for deployed canards
            if (_deployAnimation != null)
            {
                _deployAnimation.Toggle();
            }

            if (_liftingSurface != null)
            {
                _liftingSurface.deflectionLiftCoeff = 1f;
                _liftingSurface.useInternalDragModel = true;
            }

            Debug.Log(string.Format("{0} Deploying canards", _myModTag));
        }

    }
}
