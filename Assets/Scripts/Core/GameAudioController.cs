using System.Collections.Generic;
using UnityEngine;

namespace Labyrinth.Core
{
    public enum GameSfx
    {
        Build,
        HeroCreated,
        Footstep,
        MenuOpen,
        MenuClose,
        HudOpen,
        HudClose,
        HudClick,
        HudTab,
        HudConfirm,
        HudBlocked,
        Purchase,
        PotionPurchase,
        RationPurchase,
        ForgeUpgrade,
        Potion,
        Ration,
        Pickup,
        KeyPickup,
        IngotPickup,
        Deposit,
        IngotDeposit,
        DoorOpen,
        StairsOpen,
        ChestOpen,
        GoldFound,
        EquipmentFound,
        FarmDelivery,
        LumberDelivery,
        TaxCollect,
        TaxDeposit,
        Fortify,
        TorchPlaced,
        LevelSwitch,
        CombatStart,
        CombatHit,
        Defeat,
        LevelUp,
        Victory
    }

    public sealed class GameAudioController : MonoBehaviour
    {
        private const string EditorMusicFolder = "Assets/Audio/Music";
        private const string ResourcesMusicFolder = "Music";
        private const int SampleRate = 44100;
        private const float MasterVolume = 0.64f;
        private const float SfxBusVolume = 1f;
        private const float UiBusVolume = 1.15f;
        private const float WorldMusicVolume = 0.055f;
        private const float MenuMusicVolume = 0.16f;
        private const float MinRepeatInterval = 0.045f;
        private const float TrackPauseSeconds = 5f;
        private const string MenuMusicClipName = "Menu";

        private static GameAudioController instance;

        private readonly Dictionary<GameSfx, AudioClip> clips = new Dictionary<GameSfx, AudioClip>();
        private readonly Dictionary<GameSfx, float> lastPlayedAt = new Dictionary<GameSfx, float>();
        private readonly List<AudioClip> musicClips = new List<AudioClip>();
        private readonly List<int> musicOrder = new List<int>();
        private readonly System.Random musicRandom = new System.Random();

        private AudioSource source;
        private AudioSource uiSource;
        private AudioSource musicSource;
        private MusicMode musicMode;
        private bool musicLoaded;
        private bool menuMusicLoaded;
        private bool waitingForNextTrack;
        private bool worldMusicSuspended;
        private double musicPauseEndRealtime;
        private double musicTrackEndDspTime = -1d;
        private double suspensionStartedRealtime;
        private double suspendedTrackRemainingSeconds = -1d;
        private int musicOrderIndex;
        private int lastMusicIndex = -1;
        private AudioClip menuMusicClip;

        private enum MusicMode
        {
            None,
            Menu,
            World
        }

        public static void Play(GameSfx sfx, Vector3 worldPosition, float volumeScale = 1f)
        {
            var controller = GetOrCreate();
            if (controller != null)
            {
                controller.PlayInternal(sfx, worldPosition, volumeScale, false);
            }
        }

        public static void PlayUi(GameSfx sfx, float volumeScale = 1f)
        {
            var controller = GetOrCreate();
            if (controller != null)
            {
                controller.PlayInternal(sfx, controller.transform.position, volumeScale, true);
            }
        }

        public static void StartWorldMusic()
        {
            var controller = GetOrCreate();
            if (controller != null)
            {
                controller.StartWorldMusicInternal();
            }
        }

        public static void StopWorldMusic()
        {
            if (instance != null)
            {
                instance.StopWorldMusicInternal();
            }
        }

        public static void StartMenuMusic()
        {
            var controller = GetOrCreate();
            if (controller != null)
            {
                controller.StartMenuMusicInternal();
            }
        }

        public static void StopMenuMusic()
        {
            if (instance != null)
            {
                instance.StopMenuMusicInternal();
            }
        }

