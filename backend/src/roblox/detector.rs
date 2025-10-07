use anyhow::{Result, Context};
use std::path::{Path, PathBuf};
use std::fs;
use walkdir::WalkDir;
use crate::models::RobloxInstallation;

#[cfg(target_os = "windows")]
use winreg::enums::*;
#[cfg(target_os = "windows")]
use winreg::RegKey;

pub struct RobloxDetector {
    search_paths: Vec<PathBuf>,
}

impl RobloxDetector {
    pub fn new() -> Result<Self> {
        let mut search_paths = Vec::new();

        if let Some(local_app_data) = dirs::data_local_dir() {
            search_paths.push(local_app_data.join("Roblox"));
        }

        if let Some(app_data) = dirs::data_dir() {
            search_paths.push(app_data.join("Roblox"));
        }

        search_paths.push(PathBuf::from("C:\\Program Files (x86)\\Roblox"));
        search_paths.push(PathBuf::from("C:\\Program Files\\Roblox"));

        #[cfg(target_os = "windows")]
        {
            if let Ok(reg_path) = Self::get_registry_path() {
                search_paths.push(reg_path);
            }
        }

        Ok(Self { search_paths })
    }

    #[cfg(target_os = "windows")]
    fn get_registry_path() -> Result<PathBuf> {
        let hkcu = RegKey::predef(HKEY_CURRENT_USER);
        let roblox_key = hkcu.open_subkey("Software\\Roblox\\RobloxStudioBrowser\\roblox.com")?;
        let path: String = roblox_key.get_value("InstallLocation")?;
        Ok(PathBuf::from(path))
    }

    pub fn find_installations(&self) -> Result<Vec<RobloxInstallation>> {
        let mut installations = Vec::new();

        for base_path in &self.search_paths {
            if !base_path.exists() {
                continue;
            }

            for entry in WalkDir::new(base_path)
                .max_depth(4)
                .follow_links(false)
                .into_iter()
                .filter_map(|e| e.ok())
            {
                let path = entry.path();

                if path.file_name().and_then(|n| n.to_str()) == Some("RobloxPlayerBeta.exe")
                    || path.file_name().and_then(|n| n.to_str()) == Some("RobloxStudioBeta.exe") {

                    if let Some(version_dir) = path.parent() {
                        let version = version_dir
                            .file_name()
                            .and_then(|n| n.to_str())
                            .unwrap_or("unknown")
                            .to_string();

                        let metadata = fs::metadata(path)?;
                        let modified = metadata.modified()?;
                        let datetime: chrono::DateTime<chrono::Local> = modified.into();

                        installations.push(RobloxInstallation {
                            path: version_dir.to_string_lossy().to_string(),
                            version,
                            channel: Self::detect_channel(path),
                            last_modified: datetime.format("%Y-%m-%d %H:%M:%S").to_string(),
                        });
                    }
                }
            }
        }

        Ok(installations)
    }

    pub fn get_primary_installation(&self) -> Result<PathBuf> {
        let installations = self.find_installations()?;

        installations
            .into_iter()
            .max_by_key(|i| i.last_modified.clone())
            .map(|i| PathBuf::from(i.path))
            .context("No Roblox installation found")
    }

    fn detect_channel(path: &Path) -> String {
        let path_str = path.to_string_lossy().to_lowercase();

        if path_str.contains("studio") {
            "Studio".to_string()
        } else if path_str.contains("player") {
            "Player".to_string()
        } else {
            "Unknown".to_string()
        }
    }

    pub fn get_client_settings_path(&self) -> Result<PathBuf> {
        let install_path = self.get_primary_installation()?;
        Ok(install_path.join("ClientSettings"))
    }

    pub fn ensure_client_settings_dir(&self) -> Result<PathBuf> {
        let settings_path = self.get_client_settings_path()?;

        if !settings_path.exists() {
            fs::create_dir_all(&settings_path)?;
        }

        Ok(settings_path)
    }
}
