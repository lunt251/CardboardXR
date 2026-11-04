using System.IO;
using CardboardXR.Stereo;
using UnityEngine;

namespace CardboardXR.Demos
{
    // A picture shown over the room, for tracing it by hand onto paper or a wall: the user picks it from
    // the phone's gallery (PhotoPicker), then sets its opacity, size, distance, position and turn from the
    // Tools panel. Two ways to hold it:
    //   Head: it stays where it is in the view, as the head turns and moves.
    //   Room: Pin keeps it where it is in the room (the XR Origin's space, which tracking corrects), so
    //         it sits on the wall or the page while the user moves, and Unpin brings it back to the view.
    // The picture hangs from a base transform: under the head, or pinned in the room. The base keeps the
    // head's pose at the moment of pinning, so the sliders (distance in front, x across, y up, turn) mean
    // the same in both. Lives in the Core scene so it works over any demo. Logs [Photo].
    public class PhotoViewer : MonoBehaviour
    {
        const string k_PathKey = "photoPath";

        [Tooltip("The picture's material: the Photo shader, with an opacity.")]
        [SerializeField] Material m_Material;

        CoreServices m_Core;
        Transform m_Base;
        Transform m_Quad;
        Material m_Instance;
        Texture2D m_Texture;
        float m_Aspect = 1f;

        bool m_Visible = true;
        bool m_Pinned;
        bool m_Flip;
        float m_Opacity = 0.5f;
        // Width in meters, and distance in front of the base in meters.
        float m_Size = 0.6f;
        float m_Distance = 0.8f;
        float m_X;
        float m_Y;
        float m_Rotation;

        public bool HasPhoto => m_Texture != null;
        public bool Pinned => m_Pinned;

        public bool Visible
        {
            get => m_Visible;
            set { m_Visible = value; Apply(); }
        }

        public bool Flip
        {
            get => m_Flip;
            set { m_Flip = value; Apply(); }
        }

        public float Opacity
        {
            get => m_Opacity;
            set { m_Opacity = Mathf.Clamp(value, 0.05f, 1f); Save("photoOpacity", m_Opacity * 100f); Apply(); }
        }

        // Width in meters.
        public float Size
        {
            get => m_Size;
            set { m_Size = Mathf.Clamp(value, 0.05f, 5f); Save("photoSize", m_Size * 100f); Apply(); }
        }

        public float Distance
        {
            get => m_Distance;
            set { m_Distance = Mathf.Clamp(value, 0.2f, 5f); Save("photoDistance", m_Distance * 100f); Apply(); }
        }

        public float X { get => m_X; set { m_X = Mathf.Clamp(value, -2f, 2f); Apply(); } }
        public float Y { get => m_Y; set { m_Y = Mathf.Clamp(value, -2f, 2f); Apply(); } }
        public float Rotation { get => m_Rotation; set { m_Rotation = Mathf.Repeat(value + 180f, 360f) - 180f; Apply(); } }

        static void Save(string name, float value) => LaunchOptions.SetSaved(name, Mathf.RoundToInt(value));

        void Start()
        {
            m_Core = CoreServices.Instance;
            m_Opacity = Mathf.Clamp(LaunchOptions.GetInt("photoOpacity", 50) / 100f, 0.05f, 1f);
            m_Size = Mathf.Clamp(LaunchOptions.GetInt("photoSize", 60) / 100f, 0.05f, 5f);
            m_Distance = Mathf.Clamp(LaunchOptions.GetInt("photoDistance", 80) / 100f, 0.2f, 5f);

            m_Base = new GameObject("Photo Base").transform;
            m_Base.SetParent(m_Core.Head, false);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Photo";
            Destroy(quad.GetComponent<Collider>());
            m_Quad = quad.transform;
            m_Quad.SetParent(m_Base, false);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            m_Instance = new Material(m_Material) { name = "Photo (instance)" };
            renderer.sharedMaterial = m_Instance;
            quad.SetActive(false);

            var path = PlayerPrefs.GetString(k_PathKey, "");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                Load(path);
        }

        void OnDestroy()
        {
            if (m_Texture != null)
                Destroy(m_Texture);
            if (m_Instance != null)
                Destroy(m_Instance);
        }

        // Opens the phone's gallery. The picture comes back through OnPhotoPicked.
        public void Choose() => PhotoPicker.Pick(gameObject.name, nameof(OnPhotoPicked));

        // Called by the Android picker (UnitySendMessage) with the picked picture's file, or "" for none.
        public void OnPhotoPicked(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("[Photo] Nothing picked");
                return;
            }
            if (Load(path))
            {
                PlayerPrefs.SetString(k_PathKey, path);
                PlayerPrefs.Save();
                m_Visible = true;
                Apply();
            }
        }

        bool Load(string path)
        {
            try
            {
                var bytes = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true);
                if (!texture.LoadImage(bytes))
                {
                    Destroy(texture);
                    Debug.LogWarning($"[Photo] Couldn't read the picture at {path}");
                    return false;
                }
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.anisoLevel = 4;
                if (m_Texture != null)
                    Destroy(m_Texture);
                m_Texture = texture;
                m_Aspect = texture.width / (float)texture.height;
                m_Instance.mainTexture = texture;
                Debug.Log($"[Photo] Loaded {texture.width}x{texture.height}, {bytes.Length / 1024} kB");
                Apply();
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Photo] Couldn't load {path}: {e.Message}");
                return false;
            }
        }

        // Holds the picture where it is in the room.
        public void Pin()
        {
            if (m_Pinned || m_Core == null)
                return;
            m_Base.SetParent(m_Core.Boundary.transform, true);
            m_Pinned = true;
            Debug.Log("[Photo] Pinned in the room");
        }

        // Brings the picture back to the view, in front of the head.
        public void Unpin()
        {
            if (!m_Pinned)
                return;
            m_Base.SetParent(m_Core.Head, false);
            m_Base.localPosition = Vector3.zero;
            m_Base.localRotation = Quaternion.identity;
            m_Pinned = false;
            Debug.Log("[Photo] Back in the view");
        }

        public void ResetPlacement()
        {
            m_X = m_Y = m_Rotation = 0f;
            m_Flip = false;
            Unpin();
            Apply();
        }

        void Apply()
        {
            if (m_Quad == null)
                return;
            m_Quad.gameObject.SetActive(m_Visible && m_Texture != null);
            m_Quad.localPosition = new Vector3(m_X, m_Y, m_Distance);
            m_Quad.localRotation = Quaternion.Euler(0f, 0f, m_Rotation);
            m_Quad.localScale = new Vector3(m_Flip ? -m_Size : m_Size, m_Size / m_Aspect, 1f);
            m_Instance.SetFloat("_Opacity", m_Opacity);
        }
    }
}
