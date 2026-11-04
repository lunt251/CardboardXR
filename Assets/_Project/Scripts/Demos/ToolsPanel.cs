using System;
using System.Collections.Generic;
using CardboardXR.Stereo;
using CardboardXR.UI;
using TMPro;
using UnityEngine;

namespace CardboardXR.Demos
{
    // The Tools panel: the controls for the camera view, the photo overlay and the app's own settings, in
    // four tabs. It is built at runtime from the Material kit, floats in front of the user where it was
    // opened (the settings panel's Tools button toggles it) and works in every scene, over the demo's own
    // controls.
    //   View   zoom and pan the camera image, freeze it.
    //   Look   brightness, contrast, colour and opacity of the camera image; autofocus; sharper image.
    //   Photo  choose a picture from the gallery and set its opacity, size, distance, position and turn,
    //          pin it in the room, flip it (see PhotoViewer).
    //   App    frame rate, render scale, anti-aliasing and hand tracking, kept between launches.
    // Sliders set the value where the gaze clicks along them. Logs [Tools].
    public class ToolsPanel : MonoBehaviour
    {
        const float k_Width = 480f;
        const float k_Height = 600f;
        static readonly string[] k_Tabs = { "View", "Look", "Photo", "App" };
        static readonly string[] k_SavedKeys =
        {
            "fps", "renderScale", "msaa", "handsOff", "autofocus", "ptHeight", "ptZoom", "ptPanX", "ptPanY",
            "ptBright", "ptContrast", "ptSat", "ptOpacity", "photoOpacity", "photoSize", "photoDistance",
        };

        CoreServices m_Core;
        PassthroughController m_Passthrough;
        PhotoViewer m_Photo;
        RectTransform m_Panel;
        readonly RectTransform[] m_Pages = new RectTransform[4];
        M3Button[] m_TabButtons;
        readonly List<Action> m_Refreshers = new();

        public bool IsOpen => m_Panel != null && M3Fade.IsShown(m_Panel.gameObject);

        void Start()
        {
            m_Core = CoreServices.Instance;
            m_Passthrough = m_Core.Passthrough;
            m_Photo = FindFirstObjectByType<PhotoViewer>();

            m_Panel = WorldUi.Panel("Tools", null, new Vector2(k_Width, k_Height), M3Theme.k_OverlayDistance);
            m_TabButtons = M3.Segments("Tabs", m_Panel, k_Tabs, new Vector2(0f, 262f), 104f, ShowPage);
            for (var i = 0; i < m_Pages.Length; i++)
                m_Pages[i] = WorldUi.CreateGroup(k_Tabs[i], m_Panel);
            BuildViewPage(m_Pages[0]);
            BuildLookPage(m_Pages[1]);
            BuildPhotoPage(m_Pages[2]);
            BuildAppPage(m_Pages[3]);
            M3.Button("Close", m_Panel, "Close", M3Button.Variant.Tonal, new Vector2(0f, -262f), Close, "close", 150f);
            ShowPage(0);
            m_Panel.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (m_Panel != null)
                Destroy(m_Panel.gameObject);
        }

        public void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }

        public void Open()
        {
            Refresh();
            WorldUi.PlaceInFront(m_Panel, m_Core.Head, M3Theme.k_OverlayDistance, 0f);
            M3Fade.Show(m_Panel.gameObject);
            Debug.Log("[Tools] Opened");
        }

        public void Close() => M3Fade.Hide(m_Panel.gameObject);

        void ShowPage(int index)
        {
            for (var i = 0; i < m_Pages.Length; i++)
            {
                m_Pages[i].gameObject.SetActive(i == index);
                m_TabButtons[i].Selected = i == index;
            }
            Refresh();
        }

        void Refresh()
        {
            foreach (var refresh in m_Refreshers)
                refresh();
        }

        // ---------------------------------------------------------------- pages

        void BuildViewPage(RectTransform page)
        {
            Slider(page, 196f, "Zoom", 1f, 4f, () => m_Passthrough.Zoom, v => m_Passthrough.Zoom = v, v => $"{v:0.0}x");
            Slider(page, 140f, "Pan left/right", -0.4f, 0.4f, () => m_Passthrough.PanX, v => m_Passthrough.PanX = v,
                v => $"{v * 100f:+0;-0;0}%");
            Slider(page, 84f, "Pan up/down", -0.4f, 0.4f, () => m_Passthrough.PanY, v => m_Passthrough.PanY = v,
                v => $"{v * 100f:+0;-0;0}%");
            var freeze = M3.Button("Freeze", page, "Freeze", M3Button.Variant.Tonal, new Vector2(-95f, 10f),
                () => { m_Passthrough.Frozen = !m_Passthrough.Frozen; Refresh(); }, "pause", 170f);
            M3.Button("Reset", page, "Reset view", M3Button.Variant.Tonal, new Vector2(95f, 10f),
                () => { m_Passthrough.ResetLook(); Refresh(); }, "replay", 170f);
            m_Refreshers.Add(() => freeze.Selected = m_Passthrough.Frozen);
            Hint(page, -70f, "Zoom crops into the middle of the camera image. Pan moves the crop. " +
                             "Freeze holds the picture still in your view. Needs passthrough on.");
        }

