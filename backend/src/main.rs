mod api;
mod config;
mod roblox;
mod server;
mod models;

use anyhow::Result;
use server::Server;

#[tokio::main]
async fn main() -> Result<()> {
    println!("Debeon Backend Server Starting...");

    let server = Server::new()?;
    server.run().await?;

    Ok(())
}
