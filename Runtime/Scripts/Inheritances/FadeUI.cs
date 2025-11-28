using System;
#if CYSHARP_UNITASK
using Cysharp.Threading.Tasks;
#endif
using UnityEngine;

namespace BattleTurn.UI_Panel.Runtime
{
    public class FadeUI : BaseFadeUI
    {
        [SerializeField] CanvasGroup canvasGroup;
        // [SerializeField] private float duration = 0.5f;
        private float _duration = .1f;

        public override async void DoFadeIn(Action onComplete = null)
        {
            await FadeToAlpha(onComplete, 1);
        }

        // DoFade method that you can call for fading in or fading out
        public override async void DoFadeOut(Action onComplete = null)
        {
            await FadeToAlpha(onComplete, 0);
        }

        private void OnDisable()
        {
            StopAllCoroutines();  // Stop all coroutines when the object is disabled
            canvasGroup.alpha = 0;  // Reset the alpha value to 0 when disabled
        }

        // Coroutine that fades the alpha value of the CanvasGroup to the target value
#if CYSHARP_UNITASK
        private async UniTask FadeToAlpha(Action onCompleted, float targetAlpha)
#else
        private async System.Threading.Tasks.Task FadeToAlpha(Action onCompleted, float targetAlpha)
#endif
        {
            Debug.Log($"FadeToAlpha: {canvasGroup}, {gameObject}");
            float startAlpha = canvasGroup.alpha;  // The current alpha value
            float elapsedTime = 0f;  // Time elapsed in the fade process

            // Fade from the current alpha value to the target value
            while (elapsedTime < _duration)
            {
                elapsedTime += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / _duration);
#if CYSHARP_UNITASK
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
#else
                await System.Threading.Tasks.Task.Yield();
#endif
            }

            // Ensure the final alpha value is set
            canvasGroup.alpha = targetAlpha;
            onCompleted?.Invoke();
        }
    }
}