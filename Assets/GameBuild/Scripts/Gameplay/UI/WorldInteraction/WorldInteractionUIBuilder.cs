using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DZ_3C.UI.WorldInteraction
{
  public static class WorldInteractionUIBuilder
  {
    public const string DefaultPromptPrefabPath = "Assets/GameBuild/Resources/WorldInteraction/WorldInteractionPrompt.prefab";
    public const string DefaultMarkerPrefabPath = "Assets/GameBuild/Resources/WorldInteraction/WorldInteractionMarker.prefab";

    public static WorldInteractionPromptView CreatePrompt(Transform parent, string name = "WorldInteractionPrompt")
    {
      var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
        typeof(CanvasGroup), typeof(WorldInteractionPromptBillboard), typeof(WorldInteractionPromptView));
      root.transform.SetParent(parent, false);

      var canvas = root.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.WorldSpace;
      canvas.sortingOrder = 130;

      var canvasGroup = root.GetComponent<CanvasGroup>();
      canvasGroup.blocksRaycasts = false;
      canvasGroup.interactable = false;

      var rect = root.GetComponent<RectTransform>();
      rect.sizeDelta = new Vector2(320f, 80f);

      var row = CreateUiObject<RectTransform>("Row", root.transform);
      var rowRect = row.GetComponent<RectTransform>();
      rowRect.anchorMin = Vector2.zero;
      rowRect.anchorMax = Vector2.one;
      rowRect.offsetMin = Vector2.zero;
      rowRect.offsetMax = Vector2.zero;

      var keyBlockGo = CreateUiObject<Image>("KeyBlock", row.transform);
      var keyBlock = keyBlockGo.GetComponent<Image>();
      var keyRect = keyBlockGo.GetComponent<RectTransform>();
      keyRect.anchorMin = new Vector2(0f, 0.5f);
      keyRect.anchorMax = new Vector2(0f, 0.5f);
      keyRect.pivot = new Vector2(0f, 0.5f);
      keyRect.anchoredPosition = new Vector2(0f, 0f);
      keyRect.sizeDelta = new Vector2(44f, 44f);
      keyBlock.color = new Color(1f, 1f, 1f, 0.95f);
      keyBlock.raycastTarget = false;

      var keyTextGo = CreateUiObject<TextMeshProUGUI>("KeyLabel", keyBlockGo.transform);
      var keyText = keyTextGo.GetComponent<TextMeshProUGUI>();
      keyText.text = "E";
      keyText.fontSize = 26f;
      keyText.fontStyle = FontStyles.Bold;
      keyText.alignment = TextAlignmentOptions.Center;
      keyText.color = Color.black;
      keyText.raycastTarget = false;
      Stretch(keyTextGo.GetComponent<RectTransform>());

      var textColumn = CreateUiObject<RectTransform>("TextColumn", row.transform);
      var textColumnRect = textColumn.GetComponent<RectTransform>();
      textColumnRect.anchorMin = new Vector2(0f, 0f);
      textColumnRect.anchorMax = new Vector2(1f, 1f);
      textColumnRect.offsetMin = new Vector2(52f, 0f);
      textColumnRect.offsetMax = new Vector2(0f, 0f);

      var actionGo = CreateUiObject<TextMeshProUGUI>("ActionText", textColumn.transform);
      var actionText = actionGo.GetComponent<TextMeshProUGUI>();
      actionText.text = string.Empty;
      actionText.fontSize = 22f;
      actionText.alignment = TextAlignmentOptions.MidlineLeft;
      actionText.color = Color.white;
      actionText.raycastTarget = false;
      var actionRect = actionGo.GetComponent<RectTransform>();
      actionRect.anchorMin = new Vector2(0f, 0.5f);
      actionRect.anchorMax = new Vector2(1f, 1f);
      actionRect.offsetMin = Vector2.zero;
      actionRect.offsetMax = Vector2.zero;

      var barRoot = CreateUiObject<RectTransform>("HoldBarRoot", textColumn.transform);
      var barRootRect = barRoot.GetComponent<RectTransform>();
      barRootRect.anchorMin = new Vector2(0f, 0f);
      barRootRect.anchorMax = new Vector2(1f, 0.45f);
      barRootRect.offsetMin = Vector2.zero;
      barRootRect.offsetMax = Vector2.zero;

      var barBgGo = CreateUiObject<Image>("HoldBarBg", barRoot.transform);
      var barBg = barBgGo.GetComponent<Image>();
      barBg.color = new Color(0f, 0f, 0f, 0.45f);
      barBg.raycastTarget = false;
      Stretch(barBgGo.GetComponent<RectTransform>());

      var fillArea = CreateUiObject<RectTransform>("FillArea", barRoot.transform);
      Stretch(fillArea.GetComponent<RectTransform>());

      var fillGo = CreateUiObject<Image>("Fill", fillArea.transform);
      var fill = fillGo.GetComponent<Image>();
      fill.color = new Color(1f, 1f, 1f, 0.9f);
      fill.raycastTarget = false;
      Stretch(fillGo.GetComponent<RectTransform>());

      var slider = barRoot.AddComponent<Slider>();
      slider.fillRect = fillGo.GetComponent<RectTransform>();
      slider.targetGraphic = fill;
      slider.direction = Slider.Direction.LeftToRight;
      slider.minValue = 0f;
      slider.maxValue = 1f;
      slider.value = 0f;
      slider.interactable = false;

      var view = root.GetComponent<WorldInteractionPromptView>();
      view.Bind(root, keyText, actionText, barRoot, slider);
      root.SetActive(false);
      return view;
    }

    public static WorldInteractionMarkerView CreateMarker(Transform parent, string name = "WorldInteractionMarker")
    {
      var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
        typeof(CanvasGroup), typeof(WorldInteractionPromptBillboard), typeof(WorldInteractionMarkerView));
      root.transform.SetParent(parent, false);

      var canvas = root.GetComponent<Canvas>();
      canvas.renderMode = RenderMode.WorldSpace;
      canvas.sortingOrder = 120;

      var canvasGroup = root.GetComponent<CanvasGroup>();
      canvasGroup.blocksRaycasts = false;
      canvasGroup.interactable = false;

      var billboard = root.GetComponent<WorldInteractionPromptBillboard>();
      billboard.WorldScale = 0.0025f;

      var rect = root.GetComponent<RectTransform>();
      rect.sizeDelta = new Vector2(24f, 24f);

      var dotGo = CreateUiObject<Image>("Dot", root.transform);
      var dot = dotGo.GetComponent<Image>();
      dot.color = new Color(1f, 0.92f, 0.35f, 0.95f);
      dot.raycastTarget = false;
      Stretch(dotGo.GetComponent<RectTransform>());

      var view = root.GetComponent<WorldInteractionMarkerView>();
      view.Bind(root, dot);
      root.SetActive(false);
      return view;
    }

    private static GameObject CreateUiObject<T>(string objectName, Transform parent) where T : Component
    {
      var go = new GameObject(objectName, typeof(RectTransform));
      go.transform.SetParent(parent, false);
      if (typeof(T) != typeof(RectTransform))
      {
        go.AddComponent<T>();
      }

      return go;
    }

    private static void Stretch(RectTransform rect)
    {
      rect.anchorMin = Vector2.zero;
      rect.anchorMax = Vector2.one;
      rect.offsetMin = Vector2.zero;
      rect.offsetMax = Vector2.zero;
    }
  }
}
