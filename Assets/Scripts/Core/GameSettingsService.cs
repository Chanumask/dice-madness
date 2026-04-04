using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace DiceMadness.Core
{
    public enum DisplayModeOption
    {
        Windowed = 0,
        Borderless = 1,
        Fullscreen = 2,
    }

    [Serializable]
    public struct ResolutionOption
    {
        public int width;
        public int height;

        public ResolutionOption(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public override string ToString()
        {
            return $"{width} x {height}";
        }
    }

    [Serializable]
    public struct GameSettingsData
    {
        public KeyCode rollKey;
        public KeyCode backKey;
        public int resolutionWidth;
        public int resolutionHeight;
        public DisplayModeOption displayMode;
        public float masterVolume;
        public float musicVolume;
        public float sfxVolume;
        public float uiScale;
        public bool showDetailedRollBreakdown;
        public bool vSyncEnabled;
    }

    public static class GameSettingsService
    {
        private const string RollKeyPref = "DiceMadness.Settings.RollKey";
        private const string BackKeyPref = "DiceMadness.Settings.BackKey";
        private const string ResolutionWidthPref = "DiceMadness.Settings.ResolutionWidth";
        private const string ResolutionHeightPref = "DiceMadness.Settings.ResolutionHeight";
        private const string DisplayModePref = "DiceMadness.Settings.DisplayMode";
        private const string MasterVolumePref = "DiceMadness.Settings.MasterVolume";
        private const string MusicVolumePref = "DiceMadness.Settings.MusicVolume";
        private const string SfxVolumePref = "DiceMadness.Settings.SfxVolume";
        private const string UiScalePref = "DiceMadness.Settings.UiScale";
        private const string DetailedRollBreakdownPref = "DiceMadness.Settings.DetailedRollBreakdown";
        private const string VSyncPref = "DiceMadness.Settings.VSync";

        private static readonly Vector2 BaseReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly List<ResolutionOption> CachedResolutions = new List<ResolutionOption>();

        private static bool loaded;
        private static GameSettingsData current;

        public static event Action SettingsChanged;

        public static GameSettingsData Current
        {
            get
            {
                EnsureLoaded();
                return current;
            }
        }

        public static IReadOnlyList<ResolutionOption> AvailableResolutions
        {
            get
            {
                EnsureLoaded();
                return CachedResolutions;
            }
        }

        public static void EnsureLoaded()
        {
            if (loaded)
            {
                return;
            }

            RefreshResolutionOptions();

            ResolutionOption fallbackResolution = CachedResolutions.Count > 0
                ? CachedResolutions[CachedResolutions.Count - 1]
                : new ResolutionOption(Screen.currentResolution.width, Screen.currentResolution.height);

            current = new GameSettingsData
            {
                rollKey = (KeyCode)PlayerPrefs.GetInt(RollKeyPref, (int)KeyCode.Space),
                backKey = (KeyCode)PlayerPrefs.GetInt(BackKeyPref, (int)KeyCode.Escape),
                resolutionWidth = PlayerPrefs.GetInt(ResolutionWidthPref, fallbackResolution.width),
                resolutionHeight = PlayerPrefs.GetInt(ResolutionHeightPref, fallbackResolution.height),
                displayMode = (DisplayModeOption)PlayerPrefs.GetInt(DisplayModePref, (int)DisplayModeOption.Windowed),
                masterVolume = PlayerPrefs.GetFloat(MasterVolumePref, 1f),
                musicVolume = PlayerPrefs.GetFloat(MusicVolumePref, 0.8f),
                sfxVolume = PlayerPrefs.GetFloat(SfxVolumePref, 0.9f),
                uiScale = PlayerPrefs.GetFloat(UiScalePref, 1f),
                showDetailedRollBreakdown = PlayerPrefs.GetInt(DetailedRollBreakdownPref, 1) == 1,
                vSyncEnabled = PlayerPrefs.GetInt(VSyncPref, 0) == 1,
            };

            SanitizeCurrentSettings();
            ApplySystemSettings();
            loaded = true;
        }

        public static void RefreshResolutionOptions()
        {
            CachedResolutions.Clear();
            HashSet<string> seen = new HashSet<string>();
            Resolution[] resolutions = Screen.resolutions;

            if (resolutions != null && resolutions.Length > 0)
            {
                for (int i = 0; i < resolutions.Length; i++)
                {
                    Resolution resolution = resolutions[i];
                    string key = $"{resolution.width}x{resolution.height}";
                    if (seen.Add(key))
                    {
                        CachedResolutions.Add(new ResolutionOption(resolution.width, resolution.height));
                    }
                }
            }

            if (CachedResolutions.Count == 0)
            {
                CachedResolutions.Add(new ResolutionOption(Screen.currentResolution.width, Screen.currentResolution.height));
            }

            CachedResolutions.Sort((left, right) =>
            {
                int widthCompare = left.width.CompareTo(right.width);
                return widthCompare != 0 ? widthCompare : left.height.CompareTo(right.height);
            });
        }

        public static void ApplyToCanvasScaler(CanvasScaler scaler)
        {
            if (scaler == null)
            {
                return;
            }

            EnsureLoaded();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = BaseReferenceResolution / Mathf.Max(0.5f, current.uiScale);
            scaler.matchWidthOrHeight = 0.5f;
        }

        public static void ApplySystemSettings()
        {
            ApplyAudioSettings();
            ApplyVideoSettings();
        }

        private static void ApplyAudioSettings()
        {
            AudioListener.volume = Mathf.Clamp01(current.masterVolume);
        }

        private static void ApplyVideoSettings()
        {
            EnsureResolutionAvailable();
            QualitySettings.vSyncCount = current.vSyncEnabled ? 1 : 0;

            FullScreenMode mode = ToFullScreenMode(current.displayMode);
            Screen.SetResolution(current.resolutionWidth, current.resolutionHeight, mode);
        }

        public static int GetSelectedResolutionIndex()
        {
            EnsureLoaded();

            for (int i = 0; i < CachedResolutions.Count; i++)
            {
                if (CachedResolutions[i].width == current.resolutionWidth &&
                    CachedResolutions[i].height == current.resolutionHeight)
                {
                    return i;
                }
            }

            return Mathf.Clamp(CachedResolutions.Count - 1, 0, int.MaxValue);
        }

        public static void SetResolutionByIndex(int index)
        {
            EnsureLoaded();
            if (CachedResolutions.Count == 0)
            {
                return;
            }

            index = Mathf.Clamp(index, 0, CachedResolutions.Count - 1);
            current.resolutionWidth = CachedResolutions[index].width;
            current.resolutionHeight = CachedResolutions[index].height;
            SaveAndApply(applyVideoSettings: true);
        }

        public static void SetDisplayMode(DisplayModeOption mode)
        {
            EnsureLoaded();
            current.displayMode = mode;
            SaveAndApply(applyVideoSettings: true);
        }

        public static void SetRollKey(KeyCode key)
        {
            EnsureLoaded();
            if (key == KeyCode.None)
            {
                return;
            }

            current.rollKey = key;
            SaveAndApply(false);
        }

        public static void SetBackKey(KeyCode key)
        {
            EnsureLoaded();
            if (key == KeyCode.None)
            {
                return;
            }

            current.backKey = key;
            SaveAndApply(false);
        }

        public static void SetMasterVolume(float value)
        {
            EnsureLoaded();
            current.masterVolume = Mathf.Clamp01(value);
            SaveAndApply(applyAudioSettings: true);
        }

        public static void SetMusicVolume(float value)
        {
            EnsureLoaded();
            current.musicVolume = Mathf.Clamp01(value);
            SaveAndApply(false);
        }

        public static void SetSfxVolume(float value)
        {
            EnsureLoaded();
            current.sfxVolume = Mathf.Clamp01(value);
            SaveAndApply(false);
        }

        public static void SetUiScale(float value)
        {
            EnsureLoaded();
            current.uiScale = Mathf.Clamp(value, 0.75f, 1.35f);
            SaveAndApply(false);
        }

        public static void SetDetailedRollBreakdown(bool enabled)
        {
            EnsureLoaded();
            current.showDetailedRollBreakdown = enabled;
            SaveAndApply(false);
        }

        public static void SetVSync(bool enabled)
        {
            EnsureLoaded();
            current.vSyncEnabled = enabled;
            SaveAndApply(applyVideoSettings: true);
        }

        public static bool WasRollPressed()
        {
            EnsureLoaded();
            return WasBindingPressed(current.rollKey);
        }

        public static bool WasBackPressed()
        {
            EnsureLoaded();
            return WasBindingPressed(current.backKey);
        }

        public static bool TryCaptureNextBinding(out KeyCode keyCode)
        {
            EnsureLoaded();
            keyCode = KeyCode.None;

#if ENABLE_INPUT_SYSTEM
            if (TryCaptureNextBindingWithInputSystem(out keyCode))
            {
                return true;
            }
#endif

            return false;
        }

        public static string GetBindingDisplayName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Mouse0: return "Mouse Left";
                case KeyCode.Mouse1: return "Mouse Right";
                case KeyCode.Mouse2: return "Mouse Middle";
                case KeyCode.Mouse3: return "Mouse 4";
                case KeyCode.Mouse4: return "Mouse 5";
                case KeyCode.Space: return "Space";
                case KeyCode.Escape: return "Escape";
                case KeyCode.Return: return "Enter";
                case KeyCode.Backspace: return "Backspace";
                default:
                    return key.ToString();
            }
        }

        private static bool WasBindingPressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (WasBindingPressedWithInputSystem(key))
            {
                return true;
            }
#endif
            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private static bool TryCaptureNextBindingWithInputSystem(out KeyCode keyCode)
        {
            keyCode = KeyCode.None;

            if (Keyboard.current != null)
            {
                foreach (KeyControl key in Keyboard.current.allKeys)
                {
                    if (key != null && key.wasPressedThisFrame)
                    {
                        if (TryMapInputSystemKeyToKeyCode(key.keyCode, out keyCode))
                        {
                            return true;
                        }
                    }
                }
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    keyCode = KeyCode.Mouse0;
                    return true;
                }

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    keyCode = KeyCode.Mouse1;
                    return true;
                }

                if (Mouse.current.middleButton.wasPressedThisFrame)
                {
                    keyCode = KeyCode.Mouse2;
                    return true;
                }

                if (Mouse.current.forwardButton != null && Mouse.current.forwardButton.wasPressedThisFrame)
                {
                    keyCode = KeyCode.Mouse3;
                    return true;
                }

                if (Mouse.current.backButton != null && Mouse.current.backButton.wasPressedThisFrame)
                {
                    keyCode = KeyCode.Mouse4;
                    return true;
                }
            }

            return false;
        }

        private static bool WasBindingPressedWithInputSystem(KeyCode key)
        {
            if (key == KeyCode.None)
            {
                return false;
            }

            if (!TryMapKeyCodeToInputSystemKey(key, out UnityEngine.InputSystem.Key mappedKey))
            {
                return false;
            }

            if (Keyboard.current != null)
            {
                foreach (KeyControl keyControl in Keyboard.current.allKeys)
                {
                    if (keyControl != null &&
                        keyControl.keyCode == mappedKey &&
                        keyControl.wasPressedThisFrame)
                    {
                        return true;
                    }
                }
            }

            if (Mouse.current != null)
            {
                switch (key)
                {
                    case KeyCode.Mouse0:
                        return Mouse.current.leftButton.wasPressedThisFrame;
                    case KeyCode.Mouse1:
                        return Mouse.current.rightButton.wasPressedThisFrame;
                    case KeyCode.Mouse2:
                        return Mouse.current.middleButton.wasPressedThisFrame;
                    case KeyCode.Mouse3:
                        return Mouse.current.forwardButton != null && Mouse.current.forwardButton.wasPressedThisFrame;
                    case KeyCode.Mouse4:
                        return Mouse.current.backButton != null && Mouse.current.backButton.wasPressedThisFrame;
                }
            }

            return false;
        }

        private static bool TryMapInputSystemKeyToKeyCode(UnityEngine.InputSystem.Key key, out KeyCode keyCode)
        {
            switch (key)
            {
                case UnityEngine.InputSystem.Key.None:
                    keyCode = KeyCode.None;
                    return false;
                case UnityEngine.InputSystem.Key.Space:
                    keyCode = KeyCode.Space;
                    return true;
                case UnityEngine.InputSystem.Key.Enter:
                case UnityEngine.InputSystem.Key.NumpadEnter:
                    keyCode = KeyCode.Return;
                    return true;
                case UnityEngine.InputSystem.Key.Tab:
                    keyCode = KeyCode.Tab;
                    return true;
                case UnityEngine.InputSystem.Key.Backspace:
                    keyCode = KeyCode.Backspace;
                    return true;
                case UnityEngine.InputSystem.Key.Escape:
                    keyCode = KeyCode.Escape;
                    return true;
                case UnityEngine.InputSystem.Key.LeftShift:
                    keyCode = KeyCode.LeftShift;
                    return true;
                case UnityEngine.InputSystem.Key.RightShift:
                    keyCode = KeyCode.RightShift;
                    return true;
                case UnityEngine.InputSystem.Key.LeftCtrl:
                    keyCode = KeyCode.LeftControl;
                    return true;
                case UnityEngine.InputSystem.Key.RightCtrl:
                    keyCode = KeyCode.RightControl;
                    return true;
                case UnityEngine.InputSystem.Key.LeftAlt:
                    keyCode = KeyCode.LeftAlt;
                    return true;
                case UnityEngine.InputSystem.Key.RightAlt:
                    keyCode = KeyCode.RightAlt;
                    return true;
                case UnityEngine.InputSystem.Key.LeftArrow:
                    keyCode = KeyCode.LeftArrow;
                    return true;
                case UnityEngine.InputSystem.Key.RightArrow:
                    keyCode = KeyCode.RightArrow;
                    return true;
                case UnityEngine.InputSystem.Key.UpArrow:
                    keyCode = KeyCode.UpArrow;
                    return true;
                case UnityEngine.InputSystem.Key.DownArrow:
                    keyCode = KeyCode.DownArrow;
                    return true;
                case UnityEngine.InputSystem.Key.PageUp:
                    keyCode = KeyCode.PageUp;
                    return true;
                case UnityEngine.InputSystem.Key.PageDown:
                    keyCode = KeyCode.PageDown;
                    return true;
                case UnityEngine.InputSystem.Key.Home:
                    keyCode = KeyCode.Home;
                    return true;
                case UnityEngine.InputSystem.Key.End:
                    keyCode = KeyCode.End;
                    return true;
                case UnityEngine.InputSystem.Key.Insert:
                    keyCode = KeyCode.Insert;
                    return true;
                case UnityEngine.InputSystem.Key.Delete:
                    keyCode = KeyCode.Delete;
                    return true;
                case UnityEngine.InputSystem.Key.F1:
                    keyCode = KeyCode.F1;
                    return true;
                case UnityEngine.InputSystem.Key.F2:
                    keyCode = KeyCode.F2;
                    return true;
                case UnityEngine.InputSystem.Key.F3:
                    keyCode = KeyCode.F3;
                    return true;
                case UnityEngine.InputSystem.Key.F4:
                    keyCode = KeyCode.F4;
                    return true;
                case UnityEngine.InputSystem.Key.F5:
                    keyCode = KeyCode.F5;
                    return true;
                case UnityEngine.InputSystem.Key.F6:
                    keyCode = KeyCode.F6;
                    return true;
                case UnityEngine.InputSystem.Key.F7:
                    keyCode = KeyCode.F7;
                    return true;
                case UnityEngine.InputSystem.Key.F8:
                    keyCode = KeyCode.F8;
                    return true;
                case UnityEngine.InputSystem.Key.F9:
                    keyCode = KeyCode.F9;
                    return true;
                case UnityEngine.InputSystem.Key.F10:
                    keyCode = KeyCode.F10;
                    return true;
                case UnityEngine.InputSystem.Key.F11:
                    keyCode = KeyCode.F11;
                    return true;
                case UnityEngine.InputSystem.Key.F12:
                    keyCode = KeyCode.F12;
                    return true;
                case UnityEngine.InputSystem.Key.Digit1:
                    keyCode = KeyCode.Alpha1;
                    return true;
                case UnityEngine.InputSystem.Key.Digit2:
                    keyCode = KeyCode.Alpha2;
                    return true;
                case UnityEngine.InputSystem.Key.Digit3:
                    keyCode = KeyCode.Alpha3;
                    return true;
                case UnityEngine.InputSystem.Key.Digit4:
                    keyCode = KeyCode.Alpha4;
                    return true;
                case UnityEngine.InputSystem.Key.Digit5:
                    keyCode = KeyCode.Alpha5;
                    return true;
                case UnityEngine.InputSystem.Key.Digit6:
                    keyCode = KeyCode.Alpha6;
                    return true;
                case UnityEngine.InputSystem.Key.Digit7:
                    keyCode = KeyCode.Alpha7;
                    return true;
                case UnityEngine.InputSystem.Key.Digit8:
                    keyCode = KeyCode.Alpha8;
                    return true;
                case UnityEngine.InputSystem.Key.Digit9:
                    keyCode = KeyCode.Alpha9;
                    return true;
                case UnityEngine.InputSystem.Key.Digit0:
                    keyCode = KeyCode.Alpha0;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad1:
                    keyCode = KeyCode.Keypad1;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad2:
                    keyCode = KeyCode.Keypad2;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad3:
                    keyCode = KeyCode.Keypad3;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad4:
                    keyCode = KeyCode.Keypad4;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad5:
                    keyCode = KeyCode.Keypad5;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad6:
                    keyCode = KeyCode.Keypad6;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad7:
                    keyCode = KeyCode.Keypad7;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad8:
                    keyCode = KeyCode.Keypad8;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad9:
                    keyCode = KeyCode.Keypad9;
                    return true;
                case UnityEngine.InputSystem.Key.Numpad0:
                    keyCode = KeyCode.Keypad0;
                    return true;
                default:
                    if (System.Enum.TryParse(key.ToString(), out KeyCode parsed))
                    {
                        keyCode = parsed;
                        return true;
                    }

                    keyCode = KeyCode.None;
                    return false;
            }
        }

        private static bool TryMapKeyCodeToInputSystemKey(KeyCode keyCode, out UnityEngine.InputSystem.Key inputKey)
        {
            switch (keyCode)
            {
                case KeyCode.Space:
                    inputKey = UnityEngine.InputSystem.Key.Space;
                    return true;
                case KeyCode.Return:
                    inputKey = UnityEngine.InputSystem.Key.Enter;
                    return true;
                case KeyCode.KeypadEnter:
                    inputKey = UnityEngine.InputSystem.Key.NumpadEnter;
                    return true;
                case KeyCode.Tab:
                    inputKey = UnityEngine.InputSystem.Key.Tab;
                    return true;
                case KeyCode.Backspace:
                    inputKey = UnityEngine.InputSystem.Key.Backspace;
                    return true;
                case KeyCode.Escape:
                    inputKey = UnityEngine.InputSystem.Key.Escape;
                    return true;
                case KeyCode.LeftShift:
                    inputKey = UnityEngine.InputSystem.Key.LeftShift;
                    return true;
                case KeyCode.RightShift:
                    inputKey = UnityEngine.InputSystem.Key.RightShift;
                    return true;
                case KeyCode.LeftControl:
                    inputKey = UnityEngine.InputSystem.Key.LeftCtrl;
                    return true;
                case KeyCode.RightControl:
                    inputKey = UnityEngine.InputSystem.Key.RightCtrl;
                    return true;
                case KeyCode.LeftAlt:
                    inputKey = UnityEngine.InputSystem.Key.LeftAlt;
                    return true;
                case KeyCode.RightAlt:
                    inputKey = UnityEngine.InputSystem.Key.RightAlt;
                    return true;
                case KeyCode.LeftArrow:
                    inputKey = UnityEngine.InputSystem.Key.LeftArrow;
                    return true;
                case KeyCode.RightArrow:
                    inputKey = UnityEngine.InputSystem.Key.RightArrow;
                    return true;
                case KeyCode.UpArrow:
                    inputKey = UnityEngine.InputSystem.Key.UpArrow;
                    return true;
                case KeyCode.DownArrow:
                    inputKey = UnityEngine.InputSystem.Key.DownArrow;
                    return true;
                case KeyCode.PageUp:
                    inputKey = UnityEngine.InputSystem.Key.PageUp;
                    return true;
                case KeyCode.PageDown:
                    inputKey = UnityEngine.InputSystem.Key.PageDown;
                    return true;
                case KeyCode.Home:
                    inputKey = UnityEngine.InputSystem.Key.Home;
                    return true;
                case KeyCode.End:
                    inputKey = UnityEngine.InputSystem.Key.End;
                    return true;
                case KeyCode.Insert:
                    inputKey = UnityEngine.InputSystem.Key.Insert;
                    return true;
                case KeyCode.Delete:
                    inputKey = UnityEngine.InputSystem.Key.Delete;
                    return true;
                case KeyCode.F1:
                    inputKey = UnityEngine.InputSystem.Key.F1;
                    return true;
                case KeyCode.F2:
                    inputKey = UnityEngine.InputSystem.Key.F2;
                    return true;
                case KeyCode.F3:
                    inputKey = UnityEngine.InputSystem.Key.F3;
                    return true;
                case KeyCode.F4:
                    inputKey = UnityEngine.InputSystem.Key.F4;
                    return true;
                case KeyCode.F5:
                    inputKey = UnityEngine.InputSystem.Key.F5;
                    return true;
                case KeyCode.F6:
                    inputKey = UnityEngine.InputSystem.Key.F6;
                    return true;
                case KeyCode.F7:
                    inputKey = UnityEngine.InputSystem.Key.F7;
                    return true;
                case KeyCode.F8:
                    inputKey = UnityEngine.InputSystem.Key.F8;
                    return true;
                case KeyCode.F9:
                    inputKey = UnityEngine.InputSystem.Key.F9;
                    return true;
                case KeyCode.F10:
                    inputKey = UnityEngine.InputSystem.Key.F10;
                    return true;
                case KeyCode.F11:
                    inputKey = UnityEngine.InputSystem.Key.F11;
                    return true;
                case KeyCode.F12:
                    inputKey = UnityEngine.InputSystem.Key.F12;
                    return true;
                case KeyCode.Alpha0:
                    inputKey = UnityEngine.InputSystem.Key.Digit0;
                    return true;
                case KeyCode.Alpha1:
                    inputKey = UnityEngine.InputSystem.Key.Digit1;
                    return true;
                case KeyCode.Alpha2:
                    inputKey = UnityEngine.InputSystem.Key.Digit2;
                    return true;
                case KeyCode.Alpha3:
                    inputKey = UnityEngine.InputSystem.Key.Digit3;
                    return true;
                case KeyCode.Alpha4:
                    inputKey = UnityEngine.InputSystem.Key.Digit4;
                    return true;
                case KeyCode.Alpha5:
                    inputKey = UnityEngine.InputSystem.Key.Digit5;
                    return true;
                case KeyCode.Alpha6:
                    inputKey = UnityEngine.InputSystem.Key.Digit6;
                    return true;
                case KeyCode.Alpha7:
                    inputKey = UnityEngine.InputSystem.Key.Digit7;
                    return true;
                case KeyCode.Alpha8:
                    inputKey = UnityEngine.InputSystem.Key.Digit8;
                    return true;
                case KeyCode.Alpha9:
                    inputKey = UnityEngine.InputSystem.Key.Digit9;
                    return true;
                case KeyCode.Keypad0:
                    inputKey = UnityEngine.InputSystem.Key.Numpad0;
                    return true;
                case KeyCode.Keypad1:
                    inputKey = UnityEngine.InputSystem.Key.Numpad1;
                    return true;
                case KeyCode.Keypad2:
                    inputKey = UnityEngine.InputSystem.Key.Numpad2;
                    return true;
                case KeyCode.Keypad3:
                    inputKey = UnityEngine.InputSystem.Key.Numpad3;
                    return true;
                case KeyCode.Keypad4:
                    inputKey = UnityEngine.InputSystem.Key.Numpad4;
                    return true;
                case KeyCode.Keypad5:
                    inputKey = UnityEngine.InputSystem.Key.Numpad5;
                    return true;
                case KeyCode.Keypad6:
                    inputKey = UnityEngine.InputSystem.Key.Numpad6;
                    return true;
                case KeyCode.Keypad7:
                    inputKey = UnityEngine.InputSystem.Key.Numpad7;
                    return true;
                case KeyCode.Keypad8:
                    inputKey = UnityEngine.InputSystem.Key.Numpad8;
                    return true;
                case KeyCode.Keypad9:
                    inputKey = UnityEngine.InputSystem.Key.Numpad9;
                    return true;
                case KeyCode.Mouse0:
                case KeyCode.Mouse1:
                case KeyCode.Mouse2:
                case KeyCode.Mouse3:
                case KeyCode.Mouse4:
                    inputKey = UnityEngine.InputSystem.Key.None;
                    return false;
                default:
                    if (System.Enum.TryParse(keyCode.ToString(), out UnityEngine.InputSystem.Key parsed))
                    {
                        inputKey = parsed;
                        return true;
                    }

                    inputKey = UnityEngine.InputSystem.Key.None;
                    return false;
            }
        }
