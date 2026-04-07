using UnityEngine;

namespace DiceMadness.Core
{
    [DefaultExecutionOrder(-220)]
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("UI Clips")]
        [SerializeField] private AudioClip uiClickClip;
        [SerializeField] private AudioClip uiConfirmClip;
        [SerializeField] private AudioClip uiDenyClip;
        [SerializeField] private AudioClip uiHoverClip;

        [Header("SFX Clips")]
        [SerializeField] private AudioClip diceHitTableClip;
        [SerializeField] private AudioClip diceHitDiceClip;
        [SerializeField] private AudioClip fullDiceRollClip;

        [Header("Placeholder Volumes")]
        [Range(0f, 1f)]
        [SerializeField] private float uiVolume = 1f;

        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 1f;

        [Header("Per-Sound Multipliers")]
        [Range(0f, 1f)]
        [SerializeField] private float uiHoverVolumeScale = 0.2f;

        public float UiVolume => uiVolume;
        public float SfxVolume => sfxVolume;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            ApplyVolumes();
            GameSettingsService.SettingsChanged -= HandleSettingsChanged;
            GameSettingsService.SettingsChanged += HandleSettingsChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                GameSettingsService.SettingsChanged -= HandleSettingsChanged;
                Instance = null;
            }
        }

        private void OnValidate()
        {
            uiVolume = Mathf.Clamp01(uiVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            uiHoverVolumeScale = Mathf.Clamp01(uiHoverVolumeScale);

            if (!Application.isPlaying)
            {
                SyncExistingAudioSources();
            }

            ApplyVolumes();
        }

        public void SetUiVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void PlayUISound(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || uiSource == null)
            {
                return;
            }

            uiSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position)
        {
            if (clip == null)
            {
                return;
            }

            GameObject tempObject = new GameObject("TempSFX");
            tempObject.transform.position = position;

            AudioSource tempSource = tempObject.AddComponent<AudioSource>();
            tempSource.playOnAwake = false;
            tempSource.loop = false;
            tempSource.spatialBlend = 1f;
            tempSource.volume = ResolveSfxVolume();
            tempSource.clip = clip;
            tempSource.Play();

            Destroy(tempObject, Mathf.Max(clip.length, 0.1f) + 0.05f);
        }

        public void PlayUIClick()
        {
            PlayUISound(uiClickClip);
        }

        public void PlayUIConfirm()
        {
            PlayUISound(uiConfirmClip);
        }

        public void PlayUIDeny()
        {
            PlayUISound(uiDenyClip);
        }

        public void PlayUIHover()
        {
            PlayUISound(uiHoverClip, uiHoverVolumeScale);
        }

        public void PlayDiceHitTable()
        {
            PlaySFX(diceHitTableClip);
        }

        public void PlayDiceHitDice()
        {
            PlaySFX(diceHitDiceClip);
        }

        public void PlayFullDiceRollFallback()
        {
            PlaySFX(fullDiceRollClip);
        }

        private void EnsureAudioSources()
        {
            uiSource = EnsureChildSource("UIAudioSource", uiSource);
            sfxSource = EnsureChildSource("SFXAudioSource", sfxSource);

            ConfigureUiSource(uiSource);
            ConfigureSfxSource(sfxSource);
        }

        private void SyncExistingAudioSources()
        {
            uiSource = FindExistingChildSource("UIAudioSource", uiSource);
            sfxSource = FindExistingChildSource("SFXAudioSource", sfxSource);

            ConfigureUiSource(uiSource);
            ConfigureSfxSource(sfxSource);
        }

        private AudioSource EnsureChildSource(string childName, AudioSource existingSource)
        {
            if (existingSource != null)
            {
                return existingSource;
            }

            Transform child = null;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform candidate = transform.GetChild(i);
                if (candidate.name == childName)
                {
                    child = candidate;
                    break;
                }
            }

            if (child == null)
            {
                GameObject childObject = new GameObject(childName);
                childObject.transform.SetParent(transform, false);
                child = childObject.transform;
            }

            AudioSource source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            return source;
        }

        private AudioSource FindExistingChildSource(string childName, AudioSource existingSource)
        {
            if (existingSource != null)
            {
                return existingSource;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform candidate = transform.GetChild(i);
                if (candidate.name != childName)
                {
                    continue;
                }

                AudioSource source = candidate.GetComponent<AudioSource>();
                if (source != null)
                {
                    return source;
                }
            }

            return null;
        }

        private void ApplyVolumes()
        {
            if (uiSource != null)
            {
                uiSource.volume = ResolveUiVolume();
            }

            if (sfxSource != null)
            {
                sfxSource.volume = ResolveSfxVolume();
            }
        }

        private void HandleSettingsChanged()
        {
            ApplyVolumes();
        }

        private float ResolveUiVolume()
        {
            GameSettingsData settings = GameSettingsService.Current;
            return Mathf.Clamp01(settings.sfxVolume * uiVolume);
        }

        private float ResolveSfxVolume()
        {
            GameSettingsData settings = GameSettingsService.Current;
            return Mathf.Clamp01(settings.sfxVolume * sfxVolume);
        }

        private static void ConfigureUiSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }

        private static void ConfigureSfxSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.volume = 1f;
        }
    }
}