        private void Awake()
        {
            instance = this;
            source = CreateBusSource("SFX Bus", 0.12f, SfxBusVolume, true);
            uiSource = CreateBusSource("UI Bus", 0f, UiBusVolume, true);
            musicSource = CreateBusSource("Music Bus", 0f, WorldMusicVolume, false);
            musicSource.loop = false;
            ConfigureMusicEffects(musicSource.gameObject);
        }

        private void Update()
        {
            if (musicMode != MusicMode.World || musicSource == null)
            {
                return;
            }

            if (ShouldSuspendWorldMusic())
            {
                SuspendWorldMusic();
                return;
            }

            ResumeWorldMusicIfSuspended();

            if (musicSource.clip != null && !waitingForNextTrack)
            {
                if (musicSource.isPlaying
                    && (musicTrackEndDspTime <= 0d || AudioSettings.dspTime + 0.05d < musicTrackEndDspTime))
                {
                    return;
                }

                musicSource.Stop();
                musicSource.clip = null;
                musicTrackEndDspTime = -1d;
                waitingForNextTrack = true;
                musicPauseEndRealtime = Time.realtimeSinceStartupAsDouble + TrackPauseSeconds;
                return;
            }

            if (waitingForNextTrack)
            {
                if (Time.realtimeSinceStartupAsDouble < musicPauseEndRealtime)
                {
                    return;
                }

                waitingForNextTrack = false;
            }

            PlayNextMusicTrack();
        }

        private static GameAudioController GetOrCreate()
        {
            if (instance != null)
            {
                return instance;
            }

            var audioObject = new GameObject("GameAudioController");
            return audioObject.AddComponent<GameAudioController>();
        }

        private AudioSource CreateBusSource(string busName, float spatialBlend, float busVolume, bool addReverb)
        {
            var busObject = new GameObject(busName);
            busObject.transform.SetParent(transform, false);
            var audioSource = busObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = spatialBlend;
            audioSource.volume = MasterVolume * busVolume;
            if (addReverb)
            {
                ConfigureDungeonReverb(busObject);
            }

            return audioSource;
        }

        private static void ConfigureDungeonReverb(GameObject busObject)
        {
            var reverb = busObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.StoneCorridor;
            reverb.dryLevel = 0f;
            reverb.room = -2600f;
            reverb.roomHF = -1800f;
            reverb.decayTime = 0.78f;
            reverb.reverbLevel = -1300f;
        }

        private static void ConfigureMusicEffects(GameObject busObject)
        {
            var lowPass = busObject.AddComponent<AudioLowPassFilter>();
            lowPass.cutoffFrequency = 17500f;
            lowPass.lowpassResonanceQ = 1f;
        }

        private void PlayInternal(GameSfx sfx, Vector3 worldPosition, float volumeScale, bool forceUiBus)
        {
            if (lastPlayedAt.TryGetValue(sfx, out var lastTime)
                && Time.unscaledTime - lastTime < MinRepeatInterval)
            {
                return;
            }

            lastPlayedAt[sfx] = Time.unscaledTime;
            var clip = GetClip(sfx);
            var targetSource = forceUiBus || IsUiSound(sfx) ? uiSource : source;
            var busVolume = targetSource == uiSource ? UiBusVolume : SfxBusVolume;
            var volume = Mathf.Clamp01(MasterVolume * busVolume * GetVolume(sfx) * Mathf.Max(0f, volumeScale));
            targetSource.transform.position = forceUiBus || IsUiSound(sfx) ? transform.position : worldPosition;
            targetSource.PlayOneShot(clip, volume);
        }

