using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#else
using Input = UnityEngine.Input;
#endif

namespace DebugMenuKit
{
    /// <summary>
    /// Two-level in-game debug menu driven by [DebugMethod] attributes. Drop the DebugMenu
    /// prefab into your boot scene, then either:
    ///  - add your own MonoBehaviour with [DebugMethod("Category", "Name")] methods onto the
    ///    prefab instance (or any child) — they are picked up automatically, or
    ///  - call DebugMenuManager.Instance.AddButton("Category", "Name", () => ...) at runtime.
    /// Toggle visibility with a 4-finger tap on device, or F1 in the Editor / development builds.
    /// </summary>
    public class DebugMenuManager : MonoBehaviour
    {
        public static DebugMenuManager Instance;

        [Header("Prefab")]
        public GameObject debugRowPrefab;

        [Header("Public Object")]
        public Button btnOpenDebugMenu;
        public GameObject buttonsMenu1;
        public GameObject buttonsMenu2;
        public GameObject scroll1;
        public GameObject scroll2;
        public GameObject extraMenu;
        public Text fpsText;
        public Text versionText;
        public Text buildNumberText;
        public TextMeshProUGUI debugLogText;
        public RectTransform topLeft;
        public GameObject debugLogConsole;

        [Header("Options")]
        [Tooltip("Start with the open button hidden; reveal with a 4-finger tap or F1.")]
        public bool hideOnStart = false;
        [Tooltip("Categories listed first, in this order; the rest follow alphabetically.")]
        public List<string> categoryOrder = new List<string> { "User" };
        [Tooltip("Optional panel for the Status > TestExtraView button; unassigned shows a built-in red dummy panel.")]
        public GameObject testExtraViewPrefab;

        private const int FINGER_COUNT_TO_ENABLE_DEBUG = 4;
        private const float PANEL_RIGHT_EDGE_X = -71f;
        private const float PANEL_GAP = 1f;
        private const float PANEL_MAX_WIDTH = 480f;
        private const float PANEL_EXTRA_WIDTH = 0f;

        private readonly List<DebugMenuEntry> customEntries = new List<DebugMenuEntry>();
        private List<DebugMenuEntry> entries = new List<DebugMenuEntry>();
        private bool enableFPS = false;
        private float deltaTime = 0.0f;
        private GameObject lastExtraMenu;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            btnOpenDebugMenu.onClick.RemoveAllListeners();
            btnOpenDebugMenu.onClick.AddListener(EventOpenDebugMenu);

            scroll1.SetActive(false);
            scroll2.SetActive(false);
            HideExtraMenu();

            if (fpsText != null)
            {
                fpsText.gameObject.SetActive(enableFPS);
            }
            if (versionText != null)
            {
                versionText.text = "v" + Application.version;
            }
            if (buildNumberText != null)
            {
                buildNumberText.text = Application.isEditor ? "editor" : Application.buildGUID;
            }

            RefreshEntries();

            if (hideOnStart)
            {
                SetMenuButtonVisible(false);
            }
        }

        void Update()
        {
            if (enableFPS && fpsText != null)
            {
                deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
                fpsText.text = "FPS:" + Mathf.FloorToInt(1.0f / deltaTime);
            }

            HandleToggleInput();
        }

        // Works with either Active Input Handling setting: the new Input System package
        // (ENABLE_INPUT_SYSTEM) or the legacy Input Manager.
        private void HandleToggleInput()
        {
#if ENABLE_INPUT_SYSTEM
            Touchscreen touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                int fingers = 0;
                bool releasedThisFrame = false;
                var touches = touchscreen.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    bool released = touches[i].press.wasReleasedThisFrame;
                    if (touches[i].press.isPressed || released)
                    {
                        fingers++;
                    }
                    releasedThisFrame |= released;
                }
                if (releasedThisFrame && fingers == FINGER_COUNT_TO_ENABLE_DEBUG)
                {
                    ToggleDebugMenu();
                }
            }

            if ((Application.isEditor || Debug.isDebugBuild)
                && Keyboard.current != null && Keyboard.current.f1Key.wasReleasedThisFrame
                && !scroll2.activeSelf)
            {
                ToggleDebugMenu();
            }
