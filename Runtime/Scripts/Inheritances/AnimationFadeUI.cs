using System;
#if CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif
using UnityEngine;

namespace BattleTurn.UI_Panel.Runtime
{
    public class AnimationFadeUI : BaseFadeUI
    {
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private string _fadeIn = "Panel-In";
        [SerializeField]
        private string _fadeOut = "Panel-Out";

        private int _fadeOutHash = int.MinValue;
        private int _fadeInHash = int.MinValue;

        private void Awake()
        {
            _fadeOutHash = Animator.StringToHash(_fadeOut);
            _fadeInHash = Animator.StringToHash(_fadeIn);
        }

        public async override void DoFadeOut(Action onComplete = null)
        {
            if (_fadeOutHash == int.MinValue)
            {
                _fadeOutHash = Animator.StringToHash(_fadeOut);
            }
            _animator.Play(_fadeOutHash);
#if CYSHARP_UNITASK
            await UniTask.Delay(500);
#else
            await System.Threading.Tasks.Task.Delay(500);
#endif
            onComplete?.Invoke();
        }

        public async override void DoFadeIn(Action onComplete = null)
        {
            if (_fadeInHash == int.MinValue)
            {
                _fadeInHash = Animator.StringToHash(_fadeIn);
            }
            _animator.Play(_fadeInHash);
#if CYSHARP_UNITASK
            await UniTask.Delay(500);
#else
            await System.Threading.Tasks.Task.Delay(500);
#endif
            onComplete?.Invoke();
        }
    }
}
