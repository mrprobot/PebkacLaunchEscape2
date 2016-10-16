
using UnityEngine;

namespace PebkacLaunchEscape2
{
    class ModulePebkacLiftingSurface : ModuleLiftingSurface
    {
        private static string _myModTag = "[PEBKAC LES v2]";

        [KSPField]
        public string transformName;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!string.IsNullOrEmpty(transformName))
            {
                Transform testTransform = part.FindModelTransform(transformName);
                if (testTransform != null)
                    baseTransform = testTransform;
                else
                    Debug.LogError(string.Format("{0} ERROR: could not find transform named '{transformName}'", _myModTag , transformName));
            }
        }


    }
}
