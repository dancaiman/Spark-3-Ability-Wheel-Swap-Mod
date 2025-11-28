using System;
using System.Reflection;

namespace Harmony
{
    internal class HarmonyClass
    {
        private string v;

        public HarmonyClass(string v)
        {
            this.v = v;
        }

        internal void PatchAll(Assembly assembly)
        {
            throw new NotImplementedException();
        }
    }
}