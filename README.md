# Debeon - Roblox Bootstrapper

Debeon is a powerful Roblox bootstrapper that provides advanced configuration and customization options for the Roblox client. Built with a modern C# WPF frontend and a high-performance Rust backend, Debeon gives you complete control over your Roblox experience.

## Features

- **Graphics Configuration**: Fine-tune quality settings, shadows, textures, anti-aliasing, and more
- **Audio Settings**: Control volume levels, spatial audio, and device selection
- **Controls Management**: Customize mouse sensitivity, key bindings, and gamepad settings
- **Performance Optimization**: Configure frame rates, rendering features, and optimization modes
- **Fast Flags**: Direct access to Roblox's internal configuration flags with preset configurations
- **Profile System**: Save and load complete configuration profiles for different scenarios
- **Asset Loader**: Fetch and view information about Roblox assets via the API
- **Modern UI**: Clean, dark-themed interface with smooth navigation

## Architecture

### Backend (Rust)
- **Roblox Detection**: Automatically finds and manages Roblox installations
- **Configuration Management**: Applies settings directly to Roblox's ClientSettings
- **API Integration**: Connects to Roblox's public APIs for asset information
- **REST Server**: Provides HTTP API for frontend communication on port 8080

### Frontend (C# WPF)
- **Modern UI**: Built with ModernWpfUI for a contemporary look and feel
- **Navigation System**: Easy-to-use sidebar navigation between different settings pages
- **Real-time Updates**: Instant feedback when applying configurations
- **Profile Management**: Save, load, and manage multiple configuration profiles

## Getting Started

### Prerequisites

#### Backend
- Rust 1.70 or higher
- Cargo package manager

#### Frontend
- .NET 8.0 SDK or higher
- Windows 10/11

### Building the Backend

```bash
cd backend
cargo build --release
```

The compiled binary will be in `backend/target/release/debeon-backend.exe`

### Building the Frontend

```bash
cd frontend
dotnet build -c Release
```

The compiled executable will be in `frontend/bin/Release/net8.0-windows/Debeon.exe`

### Running Debeon

1. Start the backend server:
```bash
cd backend
cargo run --release
```

2. Launch the frontend application:
```bash
cd frontend
dotnet run
```

Or simply run the compiled `Debeon.exe` after building.

## Usage

### Dashboard
View system information, current configuration summary, and quick preset profiles.

### Graphics Settings
- Adjust graphics quality levels (1-21)
- Configure shadow, texture, and particle quality
- Set anti-aliasing and anisotropic filtering
- Enable/disable V-Sync and fullscreen mode
- Customize resolution

### Audio Settings
- Control master, music, SFX, and voice volumes
- Enable spatial audio
- Select input/output devices

### Controls Settings
- Adjust mouse and gamepad sensitivity
- Configure camera behavior
- Customize key bindings
- Reset to default controls

### Performance Settings
- Set frame rate limits or uncap FPS
- Toggle rendering features:
  - Dynamic Lighting
  - Post Processing
  - Bloom
  - Depth of Field
  - Motion Blur
  - Ambient Occlusion
  - Reflections
  - Global Illumination
- Enable low latency mode
- Configure GPU preferences

### Fast Flags
- Load current Roblox flags
- Apply preset configurations:
  - Uncap FPS
  - Low Latency
  - Ultra Graphics
  - Potato Mode
- Add custom flags manually
- Clear all custom flags

### Profiles
- Save current configuration as a named profile
- Load previously saved profiles
- Delete unwanted profiles
- Quick switching between configurations

### Assets
- Load asset information by ID
- View asset details (name, creator, type)
- Quick links to Roblox catalog, library, and more

## Configuration Files

Debeon stores configuration in the following locations:

- **Profiles**: `%APPDATA%/Debeon/profiles/`
- **Backups**: `%LOCALAPPDATA%/Debeon/backups/`
- **Asset Cache**: `%LOCALAPPDATA%/Debeon/assets/`

## API Endpoints

The Rust backend exposes the following REST API endpoints:

- `GET /api/installations` - List detected Roblox installations
- `GET /api/config/{name}` - Load a configuration profile
- `POST /api/config/{name}` - Save a configuration profile
- `POST /api/apply` - Apply configuration to Roblox
- `GET /api/profiles` - List all saved profiles
- `GET /api/flags` - Get current Fast Flags
- `POST /api/flags` - Set Fast Flags
- `GET /api/user/{id}` - Get Roblox user information
- `GET /api/asset/{id}` - Get asset details
- `GET /api/download/asset/{id}` - Download asset data

## Security Notes

- Debeon only modifies Roblox's official configuration files
- No game files are modified or patched
- All settings are applied through Roblox's supported ClientSettings system
- The application requires no special permissions beyond file system access

## Troubleshooting

### Backend won't start
- Ensure port 8080 is not in use
- Check that Roblox is installed
- Verify Rust dependencies are installed

### Configuration not applying
- Make sure Roblox is closed when applying settings
- Verify the backend server is running
- Check that you have write permissions to the Roblox directory

### UI not connecting to backend
- Confirm the backend is running on port 8080
- Check Windows Firewall settings
- Verify no antivirus is blocking the connection

## Contributing

This project is built for educational and personal use. Feel free to fork and modify as needed.

## License

This project is provided as-is for personal use. Roblox is a trademark of Roblox Corporation.

## Disclaimer

This tool modifies Roblox client settings. Use at your own risk. The developers are not responsible for any issues that may arise from using this software.
