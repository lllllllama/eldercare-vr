using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace PicoElderCare.Rehab
{
    [Serializable]
    public class RehabMovementVideoBinding
    {
        public string movementId;
        public string movementName;
        public VideoClip videoClip;
    }

    public enum RehabVideoDisplayMode
    {
        RawImage,
        QuadMaterial
    }

    public class RehabVideoGuideController : MonoBehaviour
    {
        private static readonly string[] DefaultBaduanjinMovementNames =
        {
            "双手托天理三焦",
            "左右开弓似射雕",
            "调理脾胃须单举",
            "五劳七伤往后瞧",
            "摇头摆尾去心火",
            "两手攀足固肾腰",
            "攒拳怒目增气力",
            "背后七颠百病消"
        };

        public GameObject videoPanel;
        public GameObject displayRoot;
        public RawImage rawImage;
        public VideoPlayer videoPlayer;
        public AudioSource audioSource;
        public GameObject videoQuad;
        public Renderer videoQuadRenderer;
        public Material videoMaterial;
        public RenderTexture renderTexture;
        public RehabVideoPanelLayoutController layoutController;
        public RehabSessionManager sessionManager;
        public RehabVideoDisplayMode displayMode = RehabVideoDisplayMode.QuadMaterial;
        public bool requireActiveSession = true;
        public bool muteAudio;
        public float volume = 0.7f;
        public bool loopVideo = false;
        public bool showDebugFrame = false;
        public bool autoCreateVideoFrame = true;
        public Color videoFrameColor = new Color(0.06f, 0.18f, 0.2f, 0.96f);
        public Color videoFrameAccentColor = new Color(0.2f, 0.92f, 1f, 0.72f);
        public float videoFramePadding = 0.045f;
        public float videoFrameThickness = 0.035f;
        public GameObject debugBackground;
        public GameObject debugBorder;
        public RehabMovementVideoBinding[] bindings;

        private bool _playWhenPrepared;
        private string _currentMovementKey;
        private Transform _videoFrameRoot;
        private Material _videoFrameMaterial;
        private Material _videoFrameAccentMaterial;
        private RehabSpatialRayControl _spatialRayControl;
        private bool _pauseWhenPrepared;

        private void Awake()
        {
            ResolveReferences();
            HideAllDisplays();
        }

        private void Reset()
        {
            if (videoPanel == null)
            {
                videoPanel = gameObject;
            }

            ResolveReferences();
        }

        private void OnDisable()
        {
            UnsubscribePrepareCompleted();
        }

        public void PlayForMovement(string movementId, string movementName)
        {
            ResolveReferences();

            if (requireActiveSession && sessionManager != null && !sessionManager.IsTrainingActive)
            {
                StopAndHide();
                Debug.Log($"Skip rehab video guide because current movement is not training. movementId={movementId}, movementName={movementName}", this);
                return;
            }

            if (string.IsNullOrWhiteSpace(movementName))
            {
                StopAndHide();
                Debug.Log($"Skip rehab video guide because movementName is empty. movementId={movementId}", this);
                return;
            }

            var binding = FindBinding(movementId, movementName);
            if (binding == null || binding.videoClip == null)
            {
                StopAndHide();
                Debug.Log($"Current movement has no bound video. movementId={movementId}, movementName={movementName}", this);
                return;
            }

            if (videoPlayer == null)
            {
                StopAndHide();
                Debug.LogWarning("Cannot play rehab video guide because VideoPlayer is not assigned.", this);
                return;
            }

            EnsurePlaybackObjectsReady();
            PlacePanelForCurrentView();
            ApplyDisplayVisible(true);

            _playWhenPrepared = false;
            UnsubscribePrepareCompleted();
            videoPlayer.Stop();
            videoPlayer.clip = binding.videoClip;
            loopVideo = false;
            videoPlayer.isLooping = false;
            _currentMovementKey = movementId + "|" + movementName;
            EnsureRenderTextureBinding();
            ConfigureAudio();
            ApplyDebugFrameVisible();

            _playWhenPrepared = true;
            _pauseWhenPrepared = false;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();

            Debug.Log($"Playing rehab video guide for movement: {binding.movementName} ({binding.videoClip.name})", this);
            LogVideoState("Prepare requested", binding.videoClip);
        }

        public void Stop()
        {
            StopAndHide();
        }

        public void Pause()
        {
            ResolveReferences();
            if (videoPlayer == null || videoPlayer.clip == null)
            {
                Debug.Log("Pause rehab video guide skipped because no current video is loaded.", this);
                return;
            }

            EnsureDisplayVisibleWhilePaused();

            if (videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
                _playWhenPrepared = false;
                _pauseWhenPrepared = false;
            }
            else if (!videoPlayer.isPrepared)
            {
                _playWhenPrepared = true;
                _pauseWhenPrepared = true;
                UnsubscribePrepareCompleted();
                videoPlayer.prepareCompleted += OnVideoPrepared;
                videoPlayer.Prepare();
            }

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Pause();
            }

            Debug.Log("Pause rehab video guide", this);
        }

        public void Resume()
        {
            ResolveReferences();
            _pauseWhenPrepared = false;

            if (videoPlayer == null || videoPlayer.clip == null)
            {
                return;
            }

            EnsurePlaybackObjectsReady();
            PlacePanelForCurrentView();
            ApplyDisplayVisible(true);
            EnsureRenderTextureBinding();
            ConfigureAudio();
            ApplyDebugFrameVisible();

            if (videoPlayer.isPrepared)
            {
                videoPlayer.Play();
                if (audioSource != null)
                {
                    audioSource.UnPause();
                }

                Debug.Log("Resume rehab video guide", this);
                return;
            }

            _playWhenPrepared = true;
            UnsubscribePrepareCompleted();
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
            Debug.Log("Prepare rehab video guide for resume", this);
        }

        public void StopAndHide()
        {
            _playWhenPrepared = false;
            _pauseWhenPrepared = false;
            UnsubscribePrepareCompleted();
            StopCurrentVideo();
            if (videoPlayer != null)
            {
                videoPlayer.clip = null;
            }

            if (audioSource != null)
            {
                audioSource.Stop();
            }

            _currentMovementKey = null;
            HideAllDisplays();
            Debug.Log("Stop video guide", this);
        }

        public float GetVideoDurationForMovement(string movementId, string movementName)
        {
            ResolveReferences();

            var binding = FindBinding(movementId, movementName);
            if (binding != null && binding.videoClip != null && binding.videoClip.length > 0f)
            {
                return (float)binding.videoClip.length;
            }

            return -1f;
        }

        public bool HasPlayableCurrentVideo()
        {
            return videoPlayer != null && videoPlayer.clip != null && videoPlayer.clip.length > 0f;
        }

        public float GetCurrentVideoDurationSeconds()
        {
            if (!HasPlayableCurrentVideo()) return -1f;
            return (float)videoPlayer.clip.length;
        }

        public float GetCurrentVideoTimeSeconds()
        {
            if (!HasPlayableCurrentVideo()) return 0f;
            var time = videoPlayer.time;
            if (double.IsNaN(time) || double.IsInfinity(time))
            {
                return 0f;
            }

            return Mathf.Clamp((float)time, 0f, GetCurrentVideoDurationSeconds());
        }

        public float GetCurrentVideoRemainingSeconds(float fallbackSeconds)
        {
            var duration = GetCurrentVideoDurationSeconds();
            if (duration <= 0f) return Mathf.Max(0f, fallbackSeconds);

            var time = GetCurrentVideoTimeSeconds();
            return Mathf.Max(0f, duration - time);
        }

        public bool CurrentVideoReachedEnd(float epsilonSeconds = 0.08f)
        {
            var duration = GetCurrentVideoDurationSeconds();
            if (duration <= 0f) return false;

            return GetCurrentVideoTimeSeconds() >= duration - Mathf.Max(0f, epsilonSeconds);
        }

        public void SetSessionManager(RehabSessionManager manager)
        {
            sessionManager = manager;
        }

        public void PlacePanelForCurrentView()
        {
            ResolveReferences();
            if (layoutController != null)
            {
                layoutController.PlaceInRightFrontOfUserOnce();
            }
        }

        public void EnsureDisplayVisibleWhilePaused()
        {
            ResolveReferences();
            EnsurePlaybackObjectsReady();
            ApplyDisplayVisible(true);
            EnsureRenderTextureBinding();
            ApplyDebugFrameVisible();
        }

        [ContextMenu("Create Default Baduanjin Video Bindings")]
        private void CreateDefaultBaduanjinVideoBindings()
        {
            var previousBindings = bindings;
            var defaultBindings = new RehabMovementVideoBinding[DefaultBaduanjinMovementNames.Length];

            for (var i = 0; i < DefaultBaduanjinMovementNames.Length; i++)
            {
                var movementName = DefaultBaduanjinMovementNames[i];
                var existing = FindExistingBindingForDefault(previousBindings, movementName, i);
                defaultBindings[i] = new RehabMovementVideoBinding
                {
                    movementId = existing != null ? existing.movementId : string.Empty,
                    movementName = movementName,
                    videoClip = existing != null ? existing.videoClip : null
                };
            }

            bindings = defaultBindings;
            Debug.Log("Created default Baduanjin video bindings.", this);
        }

        private RehabMovementVideoBinding FindBinding(string movementId, string movementName)
        {
            if (bindings == null || bindings.Length == 0)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(movementId))
            {
                for (var i = 0; i < bindings.Length; i++)
                {
                    var binding = bindings[i];
                    if (binding != null && string.Equals(binding.movementId, movementId, StringComparison.Ordinal))
                    {
                        return binding;
                    }
                }
            }

            if (!string.IsNullOrEmpty(movementName))
            {
                for (var i = 0; i < bindings.Length; i++)
                {
                    var binding = bindings[i];
                    if (binding != null && string.Equals(binding.movementName, movementName, StringComparison.Ordinal))
                    {
                        return binding;
                    }
                }
            }

            return null;
        }

        private RehabMovementVideoBinding FindExistingBindingForDefault(
            RehabMovementVideoBinding[] previousBindings,
            string movementName,
            int defaultIndex)
        {
            if (previousBindings == null || previousBindings.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < previousBindings.Length; i++)
            {
                var binding = previousBindings[i];
                if (binding != null && string.Equals(binding.movementName, movementName, StringComparison.Ordinal))
                {
                    return binding;
                }
            }

            if (defaultIndex >= 0 && defaultIndex < previousBindings.Length)
            {
                var indexedBinding = previousBindings[defaultIndex];
                if (indexedBinding != null && indexedBinding.videoClip != null)
                {
                    return indexedBinding;
                }
            }

            return null;
        }

        private void ConfigureAudio()
        {
            if (audioSource == null) return;

            audioSource.enabled = true;
            muteAudio = false;
            audioSource.playOnAwake = false;
            audioSource.mute = false;
            audioSource.volume = Mathf.Clamp01(volume);
            audioSource.spatialBlend = 0f;

            if (videoPlayer == null) return;

            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.controlledAudioTrackCount = 1;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audioSource);
        }

        private void EnsureRenderTextureBinding()
        {
            if (renderTexture == null && videoPlayer != null)
            {
                renderTexture = videoPlayer.targetTexture;
            }

            if (renderTexture == null && rawImage != null)
            {
                renderTexture = rawImage.texture as RenderTexture;
            }

            if (renderTexture == null && videoMaterial != null)
            {
                renderTexture = videoMaterial.mainTexture as RenderTexture;
            }

            if (renderTexture != null)
            {
                if (videoPlayer != null)
                {
                    videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                    videoPlayer.targetTexture = renderTexture;
                }

                if (rawImage != null)
                {
                    rawImage.texture = renderTexture;
                    rawImage.color = Color.white;
                }

                ApplyRenderTextureToQuadMaterial();

                return;
            }

            if (rawImage == null || rawImage.texture == null)
            {
                Debug.LogWarning("Rehab video guide has no RenderTexture assigned to VideoPlayer or RawImage.", this);
            }
        }

        private void StopCurrentVideo()
        {
            if (videoPlayer != null)
            {
                videoPlayer.Stop();
            }
        }

        private void ApplyDisplayVisible(bool visible)
        {
            if (displayMode == RehabVideoDisplayMode.QuadMaterial)
            {
                if (videoQuad != null)
                {
                    videoQuad.SetActive(visible);
                }

                if (displayRoot != null)
                {
                    displayRoot.SetActive(false);
                }

                SetSpatialControlsVisible(visible);
                return;
            }

            if (displayRoot != null)
            {
                displayRoot.SetActive(visible);
            }

            if (videoQuad != null)
            {
                videoQuad.SetActive(false);
            }

            SetSpatialControlsVisible(visible);
        }

        private void HideAllDisplays()
        {
            if (videoQuad != null)
            {
                videoQuad.SetActive(false);
            }

            if (displayRoot != null)
            {
                displayRoot.SetActive(false);
            }

            SetSpatialControlsVisible(false);
            ApplyDebugFrameVisible();
        }

        private void EnsurePlaybackObjectsReady()
        {
            if (videoPanel != null && !videoPanel.activeSelf)
            {
                videoPanel.SetActive(true);
            }

            if (displayMode == RehabVideoDisplayMode.RawImage && displayRoot != null && !displayRoot.activeSelf)
            {
                displayRoot.SetActive(true);
            }

            if (displayMode == RehabVideoDisplayMode.RawImage && rawImage != null && !rawImage.gameObject.activeSelf)
            {
                rawImage.gameObject.SetActive(true);
            }

            if (displayMode == RehabVideoDisplayMode.QuadMaterial && videoQuad != null && !videoQuad.activeSelf)
            {
                videoQuad.SetActive(true);
            }

            EnsureVideoPanelVisuals();

            if (videoPlayer != null)
            {
                videoPlayer.enabled = true;
                videoPlayer.playOnAwake = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.isLooping = false;
            }

            if (audioSource != null)
            {
                audioSource.enabled = true;
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }
        }

        private void OnVideoPrepared(VideoPlayer source)
        {
            if ((!_playWhenPrepared && !_pauseWhenPrepared) || source != videoPlayer) return;

            EnsurePlaybackObjectsReady();
            PlacePanelForCurrentView();
            ApplyDisplayVisible(true);
            EnsureRenderTextureBinding();
            ApplyDebugFrameVisible();
            if (_pauseWhenPrepared)
            {
                source.Play();
                source.Pause();
                if (audioSource != null)
                {
                    audioSource.Pause();
                }

                _playWhenPrepared = false;
                _pauseWhenPrepared = false;
                LogVideoState("Prepared and paused", source.clip);
                return;
            }

            source.Play();
            if (audioSource != null)
            {
                audioSource.UnPause();
            }

            LogVideoState("Playback started", source.clip);
        }

        private void UnsubscribePrepareCompleted()
        {
            if (videoPlayer != null)
            {
                videoPlayer.prepareCompleted -= OnVideoPrepared;
            }
        }

        private void ApplyDebugFrameVisible()
        {
            if (debugBackground != null)
            {
                debugBackground.SetActive(false);
            }

            if (debugBorder != null)
            {
                debugBorder.SetActive(showDebugFrame);
            }
        }

        private void ApplyRenderTextureToQuadMaterial()
        {
            if (renderTexture == null) return;

            if (videoMaterial == null && videoQuadRenderer != null)
            {
                videoMaterial = videoQuadRenderer.sharedMaterial;
            }

            if (videoMaterial == null) return;

            EnsureVideoMaterialShader(videoMaterial);
            SetMaterialTexture(videoMaterial, renderTexture);

            if (videoQuadRenderer != null && videoQuadRenderer.sharedMaterial != videoMaterial)
            {
                videoQuadRenderer.sharedMaterial = videoMaterial;
            }
        }

        private static void EnsureVideoMaterialShader(Material material)
        {
            if (material == null) return;

            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Texture");
            if (shader != null && material.shader != shader)
            {
                material.shader = shader;
            }
        }

        private static void SetMaterialTexture(Material material, Texture texture)
        {
            if (material == null || texture == null) return;

            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_Cull"))
            {
                material.SetFloat("_Cull", 0f);
            }
        }

        private void LogVideoState(string phase, VideoClip clip)
        {
            var targetTextureName = videoPlayer != null && videoPlayer.targetTexture != null
                ? videoPlayer.targetTexture.name
                : "null";
            var rawImageTextureName = rawImage != null && rawImage.texture != null
                ? rawImage.texture.name
                : "null";
            var displayActive = displayRoot != null
                ? $"{displayRoot.activeSelf}/{displayRoot.activeInHierarchy}"
                : "null";
            var rawImageActive = rawImage != null
                ? $"{rawImage.gameObject.activeSelf}/{rawImage.gameObject.activeInHierarchy}"
                : "null";
            var quadActive = videoQuad != null
                ? $"{videoQuad.activeSelf}/{videoQuad.activeInHierarchy}"
                : "null";
            var quadTextureName = videoQuadRenderer != null &&
                                  videoQuadRenderer.sharedMaterial != null &&
                                  videoQuadRenderer.sharedMaterial.mainTexture != null
                ? videoQuadRenderer.sharedMaterial.mainTexture.name
                : "null";
            var panelPosition = videoPanel != null ? videoPanel.transform.position.ToString("F3") : "null";
            var panelRotation = videoPanel != null ? videoPanel.transform.rotation.eulerAngles.ToString("F1") : "null";
            var isPrepared = videoPlayer != null && videoPlayer.isPrepared;
            var isPlaying = videoPlayer != null && videoPlayer.isPlaying;

            Debug.Log(
                $"Rehab video guide {phase}: clip={(clip != null ? clip.name : "null")}, " +
                $"targetTexture={targetTextureName}, rawImageTexture={rawImageTextureName}, " +
                $"displayMode={displayMode}, videoQuad activeSelf/activeInHierarchy={quadActive}, " +
                $"videoQuadTexture={quadTextureName}, " +
                $"videoCanvas activeSelf/activeInHierarchy={displayActive}, " +
                $"videoRawImage activeSelf/activeInHierarchy={rawImageActive}, " +
                $"panelPosition={panelPosition}, panelRotation={panelRotation}, " +
                $"isPrepared={isPrepared}, isPlaying={isPlaying}",
                this);
        }

        private void ResolveReferences()
        {
            if (videoPanel == null)
            {
                videoPanel = gameObject;
            }

            if (rawImage == null)
            {
                rawImage = GetComponentInChildren<RawImage>(true);
            }

            if (videoPlayer == null)
            {
                videoPlayer = GetComponentInChildren<VideoPlayer>(true);
            }

            if (audioSource == null)
            {
                audioSource = GetComponentInChildren<AudioSource>(true);
            }

            if (videoQuad == null)
            {
                var quad = transform.Find("VideoQuad");
                if (quad != null)
                {
                    videoQuad = quad.gameObject;
                }
            }

            if (videoQuadRenderer == null && videoQuad != null)
            {
                videoQuadRenderer = videoQuad.GetComponent<Renderer>();
            }

            if (videoMaterial == null && videoQuadRenderer != null)
            {
                videoMaterial = videoQuadRenderer.sharedMaterial;
            }

            if (layoutController == null)
            {
                layoutController = GetComponent<RehabVideoPanelLayoutController>();
            }

            if (layoutController != null)
            {
                if (layoutController.panelRoot == null)
                {
                    layoutController.panelRoot = transform;
                }

                if (layoutController.videoQuad == null && videoQuad != null)
                {
                    layoutController.videoQuad = videoQuad.transform;
                }

                layoutController.followTrainingAreaRoot = false;
                layoutController.preferPromptCanvasLayout = false;
                if (layoutController.panelDistance < 2.05f)
                {
                    layoutController.panelDistance = 2.15f;
                }

                if (layoutController.heightOffset < 0.02f)
                {
                    layoutController.heightOffset = 0.08f;
                }
            }

            if (sessionManager == null)
            {
                sessionManager = FindObjectOfType<RehabSessionManager>(true);
            }

            if (displayRoot == null)
            {
                var canvas = GetComponentInChildren<Canvas>(true);
                if (canvas != null && canvas.gameObject != gameObject)
                {
                    displayRoot = canvas.gameObject;
                }
                else if (rawImage != null)
                {
                    displayRoot = rawImage.gameObject;
                }
            }

            if (debugBackground == null && displayRoot != null)
            {
                var background = displayRoot.transform.Find("DebugBackground");
                if (background != null)
                {
                    debugBackground = background.gameObject;
                }
            }

            if (debugBorder == null && displayRoot != null)
            {
                var border = displayRoot.transform.Find("DebugBorder");
                if (border != null)
                {
                    debugBorder = border.gameObject;
                }
            }

            EnsureRenderTextureBinding();
            EnsureVideoPanelVisuals();

            if (layoutController != null)
            {
                _spatialRayControl = RehabSpatialRayControl.EnsureRuntime(sessionManager, layoutController);
            }
        }

        private void SetSpatialControlsVisible(bool visible)
        {
            if (layoutController == null)
            {
                return;
            }

            _spatialRayControl = _spatialRayControl != null
                ? _spatialRayControl
                : RehabSpatialRayControl.EnsureRuntime(sessionManager, layoutController);

            if (_spatialRayControl != null)
            {
                _spatialRayControl.SetControlCanvasVisible(visible);
            }
        }

        private void EnsureVideoPanelVisuals()
        {
            if (!autoCreateVideoFrame || videoQuad == null) return;

            var root = videoQuad.transform.Find("VideoFrameRoot");
            if (root == null)
            {
                var rootObject = new GameObject("VideoFrameRoot");
                rootObject.transform.SetParent(videoQuad.transform, false);
                root = rootObject.transform;
            }

            _videoFrameRoot = root;
            _videoFrameRoot.localPosition = new Vector3(0f, 0f, -0.012f);
            _videoFrameRoot.localRotation = Quaternion.identity;
            _videoFrameRoot.localScale = Vector3.one;

            var padding = Mathf.Clamp(videoFramePadding, 0.01f, 0.14f);
            var thickness = Mathf.Clamp(videoFrameThickness, 0.012f, 0.09f);
            var width = 1f + padding * 2f;
            var height = 1f + padding * 2f;
            var depth = 0.018f;

            ConfigureFrameBar("FrameTop", new Vector3(0f, 0.5f + padding + thickness * 0.5f, 0f), new Vector3(width + thickness * 2f, thickness, depth), true);
            ConfigureFrameBar("FrameBottom", new Vector3(0f, -0.5f - padding - thickness * 0.5f, 0f), new Vector3(width + thickness * 2f, thickness, depth), false);
            ConfigureFrameBar("FrameLeft", new Vector3(-0.5f - padding - thickness * 0.5f, 0f, 0f), new Vector3(thickness, height, depth), false);
            ConfigureFrameBar("FrameRight", new Vector3(0.5f + padding + thickness * 0.5f, 0f, 0f), new Vector3(thickness, height, depth), false);
        }

        private void ConfigureFrameBar(string name, Vector3 localPosition, Vector3 localScale, bool accent)
        {
            if (_videoFrameRoot == null) return;

            var child = _videoFrameRoot.Find(name);
            GameObject bar;
            if (child == null)
            {
                bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = name;
                bar.transform.SetParent(_videoFrameRoot, false);
                var collider = bar.GetComponent<Collider>();
                if (collider != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(collider);
                    }
                    else
                    {
                        DestroyImmediate(collider);
                    }
                }
            }
            else
            {
                bar = child.gameObject;
            }

            bar.transform.localPosition = localPosition;
            bar.transform.localRotation = Quaternion.identity;
            bar.transform.localScale = localScale;

            var renderer = bar.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = accent ? GetVideoFrameAccentMaterial() : GetVideoFrameMaterial();
            }
        }

        private Material GetVideoFrameMaterial()
        {
            if (_videoFrameMaterial == null)
            {
                _videoFrameMaterial = CreateVideoFrameMaterial(videoFrameColor);
            }

            return _videoFrameMaterial;
        }

        private Material GetVideoFrameAccentMaterial()
        {
            if (_videoFrameAccentMaterial == null)
            {
                _videoFrameAccentMaterial = CreateVideoFrameMaterial(videoFrameAccentColor);
            }

            return _videoFrameAccentMaterial;
        }

        private static Material CreateVideoFrameMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ??
                         Shader.Find("Unlit/Color") ??
                         Shader.Find("Standard");
            if (shader == null) return null;

            var material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }
    }
}
