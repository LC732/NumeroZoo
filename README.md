# NumeroZoo

**NumeroZoo** is an open-source educational serious game designed to help children develop early numeracy skills (numbers $0$ to $10$) through embodied interaction and real-time computer vision. Using a standard webcam, the software counts raised fingers across both hands and projects the child's segmented silhouette into interactive 3D ecosystems populated by $N$ animals corresponding to the detected finger count.

## Authors

* **Luan Melo** — *Instituto Federal Goiano, Iporá, GO, Brazil*

* **Thamer Horbylon Nascimento** — *Instituto Federal Goiano, Iporá, GO, Brazil; Instituto de Informática, Universidade Federal de Goiás, Goiânia, GO, Brazil*

## Key Features

* **Embodied 0–10 Finger Counting:** Real-time two-hand landmark tracking (`hand_landmarker.task`) combined with a $500\text{ ms}$ temporal stabilization filter to prevent visual flickering during hand transitions.

* **Live Silhouette Integration:** Real-time person segmentation (`selfie_segmenter.tflite`) embeds the child's cutout directly inside the virtual habitat, reinforcing self-presence and number-quantity association.

* **Three Interactive Biomes:**

  * **Aquário (`Aquario.unity`):** Marine reef environment with animated clownfish.

  * **Jardim (`Jardin.unity`):** Flower garden environment with animated bees.

  * **Cerrado (`Cerrado.unity`):** Brazilian savanna environment with animated parrots.

* **Privacy-First Local Processing:** Includes an explicit camera consent dialog at startup; all video frames are processed strictly on `127.0.0.1` and are never stored or transmitted externally.

* **Keyboard Fallback Mode:** Full support for keys `0`–`9` when running or reviewing the software without a webcam.

## System Architecture

NumeroZoo decouples computer vision inference from 3D rendering using two local processes communicating over TCP (`127.0.0.1:5555`):

1. **Python Vision Server (`PythonServer/Server.py`):** Captures `640x480` webcam frames via OpenCV, executes MediaPipe Tasks for hand landmark detection and person segmentation, and streams binary packets consisting of a 16-byte little-endian header (`width`, `height`, `payload_bytes`, `stable_gesture`) followed by raw `RGBA` pixel bytes (where the alpha channel encodes the silhouette mask).

2. **Unity 6 Client (`Assets/_Project/`):** Built on the Universal Render Pipeline (URP). `ServidorPython.cs` manages the lifecycle of the vision process (launching pre-packaged standalone binaries or connecting to an external Python instance), reconstructs the silhouette texture in real time, plays spoken number narration (`0`–`10`), and dynamically spawns or despawns 3D creatures.

## Repository Structure

```
NumeroZoo/
├── Assets/
│   ├── _Project/
│   │   ├── Art/                 # 3D models, materials, custom shaders, particles, terrains
│   │   ├── Audio/               # Spoken number narration (0-10) and background music
│   │   ├── Editor/              # Menu generator and post-build platform cleanup tools
│   │   ├── Resources/           # Runtime settings UI prefab (MenuConfiguracoes)
│   │   ├── Scenes/              # MenuPrincipal, Aquario, Jardin, and Cerrado scenes
│   │   ├── Scripts/             # C# scripts organized into Core, Creatures, Audio, UI, Vision
│   │   └── Settings/            # URP assets and Input System actions
│   ├── StreamingAssets/         # Target directory for optional PyInstaller standalone builds
│   ├── TextMesh Pro/            # LiberationSans (SIL OFL) font and TMP shaders
│   └── ThirdParty/              # External free environment textures, skyboxes, and plugins
├── Packages/                    # Unity Package Manager manifest and lockfile
├── ProjectSettings/             # Unity 6 project configuration
├── PythonServer/
│   ├── models/                  # MediaPipe hand_landmarker.task and selfie_segmenter.tflite
│   ├── requirements.txt         # Python dependencies
│   └── Server.py                # MediaPipe TCP vision server source code
├── CITATION.cff                 # Academic citation metadata
├── LICENSE                      # MIT License
└── README.md                    # Project documentation

```

