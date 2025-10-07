use anyhow::Result;
use std::collections::HashMap;
use std::fs;
use serde_json::Value;
use super::detector::RobloxDetector;

pub struct FlagManager {
    detector: RobloxDetector,
}

impl FlagManager {
    pub fn new() -> Result<Self> {
        Ok(Self {
            detector: RobloxDetector::new()?,
        })
    }

    pub fn apply_flags(&self, flags: &HashMap<String, String>) -> Result<()> {
        let settings_path = self.detector.ensure_client_settings_dir()?;
        let settings_file = settings_path.join("ClientAppSettings.json");

        let mut settings = if settings_file.exists() {
            let content = fs::read_to_string(&settings_file)?;
            serde_json::from_str::<Value>(&content).unwrap_or_else(|_| serde_json::json!({}))
        } else {
            serde_json::json!({})
        };

        let settings_obj = settings.as_object_mut().unwrap();

        for (key, value) in flags {
            let json_value = if let Ok(num) = value.parse::<i64>() {
                Value::Number(num.into())
            } else if let Ok(float) = value.parse::<f64>() {
                Value::Number(serde_json::Number::from_f64(float).unwrap())
            } else if let Ok(boolean) = value.parse::<bool>() {
                Value::Bool(boolean)
            } else {
                Value::String(value.clone())
            };

            settings_obj.insert(key.clone(), json_value);
        }

        let json_output = serde_json::to_string_pretty(&settings)?;
        fs::write(settings_file, json_output)?;

        Ok(())
    }

    pub fn read_flags(&self) -> Result<HashMap<String, String>> {
        let settings_path = self.detector.get_client_settings_path()?;
        let settings_file = settings_path.join("ClientAppSettings.json");

        if !settings_file.exists() {
            return Ok(HashMap::new());
        }

        let content = fs::read_to_string(&settings_file)?;
        let settings: Value = serde_json::from_str(&content)?;

        let mut flags = HashMap::new();

        if let Some(obj) = settings.as_object() {
            for (key, value) in obj {
                let value_str = match value {
                    Value::String(s) => s.clone(),
                    Value::Number(n) => n.to_string(),
                    Value::Bool(b) => b.to_string(),
                    _ => continue,
                };
                flags.insert(key.clone(), value_str);
            }
        }

        Ok(flags)
    }

    pub fn get_common_flags() -> HashMap<String, Vec<FlagPreset>> {
        let mut categories = HashMap::new();

        categories.insert(
            "Performance".to_string(),
            vec![
                FlagPreset {
                    name: "Uncap FPS".to_string(),
                    description: "Remove FPS limit for maximum performance".to_string(),
                    flags: vec![
                        ("DFIntTaskSchedulerTargetFps".to_string(), "999".to_string()),
                    ],
                },
                FlagPreset {
                    name: "Low Latency".to_string(),
                    description: "Reduce input lag and network latency".to_string(),
                    flags: vec![
                        ("FFlagEnableLowLatencyMode".to_string(), "true".to_string()),
                        ("DFIntConnectionMTUSize".to_string(), "1492".to_string()),
                    ],
                },
                FlagPreset {
                    name: "Memory Optimization".to_string(),
                    description: "Optimize memory usage".to_string(),
                    flags: vec![
                        ("FFlagEnableMemoryOptimization".to_string(), "true".to_string()),
                        ("DFIntHttpCacheCleanMaxFileSizeMB".to_string(), "128".to_string()),
                    ],
                },
            ],
        );

        categories.insert(
            "Graphics".to_string(),
            vec![
                FlagPreset {
                    name: "Ultra Graphics".to_string(),
                    description: "Maximum visual quality".to_string(),
                    flags: vec![
                        ("DFIntDebugFRMQualityLevelOverride".to_string(), "21".to_string()),
                        ("FIntRenderShadowIntensity".to_string(), "100".to_string()),
                        ("DFIntTextureQualityOverride".to_string(), "3".to_string()),
                    ],
                },
                FlagPreset {
                    name: "Potato Mode".to_string(),
                    description: "Minimum graphics for maximum FPS".to_string(),
                    flags: vec![
                        ("DFIntDebugFRMQualityLevelOverride".to_string(), "1".to_string()),
                        ("FFlagDisablePostFx".to_string(), "true".to_string()),
                        ("FIntRenderShadowIntensity".to_string(), "0".to_string()),
                    ],
                },
            ],
        );

        categories.insert(
            "UI".to_string(),
            vec![
                FlagPreset {
                    name: "Show FPS Counter".to_string(),
                    description: "Display FPS counter in-game".to_string(),
                    flags: vec![
                        ("FFlagDebugDisplayFPS".to_string(), "true".to_string()),
                    ],
                },
                FlagPreset {
                    name: "Minimal UI".to_string(),
                    description: "Hide unnecessary UI elements".to_string(),
                    flags: vec![
                        ("FFlagEnableMinimalUI".to_string(), "true".to_string()),
                    ],
                },
            ],
        );

        categories
    }
}

#[derive(Debug, Clone)]
pub struct FlagPreset {
    pub name: String,
    pub description: String,
    pub flags: Vec<(String, String)>,
}
