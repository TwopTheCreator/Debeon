use anyhow::{Result, Context};
use reqwest::blocking::Client;
use serde_json::Value;
use super::{UserInfo, GameInfo, AssetDetails, Creator};

pub struct RobloxApiClient {
    client: Client,
    base_url: String,
    games_api_url: String,
    economy_api_url: String,
}

impl RobloxApiClient {
    pub fn new() -> Result<Self> {
        let client = Client::builder()
            .user_agent("Debeon/1.0")
            .timeout(std::time::Duration::from_secs(30))
            .build()?;

        Ok(Self {
            client,
            base_url: String::from("https://users.roblox.com/v1"),
            games_api_url: String::from("https://games.roblox.com/v1"),
            economy_api_url: String::from("https://economy.roblox.com/v2"),
        })
    }

    pub fn get_user_info(&self, user_id: u64) -> Result<UserInfo> {
        let url = format!("{}/users/{}", self.base_url, user_id);
        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to fetch user info: {}", response.status());
        }

        let data: Value = response.json()?;

        Ok(UserInfo {
            id: data["id"].as_u64().unwrap_or(0),
            name: data["name"].as_str().unwrap_or("").to_string(),
            display_name: data["displayName"].as_str().unwrap_or("").to_string(),
            description: data["description"].as_str().unwrap_or("").to_string(),
            created: data["created"].as_str().unwrap_or("").to_string(),
            is_banned: data["isBanned"].as_bool().unwrap_or(false),
        })
    }

    pub fn get_game_info(&self, universe_id: u64) -> Result<GameInfo> {
        let url = format!("{}/games?universeIds={}", self.games_api_url, universe_id);
        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to fetch game info: {}", response.status());
        }

        let data: Value = response.json()?;

        let game_data = &data["data"][0];

        Ok(GameInfo {
            id: game_data["id"].as_u64().unwrap_or(0),
            name: game_data["name"].as_str().unwrap_or("").to_string(),
            description: game_data["description"].as_str().unwrap_or("").to_string(),
            creator: Creator {
                id: game_data["creator"]["id"].as_u64().unwrap_or(0),
                name: game_data["creator"]["name"].as_str().unwrap_or("").to_string(),
                creator_type: game_data["creator"]["type"].as_str().unwrap_or("").to_string(),
            },
            price: game_data["price"].as_u64(),
            playing: game_data["playing"].as_u64().unwrap_or(0),
            visits: game_data["visits"].as_u64().unwrap_or(0),
            favorites: game_data["favoritedCount"].as_u64().unwrap_or(0),
            max_players: game_data["maxPlayers"].as_u64().unwrap_or(0) as u32,
            genre: game_data["genre"].as_str().unwrap_or("").to_string(),
        })
    }

    pub fn get_asset_details(&self, asset_id: u64) -> Result<AssetDetails> {
        let url = format!("{}/assets/{}/details", self.economy_api_url, asset_id);
        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to fetch asset details: {}", response.status());
        }

        let data: Value = response.json()?;

        Ok(AssetDetails {
            id: data["AssetId"].as_u64().unwrap_or(0),
            name: data["Name"].as_str().unwrap_or("").to_string(),
            description: data["Description"].as_str().unwrap_or("").to_string(),
            asset_type: data["AssetTypeId"].as_u64().unwrap_or(0).to_string(),
            creator: Creator {
                id: data["Creator"]["Id"].as_u64().unwrap_or(0),
                name: data["Creator"]["Name"].as_str().unwrap_or("").to_string(),
                creator_type: data["Creator"]["CreatorType"].as_str().unwrap_or("").to_string(),
            },
            price: data["PriceInRobux"].as_u64(),
            is_for_sale: data["IsForSale"].as_bool().unwrap_or(false),
            is_limited: data["IsLimited"].as_bool().unwrap_or(false),
            is_limited_unique: data["IsLimitedUnique"].as_bool().unwrap_or(false),
            remaining: data["Remaining"].as_u64(),
        })
    }

    pub fn search_users(&self, keyword: &str) -> Result<Vec<UserInfo>> {
        let url = format!("{}/users/search?keyword={}&limit=10", self.base_url, keyword);
        let response = self.client.get(&url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to search users: {}", response.status());
        }

        let data: Value = response.json()?;
        let mut users = Vec::new();

        if let Some(user_array) = data["data"].as_array() {
            for user in user_array {
                users.push(UserInfo {
                    id: user["id"].as_u64().unwrap_or(0),
                    name: user["name"].as_str().unwrap_or("").to_string(),
                    display_name: user["displayName"].as_str().unwrap_or("").to_string(),
                    description: String::new(),
                    created: String::new(),
                    is_banned: false,
                });
            }
        }

        Ok(users)
    }

    pub fn get_client_version(&self) -> Result<String> {
        let url = "https://clientsettingscdn.roblox.com/v2/client-version/WindowsPlayer";
        let response = self.client.get(url).send()?;

        if !response.status().is_success() {
            anyhow::bail!("Failed to fetch client version: {}", response.status());
        }

        let data: Value = response.json()?;
        Ok(data["clientVersionUpload"].as_str().unwrap_or("unknown").to_string())
    }
}
