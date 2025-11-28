using System;
using UnityEngine;

namespace BattleTurn.UI_Panel.Runtime
{
    public abstract class BaseFadeUI : MonoBehaviour
    {
        public abstract void DoFadeIn(Action onComplete = null);
        public abstract void DoFadeOut(Action onComplete = null);
    }
}