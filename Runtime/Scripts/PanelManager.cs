using System;
using System.Collections.Generic;
using System.Linq;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BattleTurn.UI_Panel.Runtime
{
    [Serializable]
    public sealed class PanelInfo
    {
        public string Id;
        public BasePanel Panel;
        public PanelType Type = PanelType.Screen;
        public bool destroyOnHide = false;
    }

    public enum PanelType
    {
        Screen,
        Popup
    }

    public class PanelManager : MonoBehaviour
    {
        private const string DEBUGS = "DEBUGS";
        private const string SETTINGS = "SETTINGS";
        private const string REFERENCES = "REFERENCES";

        [SerializeField]
#if ODIN_INSPECTOR
        [BoxGroup(SETTINGS)]
#endif
        private List<PanelInfo> _panelMap = new();

        [Header("-- UI References --")]
#if ODIN_INSPECTOR
        [BoxGroup(REFERENCES, Order = -1)]
#endif
        [SerializeField]
        private Camera _uiCamera;

        [SerializeField]
#if ODIN_INSPECTOR
        [BoxGroup(REFERENCES, Order = -1)]
#endif
        private Canvas _uiCanvas;

        [SerializeField]
#if ODIN_INSPECTOR
        [BoxGroup(REFERENCES, Order = -1)]
#endif
        private RectTransform _container;

#if ODIN_INSPECTOR
        [BoxGroup(SETTINGS, Order = -1)]
#endif
#if ENABLE_INPUT_SYSTEM
        [SerializeField]
        private InputAction _backAction;
#endif
#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
        [FoldoutGroup(DEBUGS, Order = 99)]
#endif
        private Dictionary<string, PanelInfo> _panels = new();
#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
        [FoldoutGroup(DEBUGS, Order = 99)]
#endif
        private Dictionary<string, List<BasePanel>> _shownPopupMap = new();
#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
        [FoldoutGroup(DEBUGS, Order = 99)]
#endif
        private Dictionary<string, BasePanel> _shownScreenMap = new();

#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
        [FoldoutGroup(DEBUGS, Order = 99)]
#endif
        private Dictionary<string, BasePanel> _recycleScreenMap = new();

#if ODIN_INSPECTOR
        [ReadOnly]
        [ShowInInspector]
        [FoldoutGroup(DEBUGS, Order = 99)]
#endif
        private Dictionary<string, List<BasePanel>> _recyclePopupMap = new();

        #region Properties
        public Camera UICamera
        {
            get => _uiCamera;
        }

        public Canvas UICanvas
        {
            get => _uiCanvas;
        }
        #endregion

        private void Awake()
        {
            // Initialize panel references
            foreach (var panel in _panelMap)
            {
                if (panel.Panel != null)
                {
                    panel.Panel.Id = panel.Id;
                }
            }

#if ENABLE_INPUT_SYSTEM
            // _backAction = new InputAction("Back", binding: "<Keyboard>/escape");
            // _backAction.performed += ctx => 
            // {
            //     if (ctx.pre&& _screenStack.Count > 0)
            //     {
            //         ProcessBackButton(Peek());
            //     }
            // };
            // _backAction.Enable();
#else
            // The legacy Input system handling is already in Update()
#endif
        }

        public T Show<T>(string id) where T : BasePanel
        {
            InitDict();
            Type panelType = typeof(T);
            PanelInfo panelInfo = _panels.ContainsKey(id) ? _panels[id] : null;
            if (panelInfo == null)
            {
                Debug.LogError($"[PanelManager] No panel of type {panelType} with ID '{id}' found in the panel map.");
                return null;
            }

            switch (panelInfo.Type)
            {
                case PanelType.Screen:
                    {
                        T screen;
                        if (_recycleScreenMap.ContainsKey(panelInfo.Id))
                        {
                            screen = _recycleScreenMap[panelInfo.Id] as T;
                            Debug.Log("Reusing existing screen instance: " + screen.gameObject.name);
                            _recycleScreenMap.Remove(panelInfo.Id);
                            screen.transform.SetAsLastSibling();
                        }
                        else if (_shownScreenMap.ContainsKey(panelInfo.Id))
                        {
                            screen = _shownScreenMap[panelInfo.Id] as T;
                            Debug.Log("Screen instance already shown: " + screen.gameObject.name);
                            screen.transform.SetAsLastSibling();
                        }
                        else
                        {
                            Debug.Log("Creating new screen instance: " + panelInfo.Panel.gameObject.name);
                            screen = Instantiate(panelInfo.Panel, _container) as T;
                            screen.panelManager = this;
                            screen.Id = panelInfo.Id;
                            screen.gameObject.name = panelInfo.Panel.gameObject.name + "__Screen";
                            screen.ShowInternal();
                        }

                        screen.ShowInternal();
                        _shownScreenMap[panelInfo.Id] = screen;
                        return screen;
                    }
                case PanelType.Popup:
                    {
                        if (!_shownPopupMap.ContainsKey(panelInfo.Id))
                            _shownPopupMap[panelInfo.Id] = new List<BasePanel>();

                        BasePanel popup;
                        bool isDuplicate = _shownPopupMap[panelInfo.Id].Count > 0;
                        if (_recyclePopupMap.ContainsKey(panelInfo.Id) && _recyclePopupMap[panelInfo.Id].Count > 0)
                        {
                            popup = _recyclePopupMap[panelInfo.Id].FirstOrDefault();
                            _recyclePopupMap[panelInfo.Id].Remove(popup);
                            Debug.Log("Reusing existing popup instance: " + popup.gameObject.name);
                        }
                        else
                        {
                            Debug.Log("Creating new popup instance: " + panelInfo.Panel.gameObject.name);
                            popup = Instantiate(panelInfo.Panel, _container);
                            popup.panelManager = this;
                            popup.Id = panelInfo.Id; // keep logical id
                            popup.gameObject.name = panelInfo.Panel.gameObject.name + (isDuplicate ? "__Dup" : "__Popup");
                        }

                        _shownPopupMap[panelInfo.Id].Add(popup);
                        popup.transform.SetAsLastSibling();
                        popup.ShowInternal();
                        return popup as T;
                    }
                default:
                    Debug.LogError($"[PanelManager] Unknown panel type {panelInfo.Type} for panel ID '{panelInfo.Id}'.");
                    return null;
            }
        }

        public void Show<T>(T panel) where T : BasePanel
        {
            InitDict();
            if (panel == null || string.IsNullOrEmpty(panel.Id) || !_panels.ContainsKey(panel.Id))
            {
                Debug.LogWarning($"[PanelManager] Show: Unknown panel id '{panel?.Id}'.");
                return;
            }

            PanelInfo panelInfo = _panels.ContainsKey(panel.Id) ? _panels[panel.Id] : null;
            switch (panelInfo.Type)
            {
                case PanelType.Screen:
                    if (_shownScreenMap.ContainsKey(panel.Id) && _shownScreenMap[panel.Id] == panel)
                    {
                        Debug.Log("Screen instance already shown: " + panel.gameObject.name);
                        panel.transform.SetAsLastSibling();
                        panel.ShowInternal();
                        return;
                    }
                    Debug.Log("Showing screen instance: " + panel.gameObject.name);
                    _shownScreenMap[panel.Id] = panel;
                    break;
                case PanelType.Popup:
                    if (!_shownPopupMap.ContainsKey(panel.Id))
                        _shownPopupMap[panel.Id] = new List<BasePanel>();
                    Debug.Log("Showing popup instance: " + panel.gameObject.name);
                    _shownPopupMap[panel.Id].Add(panel);
                    break;
            }

            panel.panelManager = this;
            panel.ShowInternal();
        }

        private void InitDict()
        {
            if (_panels == null)
                _panels = new Dictionary<string, PanelInfo>();

            if (_panels.Count == _panelMap.Count)
            {
                return;
            }

            _panels.Clear();
            foreach (var panelInfo in _panelMap)
            {
                if (panelInfo.Panel != null)
                {
                    _panels[panelInfo.Id] = panelInfo;
                }
            }
        }

        // Hide by id: Screens hide single instance; Popups hide the last shown (LIFO).
        public void Hide(string id, Action onHideCallBack = null)
        {
            InitDict();
            if (string.IsNullOrEmpty(id) || !_panels.ContainsKey(id))
            {
                Debug.LogWarning($"[PanelManager] Hide: Unknown panel id '{id}'.");
                onHideCallBack?.Invoke();
                return;
            }

            var def = _panels[id];
            switch (def.Type)
            {
                case PanelType.Screen:
                    Debug.Log("Hiding screen with id: " + id);
                    bool isContain = _shownScreenMap.TryGetValue(id, out var screen);
                    Debug.Log("isContain: " + isContain + ", screen: " + (screen != null ? screen.gameObject.name : "null"));
                    if (isContain && screen != null)
                    {
                        Debug.Log("<color=yellow>Hiding screen instance: </color>" + screen.gameObject.name);
                        HideScreenPanel(screen, onHideCallBack);
                    }
                    else
                    {
                        onHideCallBack?.Invoke();
                    }
                    break;
                case PanelType.Popup:
                    if (_shownPopupMap.TryGetValue(id, out var list) && list != null && list.Count > 0)
                    {
                        HidePopupPanel(list[list.Count - 1], onHideCallBack);
                    }
                    break;
            }
        }

        // Hide by panel reference (find and remove from proper map)
        public void Hide(BasePanel panel, Action onHideCallBack = null)
        {
            if (panel == null)
            {
                onHideCallBack?.Invoke();
                return;
            }

            // Try screens
            switch (_panels[panel.Id].Type)
            {
                case PanelType.Screen:
                    if (_shownScreenMap.ContainsKey(panel.Id) && _shownScreenMap[panel.Id] == panel)
                    {
                        HideScreenPanel(panel, onHideCallBack);
                        return;
                    }
                    break;
                case PanelType.Popup:
                    HidePopupPanel(panel, onHideCallBack);
                    return;

            }

        }

        private void HideScreenPanel(BasePanel screen, Action onHideCallBack)
        {
            screen.HideInternal(() =>
            {
                if (!_shownScreenMap.ContainsKey(screen.Id))
                {
                    return;
                }

                _shownScreenMap.Remove(screen.Id);
                if (_panels[screen.Id].destroyOnHide)
                {
                    Destroy(screen.gameObject);
                }
                else
                {
                    _recycleScreenMap[screen.Id] = screen;
                }
                onHideCallBack?.Invoke();
            });
        }

        private bool HidePopupPanel(BasePanel panel, Action onHideCallBack)
        {
            List<BasePanel> popupList = _shownPopupMap[panel.Id];

            for (int i = 0; i < popupList.Count; i++)
            {
                if (popupList[i] != panel) continue;

                // Hide specific popup instance, maintaining order
                var info = popupList[i];
                popupList.RemoveAt(i);
                if (info != null)
                {
                    info.HideInternal(() =>
                    {
                        _shownPopupMap[panel.Id].Remove(info);
                        if (popupList.Count == 0) _shownPopupMap.Remove(panel.Id);
                        if (_panels[panel.Id].destroyOnHide)
                        {
                            Destroy(info.gameObject);
                            onHideCallBack?.Invoke();
                            return;
                        }

                        if (!_recyclePopupMap.ContainsKey(panel.Id))
                            _recyclePopupMap[panel.Id] = new List<BasePanel>();

                        _recyclePopupMap[panel.Id].Add(info);
                        onHideCallBack?.Invoke();
                    });
                }
                else
                {
                    if (popupList.Count == 0) _shownPopupMap.Remove(panel.Id);
                    onHideCallBack?.Invoke();
                }
                return false;
            }

            // Not found
            onHideCallBack?.Invoke();
            return true;
        }

        public bool TryGet<T>(BasePanel basePanel, out T panel) where T : BasePanel
        {
            panel = null;
            if (_shownPopupMap.ContainsKey(basePanel.Id))
            {
                var list = _shownPopupMap[basePanel.Id];
                if (list != null && list.Count > 0)
                {
                    var found = _shownPopupMap[basePanel.Id].Find(p => p == basePanel);
                    if (found != null)
                    {
                        panel = (T)found;
                        return true;
                    }
                }
                return false;
            }

            if (_shownScreenMap.ContainsKey(basePanel.Id))
            {
                var screen = _shownScreenMap[basePanel.Id];
                if (screen is T)
                {
                    panel = (T)screen;
                    return true;
                }
                return false;
            }
            return false;
        }
    }

}