using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: enter game, credits overlay, quit.
/// Wire references on MainMenuCanvas in the Inspector.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("场景")]
    [Tooltip("点击「進入太空」后加载的场景名（需在 Build Settings 中）。")]
    [SerializeField] private string gameplaySceneName = "Timeline";

    [Header("按钮")]
    [SerializeField] private Button enterSpaceButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button creditsBackButton;

    [Header("标题（主创时隐藏）")]
    [SerializeField] private GameObject[] titleElements;

    [Header("主创面板")]
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private TMP_Text creditsBodyText;

    [TextArea(6, 20)]
    [SerializeField] private string creditsText =
        "主創人員\n\n" +
        "製作人 · \n" +
        "程序 · \n" +
        "美术 · \n" +
        "音效 · \n";

    private void Awake()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (creditsBodyText != null)
            creditsBodyText.text = creditsText;

        enterSpaceButton?.onClick.AddListener(OnEnterSpaceClicked);
        creditsButton?.onClick.AddListener(OnCreditsClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
        creditsBackButton?.onClick.AddListener(OnCreditsBackClicked);
    }

    private void OnDestroy()
    {
        enterSpaceButton?.onClick.RemoveListener(OnEnterSpaceClicked);
        creditsButton?.onClick.RemoveListener(OnCreditsClicked);
        quitButton?.onClick.RemoveListener(OnQuitClicked);
        creditsBackButton?.onClick.RemoveListener(OnCreditsBackClicked);
    }

    public void OnEnterSpaceClicked()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName))
        {
            Debug.LogError("[MainMenuUI] gameplaySceneName 未配置。", this);
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnCreditsClicked()
    {
        SetTitleVisible(false);
        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void OnCreditsBackClicked()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);
        SetTitleVisible(true);
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SetTitleVisible(bool visible)
    {
        if (titleElements == null)
            return;

        for (int i = 0; i < titleElements.Length; i++)
        {
            if (titleElements[i] != null)
                titleElements[i].SetActive(visible);
        }
    }
}
