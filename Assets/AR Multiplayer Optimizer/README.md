# AR Multiplayer Optimizer - Enhanced Synchronization

## Overview
The AR Multiplayer Optimizer now includes **proper anchor root synchronization** across all clients in a Photon multiplayer session. This ensures that virtual objects appear in the same position relative to the real world for all players.

## Key Features

### ✅ **Fixed Issues**
- **Anchor Root Synchronization**: The anchor root position is now synchronized across all clients
- **Late-Joining Client Support**: New players receive the current anchor position when joining
- **Continuous Synchronization**: Anchor position is continuously streamed via Photon
- **Editor vs APK Consistency**: Objects now appear in the same position in both Unity Editor and APK builds

### 🔧 **How It Works**

1. **First Client Alignment**: When the first client detects the Vuforia Image Target, it:
   - Snaps the AnchorRoot to the Image Target position
   - Sends the anchor position to all other clients via RPC
   - Marks itself as aligned

2. **Other Clients**: When other clients receive the anchor position:
   - They apply the same anchor position to their AnchorRoot
   - All virtual objects now appear in the same world position
   - They mark themselves as aligned

3. **Late-Joining Clients**: When a new player joins:
   - They automatically receive the current anchor position
   - Their AnchorRoot is positioned to match other clients
   - Virtual objects appear in the correct position immediately

## Setup Instructions

### 1. **Automatic Setup (Recommended)**
1. Go to `AR Multiplayer Optimizer > Setup Helper` in the Unity menu
2. Click "Create AMOConfig" to create the configuration asset
3. Click "Setup AMOSessionManager" to configure the session manager
4. Click "Test Configuration" to verify everything is set up correctly

### 2. **Manual Setup**

#### Step 1: Create AMOConfig
1. Right-click in Project window → Create → AR Multiplayer Optimizer → Config
2. Set the Image Target name (e.g., "ARMascot")
3. Configure other settings as needed
4. Save the asset in the `Resources` folder

#### Step 2: Setup AMOSessionManager
1. Create an empty GameObject named "AMOSessionManager"
2. Add the `AMOSessionManager` script
3. Add a `PhotonView` component
4. Configure the PhotonView:
   - Add `AMOSessionManager` to Observed Components
   - Set Synchronization to "Unreliable On Change"

#### Step 3: Configure NetworkManager
The NetworkManager should automatically call the AR Multiplayer Optimizer when players join.

## Configuration Options

### AMOConfig Settings
- **Auto Fix On Play**: Automatically align when Image Target is detected
- **Image Target Name**: Name of your Vuforia Image Target
- **Anchor Root Name**: Name of the anchor root GameObject
- **Wait For All Clients**: Wait for all players to align before starting
- **Align Smoothing**: Smoothing factor for position updates

## Troubleshooting

### Common Issues

#### Objects Still Not Synchronized
1. **Check Image Target Name**: Ensure the name in AMOConfig matches your Vuforia Image Target
2. **Verify PhotonView**: Make sure AMOSessionManager has a PhotonView component
3. **Check Network Connection**: Ensure all clients are connected to the same Photon room

#### Late-Joining Players See Wrong Positions
1. **Check NetworkManager Integration**: Ensure NetworkManager calls `HandlePlayerEnteredRoom`
2. **Verify RPC Calls**: Check console for RPC synchronization messages

#### Editor vs APK Differences
1. **Check Vuforia Configuration**: Ensure Vuforia settings are identical between editor and build
2. **Verify Image Target**: Make sure the same Image Target is used in both cases
3. **Test with Same Image**: Use the exact same physical image for testing

### Debug Information
The system provides detailed debug logs:
- `[AMOSession] Syncing anchor root from remote client: (x, y, z)`
- `[AMOSession] All clients aligned. Gameplay may proceed.`

## Technical Details

### Synchronization Methods
1. **RPC Synchronization**: Initial anchor position sent via RPC
2. **Stream Synchronization**: Continuous position updates via PhotonStream
3. **Late-Joining Support**: New players receive current anchor position

### Performance Considerations
- Anchor position is only synchronized when alignment occurs
- Minimal network overhead with efficient RPC calls
- Smooth interpolation for position updates

## API Reference

### AMOSessionManager
- `IsAligned`: Returns true when the client is aligned
- `HandlePlayerEnteredRoom(Player)`: Called when a new player joins

### AMOConfig
- `imageTargetName`: Name of the Vuforia Image Target
- `anchorRootName`: Name of the anchor root GameObject
- `waitForAllClients`: Whether to wait for all clients to align
- `alignSmoothing`: Smoothing factor for position updates

## Migration from Previous Version

If you're upgrading from a previous version:

1. **Backup your project** before making changes
2. **Update AMOSessionManager**: The new version includes additional synchronization methods
3. **Update NetworkManager**: Add the AR Multiplayer Optimizer integration
4. **Test thoroughly**: Verify synchronization works in both editor and APK builds

## Support

For issues or questions:
1. Check the console for debug messages
2. Use the Setup Helper to verify configuration
3. Test with the same physical image target on all devices
4. Ensure all clients are in the same Photon room

---

**Note**: This enhanced version ensures that virtual objects appear in the same position for all players, solving the synchronization issues between Unity Editor and APK builds.