        void BuildLookPage(RectTransform page)
        {
            Slider(page, 196f, "Brightness", 0.3f, 2f, () => m_Passthrough.Brightness, v => m_Passthrough.Brightness = v,
                v => $"{v * 100f:0}%");
            Slider(page, 140f, "Contrast", 0.3f, 2f, () => m_Passthrough.Contrast, v => m_Passthrough.Contrast = v,
                v => $"{v * 100f:0}%");
            Slider(page, 84f, "Colour", 0f, 2f, () => m_Passthrough.Saturation, v => m_Passthrough.Saturation = v,
                v => $"{v * 100f:0}%");
            Slider(page, 28f, "Opacity", 0.1f, 1f, () => m_Passthrough.Opacity, v => m_Passthrough.Opacity = v,
                v => $"{v * 100f:0}%");
            var focus = M3.Button("Autofocus", page, "Autofocus", M3Button.Variant.Tonal, new Vector2(-95f, -44f),
                () => { m_Passthrough.AutoFocus = !m_Passthrough.AutoFocus; Refresh(); }, "visibility", 170f);
            var hd = M3.Button("HD", page, "Sharper", M3Button.Variant.Tonal, new Vector2(95f, -44f), () =>
            {
                m_Passthrough.TextureHeight = m_Passthrough.TextureHeight >= 1000 ? 720 : 1080;
                Refresh();
            }, "aspect_ratio", 170f);
            m_Refreshers.Add(() =>
            {
                focus.Selected = m_Passthrough.AutoFocus;
                hd.Selected = m_Passthrough.TextureHeight >= 1000;
            });
            Hint(page, -120f, "Autofocus off holds the focus where it is. Sharper draws the camera image at 1080 " +
                              "pixels high instead of 720: clearer, but warmer.");
        }

        void BuildPhotoPage(RectTransform page)
        {
            if (m_Photo == null)
            {
                Hint(page, 0f, "The photo viewer isn't in this build.");
                return;
            }
            Slider(page, 196f, "Opacity", 0.05f, 1f, () => m_Photo.Opacity, v => m_Photo.Opacity = v, v => $"{v * 100f:0}%");
            Slider(page, 140f, "Size", 0.1f, 3f, () => m_Photo.Size, v => m_Photo.Size = v, v => $"{v * 100f:0} cm");
            Slider(page, 84f, "Distance", 0.3f, 3f, () => m_Photo.Distance, v => m_Photo.Distance = v, v => $"{v * 100f:0} cm");
            Slider(page, 28f, "Turn", -180f, 180f, () => m_Photo.Rotation, v => m_Photo.Rotation = v, v => $"{v:0}\u00b0");
            Slider(page, -28f, "Left/right", -1f, 1f, () => m_Photo.X, v => m_Photo.X = v, v => $"{v * 100f:+0;-0;0} cm");
            Slider(page, -84f, "Up/down", -1f, 1f, () => m_Photo.Y, v => m_Photo.Y = v, v => $"{v * 100f:+0;-0;0} cm");

            M3.Button("Choose", page, "Choose photo", M3Button.Variant.Filled, new Vector2(-100f, -144f), m_Photo.Choose, "movie", 200f);
            var show = M3.Button("Show", page, "Hide", M3Button.Variant.Tonal, new Vector2(110f, -144f),
                () => { m_Photo.Visible = !m_Photo.Visible; Refresh(); }, "visibility", 160f);
            var pin = M3.Button("Pin", page, "Pin to room", M3Button.Variant.Tonal, new Vector2(-100f, -200f), () =>
            {
                if (m_Photo.Pinned)
                    m_Photo.Unpin();
                else
                    m_Photo.Pin();
                Refresh();
            }, "my_location", 200f);
            var flip = M3.Button("Flip", page, "Flip", M3Button.Variant.Tonal, new Vector2(110f, -200f),
                () => { m_Photo.Flip = !m_Photo.Flip; Refresh(); }, "restart_alt", 160f);
            m_Refreshers.Add(() =>
            {
                show.Text = m_Photo.Visible ? "Hide" : "Show";
                pin.Text = m_Photo.Pinned ? "Unpin" : "Pin to room";
                pin.Selected = m_Photo.Pinned;
                flip.Selected = m_Photo.Flip;
            });
        }