        private void StartWorldMusicInternal()
        {
            ReloadWorldMusicClips();
            if (musicClips.Count == 0)
            {
                GameDebugLog.Warning("Audio", $"No music clips found in {EditorMusicFolder} or Resources/{ResourcesMusicFolder}.");
                return;
            }

            musicMode = MusicMode.World;
            waitingForNextTrack = false;
            worldMusicSuspended = false;
            musicPauseEndRealtime = 0d;
            musicTrackEndDspTime = -1d;
            suspendedTrackRemainingSeconds = -1d;
            musicSource.Stop();
            musicSource.loop = false;
            musicSource.clip = null;
            if (!musicSource.isPlaying)
            {
                PlayNextMusicTrack();
            }

            GameDebugLog.Info("Audio", $"World music started: tracks={musicClips.Count}, pause={TrackPauseSeconds:0.#} real seconds.");
        }

        private void StopWorldMusicInternal()
        {
            if (musicMode != MusicMode.World)
            {
                return;
            }

            StopActiveMusic();
        }

        private void StartMenuMusicInternal()
        {
            ReloadMenuMusicClip();
            if (menuMusicClip == null)
            {
                GameDebugLog.Warning("Audio", $"No menu music clip found as {MenuMusicClipName} in {EditorMusicFolder} or Resources/{ResourcesMusicFolder}.");
                return;
            }

            if (musicMode == MusicMode.Menu && musicSource.isPlaying && musicSource.clip == menuMusicClip)
            {
                return;
            }

            musicMode = MusicMode.Menu;
            waitingForNextTrack = false;
            worldMusicSuspended = false;
            musicPauseEndRealtime = 0d;
            musicTrackEndDspTime = -1d;
            suspendedTrackRemainingSeconds = -1d;
            musicSource.Stop();
            musicSource.loop = true;
            musicSource.clip = menuMusicClip;
            musicSource.volume = MenuMusicVolume;
            musicSource.Play();
            GameDebugLog.Info("Audio", $"Menu music started: {menuMusicClip.name}.");
        }

        private void StopMenuMusicInternal()
        {
            if (musicMode != MusicMode.Menu)
            {
                return;
            }

            StopActiveMusic();
        }

        private void StopActiveMusic()
        {
            musicMode = MusicMode.None;
            waitingForNextTrack = false;
            worldMusicSuspended = false;
            musicPauseEndRealtime = 0d;
            musicTrackEndDspTime = -1d;
            suspendedTrackRemainingSeconds = -1d;
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.loop = false;
                musicSource.clip = null;
            }
        }

        private void PlayNextMusicTrack()
        {
            if (musicClips.Count == 0)
            {
                return;
            }

            var nextClip = GetNextMusicClip();
            if (nextClip == null)
            {
                return;
            }

            musicSource.clip = nextClip;
            musicSource.loop = false;
            musicSource.volume = WorldMusicVolume;
            musicSource.Play();
            musicTrackEndDspTime = AudioSettings.dspTime + Mathf.Max(0.1f, nextClip.length);
            GameDebugLog.Info("Audio", $"Music track started: {nextClip.name}, length={nextClip.length:0.##}s.");
        }

        private static bool ShouldSuspendWorldMusic()
        {
            return !Application.isFocused || Time.timeScale <= 0f;
        }

        private void SuspendWorldMusic()
        {
            if (worldMusicSuspended)
            {
                return;
            }

            worldMusicSuspended = true;
            suspensionStartedRealtime = Time.realtimeSinceStartupAsDouble;
            suspendedTrackRemainingSeconds = -1d;

            if (musicSource != null && musicSource.clip != null && musicSource.isPlaying)
            {
                suspendedTrackRemainingSeconds = musicTrackEndDspTime > 0d
                    ? System.Math.Max(0.1d, musicTrackEndDspTime - AudioSettings.dspTime)
                    : System.Math.Max(0.1d, musicSource.clip.length - musicSource.time);
                musicSource.Pause();
            }
        }

