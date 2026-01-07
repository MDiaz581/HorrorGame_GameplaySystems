using System.IO;
using UnityEngine;


[System.Serializable]
public class GameSettings
{
    public int resolutionWidth;
    public int resolutionHeight;
    public SettingsBehavior.windowMode wMode; // Save window mode
    public SettingsBehavior.graphicsQuality gQuality;
    public int int_fieldOfView;
    public bool vsyncEnabled;
    public int int_displayValue;
    public int int_masterVolume;
    public int int_sfxVolume;
    public int int_voiceIncomingVolume;
    public int int_voiceOutgoingVolume;
    public float float_mouseSensitivity;
}