        void BuildAppPage(RectTransform page)
        {
            Choice(page, 196f, "Frame rate", new[] { "60", "90", "120" }, new[] { 60, 90, 120 }, 64f,
                () => LaunchOptions.GetInt("fps", 120), v =>
                {
                    LaunchOptions.SetSaved("fps", v);
                    Application.targetFrameRate = v;
                    DisplayRefreshRate.Request(v);
                });
            Choice(page, 130f, "Render scale", new[] { "70%", "85%", "100%" }, new[] { 70, 85, 100 }, 64f,
                () => { var v = LaunchOptions.GetInt("renderScale", 0); return v <= 0 ? 100 : v; },
                v => LaunchOptions.SetSaved("renderScale", v));
            Choice(page, 64f, "Anti-aliasing", new[] { "Off", "2x", "4x" }, new[] { 1, 2, 4 }, 64f,
                () => LaunchOptions.GetInt("msaa", 2), v => LaunchOptions.SetSaved("msaa", v));
            Choice(page, -2f, "Hand tracking", new[] { "On", "Off" }, new[] { 0, 1 }, 80f,
                () => m_Core.HandsForcedOff ? 1 : 0, v => m_Core.HandsForcedOff = v == 1);
            Hint(page, -78f, "Frame rate and hand tracking apply now. Render scale and anti-aliasing apply the next " +
                             "time the app starts. Hands off saves battery and heat; look at a button and tap the " +
                             "screen to click.");
            M3.Button("Reset all", page, "Reset all settings", M3Button.Variant.Tonal, new Vector2(0f, -170f),
                ResetAll, "refresh", 300f);
        }

        // ---------------------------------------------------------------- building blocks

        void Slider(RectTransform page, float y, string label, float min, float max, Func<float> get, Action<float> set,
            Func<float, string> format)
        {
            M3.Text($"{label} label", page, label, M3Theme.Type.BodyLarge, new Vector2(-160f, y), new Vector2(140f, 32f),
                null, TextAlignmentOptions.Left);
            var slider = M3.Slider(label, page, new Vector2(30f, y), 190f);
            var value = M3.Text($"{label} value", page, "", M3Theme.Type.BodyMedium, new Vector2(190f, y), new Vector2(80f, 32f),
                M3Theme.OnSurfaceVariant, TextAlignmentOptions.Right);
            slider.Clicked += fraction =>
            {
                set(Mathf.Lerp(min, max, fraction));
                Refresh();
            };
            m_Refreshers.Add(() =>
            {
                var current = get();
                slider.Value = Mathf.InverseLerp(min, max, current);
                value.text = format(current);
            });
        }

        // A row of segments to pick one of several values.
        void Choice(RectTransform page, float y, string label, string[] names, int[] values, float segmentWidth,
            Func<int> get, Action<int> set)
        {
            M3.Text($"{label} label", page, label, M3Theme.Type.BodyLarge, new Vector2(-150f, y), new Vector2(160f, 32f),
                null, TextAlignmentOptions.Left);
            var segments = M3.Segments(label, page, names, new Vector2(95f, y), segmentWidth, index =>
            {
                set(values[index]);
                Refresh();
            });
            m_Refreshers.Add(() =>
            {
                var current = get();
                for (var i = 0; i < segments.Length; i++)
                    segments[i].Selected = values[i] == current;
            });
        }

        static void Hint(RectTransform page, float y, string text)
        {
            M3.Text("Hint", page, text, M3Theme.Type.BodyMedium, new Vector2(0f, y), new Vector2(k_Width - 60f, 80f),
                M3Theme.OnSurfaceVariant, TextAlignmentOptions.Center);
        }

        void ResetAll()
        {
            m_Passthrough.ResetLook();
            m_Passthrough.Frozen = false;
            m_Passthrough.AutoFocus = true;
            m_Passthrough.TextureHeight = 720;
            m_Core.HandsForcedOff = false;
            if (m_Photo != null)
            {
                m_Photo.ResetPlacement();
                m_Photo.Opacity = 0.5f;
                m_Photo.Size = 0.6f;
                m_Photo.Distance = 0.8f;
            }
            foreach (var key in k_SavedKeys)
                LaunchOptions.ClearSaved(key);
            Application.targetFrameRate = 120;
            DisplayRefreshRate.Request(120);
            Refresh();
            Debug.Log("[Tools] All saved settings reset");
        }
    }
}
