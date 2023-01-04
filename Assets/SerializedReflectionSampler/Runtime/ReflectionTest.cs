using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SerializedReflectionSampler.Runtime
{
    public class ReflectionTest : MonoBehaviour
    {
        [SerializeField] Component _target;

        [ContextMenu("Next Target")]
        void NextTarget()
        {
            var comps = _target.gameObject.GetComponents<Component>().ToList();
            var index = comps.IndexOf(_target);
            index++;
            if (index >= comps.Count)
                index = 0;
            _target = comps[index];
        }

        [ContextMenu("Get Methods")]
        void ExtractMethodInfo()
        {
            var type = _target.GetType();
            var methods = type.GetMethods();

            foreach (var method in methods.Where(m => !m.IsSpecialName))
            {
                Debug.Log(method.Name);
            }
        }

        [ContextMenu("Get Properties")]
        void ExtractPropertyInfo()
        {
            var type = _target.GetType();
            var properties = type.GetProperties();

            foreach (var property in properties.Where(m => !m.IsSpecialName))
            {
                Debug.Log(property.Name);
            }
        }
    }
}