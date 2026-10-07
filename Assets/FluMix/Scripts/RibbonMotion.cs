using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FluMix
{
    public class RibbonMotion : MonoBehaviour
    {
        void Update()
        {
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / 3f) * 4f);
            var heart = transform.Find("Heart");
            if (heart != null)
            {
                float s = 1f + 0.06f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f));
                heart.localScale = Vector3.one * s;
            }
        }
    }
}