        private void ResumeWorldMusicIfSuspended()
        {
            if (!worldMusicSuspended)
            {
                return;
            }

            var suspendedDuration = Time.realtimeSinceStartupAsDouble - suspensionStartedRealtime;
            if (waitingForNextTrack)
            {
                musicPauseEndRealtime += suspendedDuration;
            }

            if (musicSource != null && musicSource.clip != null && suspendedTrackRemainingSeconds >= 0d)
            {
                musicTrackEndDspTime = AudioSettings.dspTime + suspendedTrackRemainingSeconds;
                musicSource.UnPause();
            }

            worldMusicSuspended = false;
            suspendedTrackRemainingSeconds = -1d;
        }

        private AudioClip GetNextMusicClip()
        {
            if (musicOrderIndex >= musicOrder.Count)
            {
                RebuildMusicOrder();
            }

            if (musicOrder.Count == 0)
            {
                return null;
            }

            var index = musicOrder[musicOrderIndex++];
            lastMusicIndex = index;
            return musicClips[index];
        }

        private void RebuildMusicOrder()
        {
            musicOrder.Clear();
            for (var i = 0; i < musicClips.Count; i++)
            {
                musicOrder.Add(i);
            }

            for (var i = musicOrder.Count - 1; i > 0; i--)
            {
                var j = musicRandom.Next(i + 1);
                var temp = musicOrder[i];
                musicOrder[i] = musicOrder[j];
                musicOrder[j] = temp;
            }

            if (musicOrder.Count > 1 && musicOrder[0] == lastMusicIndex)
            {
                var swapIndex = musicRandom.Next(1, musicOrder.Count);
                var temp = musicOrder[0];
                musicOrder[0] = musicOrder[swapIndex];
                musicOrder[swapIndex] = temp;
            }

            musicOrderIndex = 0;
        }

        private void LoadMusicClipsIfNeeded()
        {
            if (musicLoaded)
            {
                return;
            }

            ReloadWorldMusicClips();
        }

        private void ReloadWorldMusicClips()
        {
            musicLoaded = true;
            musicClips.Clear();
            musicOrder.Clear();
            musicOrderIndex = 0;
            lastMusicIndex = -1;
            RefreshEditorAudioAssets();
            LoadMusicFromResources();
            LoadMusicFromEditorAssets();
            musicClips.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
        }

        private void LoadMenuMusicClipIfNeeded()
        {
            if (menuMusicLoaded)
            {
                return;
            }

            ReloadMenuMusicClip();
        }

        private void ReloadMenuMusicClip()
        {
            menuMusicLoaded = true;
            menuMusicClip = null;
            RefreshEditorAudioAssets();
            menuMusicClip = Resources.Load<AudioClip>($"{ResourcesMusicFolder}/{MenuMusicClipName}");
            if (menuMusicClip == null)
            {
                LoadMenuMusicFromEditorAssets();
            }
        }

        private void LoadMusicFromResources()
        {
            var resourceClips = Resources.LoadAll<AudioClip>(ResourcesMusicFolder);
            foreach (var clip in resourceClips)
            {
                AddUniqueMusicClip(clip);
            }
        }

        private void LoadMusicFromEditorAssets()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { EditorMusicFolder });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                AddUniqueMusicClip(UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
#endif
        }

        private void LoadMenuMusicFromEditorAssets()
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets("t:AudioClip", new[] { EditorMusicFolder });
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null && IsMenuMusicClip(clip))
                {
                    menuMusicClip = clip;
                    return;
                }
            }
