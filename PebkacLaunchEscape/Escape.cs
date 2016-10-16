using System.Linq;
using UnityEngine;

namespace PebkacLaunchEscape 
{
    /// <summary>
    /// ModulePebkacLesEscape should be applied to the LES part that has an escape motor
    /// </summary>
    class ModulePebkacLesEscape : PartModule
    {
        private static string _myModTag = "[PEBKAC LES]";

        #region PartModules contained on various LES parts

        // the escape engine
        private ModuleEnginesFX _escapeEngine;

        // decoupler for the LES
        private ModuleDecouple _escapeDecoupler;
        
        #region Helpers

        private ModuleEnginesFX GetEscapeEngine()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesEscape.GetEscapeEngine", _myModTag));
            ModuleEnginesFX myEngine = null;

            try
            {
                myEngine = part.FindModulesImplementing<ModuleEnginesFX>().Where(e => e.engineID == "LES_Escape").FirstOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myEngine)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleEnginesFX on LES escape rocket!", _myModTag));
            }

            return myEngine;
        }

        private ModuleDecouple GetEscapeDecoupler()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesEscape.GetEscapeDecoupler", _myModTag));
            ModuleDecouple myDecoupler = null;

            try
            {
                myDecoupler = part.FindModulesImplementing<ModuleDecouple>().SingleOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myDecoupler)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleDecouple on LES escape tower!", _myModTag));
            }

            return myDecoupler;
        }

        #endregion

        #endregion
        
        public override void OnStart(StartState state)
        {
            Debug.Log(string.Format("{0} ModulePebkacLesEscape.OnStart", _myModTag));
            _escapeEngine = GetEscapeEngine();
            _escapeDecoupler = GetEscapeDecoupler();
            base.OnStart(state);
        }

        internal void Abort()
        {
            if (_escapeEngine != null)
            {
                _escapeEngine.Activate();
                Debug.Log(string.Format("{0} Escape motor fired!", _myModTag));
            }
        }

        internal void Decouple()
        {
            if (_escapeDecoupler != null)
            {
                _escapeDecoupler.Decouple();
                Debug.Log(string.Format("{0} LES decoupled from CM", _myModTag));
            }
        }

    }
}
