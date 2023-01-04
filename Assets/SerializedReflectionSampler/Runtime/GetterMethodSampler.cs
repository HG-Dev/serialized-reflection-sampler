using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SerializedReflectionSampler.Runtime
{
    public class GetterMethodSampler : MonoBehaviour
    {
        [SerializeField] private bool _sampleOnStart;
        public List<SamplerInfo> _samplers;

        private void Start()
        {
            if (_sampleOnStart)
            {
                EmitSample();
            }
        }

        public void EmitSample()
        {
            foreach (var sampler in _samplers)
            {
                if (sampler == null || !sampler.IsValid())
                {
#if DEBUG
                    Debug.LogWarning($"{nameof(GetterMethodSampler)}::{nameof(EmitSample)}() -- " +
                                     $"Sampler is invalid.");
#endif
                    continue;
                }

                if (sampler.TryRecreateMethodInfo(out var samplerMethod))
                {
                    var result = samplerMethod.Invoke(sampler.target, null);
                    sampler.listeners.Invoke(result.ToString());
                }
                else
                {
                    Debug.LogWarning($"{nameof(GetterMethodSampler)}::{nameof(EmitSample)}() -- " +
                                     $"Could not recreate getter {sampler.target.GetType()}.{sampler.sampleGetterName} to obtain sample.");
                }
            }
        }
    }

}
