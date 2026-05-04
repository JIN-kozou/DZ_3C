using TMPro;
using UnityEngine;

namespace GameSoftCraft
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextVisibilityController : MonoBehaviour
    {
        public static bool Hide;

        [SerializeField, HideInInspector]
        TextMeshProUGUI _textMeshPro;

        private void OnValidate ()
        {
            if (_textMeshPro == null) {
                _textMeshPro = GetComponent<TextMeshProUGUI>();
            }
        }

        void FixedUpdate ()
        {
            var textCol = _textMeshPro.color;
            if (textCol.a == 0) return;

            if (!Hide) textCol.a = Mathf.Sin(Time.time * 5) * .5f + .75f;
            else textCol.a = Mathf.Max(textCol.a - .01f, 0f);

            _textMeshPro.color = textCol;
        }
    }
}
