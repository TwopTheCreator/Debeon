use serde::{Deserialize, Serialize};
use std::collections::HashMap;

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RobloxConfig {
    pub graphics: GraphicsConfig,
    pub audio: AudioConfig,
    pub controls: ControlsConfig,
    pub network: NetworkConfig,
    pub rendering: RenderingConfig,
    pub performance: PerformanceConfig,
    pub ui: UIConfig,
    pub custom_flags: HashMap<String, String>,
}

impl Default for RobloxConfig {
    fn default() -> Self {
        Self {
            graphics: GraphicsConfig::default(),
            audio: AudioConfig::default(),
            controls: ControlsConfig::default(),
            network: NetworkConfig::default(),
            rendering: RenderingConfig::default(),
            performance: PerformanceConfig::default(),
            ui: UIConfig::default(),
            custom_flags: HashMap::new(),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GraphicsConfig {
    pub graphics_quality: u8,
    pub render_distance: u32,
    pub shadow_quality: u8,
    pub texture_quality: u8,
    pub particle_quality: u8,
    pub vsync: bool,
    pub fullscreen: bool,
    pub resolution_width: u32,
    pub resolution_height: u32,
    pub anti_aliasing: u8,
    pub anisotropic_filtering: u8,
}

impl Default for GraphicsConfig {
    fn default() -> Self {
        Self {
            graphics_quality: 10,
            render_distance: 1000,
            shadow_quality: 3,
            texture_quality: 3,
            particle_quality: 3,
            vsync: true,
            fullscreen: false,
            resolution_width: 1920,
            resolution_height: 1080,
            anti_aliasing: 4,
            anisotropic_filtering: 16,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AudioConfig {
    pub master_volume: f32,
    pub music_volume: f32,
    pub sfx_volume: f32,
    pub voice_volume: f32,
    pub spatial_audio: bool,
    pub output_device: String,
    pub input_device: String,
}

impl Default for AudioConfig {
    fn default() -> Self {
        Self {
            master_volume: 0.8,
            music_volume: 0.7,
            sfx_volume: 0.8,
            voice_volume: 0.9,
            spatial_audio: true,
            output_device: String::from("default"),
            input_device: String::from("default"),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ControlsConfig {
    pub mouse_sensitivity: f32,
    pub invert_y_axis: bool,
    pub camera_mode: String,
    pub key_bindings: HashMap<String, String>,
    pub gamepad_enabled: bool,
    pub gamepad_sensitivity: f32,
}

impl Default for ControlsConfig {
    fn default() -> Self {
        let mut bindings = HashMap::new();
        bindings.insert("forward".to_string(), "W".to_string());
        bindings.insert("backward".to_string(), "S".to_string());
        bindings.insert("left".to_string(), "A".to_string());
        bindings.insert("right".to_string(), "D".to_string());
        bindings.insert("jump".to_string(), "Space".to_string());

        Self {
            mouse_sensitivity: 0.5,
            invert_y_axis: false,
            camera_mode: String::from("follow"),
            key_bindings: bindings,
            gamepad_enabled: false,
            gamepad_sensitivity: 0.5,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct NetworkConfig {
    pub preferred_region: String,
    pub max_ping: u32,
    pub connection_quality: String,
    pub enable_ipv6: bool,
    pub data_usage_limit: Option<u64>,
}

impl Default for NetworkConfig {
    fn default() -> Self {
        Self {
            preferred_region: String::from("auto"),
            max_ping: 200,
            connection_quality: String::from("high"),
            enable_ipv6: true,
            data_usage_limit: None,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RenderingConfig {
    pub frame_rate_limit: Option<u32>,
    pub dynamic_lighting: bool,
    pub post_processing: bool,
    pub bloom: bool,
    pub depth_of_field: bool,
    pub motion_blur: bool,
    pub ambient_occlusion: bool,
    pub reflections: bool,
    pub global_illumination: bool,
}

impl Default for RenderingConfig {
    fn default() -> Self {
        Self {
            frame_rate_limit: Some(60),
            dynamic_lighting: true,
            post_processing: true,
            bloom: true,
            depth_of_field: false,
            motion_blur: false,
            ambient_occlusion: true,
            reflections: true,
            global_illumination: true,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct PerformanceConfig {
    pub low_latency_mode: bool,
    pub power_saving_mode: bool,
    pub background_performance: String,
    pub memory_limit_mb: Option<u32>,
    pub cpu_affinity: Vec<usize>,
    pub gpu_preference: String,
}

impl Default for PerformanceConfig {
    fn default() -> Self {
        Self {
            low_latency_mode: false,
            power_saving_mode: false,
            background_performance: String::from("normal"),
            memory_limit_mb: None,
            cpu_affinity: vec![],
            gpu_preference: String::from("high_performance"),
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UIConfig {
    pub ui_scale: f32,
    pub show_fps: bool,
    pub show_ping: bool,
    pub chat_enabled: bool,
    pub gui_transparency: f32,
    pub theme: String,
    pub custom_cursor: Option<String>,
}

impl Default for UIConfig {
    fn default() -> Self {
        Self {
            ui_scale: 1.0,
            show_fps: false,
            show_ping: false,
            chat_enabled: true,
            gui_transparency: 0.0,
            theme: String::from("dark"),
            custom_cursor: None,
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct RobloxInstallation {
    pub path: String,
    pub version: String,
    pub channel: String,
    pub last_modified: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AssetInfo {
    pub asset_id: u64,
    pub name: String,
    pub description: String,
    pub creator: String,
    pub asset_type: String,
    pub created: String,
    pub updated: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ApiResponse<T> {
    pub success: bool,
    pub data: Option<T>,
    pub error: Option<String>,
}

impl<T> ApiResponse<T> {
    pub fn success(data: T) -> Self {
        Self {
            success: true,
            data: Some(data),
            error: None,
        }
    }

    pub fn error(message: String) -> Self {
        Self {
            success: false,
            data: None,
            error: Some(message),
        }
    }
}
