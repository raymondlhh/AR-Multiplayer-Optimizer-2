# PUN 2 Multiplayer Setup Guide

This guide will help you set up a complete multiplayer system using PUN 2 where all devices automatically join the same room.

## 🚀 Quick Start

### 1. Scene Setup
1. Create a new scene or use your existing scene
2. Add an empty GameObject and name it "NetworkManager"
3. Add the `NetworkManager` script to this GameObject
4. Add another empty GameObject and name it "UIManager" 
5. Add the `UIManager` script to this GameObject

### 2. Player Prefab Setup
1. Create a new GameObject and name it "PlayerPrefab"
2. Add the following components:
   - **PhotonView** (Photon Networking → PhotonView)
   - **Rigidbody** (Physics → Rigidbody)
   - **CapsuleCollider** (Physics → Capsule Collider)
   - **PlayerController** script
3. Add a visual representation (Cube, Capsule, etc.) as a child
4. Configure the PhotonView:
   - Set "Synchronization" to "Unreliable On Change"
   - Add "PlayerController" to "Observed Components"
5. Save as a prefab in the `Resources` folder

### 3. UI Setup
Create a Canvas with the following UI elements:

#### Main Menu Panel
- **Connect Button** - Connect to Photon
- **Connection Status Text** - Shows connection state
- **Feedback Text** - Shows connection messages

#### Game Panel (shown when in room)
- **Room Info Text** - Shows room name
- **Player Count Text** - Shows current/max players
- **Leave Room Button** - Leave the current room

### 4. NetworkManager Configuration
In the NetworkManager component, assign:
- **Control Panel** → Main Menu Panel
- **Feedback Text** → Feedback Text UI element
- **Connect Button** → Connect Button
- **Player Prefab** → Your PlayerPrefab from Resources
- **Spawn Points** → Array of Transform objects for player spawn positions

### 5. UIManager Configuration
In the UIManager component, assign:
- **Main Menu Panel** → Main Menu Panel
- **Game Panel** → Game Panel
- **Connection Panel** → Connection Panel (optional)
- **Connection Status Text** → Connection Status Text
- **Room Info Text** → Room Info Text
- **Player Count Text** → Player Count Text
- **Feedback Text** → Feedback Text
- **Connect Button** → Connect Button
- **Disconnect Button** → Disconnect Button
- **Leave Room Button** → Leave Room Button

## 🎮 How It Works

### Automatic Room Joining
- When the app starts, it automatically connects to Photon
- It tries to join a room named "AR_Multiplayer_Room"
- If the room doesn't exist, it creates one
- All devices will join the same room automatically

### Player Spawning
- Each player gets a unique spawn position
- Players are automatically spawned when they join
- Each player has a different color for identification
- Movement and actions are synchronized across all devices

### Room Management
- Maximum 4 players per room (configurable)
- Room is always open and visible
- Players can leave and rejoin
- Room persists as long as at least one player is in it

## 🔧 Customization

### Changing Room Settings
In `NetworkManager.cs`:
```csharp
[SerializeField] private byte maxPlayersPerRoom = 4;  // Change max players
[SerializeField] private string roomName = "AR_Multiplayer_Room";  // Change room name
```

### Adding More Spawn Points
1. Create empty GameObjects as children of NetworkManager
2. Position them where you want players to spawn
3. Assign them to the "Spawn Points" array in NetworkManager

### Customizing Player Movement
Edit `PlayerController.cs` to modify:
- Movement speed
- Rotation speed
- Input handling
- Jump mechanics

## 🧪 Testing

### Local Testing
1. Build and run the app on one device
2. Use Unity Editor to test with multiple clients:
   - Go to File → Build Settings
   - Add current scene to build
   - Build and run
   - Open the same scene in Unity Editor
   - Press Play in both instances

### Multi-Device Testing
1. Build the app for your target platform
2. Install on multiple devices
3. Run the app on all devices
4. All devices should automatically join the same room

## 🐛 Troubleshooting

### Common Issues
1. **Players not spawning**: Check if PlayerPrefab is in Resources folder
2. **No connection**: Verify Photon App ID in PhotonServerSettings
3. **UI not updating**: Check UI element assignments in UIManager
4. **Movement not syncing**: Ensure PhotonView is configured correctly

### Debug Tips
- Check the Console for Photon connection messages
- Use the feedback text to see connection status
- Verify all required components are assigned
- Test with Unity Editor + Build for local multiplayer

## 📱 Platform Notes

- Works on all platforms supported by Unity
- AR functionality can be added on top of this multiplayer system
- Network performance may vary based on platform and connection quality

## 🔄 Next Steps

Once basic multiplayer is working:
1. Add AR functionality (Vuforia, AR Foundation, etc.)
2. Implement game-specific mechanics
3. Add more sophisticated UI
4. Implement voice chat (Photon Voice)
5. Add player customization options
