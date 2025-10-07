use anyhow::{Result, Context};
use std::path::PathBuf;

pub struct RobloxInstaller {
    download_url: String,
}

impl RobloxInstaller {
    pub fn new() -> Result<Self> {
        Ok(Self {
            download_url: String::from("https://setup.rbxcdn.com"),
        })
    }

    pub fn get_latest_version_info(&self) -> Result<VersionInfo> {
        let client = reqwest::blocking::Client::new();
        let response = client
            .get("https://clientsettingscdn.roblox.com/v2/client-version/WindowsPlayer")
            .send()?
            .json::<serde_json::Value>()?;

        Ok(VersionInfo {
            version: response["clientVersionUpload"]
                .as_str()
                .unwrap_or("unknown")
                .to_string(),
            deployment_url: format!("{}/", self.download_url),
        })
    }

    pub fn download_bootstrapper(&self, destination: &PathBuf) -> Result<()> {
        let url = format!("{}/RobloxPlayerLauncher.exe", self.download_url);
        let client = reqwest::blocking::Client::new();
        let response = client.get(&url).send()?;

        let bytes = response.bytes()?;
        std::fs::write(destination, bytes)?;

        Ok(())
    }

    pub fn verify_installation(&self, path: &PathBuf) -> Result<bool> {
        if !path.exists() {
            return Ok(false);
        }

        let player_exe = path.join("RobloxPlayerBeta.exe");
        Ok(player_exe.exists())
    }
}

#[derive(Debug, Clone)]
pub struct VersionInfo {
    pub version: String,
    pub deployment_url: String,
}
