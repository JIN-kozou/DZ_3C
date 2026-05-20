using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.UI.WorldInteraction
{
  [DisallowMultipleComponent]
  public sealed class WorldInteractionMarkerView : MonoBehaviour
  {
    [SerializeField] private GameObject root;
    [SerializeField] private Image markerImage;

    public void Bind(GameObject rootObject, Image image)
    {
      root = rootObject;
      markerImage = image;
    }

    public void SetVisible(bool visible)
    {
      SetVisible(visible, null);
    }

    public void SetVisible(bool visible, Transform activationRoot)
    {
      if (root == null)
      {
        root = gameObject;
      }

      root.SetActive(visible);
      if (!visible)
      {
        return;
      }

      Transform current = root.transform.parent;
      while (current != null)
      {
        current.gameObject.SetActive(true);
        if (activationRoot != null && current == activationRoot)
        {
          break;
        }

        current = current.parent;
      }
    }

    public void SetTint(Color color)
    {
      if (markerImage != null)
      {
        markerImage.color = color;
      }
    }
  }
}
