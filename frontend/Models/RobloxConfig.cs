using System.Collections.Generic;

namespace Debeon.Models
{
    public class RobloxConfig
    {
        public GraphicsConfig Graphics { get; set; } = new GraphicsConfig();
        public AudioConfig Audio { get; set; } = new AudioConfig();
        public ControlsConfig Controls { get; set; } = new ControlsConfig();
        public NetworkConfig Network { get; set; } = new NetworkConfig();
        public RenderingConfig Rendering { get; set; } = new RenderingConfig();
        public PerformanceConfig Performance { get; set; } = new PerformanceConfig();
        public UIConfig UI { get; set; } = new UIConfig();
        public Dictionary<string, string> CustomFlags { get; set; } = new Dictionary<string, string>();
    }

    public class GraphicsConfig
    {
        public byte GraphicsQuality { get; set; } = 10;
        public uint RenderDistance { get; set; } = 1000;
        public byte ShadowQuality { get; set; } = 3;
        public byte TextureQuality { get; set; } = 3;
        public byte ParticleQuality { get; set; } = 3;
        public bool Vsync { get; set; } = true;
        public bool Fullscreen { get; set; } = false;
        public uint ResolutionWidth { get; set; } = 1920;
        public uint ResolutionHeight { get; set; } = 1080;
        public byte AntiAliasing { get; set; } = 4;
        public byte AnisotropicFiltering { get; set; } = 16;
    }

    public class AudioConfig
    {
        public float MasterVolume { get; set; } = 0.8f;
        public float MusicVolume { get; set; } = 0.7f;
        public float SfxVolume { get; set; } = 0.8f;
        public float VoiceVolume { get; set; } = 0.9f;
        public bool SpatialAudio { get; set; } = true;
        public string OutputDevice { get; set; } = "default";
        public string InputDevice { get; set; } = "default";
    }

    public class ControlsConfig
    {
        public float MouseSensitivity { get; set; } = 0.5f;
        public bool InvertYAxis { get; set; } = false;
        public string CameraMode { get; set; } = "follow";
        public Dictionary<string, string> KeyBindings { get; set; } = new Dictionary<string, string>
        {
            { "forward", "W" },
            { "backward", "S" },
            { "left", "A" },
            { "right", "D" },
            { "jump", "Space" }
        };
        public bool GamepadEnabled { get; set; } = false;
        public float GamepadSensitivity { get; set; } = 0.5f;
    }

    public class NetworkConfig
    {
        public string PreferredRegion { get; set; } = "auto";
        public uint MaxPing { get; set; } = 200;
        public string ConnectionQuality { get; set; } = "high";
        public bool EnableIpv6 { get; set; } = true;
        public ulong? DataUsageLimit { get; set; } = null;
    }

    public class RenderingConfig
    {
        public uint? FrameRateLimit { get; set; } = 60;
        public bool DynamicLighting { get; set; } = true;
        public bool PostProcessing { get; set; } = true;
        public bool Bloom { get; set; } = true;
        public bool DepthOfField { get; set; } = false;
        public bool MotionBlur { get; set; } = false;
        public bool AmbientOcclusion { get; set; } = true;
        public bool Reflections { get; set; } = true;
        public bool GlobalIllumination { get; set; } = true;
    }

    public class PerformanceConfig
    {
        public bool LowLatencyMode { get; set; } = false;
        public bool PowerSavingMode { get; set; } = false;
        public string BackgroundPerformance { get; set; } = "normal";
        public uint? MemoryLimitMb { get; set; } = null;
        public List<int> CpuAffinity { get; set; } = new List<int>();
        public string GpuPreference { get; set; } = "high_performance";
    }

    public class UIConfig
    {
        public float UiScale { get; set; } = 1.0f;
        public bool ShowFps { get; set; } = false;
        public bool ShowPing { get; set; } = false;
        public bool ChatEnabled { get; set; } = true;
        public float GuiTransparency { get; set; } = 0.0f;
        public string Theme { get; set; } = "dark";
        public string CustomCursor { get; set; } = null;
    }

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
    }

    public class RobloxInstallation
    {
        public string Path { get; set; }
        public string Version { get; set; }
        public string Channel { get; set; }
        public string LastModified { get; set; }
    }
}
