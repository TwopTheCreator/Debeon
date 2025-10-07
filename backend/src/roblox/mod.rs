mod detector;
mod installer;
mod patcher;
mod flags;

pub use detector::RobloxDetector;
pub use installer::RobloxInstaller;
pub use patcher::ConfigPatcher;
pub use flags::FlagManager;

use anyhow::Result;
use std::path::PathBuf;
use crate::models::RobloxInstallation;

pub struct RobloxManager {
    detector: RobloxDetector,
    installer: RobloxInstaller,
    patcher: ConfigPatcher,
    flag_manager: FlagManager,
}

impl RobloxManager {
    pub fn new() -> Result<Self> {
        Ok(Self {
            detector: RobloxDetector::new()?,
            installer: RobloxInstaller::new()?,
            patcher: ConfigPatcher::new()?,
            flag_manager: FlagManager::new()?,
        })
    }

    pub fn find_installations(&self) -> Result<Vec<RobloxInstallation>> {
        self.detector.find_installations()
    }

    pub fn get_install_path(&self) -> Result<PathBuf> {
        self.detector.get_primary_installation()
    }

    pub fn apply_config(&self, config: &crate::models::RobloxConfig) -> Result<()> {
        self.patcher.apply_configuration(config)
    }

    pub fn backup_config(&self) -> Result<()> {
        self.patcher.backup_current_config()
    }

    pub fn restore_config(&self, backup_name: &str) -> Result<()> {
        self.patcher.restore_from_backup(backup_name)
    }

    pub fn set_fast_flags(&self, flags: &std::collections::HashMap<String, String>) -> Result<()> {
        self.flag_manager.apply_flags(flags)
    }

    pub fn get_fast_flags(&self) -> Result<std::collections::HashMap<String, String>> {
        self.flag_manager.read_flags()
    }
}
