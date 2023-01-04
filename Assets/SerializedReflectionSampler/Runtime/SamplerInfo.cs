using System.Reflection;

namespace SerializedReflectionSampler.Runtime
{
    [System.Serializable]
    public class SamplerInfo
    {
        public UnityEngine.Object target;
        public string sampleGetterName;
        public string sampleTypeName;
        public UnityEngine.Events.UnityEvent<string> listeners;

        private MethodInfo _methodCache;

        public bool IsValid()
        {
            return (target is UnityEngine.Component)
                   && !string.IsNullOrEmpty(sampleGetterName)
                   && !string.IsNullOrEmpty(sampleTypeName);
        }

        public bool TryRecreateMethodInfo(out MethodInfo sampler)
        {
            sampler = null;

            if (IsValid())
            {
                var targetType = target.GetType();
                sampler = targetType.GetMethod(sampleGetterName);

                return sampler != null;
            }

            return false;
        }
    }
}