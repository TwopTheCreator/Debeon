use anyhow::{Result, Context};
use reqwest::blocking::Client;
use std::path::PathBuf;
use std::fs;

pub struct AssetLoader {
    client: Client,
    cache_dir: PathBuf,
}

impl AssetLoader {
    pub fn new() -> Result<Self> {
        let cache_dir = dirs::cache_dir()
            .context("Failed to get cache directory")?
            .join("Debeon")
            .join("assets");

        if !cache_dir.exists() {
            fs::create_dir_all(&cache_dir)?;
        }

        let client = Client::builder()
            .user_agent("Roblox/WinInet")
            .timeout(std::time::Duration::from_secs(60))
            .build()?;

        Ok(Self {
            client,
            cache_dir,
        })
    }

    pub fn download_asset(&self, asset_id: u64) -> Result<Vec<u8>> {
        let cache_file = self.cache_dir.join(format!("{}.rbxm", asset_id));

        if cache_file.exists() {
            return Ok(fs::read(cache_file)?);
        }

        let url = format!("https://assetdelivery.roblox.com/v1/asset/?id={}", asset_id);
        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to download asset: {}", response.status());
        }

        let bytes = response.bytes()?.to_vec();
        fs::write(&cache_file, &bytes)?;

        Ok(bytes)
    }

    pub fn download_thumbnail(&self, asset_id: u64, size: ThumbnailSize) -> Result<Vec<u8>> {
        let size_str = match size {
            ThumbnailSize::Small => "150x150",
            ThumbnailSize::Medium => "420x420",
            ThumbnailSize::Large => "768x432",
        };

        let cache_file = self.cache_dir.join(format!("{}_{}.png", asset_id, size_str));

        if cache_file.exists() {
            return Ok(fs::read(cache_file)?);
        }

        let url = format!(
            "https://thumbnails.roblox.com/v1/assets?assetIds={}&size={}x{}&format=Png",
            asset_id,
            size_str.split('x').next().unwrap(),
            size_str.split('x').last().unwrap()
        );

        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to download thumbnail: {}", response.status());
        }

        let json: serde_json::Value = response.json()?;

        if let Some(image_url) = json["data"][0]["imageUrl"].as_str() {
            let image_response = self.client.get(image_url).send()?;
            let bytes = image_response.bytes()?.to_vec();
            fs::write(&cache_file, &bytes)?;
            Ok(bytes)
        } else {
            anyhow::bail!("No thumbnail URL found")
        }
    }

    pub fn download_game_icon(&self, universe_id: u64) -> Result<Vec<u8>> {
        let cache_file = self.cache_dir.join(format!("game_{}.png", universe_id));

        if cache_file.exists() {
            return Ok(fs::read(cache_file)?);
        }

        let url = format!(
            "https://thumbnails.roblox.com/v1/games/icons?universeIds={}&size=512x512&format=Png",
            universe_id
        );

        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to download game icon: {}", response.status());
        }

        let json: serde_json::Value = response.json()?;

        if let Some(image_url) = json["data"][0]["imageUrl"].as_str() {
            let image_response = self.client.get(image_url).send()?;
            let bytes = image_response.bytes()?.to_vec();
            fs::write(&cache_file, &bytes)?;
            Ok(bytes)
        } else {
            anyhow::bail!("No game icon URL found")
        }
    }

    pub fn clear_cache(&self) -> Result<()> {
        if self.cache_dir.exists() {
            fs::remove_dir_all(&self.cache_dir)?;
            fs::create_dir_all(&self.cache_dir)?;
        }
        Ok(())
    }

    pub fn get_cache_size(&self) -> Result<u64> {
        if !self.cache_dir.exists() {
            return Ok(0);
        }

        let mut total_size = 0u64;

        for entry in walkdir::WalkDir::new(&self.cache_dir) {
            let entry = entry?;
            if entry.file_type().is_file() {
                total_size += entry.metadata()?.len();
            }
        }

        Ok(total_size)
    }
}

#[derive(Debug, Clone, Copy)]
pub enum ThumbnailSize {
    Small,
    Medium,
    Large,
}
