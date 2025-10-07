mod client;
mod asset_loader;

pub use client::RobloxApiClient;
pub use asset_loader::AssetLoader;

use anyhow::Result;
use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UserInfo {
    pub id: u64,
    pub name: String,
    pub display_name: String,
    pub description: String,
    pub created: String,
    pub is_banned: bool,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct GameInfo {
    pub id: u64,
    pub name: String,
    pub description: String,
    pub creator: Creator,
    pub price: Option<u64>,
    pub playing: u64,
    pub visits: u64,
    pub favorites: u64,
    pub max_players: u32,
    pub genre: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Creator {
    pub id: u64,
    pub name: String,
    pub creator_type: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AssetDetails {
    pub id: u64,
    pub name: String,
    pub description: String,
    pub asset_type: String,
    pub creator: Creator,
    pub price: Option<u64>,
    pub is_for_sale: bool,
    pub is_limited: bool,
    pub is_limited_unique: bool,
    pub remaining: Option<u64>,
}