#endif

        private static void SanitizeCurrentSettings()
        {
            if (current.rollKey == KeyCode.None)
            {
                current.rollKey = KeyCode.Space;
            }

            if (current.backKey == KeyCode.None)
            {
                current.backKey = KeyCode.Escape;
            }

            current.masterVolume = Mathf.Clamp01(current.masterVolume);
            current.musicVolume = Mathf.Clamp01(current.musicVolume);
            current.sfxVolume = Mathf.Clamp01(current.sfxVolume);
            current.uiScale = Mathf.Clamp(current.uiScale, 0.75f, 1.35f);
            EnsureResolutionAvailable();
        }

        private static void EnsureResolutionAvailable()
        {
            if (CachedResolutions.Count == 0)
            {
                RefreshResolutionOptions();
            }

            for (int i = 0; i < CachedResolutions.Count; i++)
            {
                if (CachedResolutions[i].width == current.resolutionWidth &&
                    CachedResolutions[i].height == current.resolutionHeight)
                {
                    return;
                }
            }

            ResolutionOption fallback = CachedResolutions[Mathf.Clamp(CachedResolutions.Count - 1, 0, CachedResolutions.Count - 1)];
            current.resolutionWidth = fallback.width;
            current.resolutionHeight = fallback.height;
        }

        private static FullScreenMode ToFullScreenMode(DisplayModeOption option)
        {
            switch (option)
            {
                case DisplayModeOption.Borderless:
                    return FullScreenMode.FullScreenWindow;
                case DisplayModeOption.Fullscreen:
                    return FullScreenMode.ExclusiveFullScreen;
                default:
                    return FullScreenMode.Windowed;
            }
        }

        private static void SaveAndApply(bool applyAudioSettings = false, bool applyVideoSettings = false)
        {
            PlayerPrefs.SetInt(RollKeyPref, (int)current.rollKey);
            PlayerPrefs.SetInt(BackKeyPref, (int)current.backKey);
            PlayerPrefs.SetInt(ResolutionWidthPref, current.resolutionWidth);
            PlayerPrefs.SetInt(ResolutionHeightPref, current.resolutionHeight);
            PlayerPrefs.SetInt(DisplayModePref, (int)current.displayMode);
            PlayerPrefs.SetFloat(MasterVolumePref, current.masterVolume);
            PlayerPrefs.SetFloat(MusicVolumePref, current.musicVolume);
            PlayerPrefs.SetFloat(SfxVolumePref, current.sfxVolume);
            PlayerPrefs.SetFloat(UiScalePref, current.uiScale);
            PlayerPrefs.SetInt(DetailedRollBreakdownPref, current.showDetailedRollBreakdown ? 1 : 0);
            PlayerPrefs.SetInt(VSyncPref, current.vSyncEnabled ? 1 : 0);
            PlayerPrefs.Save();

            if (applyAudioSettings)
            {
                ApplyAudioSettings();
            }

            if (applyVideoSettings)
            {
                ApplyVideoSettings();
            }

            SettingsChanged?.Invoke();
        }
    }
}
