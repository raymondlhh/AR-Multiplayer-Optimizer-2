# AR Multiplayer Optimizer - Setup Guide

## 🚀 **AUTOMATIC SETUP (Recommended)**

### **Option 1: Fully Automatic (Zero Manual Setup)**
1. **Add AMOAutoBoot to any GameObject in your scene**
   - Create an empty GameObject (or use existing one)
   - Add the `AMOAutoBoot` script
   - **That's it!** Everything else is automatic

### **Option 2: Use Setup Helper**
1. Go to `AR Multiplayer Optimizer > Setup Helper` in Unity menu
2. Click "Create AMOConfig" 
3. Click "Setup AMOSessionManager"
4. Click "Test Configuration"

---

## 📋 **What's AUTOMATIC vs MANUAL**

### ✅ **AUTOMATIC (No Setup Required)**
- **AnchorRoot Creation**: Automatically creates/finds the anchor root GameObject
- **AMOAnchorTracker**: Automatically creates and configures the tracker component
- **Vuforia Integration**: Automatically detects and tracks ImageTargets
- **Position Synchronization**: Automatically syncs anchor position across all clients
- **Late-Joining Support**: Automatically sends anchor position to new players
- **PhotonView Setup**: Automatically configures Photon networking

### ⚠️ **MANUAL (One-Time Setup)**
- **AMOConfig Assignment**: Assign the AMOConfig asset in Inspector (or use Setup Helper)
- **Image Target Name**: Set the correct Vuforia ImageTarget name in AMOConfig

---

## 🔧 **Quick Setup Steps**

### **For New Projects:**
1. **Add AMOAutoBoot**: Attach `AMOAutoBoot` script to any GameObject
2. **Done!** Everything else is automatic

### **For Existing Projects:**
1. **Use Setup Helper**: `AR Multiplayer Optimizer > Setup Helper`
2. **Create AMOConfig**: Click "Create AMOConfig" button
3. **Setup AMOSessionManager**: Click "Setup AMOSessionManager" button
4. **Test**: Click "Test Configuration" to verify

---

## 🎯 **Configuration Options**

### **AMOConfig Settings (Optional)**
- **Image Target Name**: Name of your Vuforia ImageTarget (default: "ARMascot")
- **Anchor Root Name**: Name for the anchor GameObject (default: "AnchorRoot")
- **Wait For All Clients**: Wait for all players to align (default: true)
- **Auto Fix On Play**: Automatically align when target detected (default: true)

### **Default Values (Works Out of the Box)**
```csharp
imageTargetName = "ARMascot"     // Your Vuforia ImageTarget name
anchorRootName = "AnchorRoot"     // Anchor root GameObject name
waitForAllClients = true          // Wait for all players
autoFixOnPlay = true             // Auto-align on target detection
alignSmoothing = 0.2f            // Position smoothing
```

---

## 🐛 **Troubleshooting**

### **"Missing AMOConfig" Warning**
- **Solution**: Use Setup Helper → "Create AMOConfig"
- **Or**: Manually create AMOConfig asset in Resources folder

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
- [ ] (Optional) Use Setup Helper to create AMOConfig
- [ ] (Optional) Set correct Image Target name in AMOConfig
- [ ] Test with multiple devices using same image target
- [ ] Verify objects appear in same position on all devices

**That's it! The system is designed to be as automatic as possible.**
