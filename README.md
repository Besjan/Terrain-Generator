[![License: MIT](https://img.shields.io/badge/License-MIT-greed.svg)](LICENSE)

## Features
- Heightmap:
    - Generate Unity terrain data from point cloud data.
    - Create custom Heightmap to support terrain data on different Unity versions.
    - Stitch terrain tiles.
    - Setup multiple terrain tiles in one terrain group from available terrain data.

- Texturing:
    - Convert satellite images from .ecw usind GDAL.
    - Combine images to match heightmap resolution.
    - Process images with ImageMagick: resize, saturate...
    
- Shading (automate workflow with MicroSplat):
    - Apply available MicroSplat material to multiple terrain tiles.
    - Apply tint map textures to MicroSplat Global Texturing module to multiple terrain tiles.
    - Apply biome mask textures to MicroSplat Procedural Texturing module to multiple terrain tiles.

----

## Dependencies
- [Odin - Inspector and Serializer](https://assetstore.unity.com/packages/tools/utilities/odin-inspector-and-serializer-89041)
- [Unity Terrain Tools](https://docs.unity3d.com/Packages/com.unity.terrain-tools@3.0/manual/index.html)
- [ProBuilder](https://docs.unity3d.com/Packages/com.unity.probuilder@4.3/manual/index.html)
- [OSGeo4W](https://trac.osgeo.org/osgeo4w/)
- [ImageMagick](https://www.imagemagick.org/)
- [MicroSplat - Global Texturing](https://assetstore.unity.com/packages/tools/terrain/microsplat-global-texturing-96482)
- [MessagePack](https://github.com/neuecc/MessagePack-CSharp)
