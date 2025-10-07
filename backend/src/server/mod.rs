use anyhow::Result;
use warp::{Filter, Reply};
use std::sync::Arc;
use tokio::sync::RwLock;
use crate::roblox::RobloxManager;
use crate::config::ConfigManager;
use crate::api::{RobloxApiClient, AssetLoader};
use crate::models::{RobloxConfig, ApiResponse};

pub struct Server {
    roblox_manager: Arc<RwLock<RobloxManager>>,
    config_manager: Arc<RwLock<ConfigManager>>,
    api_client: Arc<RobloxApiClient>,
    asset_loader: Arc<AssetLoader>,
}

impl Server {
    pub fn new() -> Result<Self> {
        Ok(Self {
            roblox_manager: Arc::new(RwLock::new(RobloxManager::new()?)),
            config_manager: Arc::new(RwLock::new(ConfigManager::new()?)),
            api_client: Arc::new(RobloxApiClient::new()?),
            asset_loader: Arc::new(AssetLoader::new()?),
        })
    }

    pub async fn run(self) -> Result<()> {
        let roblox_manager = self.roblox_manager.clone();
        let config_manager = self.config_manager.clone();
        let api_client = self.api_client.clone();
        let asset_loader = self.asset_loader.clone();

        let cors = warp::cors()
            .allow_any_origin()
            .allow_methods(vec!["GET", "POST", "PUT", "DELETE"])
            .allow_headers(vec!["Content-Type"]);

        let get_installations = warp::path!("api" / "installations")
            .and(warp::get())
            .and(with_roblox_manager(roblox_manager.clone()))
            .and_then(handle_get_installations);

        let get_config = warp::path!("api" / "config" / String)
            .and(warp::get())
            .and(with_config_manager(config_manager.clone()))
            .and_then(handle_get_config);

        let save_config = warp::path!("api" / "config" / String)
            .and(warp::post())
            .and(warp::body::json())
            .and(with_config_manager(config_manager.clone()))
            .and_then(handle_save_config);

        let apply_config = warp::path!("api" / "apply")
            .and(warp::post())
            .and(warp::body::json())
            .and(with_roblox_manager(roblox_manager.clone()))
            .and_then(handle_apply_config);

        let list_profiles = warp::path!("api" / "profiles")
            .and(warp::get())
            .and(with_config_manager(config_manager.clone()))
            .and_then(handle_list_profiles);

        let get_flags = warp::path!("api" / "flags")
            .and(warp::get())
            .and(with_roblox_manager(roblox_manager.clone()))
            .and_then(handle_get_flags);

        let set_flags = warp::path!("api" / "flags")
            .and(warp::post())
            .and(warp::body::json())
            .and(with_roblox_manager(roblox_manager.clone()))
            .and_then(handle_set_flags);

        let get_user = warp::path!("api" / "user" / u64)
            .and(warp::get())
            .and(with_api_client(api_client.clone()))
            .and_then(handle_get_user);

        let get_asset = warp::path!("api" / "asset" / u64)
            .and(warp::get())
            .and(with_api_client(api_client.clone()))
            .and_then(handle_get_asset);

        let download_asset = warp::path!("api" / "download" / "asset" / u64)
            .and(warp::get())
            .and(with_asset_loader(asset_loader.clone()))
            .and_then(handle_download_asset);

        let routes = get_installations
            .or(get_config)
            .or(save_config)
            .or(apply_config)
            .or(list_profiles)
            .or(get_flags)
            .or(set_flags)
            .or(get_user)
            .or(get_asset)
            .or(download_asset)
            .with(cors);

        println!("Server running on http://127.0.0.1:8080");
        warp::serve(routes).run(([127, 0, 0, 1], 8080)).await;

        Ok(())
    }
}

fn with_roblox_manager(
    manager: Arc<RwLock<RobloxManager>>,
) -> impl Filter<Extract = (Arc<RwLock<RobloxManager>>,), Error = std::convert::Infallible> + Clone {
    warp::any().map(move || manager.clone())
}

fn with_config_manager(
    manager: Arc<RwLock<ConfigManager>>,
) -> impl Filter<Extract = (Arc<RwLock<ConfigManager>>,), Error = std::convert::Infallible> + Clone {
    warp::any().map(move || manager.clone())
}

fn with_api_client(
    client: Arc<RobloxApiClient>,
) -> impl Filter<Extract = (Arc<RobloxApiClient>,), Error = std::convert::Infallible> + Clone {
    warp::any().map(move || client.clone())
}

fn with_asset_loader(
    loader: Arc<AssetLoader>,
) -> impl Filter<Extract = (Arc<AssetLoader>,), Error = std::convert::Infallible> + Clone {
    warp::any().map(move || loader.clone())
}

async fn handle_get_installations(
    manager: Arc<RwLock<RobloxManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.find_installations() {
        Ok(installations) => Ok(warp::reply::json(&ApiResponse::success(installations))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_get_config(
    name: String,
    manager: Arc<RwLock<ConfigManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.load_config(&name) {
        Ok(config) => Ok(warp::reply::json(&ApiResponse::success(config))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_save_config(
    name: String,
    config: RobloxConfig,
    manager: Arc<RwLock<ConfigManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.save_config(&name, &config) {
        Ok(_) => Ok(warp::reply::json(&ApiResponse::success("Config saved"))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_apply_config(
    config: RobloxConfig,
    manager: Arc<RwLock<RobloxManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.apply_config(&config) {
        Ok(_) => Ok(warp::reply::json(&ApiResponse::success("Config applied"))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_list_profiles(
    manager: Arc<RwLock<ConfigManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.list_profiles() {
        Ok(profiles) => Ok(warp::reply::json(&ApiResponse::success(profiles))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_get_flags(
    manager: Arc<RwLock<RobloxManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.get_fast_flags() {
        Ok(flags) => Ok(warp::reply::json(&ApiResponse::success(flags))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_set_flags(
    flags: std::collections::HashMap<String, String>,
    manager: Arc<RwLock<RobloxManager>>,
) -> Result<impl Reply, warp::Rejection> {
    let manager = manager.read().await;
    match manager.set_fast_flags(&flags) {
        Ok(_) => Ok(warp::reply::json(&ApiResponse::success("Flags applied"))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_get_user(
    user_id: u64,
    client: Arc<RobloxApiClient>,
) -> Result<impl Reply, warp::Rejection> {
    match client.get_user_info(user_id) {
        Ok(user) => Ok(warp::reply::json(&ApiResponse::success(user))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_get_asset(
    asset_id: u64,
    client: Arc<RobloxApiClient>,
) -> Result<impl Reply, warp::Rejection> {
    match client.get_asset_details(asset_id) {
        Ok(asset) => Ok(warp::reply::json(&ApiResponse::success(asset))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}

async fn handle_download_asset(
    asset_id: u64,
    loader: Arc<AssetLoader>,
) -> Result<impl Reply, warp::Rejection> {
    match loader.download_asset(asset_id) {
        Ok(bytes) => Ok(warp::reply::json(&ApiResponse::success(base64::encode(bytes)))),
        Err(e) => Ok(warp::reply::json(&ApiResponse::<()>::error(e.to_string()))),
    }
}
