# Shared Game Audio - Unity

Extract this package into the Unity project root. The audio files will be placed under:

`Assets/SharedGameAudio/`

## Package Contents

- 26 uniquely named WAV/OGG files grouped by gameplay purpose.
- One unique Unity `.meta` file per audio asset.
- `audio_roles.csv`: maps all 31 Unity audio roles to package paths.
- `audio_files.csv`: maps renamed files to their original source assets and SHA-256 hashes.

## Runtime Notes

- `BGM/BGM_Main_Loop_Arcane_Duel.ogg` is the looping background track.
- The current Unity mix uses music volume `0.18` and master volume `0.85` (effective source volume `0.153`).
- Collision and upgrade effects increase volume and playback rate as player speed increases. That behavior is implemented in code and is not baked into these files.
- Filenames containing `__` indicate one unique audio file shared by multiple roles.

## Licensing

The audio originated from third-party asset packs. No license or README files were present in the imported source folders when this package was built. Verify the original asset-store licenses before redistribution outside the development team.
