using System.Linq;
using UnityEngine;

namespace PebkacLaunchEscape
{
    class ModulePebkacLesJettison : PartModule
    {
        private static string _myModTag = "[PEBKAC LES]";

        #region PartModules contained on various LES parts

        // the jettison engine
        private ModuleEnginesFX _jettisonEngine;

        #region Helpers

        private ModuleEnginesFX GetJettisonEngine()
        {
            Debug.Log(string.Format("{0} ModulePebkacLesJettison.GetJettisonEngine", _myModTag));
            ModuleEnginesFX myEngine = null;

            try
            {
                myEngine = part.FindModulesImplementing<ModuleEnginesFX>().Where(e => e.engineID == "LES_Jet").FirstOrDefault();
            }
            catch (System.Exception x)
            {
                Debug.Log(string.Format("{0} ERROR: {1}", _myModTag, x.Message));
            }

            if (!myEngine)
            {
                // this shouldn't happen under normal circumstances
                Debug.LogError(string.Format("{0} ERROR: Didn't find ModuleEnginesFX on LES jettison rocket!", _myModTag));
            }

            return myEngine;
        }

        #endregion

        #endregion

        public override void OnStart(StartState state)
        {
            Debug.Log(string.Format("{0} ModulePebkacLesJettison.OnStart", _myModTag));
            _jettisonEngine = GetJettisonEngine();
            base.OnStart(state);
        }
        
        internal void Jettison()
        {
            if (_jettisonEngine != null)
            {
                // fire the jettison motor
                _jettisonEngine.Activate();
                Debug.Log(string.Format("{0} LES jettison engine fired!", _myModTag));
            }
        }

    }
}