## Requirements

* **Unity Editor:** `6000.4.5f1` (Unity 6) with Universal Render Pipeline (URP) support.

* **Python:** Python `3.10+` (for running the vision server from source or packaging standalone executables).

* **Hardware:** Standard USB or integrated webcam (optional; keyboard fallback mode is available).

* **Operating Systems:** Linux (tested on Arch/CachyOS `x86_64`), Windows 10/11, and macOS.

## Installation and Quick Start

Clone the repository:

```
git clone https://github.com/SEU_USUARIO/NumeroZoo.git
cd NumeroZoo

```

### Option A: Running from Source (Recommended for Reviewers & Developers)

1. Set up a Python virtual environment and start the vision server:

   ```
   cd PythonServer
   python -m venv .venv
   source .venv/bin/activate        # On Windows: .venv\Scripts\activate
   pip install -r requirements.txt
   python Server.py --port 5555 --camera 0 --verbose
   
   ```

2. Open the root `NumeroZoo` folder in **Unity Hub** using Unity `6000.4.5f1`.

3. Open `Assets/_Project/Scenes/MenuPrincipal.unity`.

4. Select the `ServidorPython` GameObject in the Hierarchy and **uncheck** `Iniciar No Editor` (since `Server.py` is already running in your terminal).

5. Press **Play** in the Unity Editor, click **Permitir** on the camera prompt, and select **Aquário**, **Jardim**, or **Cerrado**.

### Option B: Testing Without a Webcam (Keyboard Mode)

1. Open `Assets/_Project/Scenes/MenuPrincipal.unity` in Unity `6000.4.5f1` and press **Play**.

2. Click **Agora não** on the camera consent dialog.

3. Choose any of the three biomes and press keys **`0`** through **`9`** to change the number of active animals and trigger audio narration.

4. Press **`Esc`** (or click the top-left menu button) at any time to adjust volume, resolution, quality, fullscreen, or return to the main menu.

### Option C: Packaging the Standalone Vision Executable (Optional)

To run the Unity project or standalone builds without requiring a system Python installation, package `Server.py` using PyInstaller and place the generated folder inside `Assets/StreamingAssets/ServidorMaos/<Platform>/`:

```
cd PythonServer
pip install pyinstaller
pyinstaller --noconfirm --onedir --name servidor_maos \
  --add-data "models/hand_landmarker.task:." \
  --add-data "models/selfie_segmenter.tflite:." \
  Server.py

```

Copy the contents of `dist/servidor_maos/` into the corresponding platform folder:

* **Linux:** `Assets/StreamingAssets/ServidorMaos/Linux/`

* **Windows:** `Assets/StreamingAssets/ServidorMaos/Windows/`

* **macOS:** `Assets/StreamingAssets/ServidorMaos/Mac/`

When building a standalone player in Unity, `LimparServidorNoBuild.cs` automatically removes binaries targeting other operating systems so the final build stays lightweight.

## Controls Summary

| Input | Action | 
| ----- | ----- | 
| **0–10 Raised Fingers** | Spawns $0$ to $10$ animals in the active biome (webcam mode) | 
| **Keys `0`–`9`** | Manually sets the animal count (keyboard fallback mode) | 
| **`Esc` / Top-Left Button** | Opens/closes the audio, video, and navigation settings panel | 

## License

This project is licensed under the **MIT License** — see the [LICENSE](LICENSE) file for details.

## Citation

If you use **NumeroZoo** in academic research or educational practice, please cite:

> L. Melo and T. H. Nascimento, "NumeroZoo: An Embodied Numeracy Learning Game Powered by Real-Time Computer Vision," *GitHub repository*, v1.0.0, 2026. Available: https://github.com/LC732/NumeroZoo