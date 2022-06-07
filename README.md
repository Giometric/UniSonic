# UniSonic
A simple implementation of Sonic the Hedgehog's physics and movement controls in Unity. Done as a learning project a while back, opened up the repo so anyone interested could check it out.
  
Based on the excellent information found at http://info.sonicretro.org/Sonic_Physics_Guide  
  
The implementation is followed fairly closely, though using raycasts instead of direct reading of tile data. This makes it more flexible, but does mean that tile-specific colliders need to be created for any new tiles that are added. Using tile data directly may be added as an extra feature later, but would still rely on raycasting to find which tiles are nearby.

One consequence of using plain 2d raycasts is that any kind of collider Unity supports will work. In the demo level I use a few tilemaps for most of the environment layers, and a couple of Sprite Shape objects as well. The layer switch triggers are BoxCollider2D objects.

[![SonicGif](https://thumbs.gfycat.com/BonyGrandioseEquestrian-size_restricted.gif "A short GIF demonstrating some of the features.")](https://gfycat.com/bonygrandioseequestrian)

## Controls
Movement is done with *WASD*, *Space* to jump. Pressing down while moving to roll is supported (and has the correct movement-altering behavior), but crouching and spin-dashing are not implemented yet.
*Tab* opens and closes the debug UI on the top-left of the screen. *R* will reset Sonic to his start location, and holding *Left-Shift* will active a super-acceleration mode.

All input is done via Unity's old InputManager, and the mappings can be edited from there (Project Settings > Input Manager).

### Implemented
- General ground and air movement
- Jumping
- Underwater ground/air movement
- Slopes, running up and attaching to walls/ceilings
- Rolling
- Animation (done via Mecanim states)
- One-way platforms
- Loops and collision layer switching
  -  This has some minor issues, mostly requiring some care when placing the switch triggers
- Smooth rotation for Sonic, like what the Sonic Advance games do, is supported and can be enabled via a checkbox on the Sonic prefab; turned off by default

### Missing Features
- Damage, enemies
- Other interactable things like springs
- Looking up
- Crouching
- Spin-dashing
- Pushing
- 'About-to-fall' animation when standing near ledges

#### Notes
- Sonic the Hedgehog belongs to Sega.
- Sonic Sprites by Shinbs, with some additional credits; obtained from spriters-resource.com: https://www.spriters-resource.com/custom_edited/sonicthehedgehogcustoms/sheet/111542/. Previously was using ripped Sonic 3 sprites, but these look great and won't get anyone in trouble for copyright :)
  - New sprites should be relatively easy to put in if desired, but Unity animates sprite swaps via direct references to the assets. To fully swap out all the animations, all the animation clips would need to be updated (or new ones created).
- Pixels-per-unit setting of 1 was used to make it easier to map values from the guide to Unity units. Newly-added sprites should have the same setting, so pixels are consistently the same size.
- Project uses Unity 2021.2.18. Update to 2021.3.4 (the 2021 LTS) coming soon if there are no issues.
- SpriteShape and Tilemap are used in the test level, but aren't necessary for this implementation to work. Collisions work off of Raycast2D so any type of collision will work.
  - If mixing rigidbody-simulated objects in, note that they will likely behave strangely due to the funky scale.
- As this is based on the original implementation of Sonic's physics from the Sega Genesis games, it does also include some of the same bugs. For instance, it is impossible to stand on the center of very thin platforms.
- The project folder contains a handful of unfinished bits of code or not-yet-used assets. If something looks like it's not getting used, there's a good chance it isn't just yet.