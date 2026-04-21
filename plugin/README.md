# 🚀 Figma Importer v2.5.4 (UPM)

[![Unity](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/) 
[![Figma](https://img.shields.io/badge/Figma-API%20v1-orange.svg)](https://www.figma.com/developers/api)

Мощный инструмент для профессионального импорта UI из Figma в Unity с поддержкой **Smart Sync**, автоматизацией префабов и неразрушающим обновлением.

### New in v2.5.4 (Quality & Control Update)
- **Log Levels**: Control console output (Silent, Minimal, Verbose).
- **Image Export Scale**: Adjustable scale factor (0.5x - 4x) for high-fidelity assets.
- **Session Security**: Personal Access Tokens (PAT) are now stored only in memory (`SessionState`).
- **Build Tagging**: Window footer displays build date for easier troubleshooting.

---

## 📦 Installation (UPM)

This package is designed for the **Unity Package Manager (UPM)**.

### Option A: Via Git URL (Recommended)
1. Open your Unity project.
2. Go to `Window -> Package Manager`.
3. Click `+` -> **"Add package from git URL..."**.
4. Paste: `https://github.com/Menta1ik/figma-to-unity.git?path=plugin#v2.5.4`

### Option B: Local Disk
Select the `package.json` file inside the `plugin` folder.

---

## 🛠 Features

- **Smart Sync**: Updates only what changed. Manually added components or modified names in Unity are preserved.
- **Auto Layout**: Maps Figma's Auto Layout directly to Unity `Vertical/HorizontalLayoutGroup`.
- **9-Slice Support**: Automatically detects and applies 9-slice borders based on naming conventions.
- **Constraints Translation**: (Beta) Maps Figma constraints to Unity anchors and offsets for responsive design.
- **Stencil Guard**: Automatically prevents "depth > 8" errors by flattening hierarchy or switching to RectMask2D.

---

## 📖 Documentation

Check the `plugin/docs` folder for detailed guides:
- [Developer Manual (Russian)](docs/Developer/DEVELOPER_MANUAL.md)
- [Architecture Overview](docs/Developer/DEV_CONCEPTS.md)

---
**BrainySoftware OU © 2026**
