# Project Overview: Jojo Ping Pong - Sustained Dream

## 1. Project Description
**Jojo Ping Pong - Sustained Dream** is a rhythmic combat and music education experience that blends high-octane fighting game aesthetics (inspired by the *JoJo's Bizarre Adventure* series) with music theory and piano performance. Players engage in rhythmic "Ping-Pong" duels where success is determined by the timing and harmonic complexity of their musical input. The project serves as both a stylized game and an indirect learning tool for piano chords and MIDI integration, featuring a narrative layer involving the Naples Mafia and "Sustained Dreams."

## 2. Gameplay Flow / User Loop
1.  **Boot & Scene Entry**: The game starts in the `inicio.unity` or `Menu.unity` scenes, where the player is introduced to the "Challenge" interface.
2.  **The Challenge**: A UI panel (`painelPedido`) presents a challenge. Players can accept or refuse. Accepting triggers a transition into a match.
3.  **The Duel (Core Loop)**:
    *   **Turn-Based Exchange**: The game alternates between the player's turn (`TurnoJogador1`) and the AI's turn (`TurnoJogador2`).
    *   **Rhythmic Input**: During their turn, the player must hit a moving button or play MIDI notes within a specific time window synchronized with the background music.
    *   **Harmonic Multiplier**: Playing specific chords (Major, Minor, Diminished, Augmented) on a MIDI controller or the virtual keyboard influences the "Emotion" of the match, affecting difficulty and scoring.
4.  **Progression & Technical Skill**: Successful hits increase `pontosJogador1` and `energiaAtual`. Reaching point milestones (every 15 points) unlocks new "Public" elements or narrative events.
5.  **Game Over / Victory**: If the player fails too many times (especially in "Mafia Mode"), their "records" are erased (progression reset). Success leads to the "Supreme Master" status or saving the music.

## 3. Architecture
The project follows a centralized "Manager" pattern with a focus on event-driven video and audio synchronization.

*   **Central Controller**: `PingPongVideoManual` acts as the primary orchestrator, managing game states, UI, video transitions, and MIDI logic.
*   **Audio-Visual Sync**: The system uses `UnityEngine.Video.VideoPlayer` with a double-buffering approach (Texture A/B) to ensure seamless transitions between loops and action clips.
*   **MIDI Integration**: The project utilizes multiple layers for MIDI:
    *   `MidiJack` and `Minis`: For low-level MIDI device polling and New Input System integration.
    *   `Melanchall.DryWetMidi`: For parsing and reproducing MIDI files (`.mid`).
    *   `MidiPlayer (MPTK)`: A third-party library likely used for high-fidelity SoundFont synthesis.
*   **Data Flow**: Player progress is persisted using `PlayerPrefs` (e.g., `LornaTecnica`, `Mikalle_PontoFinal_Float`). Static references (like `PianoTheoryManager.TecnicaGlobal`) allow cross-system communication between theory lessons and the main game.

## 4. Game Systems & Domain Concepts

### Rhythm Combat System
A timing-based system where the quality of a "hit" is determined by the proximity to a musical target or the expiration of a turn timer.
*   `PingPongVideoManual`: Manages the "Turn" state machine and evaluates hits (PERFECT, GREAT, GOOD, MISS).
*   `AnimationCurve`: Used for the `curvaMultiplicadorAdrenalina` to dynamically scale difficulty.
*   `Combo System`: Tracks consecutive hits to multiply score and energy gain.
*   Location: `Assets/`

### Harmonic Emotion System (Music Theory)
A system that analyzes real-time MIDI input to determine the "vibe" of the music, which in turn affects game mechanics.
*   `PianoTheoryManager`: Defines specific chords needed for lessons and tracks technical progress.
*   `PingPongVideoManual.AnalisarEmocaoDaHarmoniaInstantanea`: Analyzes active MIDI notes to detect Major, Minor, Diminished, or Augmented chords.
*   `EmocaoMusical` (Enum): Neutra, VitoriaMaior, TensaoMenor, PerigoDiminuto, MagiaAumentada.
*   Location: `Assets/`

### Video Transition Engine
A seamless video playback system designed to mimic the flow of a fast-paced fighting game using pre-recorded clips.
*   `VideoPlayer` (vp1, vp2): Used for crossfading between a standby video and an active action video.
*   `PingPongVideoSystem`: A simplified version of the logic for basic challenge-response loops.
*   Location: `Assets/`

### MIDI & Synthesizer System
Handles the connection, playback, and visualization of musical data.
*   `PonteMidiMPTK`: Likely acts as a bridge between the core game logic and the MIDI Player ToolKit.
*   `WhisperTranscriber`: Suggests experimental voice-to-text or transcription features.
*   `MidiMaster` / `MidiDriver`: Core MidiJack components for hardware interfacing.
*   Location: `Assets/MidiJack/`, `Assets/MidiPlayer/`

## 5. Scene Overview
*   **gAME1.unity / GamePiano.unity**: The primary gameplay scenes where the "Ping-Pong" duel occurs.
*   **Menu.unity / inicio.unity**: Entry points for mode selection and introductory lore.
*   **gamepianin.unity**: A specialized scene likely focused on the `PianoTheoryManager` lessons.
*   **_Recovery/**: Contains multiple backup scenes (`0 (1).unity`, etc.), indicating an iterative or recovery-based development process.

## 6. UI System
The project primarily uses **UGUI** with a heavy emphasis on dynamic feedback and "Fighter" style overlays.
*   **Components**: Uses `TextMesh Pro` for high-quality text and standard `Image`/`Button` components.
*   **Dynamic UI**: 
    *   `GerarTextoFlutuante`: Spawns "HIT", "PERFECT", or "MISS" text in world space.
    *   `EfeitoStandAura`: Visual overlays that pulse when the player reaches high energy levels.
    *   `TecladoVisualProcedural`: Generates an 88-key piano interface at runtime for touch or visual feedback.
*   **Binding**: Logic is bound via traditional `UnityEvents` and direct references in `PingPongVideoManual`.

## 7. Asset & Data Model
*   **ScriptableObjects**: Not heavily utilized for core data; the project favors `MonoBehaviour` configurations and `PlayerPrefs`.
*   **Video Assets**: High volume of `.mp4`, `.mov`, and `.avi` files located in the root `Assets/` folder, categorized into "Jogador1", "Jogador2", "Intro", and "Climax".
*   **Audio Assets**: Categorized into `Notes/` (individual WAVs for piano keys) and root `Assets/` for stingers and background themes.
*   **MIDI Data**: `.mid` files stored in `StreamingAssets` or imported at runtime via `SimpleFileBrowser`.
*   **Naming Conventions**: Mixed (Portuguese and English), e.g., `botaoAceitar` vs `jogador1Clipes`.

## 8. Notes, Caveats & Gotchas
*   **Orientation Sensitivity**: The `PingPongVideoManual` has built-in logic to detect Portrait vs. Landscape mode (`isPortrait`) and disables certain UI elements (like the 88-key keyboard) to fit the screen.
*   **Easter Egg - Sustained Dream**: Playing the `E Major` chord can trigger a "Musical Blackout" easter egg (`AtivarApagãoMusicalEasterEgg`), which changes the game's visual state and requires a specific chord sequence (`C#m -> F# Maj -> Bm`) to restore.
*   **Mafia Mode Risk**: In the Naples/Mafia scenario, losing the match resets the player's technical progress (`registrosMusicaisRestantes`), effectively acting as a "Hardcore" mode.
*   **Input Latency**: The `compensacaoAudioVisual` variable in MIDI settings is critical for syncing external MIDI hardware with the Unity `VideoPlayer` latency.
*   **Dependency Warning**: The project relies on `MidiJack` and `Minis`, which may require specific backend configurations in the Input System package settings to recognize hardware.