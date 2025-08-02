# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 6.0.0 (migrated from 2022.3.62f1) multiplayer racing game called "ShipIT" with RevenueCat integration for in-app purchases. The game features character customization, multiplayer networking via Unity Netcode for GameObjects 2.x, and premium content monetization.

## Core Architecture

### Key Systems
- **Multiplayer Networking**: Unity Netcode for GameObjects 2.x with Unity 6 Multiplayer Services package
- **In-App Purchases**: RevenueCat SDK 7.6.0+ with Unity 6 billing system compatibility
- **Character Customization**: Modular system with ScriptableObject database for items
- **Player Movement**: Command pattern implementation for input handling with Unity Input System 1.12.0
- **Scene Management**: Custom scene transition system with pause/resume functionality
- **Camera System**: Cinemachine 3.1.0 for Unity 6 compatible camera controls

### Project Structure
- `Assets/Scripts/Multiplayer/` - Networking components, lobby management, player spawning
- `Assets/Scripts/RevenueCat/` - Purchase handling and RevenueCat integration 
- `Assets/Scripts/Level/` - Scene management, spawn points, race mechanics
- `Assets/Scripts/Player/` - Player movement, camera controls, character customization
- `Assets/Scripts/Input/` - Command pattern input system with mobile support
- `Assets/Scripts/CharacterCustomizationLevel/` - Customization UI and database
- `Assets/Prefabs/Multiplayer/` - Network-enabled prefabs for multiplayer gameplay

### Key Components
- `PurchaseManager.cs` - Singleton managing RevenueCat purchase flow and premium item validation
- `LobbyManager.cs` - Handles Unity Lobby creation, joining, and player management
- `NetworkManagerHUD.cs` - Development UI for network testing
- `Movement.cs` - NetworkBehaviour for synchronized player movement
- `SOCustomizationDatabase.cs` - ScriptableObject containing all customization items

## Development Commands

This is a Unity 6 project without external package managers. All development is done through Unity Editor:

### Building
- Open project in Unity Editor 6.0.0+ (6000.1.14f1 or later)
- File > Build Settings to configure builds
- For Android: Switch platform to Android in Build Settings
- Unity 6 supports improved build pipelines and better platform optimization

### Testing
- Play mode testing: Use Unity Editor play button
- Multiplayer testing: Use ParrelSync tool (included) to clone project for multiple instances
- Network testing: Use NetworkManagerHUD in development builds
- Unity 6 includes enhanced Multiplayer Play Mode for in-editor testing

### Package Management
- Unity packages managed through Package Manager window (Unity 6 compatible versions)
- Unity 6 Multiplayer Services package provides unified networking services
- External dependencies handled by External Dependency Manager (Google)
- RevenueCat SDK 7.6.0+ integrated via Unity Package Manager with Unity 6 support
- Unity Transport 2.4.0+ for enhanced networking capabilities

## Special Considerations

### RevenueCat Integration
- Product IDs must match RevenueCat dashboard configuration
- Purchase validation is asynchronous - avoid blocking operations
- Test purchases require RevenueCat sandbox mode

### Multiplayer Development
- Always test with multiple clients using ParrelSync
- Network objects require NetworkBehaviour components
- Scene management must account for host/client differences

### Mobile Controls
- Joystick controls implemented via Joystick Pack asset
- Touch input handled through Unity Input System
- UI scales for different screen sizes

### Character Customization
- Items stored in SOCustomizationDatabase ScriptableObject
- Premium items require RevenueCat purchase validation
- Customization persists across multiplayer sessions

## Unity 6 Migration Notes

### Package Updates
- **Netcode for GameObjects**: Upgraded to 2.1.2 for Unity 6 compatibility
- **Multiplayer Services**: Migrated from standalone Lobby/Relay to unified package 1.0.0
- **Unity Transport**: Added 2.4.0 for enhanced networking and WebGL support
- **Cinemachine**: Updated to 3.1.0 with improved input handling
- **Input System**: Updated to 1.12.0 for stable Unity 6 support
- **RevenueCat**: Confirmed 7.6.0+ compatibility with Unity 6 billing system

### API Changes
- **Cinemachine**: Updated camera input handling to use speed modifiers with direct axis assignment
- **Networking**: All NetworkVariable and RPC patterns remain compatible with Netcode 2.x
- **Transport**: No low-level API changes needed - high-level APIs remain stable
- **Authentication**: Unity Services authentication patterns compatible across versions

### Compatibility Notes
- **Backward Compatibility**: Lobby/Relay services remain functional through unified package wrapper
- **Build System**: Unity 6 build pipelines provide better optimization and platform support
- **Testing**: Enhanced Multiplayer Play Mode available for in-editor testing
- **Performance**: Unity 6 networking stack includes performance improvements for multiplayer games