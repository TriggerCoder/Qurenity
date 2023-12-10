PORTED TO GODOT:

This Project as been Archive and all worked has been ported to Godot, check out:
https://github.com/TriggerCoder/Quadot-Arena


WHY PORTED?

Because of the Unity Fiasco back in September 2022
https://web.archive.org/web/20230912151806/https://unity.com/pricing-updates
Unity can't be trusted any longer, because in the future they could pull something like this again.
This was the wake-up call



ORIGINAL README
//----------------------------------------------
WHAT IS QURENITY?

QURENITY is the QUAKE 3 ARENA module of the "United Generations" (Behemoth) Project, that aims to have all the Classic ID games (Wolf3d, DOOM, Quake1 and Quake3), playable under the Unity Engine in the spirit of the late Quake2 Generations (https://web.archive.org/web/20020306115539/http://www.planetquake.com/generations/) 

 

WHY? THERE ARE A LOT OF OTHER SOURCEPORT DEVELOPED

QURENITY is not a source port, It's done in Unity where it's read all the bsp map data, and then process it along with the model, which allows to get the geometric data of the maps, to have a polygonal representation which Unity can work with, then the gameplay elements are added on top of it to have Quake3 Arena "feel" gameplay.

 

HOW DOES IT ACHIEVE QUAKE3 ARENA GAMEPLAY?

At startup it load the default pk3 (doesn't have a pk3 selector yet), the it cache the textures, images, sounds and models, then it start reading the corresponding map information (lump entities, brushes, leaf, surfaces, etc) then it start building and populating the map dynamically, basically all of Quake3 Arena gameplay information/elements were taken from https://quake.fandom.com
 

WHY THE UNITY ENGINE?

Because of the editor and the support it has. Having an editor run at the same time as the gameplay is priceless, you can make changes to the map, add/remove stuff, check the drawcalls, etc, and not to mention for the debugging information, really priceless. 

One common problem/limitation that all source ports have is that they need to upgrade the rendering engine in order to take advantage of any new API (Vulkan, Direct3d, OpenGL, etc) or to add new visual enhancements. When developing for Unity, you don't need to worry about this (probably in the future will have to worry about Mono vs DOTS), also to have a common engine allows for a really easy integration between all these games, also using Unity allows us to port it to different platforms (including Web-GL)


TECHNICAL STUFF:

BEHIND THE CODE

Rendering and Collision

Getting everything loaded for a map had some limitations, we got the same problem that Carmack got when porting Wolf3d for the SNES, the hardware began to struggle. As everything was loaded on a map and having only Frustum Culling (even though Unity offers Occlusion Culling, it must be used for static items, as it need to bake the map in order to get the data to know what to occlude, however as everything is loaded dynamically we cannot baked it) we had a lot of overdrawn getting even more than 8000 draw calls in really populated maps. Quake3 solved this with Binary Space Partition, however as our approach was different as we were working with meshes we wanted to move forward to a more modern approach, here is a good read (http://www.robotrenegade.com/articles/id-tech-3-optimization/vis.html) so basically every surfaces has brushes and leaf which needed to be broken down to convex clusters (BSP) to be culled with a Potentially Visible Set (PVS). We then take all the static geometric data and start intersecting the planes in order to get the vertexes that are going to be use to get a convex hull (done with Iterative algorithm which is very similar to Quickhull. Thanks HABRADOR) A visualization of this algorithm can be found here: https://www.youtube.com/watch?v=Yv2OhCV1BpU
 

AI

Bots are being made with Behavior trees and A* was chosen as the pathfinding algorithm, and has proven to be fast an efficient.

External Resources

Qurenity can load external resources (HD textures, etc), the fastest way is to have everything loaded as resource in Unity and then it's packed and ready for use, however that present a huge limitation when trying to use other textures, graphics, whatsoever, so it was design to load everything externally then search for internal data and finally inside the WAD. It takes some time to load all the data when the program is launched as it need to convert the png images "friendly" to Unity (DXT1 or DXT5) 

END GOAL FOR QURENITY

The rendering code was design for a 4 player spit screen, so that would be finished as soon as we get ASAP, Online multiplayer will be implemented also to work between platforms (PC, Android, etc)

REMEMBER THIS IS STILL A WORK IN PROGRESS

Feel free to contact me by discord: -Carnage-#1309