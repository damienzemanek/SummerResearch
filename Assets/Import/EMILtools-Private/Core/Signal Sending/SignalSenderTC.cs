using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace EMILtools_Private.Core
{
    public abstract class SignalSenderTC<T> : MonoBehaviour
    {
        public InterfaceReference<ISignalReceiverTC<T>, MonoBehaviour> receiver;

        public bool presetData = false;
        public List<TaggedT> presets = new List<TaggedT>();

        [Serializable]
        public struct TaggedT
        {
            public string tag;
            public T data;
        }
    
        public void Send(TaggedT pckg) => receiver.Value?.Send(pckg.tag, pckg.data);
        public void SendPreset(int preset)
        {
            if (preset < 0 || preset >= presets.Count) { Debug.LogError($"Preset index {preset} is out of range for {name}!"); return; }
            Send(presets[preset]);
        }
    }
}
