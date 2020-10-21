# Built2Spec HoloLens App

The ERC Horizon 2020 Built2Spec project sought to reduce the gap between a building’s designed and as-built energy performance.
The Built2Spec HoloLens App allows to compare the actual surroundings and with reference and shows the differences.
This, for instance, enables to inspect the construction progress of a building with respect to a CAD model.

## Short user documentation

### Toolbar functions
**Save**  
Opens a file picker to save the current spatial mesh.

**Load**  
Opens a file picker to load previously saved geometry.

**Model Overlay**  
Opens the model overlay mode, which shows the current spatial mapping and/or the loaded model. This is the default mode at startup.

**Manipulate**  
Starts the manipulation mode, which allows to align the loaded model to the real environment.

**Difference Reasoning**  
Starts the difference reasoning mode, which will show new geometry in blue and missing geometry in red.

**Toggle UI lock**  
Changes whether the UI follows the field of view or is fixed in space.

**About**  
Shows information about the app.

### Usage Example

We present the simple usage example of recording a room's geometry and later comparing it to the same room at a different time:

1. When opening the Built2Spec app you are in model overly mode. This mode shows the current spatial mapping and/or the loaded model. Change the view to either "Model and Spatial Mapping" (default) or "Spatial Mapping only" to see the current spatial mapping.

2. Tap "Save" to save the current spatial mapping and give the file a name. The current environment is now saved and can be loaded at a later time.

3. Close the app and turn off the HoloLens, simulate change in the room by e.g. moving a chair.

4. Start up the Built2Spec app again and tap "Load". Then select your previously saved spatial mapping. Change the view to either "Model and Spatial Mapping" (default) or "Model only" to see the loaded model.

5. Tap "Manipulate" to enter the manipulation mode in order to align the loaded model to the real environment. This will bring up a mini map of the loaded model, which you can use to adjust it.

6. Pinch the mini map with two hands to rotate or scale it. Scaling is off by default, because the loaded model is usually already correctly scaled. Models previously saved by the HoloLens have the correct scaling. Scaling can be turned on with the first button in the manipulation mode tools.  
Pinch the mini map with one hand to move it. The full size model will follow your movements. Use the second button of the manipulation mode tools to turn the movement off if you need to freely reposition the mini map without moving the model. The "Movement to scale" setting scales the movement by the mini map/model ratio and can be used to quickly move the model by a large distance. This is only needed for very large models.

7. When you are satified with your alignment, tap "Difference Reasoning" to highlight the differences in your environment.

## Dependencies

This App uses the Mixed Reality Toolkit Version 2017.4.3.0 which is downloadable from [here](https://github.com/Microsoft/MixedRealityToolkit-Unity/releases/tag/2017.4.3.0). The relevant Assets are already contained in this project.

## Credits

This app was created by the Computer Vision and Geometry (CVG) Group at
ETH Zurich for the European Union's Horizon 2020 research and innovation
programme project Built2Spec (under grant agreements No. 637221).

**Developers:**  
Stefano Woerner, Pierre Beckmann, Mathis Lamarre, Xiaojuan Wang, Pablo Speciale, Martin R. Oswald
