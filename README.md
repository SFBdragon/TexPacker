# TexPacker

A small tool for maxrects packing of atlases.  
It's dead simple, quite fast, and command line-operated for creating and managing packaging configurations for your content folders.

WARNING: This is not actively maintained or developed currently, this is an old personal project.

Package outputs are as follows in {Config:Output):
- {Config:Name}.json  
  Stores an array of tuples with four fields representing the original images:
   - Item1 (string): Filepath from the source directory containing it, for identification.
   - Item2 (int): Atlas Id.
   - Item3 (bool): Whether the image has been rotated 90 degrees clockwise.
   - Item4 (struct): A rectangle with the fields "x", "y", "w", "h"
- {Config:Name}[Atlas Id].png  
  Series of atlas files indexed by name.  
  Amount of atlases as required per the specified Config:width and Config:height.  

There are no restrictions as to how you use this software commercially or otherwise, and I do not own the output of this software.  
