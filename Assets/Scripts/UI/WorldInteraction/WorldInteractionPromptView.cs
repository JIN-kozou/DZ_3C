using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.UI.WorldInteraction
{
  [DisallowMultipleComponent]
  public sealed class WorldInteractionPromptView : MonoBehaviour
  {
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text keyLabel;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private GameObject holdBarRoot;
    [SerializeField] private Slider holdBar;

    public void Bind(
      GameObject rootObject,
      TMP_Text key,
      TMP_Text action,
      GameObject barRoot,
      Slider bar)
    {
      root = rootObject;
      keyLabel = key;
      actionText = action;
      holdBarRoot = barRoot;
      holdBar = bar;
    }

    public void SetActiveVisual(bool visible)
    {
      if (root != null)
      {
        root.SetActive(visible);
        return;
      }

      gameObject.SetActive(visible);
    }

    public void SetActionText(string text)
    {
      if (actionText != null)
      {
        actionText.text = text ?? string.Empty;
      }
    }

    public void SetKeyLabel(string key)
    {
      if (keyLabel != null)
      {
        keyLabel.text = key ?? "E";
      }
    }

    public void SetHoldProgress(float normalized, bool showBar)
    {
      if (holdBarRoot != null)
      {
        holdBarRoot.SetActive(showBar);
      }

      if (holdBar != null)
      {
        holdBar.minValue = 0f;
        holdBar.maxValue = 1f;
        holdBar.value = Mathf.Clamp01(normalized);
      }
    }
  }
}
