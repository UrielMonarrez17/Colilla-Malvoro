using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Prototipo3.UI
{
    /// <summary>Presentation-only model. No player input, physics or gameplay scripts.</summary>
    public sealed class MenuHamsterLook : MonoBehaviour
    {
        public Transform model;
        public Camera portraitCamera;
        public RawImage portrait;
        [Range(0, 60)] public float yaw = 28;
        [Range(0, 30)] public float pitch = 12;
        public float smoothing = 7;
        Quaternion restRotation;
        RenderTexture target;

        void Awake()
        {
            restRotation = model.localRotation;
            target = new RenderTexture(1024, 1024, 24, RenderTextureFormat.ARGB32);
            target.name = "Menu hamster portrait (runtime)";
            target.Create();
            portraitCamera.targetTexture = target;
            portrait.texture = target;
        }

        public void LookAtScreenPoint(Vector2 point)
        {
            var canvas = portrait.canvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var center = RectTransformUtility.WorldToScreenPoint(camera, portrait.rectTransform.TransformPoint(portrait.rectTransform.rect.center));
            var delta = point - center;
            var rotation = restRotation * Quaternion.Euler(
                -Mathf.Clamp(delta.y / (Screen.height * .45f), -1, 1) * pitch,
                -Mathf.Clamp(delta.x / (Screen.width * .45f), -1, 1) * yaw, 0);
            model.localRotation = Quaternion.Slerp(model.localRotation, rotation,
                1 - Mathf.Exp(-smoothing * Time.unscaledDeltaTime));
        }

        void LateUpdate()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Application.isFocused) LookAtScreenPoint(Mouse.current.position.ReadValue());
#else
            if (Application.isFocused) LookAtScreenPoint(Input.mousePosition);
#endif
        }

        void OnDestroy()
        {
            if (portraitCamera != null) portraitCamera.targetTexture = null;
            if (target != null) { target.Release(); Destroy(target); }
        }
    }
}
