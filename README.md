# UniSonic
A simple implementation of Sonic the Hedgehog's physics and movement controls in Unity. Done as a learning project a while back, opened up the repo so anyone interested could check it out.  
  
Based on the excellent information found at http://info.sonicretro.org/Sonic_Physics_Guide  
  
The implementation is followed fairly closely, though using raycasts instead of direct reading of tile data. This makes it more flexible, but does mean that tile-specific colliders need to be created for any new tiles that are added.

![SonicGif](https://thumbs.gfycat.com/WhiteSkinnyGonolek-size_restricted.gif "A short GIF demonstrating some of the features.")  
_In the GIF above, the lines sticking out of Sonic visualize the collision raycasts._

## Controls
Movement is done with WASD, Space to jump. Pressing down while moving to roll is supported (and has the correct movement-altering behavior), but crouching and spin-dashing is not implemented.

### Implemented
- General ground and air movement
- Jumping
- Underwater ground/air movement
- Slops, running up and attaching to walls/ceilings
- Rolling
- Animation (done via Mecanim states)

### Missing Features
- Damage
- Looking up
- Crouching
- Spin-dashing
- Pushing
- 'About-to-fall' animation when standing near ledges

#### Notes
- Sonic Sprites used in this are from Sonic the Hedgehog 3 and belong to Sega.
  - New sprites should be relatively easy to put in if desired, but Unity animates sprite swaps via direct references to the assets. To fully swap out all the animations, all the animation clips would need to be updated.
- Pixels-per-unit setting of 1 was used to make it easier to map values from the guide to Unity units. Newly-added sprites should have the same setting, so pixels are consistently the same size.
  - Also, if mixing regular Box2D physical objects in, note that they will likely behave strangely due to the funky scale.
- As this is based on the original implementation of Sonic's physics from the Sega Genesis games, it does also include some of the same bugs. For instance, it is impossible to stand on the center of very thin platforms.
