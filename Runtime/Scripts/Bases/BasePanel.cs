using System;
using UnityEngine;
using BattleTurn.UI_Panel.Runtime.Attributes;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace BattleTurn.UI_Panel.Runtime
{
    public class BasePanel : MonoBehaviour
    {

        [SerializeField]
        [Tooltip("The FadeUI component that will be used to fade the panel in and out (Optional)")]
        private BaseFadeUI fadeUI;
        
#if ODIN_INSPECTOR
        [ReadOnly]
        [BoxGroup("DEBUGS")]
#endif
        [SerializeField, PanelNameDropdown]
        [Tooltip("The ID of the panel. This should be unique for each panel.")]
        protected string id;

#if ODIN_INSPECTOR
        [ReadOnly]
        [BoxGroup("DEBUGS")]
#endif
        public PanelManager panelManager;

        public string Id
        {
            get => id;
            set => id = value;
        }

        protected virtual void SetData()
        {

        }

        public void Hide()
        {
            if (panelManager == null)
            {
                HideInternal();
                return;
            }
            panelManager.Hide(this);
        }

        public void Show()
        {
            if (panelManager == null)
            {
                ShowInternal();
                return;
            }
            panelManager.Show(this);
        }

        internal void ShowInternal(Action callback = null)
        {
            ShowProtected(callback);
        }

        internal void HideInternal(Action callback = null)
        {
            HideProtected(callback);
        }

        protected virtual void ShowProtected(Action callback = null)
        {
            gameObject.SetActive(true);
            SetData();

            if (fadeUI == null)
            {
                callback?.Invoke();
                return;
            }

            fadeUI.DoFadeIn(callback);
        }

        protected virtual void HideProtected(Action callback = null)
        {
            if (fadeUI == null)
            {
                gameObject.SetActive(false);
                callback?.Invoke();
                return;
            }

            fadeUI.DoFadeOut(() =>
            {
                if (gameObject != null)
                    gameObject.SetActive(false);
                callback?.Invoke();

            });
        }
    }
}