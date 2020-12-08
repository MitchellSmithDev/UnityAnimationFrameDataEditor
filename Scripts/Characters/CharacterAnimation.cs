using System.Collections.Generic;
using UnityEngine;

namespace Characters
{
    [System.Serializable]
    public class CharacterAnimation : ScriptableObject
    {
        public bool loop = true;
        public List<CharacterFrame> frames = new List<CharacterFrame>();
    }

    [System.Serializable]
    public class CharacterFrame
    {
        public Sprite sprite;
        public int duration = 3;

        public List<HitTriggerData> hitTriggers = new List<HitTriggerData>();
        public List<HurtTriggerData> hurtTriggers = new List<HurtTriggerData>();

        public CharacterFrame() { }

        public CharacterFrame Clone()
        {
            CharacterFrame clone = new CharacterFrame();
            clone.sprite = sprite;
            clone.duration = duration;
            for(int i = 0; i < hitTriggers.Count; i++)
                clone.hitTriggers.Add(hitTriggers[i].Clone());
            for(int i = 0; i < hurtTriggers.Count; i++)
                clone.hurtTriggers.Add(hurtTriggers[i].Clone());
            return clone;
        }
    }

    [System.Serializable]
    public class TriggerData
    {
        public ColliderData collider;

        public TriggerData(ColliderType type)
        {
            collider = new ColliderData(type);
        }

        protected TriggerData() { }
    }

    [System.Serializable]
    public class HitTriggerData : TriggerData
    {
        public HitTriggerData(ColliderType type) : base(type) { }
        private HitTriggerData() { }

        public HitTriggerData Clone()
        {
            HitTriggerData clone = new HitTriggerData();
            clone.collider = collider.Clone();
            return clone;
        }
    }

    [System.Serializable]
    public class HurtTriggerData : TriggerData
    {
        public HurtTriggerData(ColliderType type) : base(type) { }
        private HurtTriggerData() { }

        public HurtTriggerData Clone()
        {
            HurtTriggerData clone = new HurtTriggerData();
            clone.collider = collider.Clone();
            return clone;
        }
    }
}