#else
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Ended && Input.touchCount == FINGER_COUNT_TO_ENABLE_DEBUG)
                {
                    ToggleDebugMenu();
                    break;
                }
            }

            if ((Application.isEditor || Debug.isDebugBuild)
                && Input.GetKeyUp(KeyCode.F1) && !scroll2.activeSelf)
            {
                ToggleDebugMenu();
            }
#endif
        }

        /// <summary>
        /// Rescans this GameObject and its children for MonoBehaviours with [DebugMethod]
        /// methods, and merges in buttons added via <see cref="AddButton"/>. Called
        /// automatically in Start; call again after adding components at runtime.
        /// </summary>
        public void RefreshEntries()
        {
            var found = new List<DebugMenuEntry>();

            MonoBehaviour[] sources = GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour source in sources)
            {
                if (source == null)
                {
                    continue;
                }

                MethodInfo[] methods = source.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (MethodInfo method in methods)
                {
                    if (method.GetCustomAttributes(typeof(DebugMethodAttribute), false) is
                        { Length: 1 } attrs && attrs[0] is DebugMethodAttribute attr)
                    {
                        found.Add(new DebugMenuEntry(attr.Category, attr.Name, method, source));
                    }
                }
            }

            found.AddRange(customEntries);

            entries = found
                .OrderBy(e => categoryOrder.IndexOf(e.category) >= 0
                    ? categoryOrder.IndexOf(e.category)
                    : int.MaxValue)
                .ThenBy(e => e.category)
                .ThenBy(e => e.name)
                .ToList();
        }

        /// <summary>Adds a debug button at runtime without writing a component.</summary>
        public void AddButton(string category, string name, Action action)
        {
            customEntries.Add(new DebugMenuEntry(category, name, action));
            RefreshEntries();
        }

        /// <summary>
        /// Shows a custom panel (your own prefab) in the extra-menu area — e.g. a level
        /// selector or amount input. The previous panel is hidden. Returns the instance.
        /// </summary>
        public GameObject ShowExtraMenu(GameObject prefab)
        {
            extraMenu.SetActive(true);
            if (lastExtraMenu != null)
            {
                lastExtraMenu.SetActive(false);
            }
            lastExtraMenu = Instantiate(prefab, extraMenu.transform);
            FitExtraMenuPosition();
            return lastExtraMenu;
        }

        public void HideExtraMenu()
        {
            foreach (Transform child in extraMenu.transform)
            {
                Destroy(child.gameObject);
            }
            lastExtraMenu = null;
            extraMenu.SetActive(false);
        }

        public void Log(string log)
        {
            if (debugLogText != null && debugLogText.gameObject.activeSelf)
            {
                debugLogText.text += $"<br>{log}";
            }
        }

        [DebugMethod("Status", "HideDebugMenu")]
        public void Status_HideDebugMenu()
        {
            Destroy(gameObject);
        }

        [DebugMethod("Status", "ShowHideFPS")]
        public void Status_ShowHideFPS()
        {
            enableFPS = !enableFPS;
            if (fpsText != null)
            {
                fpsText.gameObject.SetActive(enableFPS);
            }
        }

        [DebugMethod("Status", "ShowDebugConsole")]
        public void ShowDebugConsole()
        {
            if (debugLogConsole == null)
            {
                Debug.LogWarning("[DebugMenu] No debug console assigned (debugLogConsole)");
                return;
            }
            debugLogConsole.SetActive(!debugLogConsole.activeSelf);
        }

        // Shows a panel in the extra-menu area so testers can eyeball whether the ExtraView
        // placement collides with the two menu panels on the current device. Shows
        // testExtraViewPrefab when assigned, otherwise a runtime-built red dummy panel.
        [DebugMethod("Status", "TestExtraView")]
        public void Status_TestExtraView()
        {
            if (testExtraViewPrefab != null)
            {
                ShowExtraMenu(testExtraViewPrefab);
                return;
            }

            GameObject template = new GameObject("ExtraViewTestPanel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = (RectTransform)template.transform;
            panelRect.sizeDelta = new Vector2(350f, 250f);
            template.GetComponent<Image>().color = new Color(0.9f, 0.2f, 0.2f, 0.85f);

            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(template.transform, false);
            RectTransform labelRect = (RectTransform)labelGo.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;
            Text label = labelGo.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "ExtraView test\n350 x 250";

            ShowExtraMenu(template);
            Destroy(template);
        }

        [DebugMethod("User", "ClearUserData")]
        public void ClearUserData()
        {
            ClearPlayerPrefs();
            ClearCache();
            ClearGameData();
        }

        [DebugMethod("User", "CopyDeviceInfo")]
        public void CopyDeviceInfo()
        {
            string info = $"DeviceId: {SystemInfo.deviceUniqueIdentifier} | v{Application.version}";
            GUIUtility.systemCopyBuffer = info;
            Debug.Log($"[Copied] {info}");
        }

        private void ToggleDebugMenu()
        {
            bool active = btnOpenDebugMenu.gameObject.activeSelf;
            SetMenuButtonVisible(!active);
        }

        private void SetMenuButtonVisible(bool visible)
        {
            btnOpenDebugMenu.gameObject.SetActive(visible);
            if (topLeft != null)
            {
                topLeft.gameObject.SetActive(visible);
            }
        }

        private void EventOpenDebugMenu()
        {
            if (scroll1.activeSelf)
            {
                scroll1.SetActive(false);
            }
            else
            {
                scroll1.SetActive(true);
                ShowMenu1();
            }

            scroll2.SetActive(false);
            HideExtraMenu();
        }

        private void ShowMenu1()
        {
            for (int i = 0; i < buttonsMenu1.transform.childCount; i++)
            {
                GameObject old = buttonsMenu1.transform.GetChild(i).gameObject;
                old.SetActive(false); // Destroy is deferred; hide now so layout ignores it
                Destroy(old);
            }

            var seenCategories = new HashSet<string>();
            foreach (DebugMenuEntry entry in entries)
            {
                if (seenCategories.Add(entry.category))
                {
                    GameObject go = Instantiate(debugRowPrefab, buttonsMenu1.transform);
                    DebugRowScript row = go.GetComponent<DebugRowScript>();
                    string category = entry.category;
                    row.Init(category, () => OnMenu1Click(category));
                }
            }

            FitPanelsToContent();
        }

        private void OnMenu1Click(string category)
        {
            for (int i = 0; i < buttonsMenu2.transform.childCount; i++)
            {
                GameObject old = buttonsMenu2.transform.GetChild(i).gameObject;
                old.SetActive(false); // Destroy is deferred; hide now so layout ignores it
                Destroy(old);
            }

            if (lastExtraMenu != null)
            {
                lastExtraMenu.SetActive(false);
            }

            scroll2.SetActive(true);

            foreach (DebugMenuEntry entry in entries)
            {
                if (entry.category == category)
                {
                    GameObject go = Instantiate(debugRowPrefab, buttonsMenu2.transform);
                    DebugRowScript row = go.GetComponent<DebugRowScript>();
                    DebugMenuEntry captured = entry;
                    row.Init(entry.name, () => captured.Invoke());
                }
            }

            FitPanelsToContent();
        }

        // Each panel hugs its widest row (rows force-expand to match), then the two panels
        // are re-anchored so they stay side by side left of the open button. The prefab's
        // panels carry a localScale (1.5 in the original design), and scaling expands
        // rightward from the (0,1) pivot — so positioning must use the SCALED width or the
        // panels overlap each other.
        private void FitPanelsToContent()
        {
            FitPanelWidth(scroll1, buttonsMenu1);
            FitPanelWidth(scroll2, buttonsMenu2);

            RectTransform panel1 = (RectTransform)scroll1.transform;
            float scaledWidth1 = panel1.rect.width * panel1.localScale.x;
            panel1.anchoredPosition = new Vector2(PANEL_RIGHT_EDGE_X - scaledWidth1, panel1.anchoredPosition.y);

            RectTransform panel2 = (RectTransform)scroll2.transform;
            float scaledWidth2 = panel2.rect.width * panel2.localScale.x;
            panel2.anchoredPosition = new Vector2(
                panel1.anchoredPosition.x - PANEL_GAP - scaledWidth2, panel2.anchoredPosition.y);

            FitExtraMenuPosition();
        }

        // The ExtraView marker has a fixed prefab position and shown panels can be any size
        // (they overflow the marker), so a wide level-2 panel would cover them. Measure the
        // shown panel's world bounds and shift the marker so the panel sits left of the
        // visible menu panels with the same gap they keep between each other.
        private void FitExtraMenuPosition()
        {
            if (!extraMenu.activeSelf || lastExtraMenu == null || !lastExtraMenu.activeInHierarchy)
            {
                return;
            }

            GameObject reference = scroll2.activeSelf ? scroll2 : scroll1.activeSelf ? scroll1 : null;
            if (reference == null)
            {
                return;
            }

            RectTransform content = (RectTransform)lastExtraMenu.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);

            RectTransform referenceRect = (RectTransform)reference.transform;
            Vector3[] corners = new Vector3[4];
            referenceRect.GetWorldCorners(corners);
            float panelLeftEdge = corners[0].x;

            content.GetWorldCorners(corners);
            float contentRightEdge = corners[2].x;

            float worldGap = PANEL_GAP * referenceRect.parent.lossyScale.x;
            float shift = panelLeftEdge - worldGap - contentRightEdge;
            extraMenu.transform.position += new Vector3(shift, 0f, 0f);
        }

        private static void FitPanelWidth(GameObject scrollRoot, GameObject content)
        {
            RectTransform contentRect = (RectTransform)content.transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            RectTransform panel = (RectTransform)scrollRoot.transform;
            float maxLocalWidth = PANEL_MAX_WIDTH / Mathf.Max(0.01f, panel.localScale.x);
            float width = Mathf.Min(contentRect.rect.width + PANEL_EXTRA_WIDTH, maxLocalWidth);
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        }

        private void ClearPlayerPrefs()
        {
            try
            {
                PlayerPrefs.DeleteAll();
                Debug.Log("PlayerPrefs cleared!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DebugMenu] ClearPlayerPrefs failed: {e.Message}");
            }
        }

        private void ClearGameData()
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(Application.persistentDataPath);
                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
                Debug.Log("GameData cleared!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DebugMenu] ClearGameData failed: {e.Message}");
            }
        }

        private void ClearCache()
        {
            try
            {
                Caching.ClearCache();
                Debug.Log("Caching cleared!");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DebugMenu] ClearCache failed: {e.Message}");
            }
        }
    }

    public class DebugMenuEntry
    {
        public readonly string category;
        public readonly string name;
        public readonly MethodInfo method;
        public readonly object target;
        public readonly Action action;

        public DebugMenuEntry(string category, string name, MethodInfo method, object target)
        {
            this.category = category;
            this.name = name;
            this.method = method;
            this.target = target;
        }

        public DebugMenuEntry(string category, string name, Action action)
        {
            this.category = category;
            this.name = name;
            this.action = action;
        }

        public void Invoke()
        {
            if (action != null)
            {
                action();
            }
            else
            {
                method.Invoke(target, Array.Empty<object>());
            }
        }
    }
}
