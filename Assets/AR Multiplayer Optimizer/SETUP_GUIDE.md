# AR Multiplayer Optimizer - Setup Guide

## 🚀 **AUTOMATIC SETUP**

### **Simple Setup (Zero Manual Configuration)**
1. **Add AMOAutoBoot to any GameObject in your scene**
   - Create an empty GameObject (or use existing one)
   - Add the `AMOAutoBoot` script
   - **That's it!** Everything else is automatic

---

## 📋 **What's AUTOMATIC**

### ✅ **AUTOMATIC (No Setup Required)**
- **AnchorRoot Creation**: Automatically creates/finds the anchor root GameObject
- **AMOAnchorTracker**: Automatically creates and configures the tracker component
- **Vuforia Integration**: Automatically detects and tracks ImageTargets
- **Position Synchronization**: Automatically syncs anchor position across all clients
- **Late-Joining Support**: Automatically sends anchor position to new players
- **PhotonView Setup**: Automatically configures Photon networking
- **AMOConfig Creation**: Automatically creates configuration asset with sensible defaults

---

## 🔧 **Quick Setup Steps**

### **For All Projects:**
1. **Add AMOAutoBoot**: Attach `AMOAutoBoot` script to any GameObject
2. **Done!** Everything else is automatic

---

## 🎯 **Configuration Options**

### **AMOConfig Settings (Auto-Generated)**
- **Image Target Name**: Name of your Vuforia ImageTarget (default: "ARMascot")
- **Anchor Root Name**: Name for the anchor GameObject (default: "AnchorRoot")
- **Wait For All Clients**: Wait for all players to align (default: true)
- **Auto Fix On Play**: Automatically align when target detected (default: true)

### **Debug Visualization Settings**
- **Show Anchor Center**: Display anchor center point visualization (default: true)
- **Anchor Center Size**: Size of the visualization (range: 0.1 - 2.0)
- **Anchor Center Color**: Color of the visualization (default: red)

### **Position Stabilization Settings**
- **Enable Position Stabilization**: Prevent object drift when phone moves (default: true)
- **Update Rate**: How often to update anchor position (range: 0.01 - 1.0 seconds)
- **Smoothing Factor**: Smoothness of position updates (range: 0.1 - 10.0)
- **Max Drift Distance**: Maximum distance before forcing snap (range: 0.1 - 2.0 meters)

### **Default Values (Works Out of the Box)**
```csharp
imageTargetName = "ARMascot"     // Your Vuforia ImageTarget name
anchorRootName = "AnchorRoot"     // Anchor root GameObject name
waitForAllClients = true          // Wait for all players
autoFixOnPlay = true             // Auto-align on target detection
alignSmoothing = 0.2f            // Position smoothing
showAnchorCenter = true          // Show anchor visualization
anchorCenterSize = 0.5f          // Visualization size
anchorCenterColor = Color.red    // Visualization color
enablePositionStabilization = true  // Prevent drift when phone moves
stabilizationUpdateRate = 0.1f   // Update every 0.1 seconds
stabilizationSmoothing = 2.0f    // Smooth interpolation
maxAnchorDrift = 0.5f            // Max drift before snap
```

---

## 🐛 **Troubleshooting**

### **"Missing AMOConfig" Warning**
- **Solution**: This is automatically resolved - AMOConfig is created automatically
- **No Action Required**: The system handles this automatically

### **"Anchor Root" Field Shows "None"**
- **This is Normal**: The system automatically creates/finds the AnchorRoot at runtime
- **No Action Required**: The field will be populated automatically

### **"Anchor Tracker" Field Shows "None"**
- **This is Normal**: The system automatically creates the AMOAnchorTracker component
- **No Action Required**: The field will be populated automatically

### **Objects Still Not Synchronized**
1. **Check Image Target Name**: Ensure it matches your Vuforia ImageTarget
2. **Verify Photon Connection**: All clients must be in the same room
3. **Test with Same Image**: Use the exact same physical image on all devices
4. **Use Anchor Visualization**: Enable anchor center visualization to see the synchronization point

### **Anchor Visualization Not Showing**
1. **Check Toggle**: Ensure "Show Anchor Center" is enabled in Setup Helper
2. **Verify Alignment**: Visualization only appears after Image Target is detected
3. **Check Size**: Increase anchor center size if visualization is too small
4. **Test Colors**: Try different colors to make visualization more visible

### **Objects Drifting When Phone Moves**
1. **Enable Stabilization**: Ensure "Enable Position Stabilization" is turned on
2. **Check Update Rate**: Lower values (0.01-0.05s) provide more responsive tracking
3. **Adjust Smoothing**: Higher values (3-5) provide smoother movement but slower response
4. **Monitor Max Drift**: Increase if objects snap too frequently
5. **Verify Image Target**: Ensure Image Target is clearly visible and well-lit

---

## 📱 **Testing**

### **Local Testing**
1. Build and run APK on one device
2. Run Unity Editor with same scene
3. Both should show objects in same position

### **Multi-Device Testing**
1. Build APK and install on multiple devices
2. Run app on all devices
3. Point cameras at the same image target
4. Objects should appear in identical positions

---

## 🎉 **Success Indicators**

### **Console Messages (Look for these)**
```
[AMOAutoBoot] [AUTOMATIC] Creating AMOSessionManager...
[AMOSession] [AUTOMATIC] Created/Found AnchorRoot: AnchorRoot
[AMOSession] [AUTOMATIC] Created AMOAnchorTracker component
[AMOSession] [AUTOMATIC] Initialized AMOAnchorTracker
[AMOSession] Syncing anchor root from remote client: (x, y, z)
[AMOSession] All clients aligned. Gameplay may proceed.
```

### **Inspector Fields (Should show)**
- **AMOSessionManager**: Config assigned, Anchor Root and Anchor Tracker auto-populated
- **PhotonView**: AMOSessionManager in Observed Components

---

## 🚀 **Quick Start Checklist**

- [ ] Add `AMOAutoBoot` script to any GameObject
- [ ] Test with multiple devices using same image target
- [ ] Verify objects appear in same position on all devices

**That's it! The system is designed to be completely automatic.**