#endif
        }

        private static void RefreshEditorAudioAssets()
        {
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
#endif
        }

        private void AddUniqueMusicClip(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (IsMenuMusicClip(clip))
            {
                return;
            }

            foreach (var existingClip in musicClips)
            {
                if (existingClip == clip || existingClip.name == clip.name)
                {
                    return;
                }
            }

            musicClips.Add(clip);
        }

        private static bool IsMenuMusicClip(AudioClip clip)
        {
            return clip != null
                && string.Equals(clip.name, MenuMusicClipName, System.StringComparison.OrdinalIgnoreCase);
        }

        private AudioClip GetClip(GameSfx sfx)
        {
            if (clips.TryGetValue(sfx, out var clip))
            {
                return clip;
            }

            clip = CreateClip(sfx);
            clips[sfx] = clip;
            return clip;
        }

        private static bool IsUiSound(GameSfx sfx)
        {
            return sfx == GameSfx.MenuOpen
                || sfx == GameSfx.MenuClose
                || sfx == GameSfx.HudOpen
                || sfx == GameSfx.HudClose
                || sfx == GameSfx.HudClick
                || sfx == GameSfx.HudTab
                || sfx == GameSfx.HudConfirm
                || sfx == GameSfx.HudBlocked;
        }

        private static AudioClip CreateClip(GameSfx sfx)
        {
            switch (sfx)
            {
                case GameSfx.Build:
                    return CreateToneClip("SFX Build", 0.42f, t => Bell(t, 392f, 0.18f, 4.8f) + Bell(t - 0.08f, 523.25f, 0.14f, 5.2f));
                case GameSfx.HeroCreated:
                    return CreateToneClip("SFX Hero Created", 0.52f, t => Bell(t, 440f, 0.16f, 4.3f) + Bell(t - 0.12f, 659.25f, 0.18f, 4.8f));
                case GameSfx.Footstep:
                    return CreateToneClip("SFX Footstep", 0.16f, t => LowKnock(t, 112f, 0.075f) + Noise(t, 31) * Envelope(t, 0.11f, 0.003f, 0.09f) * 0.018f);
                case GameSfx.MenuOpen:
                    return CreateToneClip("SFX Menu Open", 0.42f, t => Bell(t, 330f, 0.08f, 4.8f) + Bell(t - 0.055f, 494f, 0.09f, 5.2f) + SoftClick(t, 0.06f));
                case GameSfx.MenuClose:
                    return CreateToneClip("SFX Menu Close", 0.34f, t => Bell(t, 494f, 0.075f, 5.8f) + Bell(t - 0.055f, 330f, 0.07f, 5.2f) + SoftClick(t - 0.09f, 0.045f));
                case GameSfx.HudOpen:
                    return CreateToneClip("SFX HUD Open", 0.28f, t => Bell(t, 587.33f, 0.075f, 6.2f) + Bell(t - 0.06f, 880f, 0.06f, 7f));
                case GameSfx.HudClose:
                    return CreateToneClip("SFX HUD Close", 0.24f, t => Bell(t, 740f, 0.06f, 7f) + Bell(t - 0.045f, 440f, 0.055f, 6.5f));
                case GameSfx.HudClick:
                    return CreateToneClip("SFX HUD Click", 0.14f, t => SoftClick(t, 0.11f) + Bell(t, 1108.73f, 0.035f, 12f));
                case GameSfx.HudTab:
                    return CreateToneClip("SFX HUD Tab", 0.18f, t => SoftClick(t, 0.08f) + Bell(t - 0.035f, 784f, 0.055f, 8.5f));
                case GameSfx.HudConfirm:
                    return CreateToneClip("SFX HUD Confirm", 0.34f, t => Bell(t, 659.25f, 0.075f, 6f) + Bell(t - 0.08f, 987.77f, 0.075f, 6.8f) + Bell(t - 0.15f, 1318.51f, 0.05f, 8f));
                case GameSfx.HudBlocked:
                    return CreateToneClip("SFX HUD Blocked", 0.22f, t => LowKnock(t, 86f, 0.08f) + Bell(t, 220f, 0.045f, 6f));
                case GameSfx.Purchase:
                    return CreateToneClip("SFX Purchase", 0.28f, t => Bell(t, 880f, 0.13f, 7f) + Bell(t - 0.055f, 1174.66f, 0.08f, 8f));
                case GameSfx.PotionPurchase:
                    return CreateToneClip("SFX Potion Purchase", 0.36f, t => Bell(t, 880f, 0.1f, 7f) + Bubble(t - 0.05f, 610f, 0.08f) + Bell(t - 0.14f, 1318.51f, 0.055f, 9f));
                case GameSfx.RationPurchase:
                    return CreateToneClip("SFX Ration Purchase", 0.32f, t => Bell(t, 740f, 0.08f, 7.5f) + SoftClick(t - 0.04f, 0.085f) + Bell(t - 0.13f, 494f, 0.06f, 7f));
                case GameSfx.ForgeUpgrade:
                    return CreateToneClip("SFX Forge Upgrade", 0.48f, t => LowKnock(t, 150f, 0.16f) + Bell(t - 0.045f, 1046.5f, 0.12f, 6.5f) + Bell(t - 0.18f, 1567.98f, 0.07f, 8f));
                case GameSfx.Potion:
                    return CreateToneClip("SFX Potion", 0.34f, t => Bubble(t, 520f, 0.13f) + Bubble(t - 0.09f, 720f, 0.1f));
                case GameSfx.Ration:
                    return CreateToneClip("SFX Ration", 0.3f, t => SoftClick(t, 0.08f) + Bell(t, 330f, 0.08f, 6.5f));
                case GameSfx.Pickup:
                    return CreateToneClip("SFX Pickup", 0.34f, t => Bell(t, 784f, 0.13f, 6f) + Bell(t - 0.07f, 1046.5f, 0.12f, 6.8f));
                case GameSfx.KeyPickup:
                    return CreateToneClip("SFX Key Pickup", 0.5f, t => Bell(t, 987.77f, 0.1f, 5.5f) + Bell(t - 0.08f, 1318.51f, 0.1f, 6f) + Bell(t - 0.18f, 1760f, 0.07f, 7f));
                case GameSfx.IngotPickup:
                    return CreateToneClip("SFX Ingot Pickup", 0.36f, t => Bell(t, 659.25f, 0.11f, 6.2f) + Bell(t - 0.055f, 987.77f, 0.1f, 7f) + LowKnock(t, 180f, 0.035f));
                case GameSfx.Deposit:
                    return CreateToneClip("SFX Deposit", 0.5f, t => Bell(t, 659.25f, 0.15f, 5f) + Bell(t - 0.08f, 880f, 0.13f, 5.6f) + Bell(t - 0.17f, 1174.66f, 0.1f, 6.5f));
                case GameSfx.IngotDeposit:
                    return CreateToneClip("SFX Ingot Deposit", 0.62f, t => LowKnock(t, 132f, 0.1f) + Bell(t - 0.05f, 587.33f, 0.14f, 5.4f) + Bell(t - 0.13f, 783.99f, 0.13f, 5.8f) + Bell(t - 0.24f, 1174.66f, 0.09f, 7f));
                case GameSfx.DoorOpen:
                    return CreateToneClip("SFX Door Open", 0.42f, t => LowKnock(t, 96f, 0.16f) + Bell(t - 0.09f, 349.23f, 0.08f, 5.5f));
                case GameSfx.StairsOpen:
                    return CreateToneClip("SFX Stairs Open", 0.72f, t => LowKnock(t, 70f, 0.18f) + LowKnock(t - 0.16f, 92f, 0.1f) + Noise(t, 77) * Envelope(t, 0.58f, 0.02f, 0.42f) * 0.018f + Bell(t - 0.38f, 392f, 0.08f, 5f));
                case GameSfx.ChestOpen:
                    return CreateToneClip("SFX Chest Open", 0.62f, t => SoftClick(t, 0.12f) + Bell(t - 0.06f, 660f, 0.22f, 4.8f) + Bell(t - 0.06f, 990f, 0.16f, 5.1f) + Bell(t - 0.18f, 1320f, 0.08f, 7f));
                case GameSfx.GoldFound:
                    return CreateToneClip("SFX Gold Found", 0.42f, t => Bell(t, 1046.5f, 0.11f, 6.3f) + Bell(t - 0.07f, 1318.51f, 0.09f, 6.8f) + Bell(t - 0.16f, 1760f, 0.06f, 8f));
                case GameSfx.EquipmentFound:
                    return CreateToneClip("SFX Equipment Found", 0.54f, t => LowKnock(t, 170f, 0.08f) + Bell(t - 0.04f, 784f, 0.12f, 5.4f) + Bell(t - 0.18f, 1174.66f, 0.1f, 6.2f));
                case GameSfx.FarmDelivery:
                    return CreateToneClip("SFX Farm Delivery", 0.38f, t => Bell(t, 392f, 0.1f, 5.2f) + Bell(t - 0.1f, 587.33f, 0.12f, 5.8f));
                case GameSfx.LumberDelivery:
                    return CreateToneClip("SFX Lumber Delivery", 0.36f, t => LowKnock(t, 120f, 0.13f) + LowKnock(t - 0.09f, 156f, 0.09f) + Bell(t - 0.16f, 440f, 0.055f, 6f));
                case GameSfx.TaxCollect:
                    return CreateToneClip("SFX Tax Collect", 0.3f, t => Bell(t, 988f, 0.11f, 7f) + Bell(t - 0.06f, 1244.51f, 0.075f, 8f));
                case GameSfx.TaxDeposit:
                    return CreateToneClip("SFX Tax Deposit", 0.48f, t => Bell(t, 784f, 0.12f, 5.6f) + Bell(t - 0.08f, 1046.5f, 0.1f, 6f) + Bell(t - 0.18f, 1567.98f, 0.07f, 7.4f));
                case GameSfx.Fortify:
                    return CreateToneClip("SFX Fortify", 0.34f, t => LowKnock(t, 136f, 0.15f) + LowKnock(t - 0.085f, 172f, 0.1f) + SoftClick(t - 0.16f, 0.065f));
                case GameSfx.TorchPlaced:
                    return CreateToneClip("SFX Torch Placed", 0.46f, t => Noise(t, 93) * Envelope(t, 0.42f, 0.02f, 0.34f) * 0.028f + Bell(t, 392f, 0.06f, 5.2f) + Bell(t - 0.11f, 587.33f, 0.055f, 5.8f));
                case GameSfx.LevelSwitch:
                    return CreateToneClip("SFX Level Switch", 0.55f, t => Bell(t, 196f, 0.1f, 3.8f) + Bell(t - 0.12f, 293.66f, 0.09f, 4.4f) + Noise(t, 119) * Envelope(t, 0.46f, 0.03f, 0.36f) * 0.012f);
                case GameSfx.CombatStart:
                    return CreateToneClip("SFX Combat Start", 0.32f, t => LowKnock(t, 140f, 0.11f) + Bell(t - 0.04f, 220f, 0.07f, 7f));
                case GameSfx.CombatHit:
                    return CreateToneClip("SFX Combat Hit", 0.22f, t => LowKnock(t, 150f, 0.18f) + Noise(t, 19) * Envelope(t, 0.22f, 0.005f, 0.19f) * 0.035f);
                case GameSfx.Defeat:
                    return CreateToneClip("SFX Defeat", 0.62f, t => Bell(t, 246.94f, 0.12f, 3.2f) + Bell(t - 0.16f, 196f, 0.1f, 3.6f));
                case GameSfx.LevelUp:
                    return CreateToneClip("SFX Level Up", 0.62f, t => Bell(t, 523.25f, 0.12f, 4.6f) + Bell(t - 0.11f, 659.25f, 0.13f, 4.8f) + Bell(t - 0.22f, 880f, 0.11f, 5.2f));
                case GameSfx.Victory:
                    return CreateToneClip("SFX Victory", 1.05f, t => Bell(t, 523.25f, 0.16f, 3.2f) + Bell(t - 0.16f, 659.25f, 0.15f, 3.4f) + Bell(t - 0.32f, 783.99f, 0.16f, 3.7f) + Bell(t - 0.5f, 1046.5f, 0.13f, 4.2f));
                default:
                    return CreateToneClip("SFX Default", 0.24f, t => Bell(t, 440f, 0.1f, 5f));
            }
        }

        private static float GetVolume(GameSfx sfx)
        {
            switch (sfx)
            {
                case GameSfx.Footstep:
                    return 0.28f;
                case GameSfx.MenuOpen:
                case GameSfx.MenuClose:
                case GameSfx.HudOpen:
                case GameSfx.HudClose:
                case GameSfx.HudClick:
                case GameSfx.HudTab:
                case GameSfx.HudConfirm:
                case GameSfx.HudBlocked:
                    return 0.9f;
                case GameSfx.CombatHit:
                    return 0.76f;
                case GameSfx.Defeat:
                    return 0.82f;
                case GameSfx.Victory:
                    return 0.92f;
                case GameSfx.ChestOpen:
                case GameSfx.StairsOpen:
                case GameSfx.LevelSwitch:
                    return 0.82f;
                case GameSfx.TorchPlaced:
                    return 0.62f;
                case GameSfx.Fortify:
                case GameSfx.LumberDelivery:
                    return 0.7f;
                default:
                    return 0.72f;
            }
        }

        private static AudioClip CreateToneClip(string clipName, float duration, System.Func<float, float> sample)
        {
            var sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var t = i / (float)SampleRate;
                data[i] = Mathf.Clamp(sample(t), -0.75f, 0.75f);
            }

            var clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Bell(float t, float frequency, float amplitude, float decay)
        {
            if (t < 0f)
            {
                return 0f;
            }

            var envelope = Mathf.Exp(-t * decay) * Mathf.Clamp01(t / 0.018f);
            var baseTone = Mathf.Sin(2f * Mathf.PI * frequency * t);
            var overtone = Mathf.Sin(2f * Mathf.PI * frequency * 2.01f * t) * 0.28f;
            return (baseTone + overtone) * envelope * amplitude;
        }

        private static float Bubble(float t, float baseFrequency, float amplitude)
        {
            if (t < 0f)
            {
                return 0f;
            }

            var duration = 0.22f;
            var progress = Mathf.Clamp01(t / duration);
            var frequency = baseFrequency + progress * 180f;
            return Mathf.Sin(2f * Mathf.PI * frequency * t) * Envelope(t, duration, 0.015f, 0.17f) * amplitude;
        }

        private static float SoftClick(float t, float amplitude)
        {
            if (t < 0f || t > 0.075f)
            {
                return 0f;
            }

            return Mathf.Sin(2f * Mathf.PI * 1180f * t) * (1f - t / 0.075f) * amplitude;
        }

        private static float LowKnock(float t, float frequency, float amplitude)
        {
            if (t < 0f)
            {
                return 0f;
            }

            return Mathf.Sin(2f * Mathf.PI * frequency * t) * Envelope(t, 0.18f, 0.004f, 0.16f) * amplitude;
        }

        private static float Envelope(float t, float duration, float attack, float release)
        {
            if (t < 0f || t > duration)
            {
                return 0f;
            }

            var attackValue = attack <= 0f ? 1f : Mathf.Clamp01(t / attack);
            var releaseStart = Mathf.Max(0f, duration - release);
            var releaseValue = t <= releaseStart || release <= 0f
                ? 1f
                : Mathf.Clamp01((duration - t) / release);
            return attackValue * releaseValue;
        }

        private static float Noise(float t, int salt)
        {
            var index = Mathf.FloorToInt(t * SampleRate);
            unchecked
            {
                var value = index * 1103515245 + salt * 12345;
                value ^= value >> 13;
                value *= 1274126177;
                return ((value & 0xffff) / 32767.5f) - 1f;
            }
        }
    }
}
