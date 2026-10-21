# Collision detection in a spherical geometry
This repository contains the source code used in a Master's thesis on optimizing collision detection for a spherical geometry. The runtime environment was created in Unity and the relevant source code is found in [Assets/scripts/](Assets/scripts/).

- Baseline bounding volume implementation: [Assets/scripts/SphericalBoundingVolume.cs](Assets/scripts/SphericalBoundingVolume.cs)
- Euclidean bounding volume implementation: [Assets/scripts/bbox.cs](Assets/scripts/bbox.cs)
- First artifact bounding volume implementation: [Assets/scripts/SphericalBoundingVolumeArtifact1.cs](Assets/scripts/SphericalBoundingVolumeArtifact1.cs)
- Second artifact bounding volume implementation: [Assets/scripts/SphericalBoundingVolumeArtifact2.cs](Assets/scripts/SphericalBoundingVolumeArtifact2.cs)
- AABB implementation [Assets/scripts/aabb.cs](Assets/scripts/aabb.cs)
- Octree implementation [Assets/scripts/octree.cs](Assets/scripts/octree.cs)
- Spherical AABB implementation [Assets/scripts/SphericalAABB.cs](Assets/scripts/SphericalAABB.cs)
- Spherical Octree implementation [Assets/scripts/SphericalOctree.cs](Assets/scripts/SphericalOctree.cs)

The simulation sequences used in the Master's thesis along with the raw runtime data, accuracy data and the spreadsheet can be found in [experiments/](experiments/)
