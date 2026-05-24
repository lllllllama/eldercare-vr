using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PicoElderCare.UI.BPlus
{
    public class BPlusSceneVideoPanelController : MonoBehaviour
    {
        public RectTransform videoRoot;
        public RawImage videoImage;
        public VideoPlayer videoPlayer;
        public AudioSource audioSource;
        public RenderTexture renderTexture;
        public Button decreaseSizeButton;
        public Button increaseSizeButton;
        public Button decreaseVolumeButton;
        public Button increaseVolumeButton;
        public Button returnButton;
        public TMP_Text sizeReadoutText;
        public TMP_Text volumeReadoutText;
        public GameObject mainEntryRoot;
        public string mainEntrySceneName = "00_MainEntry_BPlus";
        public bool returnBySceneLoad;
        public float minScale = 0.8f;
        public float maxScale = 1.3f;
        public float scaleStep = 0.1f;
        public float volumeStep = 0.1f;
        [Range(0f, 1f)] public float startVolume = 0.54f;

        private Vector2 _baseVideoSize;
        private float _videoScale = 1f;
        private float _volume = 0.54f;

        private void Awake()
        {
            ResolveReferences();
            BindButtons();
            InitializeVideoOutput();
            _volume = Mathf.Clamp01(startVolume);
            if (videoRoot != null && _baseVideoSize == Vector2.zero)
            {
                _baseVideoSize = videoRoot.sizeDelta;
            }

            ApplyState();
        }

        private void OnEnable()
        {
            ResolveReferences();
            InitializeVideoOutput();
            ApplyState();
            TryPlay();
        }

        public void DecreaseVideoSize()
        {
            _videoScale = Mathf.Clamp(_videoScale - Mathf.Max(0.01f, scaleStep), minScale, maxScale);
            ApplyState();
        }

        public void IncreaseVideoSize()
        {
            _videoScale = Mathf.Clamp(_videoScale + Mathf.Max(0.01f, scaleStep), minScale, maxScale);
            ApplyState();
        }

        public void DecreaseVolume()
        {
            _volume = Mathf.Clamp01(_volume - Mathf.Max(0.01f, volumeStep));
            ApplyState();
        }

        public void IncreaseVolume()
        {
            _volume = Mathf.Clamp01(_volume + Mathf.Max(0.01f, volumeStep));
            ApplyState();
        }

        public void ReturnToMainEntry()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }

            if (returnBySceneLoad)
            {
                SceneManager.LoadScene(mainEntrySceneName);
                return;
            }

            if (mainEntryRoot != null)
            {
                mainEntryRoot.SetActive(true);
                gameObject.SetActive(false);
            }
        }

        private void ResolveReferences()
        {
            if (videoRoot == null) videoRoot = BPlusUiRuntimeUtility.FindRect(transform, "VideoRoot");
            if (videoImage == null && videoRoot != null) videoImage = videoRoot.GetComponentInChildren<RawImage>(true);
            if (videoPlayer == null) videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>(true);
            if (decreaseSizeButton == null) decreaseSizeButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_DecreaseSize");
            if (increaseSizeButton == null) increaseSizeButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_IncreaseSize");
            if (decreaseVolumeButton == null) decreaseVolumeButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_DecreaseVolume");
            if (increaseVolumeButton == null) increaseVolumeButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_IncreaseVolume");
            if (returnButton == null) returnButton = BPlusUiRuntimeUtility.FindButton(transform, "Button_Return");
            if (sizeReadoutText == null) sizeReadoutText = BPlusUiRuntimeUtility.FindText(transform, "Size");
            if (volumeReadoutText == null) volumeReadoutText = BPlusUiRuntimeUtility.FindText(transform, "Volume");
        }

        private void BindButtons()
        {
            BPlusUiRuntimeUtility.Bind(decreaseSizeButton, DecreaseVideoSize);
            BPlusUiRuntimeUtility.Bind(increaseSizeButton, IncreaseVideoSize);
            BPlusUiRuntimeUtility.Bind(decreaseVolumeButton, DecreaseVolume);
            BPlusUiRuntimeUtility.Bind(increaseVolumeButton, IncreaseVolume);
            BPlusUiRuntimeUtility.Bind(returnButton, ReturnToMainEntry);
        }

        private void InitializeVideoOutput()
        {
            if (videoRoot == null) return;

            if (videoImage == null)
            {
                var imageGo = new GameObject("VideoRawImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                imageGo.transform.SetParent(videoRoot, false);
                var rect = imageGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                videoImage = imageGo.GetComponent<RawImage>();
                videoImage.color = new Color(1f, 1f, 1f, 0.86f);
            }

            if (renderTexture == null)
            {
                renderTexture = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32)
                {
                    name = "BPlusSceneVideoRT"
                };
            }

            videoImage.texture = renderTexture;

            if (videoPlayer == null)
            {
                videoPlayer = videoRoot.gameObject.AddComponent<VideoPlayer>();
            }

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

            if (audioSource == null)
            {
                audioSource = videoRoot.gameObject.AddComponent<AudioSource>();
            }

            videoPlayer.SetTargetAudioSource(0, audioSource);
        }

        private void TryPlay()
        {
            if (videoPlayer != null && videoPlayer.clip != null && !videoPlayer.isPlaying)
            {
                videoPlayer.Play();
            }
        }

        private void ApplyState()
        {
            _videoScale = Mathf.Clamp(_videoScale, minScale, maxScale);
            _volume = Mathf.Clamp01(_volume);

            if (videoRoot != null)
            {
                if (_baseVideoSize == Vector2.zero)
                {
                    _baseVideoSize = videoRoot.sizeDelta;
                }

                videoRoot.sizeDelta = _baseVideoSize * _videoScale;
            }

            if (audioSource != null)
            {
                audioSource.volume = _volume;
            }

            BPlusUiRuntimeUtility.SetText(sizeReadoutText, "画面大小 " + Mathf.RoundToInt(_videoScale * 100f) + "%");
            BPlusUiRuntimeUtility.SetText(volumeReadoutText, "音量 " + Mathf.RoundToInt(_volume * 100f) + "%");
        }
    }
}
