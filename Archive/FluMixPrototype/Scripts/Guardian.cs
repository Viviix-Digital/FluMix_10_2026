using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FluMix
{
    public class Guardian : MonoBehaviour
    {
        public GuardianId Id;
        public Transform Muzzle;
        Vector3 _base;
        float _recoil;

        void Awake()
        {
            _base = transform.localPosition;
            if (Muzzle == null)
            {
                var m = new GameObject("Muzzle");
                m.transform.SetParent(transform, false);
                m.transform.localPosition = new Vector3(0.7f, 0.2f, 0f);
                Muzzle = m.transform;
            }
        }

        void Update()
        {
            float bob = Mathf.Sin(Time.time * 2.2f + (int)Id) * 0.06f;
            float kick = _recoil > 0f ? -0.18f * (_recoil / 0.35f) : 0f;
            transform.localPosition = _base + new Vector3(kick, bob, 0f);
            if (_recoil > 0f) _recoil -= Time.deltaTime;
        }

        public void Recoil(float duration)
        {
            _recoil = duration;
        }
    }
}
