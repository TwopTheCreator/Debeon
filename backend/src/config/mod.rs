use anyhow::{Result, Context};
use std::fs;
use std::path::PathBuf;
use crate::models::RobloxConfig;

pub struct ConfigManager {
    config_dir: PathBuf,
    profiles_dir: PathBuf,
}

impl ConfigManager {
    pub fn new() -> Result<Self> {
        let config_dir = dirs::config_dir()
            .context("Failed to get config directory")?
            .join("Debeon");

        let profiles_dir = config_dir.join("profiles");

        if !config_dir.exists() {
            fs::create_dir_all(&config_dir)?;
        }

        if !profiles_dir.exists() {
            fs::create_dir_all(&profiles_dir)?;
        }

        Ok(Self {
            config_dir,
            profiles_dir,
        })
    }

    pub fn save_config(&self, name: &str, config: &RobloxConfig) -> Result<()> {
        let file_path = self.profiles_dir.join(format!("{}.json", name));
        let json = serde_json::to_string_pretty(config)?;
        fs::write(file_path, json)?;
        Ok(())
    }

    pub fn load_config(&self, name: &str) -> Result<RobloxConfig> {
        let file_path = self.profiles_dir.join(format!("{}.json", name));

        if !file_path.exists() {
            anyhow::bail!("Config profile not found: {}", name);
        }

        let content = fs::read_to_string(file_path)?;
        let config = serde_json::from_str(&content)?;
        Ok(config)
    }

    pub fn list_profiles(&self) -> Result<Vec<String>> {
        let mut profiles = Vec::new();

        if !self.profiles_dir.exists() {
            return Ok(profiles);
        }

        for entry in fs::read_dir(&self.profiles_dir)? {
            let entry = entry?;
            if let Some(name) = entry.file_name().to_str() {
                if name.ends_with(".json") {
                    profiles.push(name.trim_end_matches(".json").to_string());
                }
            }
        }

        profiles.sort();
        Ok(profiles)
    }

    pub fn delete_profile(&self, name: &str) -> Result<()> {
        let file_path = self.profiles_dir.join(format!("{}.json", name));

        if !file_path.exists() {
            anyhow::bail!("Config profile not found: {}", name);
        }

        fs::remove_file(file_path)?;
        Ok(())
    }

    pub fn get_default_config(&self) -> RobloxConfig {
        RobloxConfig::default()
    }

    pub fn export_config(&self, name: &str, destination: &PathBuf) -> Result<()> {
        let source = self.profiles_dir.join(format!("{}.json", name));

        if !source.exists() {
            anyhow::bail!("Config profile not found: {}", name);
        }

        fs::copy(source, destination)?;
        Ok(())
    }

    pub fn import_config(&self, source: &PathBuf, name: &str) -> Result<()> {
        if !source.exists() {
            anyhow::bail!("Source file not found");
        }

        let content = fs::read_to_string(source)?;
        let _config: RobloxConfig = serde_json::from_str(&content)?;

        let destination = self.profiles_dir.join(format!("{}.json", name));
        fs::copy(source, destination)?;

        Ok(())
    }
}
