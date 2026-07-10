using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Input = UnityEngine.Input;

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

        private const int FINGER_COUNT_TO_ENABLE_DEBUG = 4;

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
                Destroy(buttonsMenu1.transform.GetChild(i).gameObject);
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
        }

        private void OnMenu1Click(string category)
        {
            for (int i = 0; i < buttonsMenu2.transform.childCount; i++)
            {
                Destroy(buttonsMenu2.transform.GetChild(i).gameObject);
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
