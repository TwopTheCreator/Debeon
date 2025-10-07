use anyhow::{Result, Context};
use std::fs;
use std::path::PathBuf;
use serde_json::Value;
use crate::models::RobloxConfig;
use super::detector::RobloxDetector;

pub struct ConfigPatcher {
    detector: RobloxDetector,
    backup_dir: PathBuf,
}

impl ConfigPatcher {
    pub fn new() -> Result<Self> {
        let backup_dir = dirs::data_local_dir()
            .context("Failed to get local data directory")?
            .join("Debeon")
            .join("backups");

        if !backup_dir.exists() {
            fs::create_dir_all(&backup_dir)?;
        }

        Ok(Self {
            detector: RobloxDetector::new()?,
            backup_dir,
        })
    }

    pub fn apply_configuration(&self, config: &RobloxConfig) -> Result<()> {
        let settings_path = self.detector.ensure_client_settings_dir()?;
        let settings_file = settings_path.join("ClientAppSettings.json");

        let mut settings = if settings_file.exists() {
            let content = fs::read_to_string(&settings_file)?;
            serde_json::from_str::<Value>(&content).unwrap_or_else(|_| serde_json::json!({}))
        } else {
            serde_json::json!({})
        };

        let settings_obj = settings.as_object_mut().context("Invalid settings format")?;

        settings_obj.insert(
            "DFIntDebugFRMQualityLevelOverride".to_string(),
            Value::Number(config.graphics.graphics_quality.into()),
        );

        settings_obj.insert(
            "FIntRenderShadowIntensity".to_string(),
            Value::Number((config.graphics.shadow_quality * 25).into()),
        );

        settings_obj.insert(
            "DFIntTextureQualityOverride".to_string(),
            Value::Number(config.graphics.texture_quality.into()),
        );

        settings_obj.insert(
            "FFlagEnableVSync".to_string(),
            Value::Bool(config.graphics.vsync),
        );

        settings_obj.insert(
            "FFlagEnableAntiAliasing".to_string(),
            Value::Bool(config.graphics.anti_aliasing > 0),
        );

        if let Some(fps_limit) = config.rendering.frame_rate_limit {
            settings_obj.insert(
                "DFIntTaskSchedulerTargetFps".to_string(),
                Value::Number(fps_limit.into()),
            );
        }

        settings_obj.insert(
            "FFlagEnableDynamicLighting".to_string(),
            Value::Bool(config.rendering.dynamic_lighting),
        );

        settings_obj.insert(
            "FFlagEnablePostProcessing".to_string(),
            Value::Bool(config.rendering.post_processing),
        );

        settings_obj.insert(
            "FFlagEnableBloom".to_string(),
            Value::Bool(config.rendering.bloom),
        );

        settings_obj.insert(
            "FFlagEnableDepthOfField".to_string(),
            Value::Bool(config.rendering.depth_of_field),
        );

        settings_obj.insert(
            "FFlagEnableMotionBlur".to_string(),
            Value::Bool(config.rendering.motion_blur),
        );

        settings_obj.insert(
            "FFlagEnableAmbientOcclusion".to_string(),
            Value::Bool(config.rendering.ambient_occlusion),
        );

        settings_obj.insert(
            "FFlagEnableReflections".to_string(),
            Value::Bool(config.rendering.reflections),
        );

        settings_obj.insert(
            "DFIntMaxPlayers".to_string(),
            Value::Number(100.into()),
        );

        settings_obj.insert(
            "FFlagEnableLowLatencyMode".to_string(),
            Value::Bool(config.performance.low_latency_mode),
        );

        for (key, value) in &config.custom_flags {
            if let Ok(num) = value.parse::<i64>() {
                settings_obj.insert(key.clone(), Value::Number(num.into()));
            } else if let Ok(boolean) = value.parse::<bool>() {
                settings_obj.insert(key.clone(), Value::Bool(boolean));
            } else {
                settings_obj.insert(key.clone(), Value::String(value.clone()));
            }
        }

        let json_output = serde_json::to_string_pretty(&settings)?;
        fs::write(settings_file, json_output)?;

        Ok(())
    }

    pub fn backup_current_config(&self) -> Result<()> {
        let settings_path = self.detector.get_client_settings_path()?;
        let settings_file = settings_path.join("ClientAppSettings.json");

        if !settings_file.exists() {
            return Ok(());
        }

        let timestamp = chrono::Local::now().format("%Y%m%d_%H%M%S");
        let backup_file = self.backup_dir.join(format!("backup_{}.json", timestamp));

        fs::copy(settings_file, backup_file)?;

        Ok(())
    }

    pub fn restore_from_backup(&self, backup_name: &str) -> Result<()> {
        let backup_file = self.backup_dir.join(backup_name);

        if !backup_file.exists() {
            anyhow::bail!("Backup file not found");
        }

        let settings_path = self.detector.ensure_client_settings_dir()?;
        let settings_file = settings_path.join("ClientAppSettings.json");

        fs::copy(backup_file, settings_file)?;

        Ok(())
    }

    pub fn list_backups(&self) -> Result<Vec<String>> {
        let mut backups = Vec::new();

        if !self.backup_dir.exists() {
            return Ok(backups);
        }

        for entry in fs::read_dir(&self.backup_dir)? {
            let entry = entry?;
            if let Some(name) = entry.file_name().to_str() {
                if name.ends_with(".json") {
                    backups.push(name.to_string());
                }
            }
        }

        backups.sort();
        backups.reverse();

        Ok(backups)
    }
}
