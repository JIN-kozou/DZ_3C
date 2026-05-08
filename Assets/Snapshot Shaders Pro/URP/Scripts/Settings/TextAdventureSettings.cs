namespace SnapshotShaders.URP
{
    using UnityEngine;
    using UnityEngine.Rendering;
    using UnityEngine.Rendering.Universal;

    [System.Serializable, VolumeComponentMenu("Snapshot Shaders Pro/TextAdventure")]
    public sealed class TextAdventureSettings : VolumeComponent, IPostProcessComponent
    {
        public TextAdventureSettings()
        {
            displayName = "Text Adventure";
        }

        [Tooltip("Choose where to insert this pass in URP's render loop.")]
        public RenderPassEventParameter renderPassEvent = new RenderPassEventParameter(RenderPassEvent.BeforeRenderingPostProcessing);

        [Tooltip("The on-screen size of each character, in pixels.")]
        public ClampedIntParameter characterSize = new ClampedIntParameter(8, 8, 64); 

        [Tooltip("A texture containing the characters that will replace the image.\n" +
            "An (nx)-by-y texture, where there are n characters, each of which is x-by-y pixels.")]
        public TextureParameter characterAtlas = new TextureParameter(null);

        [Tooltip("How many characters are contained in the Character Atlas.")]
        public IntParameter characterCount = new IntParameter(16);

        [Tooltip("The color of the background.")]
        public ColorParameter backgroundColor = new ColorParameter(Color.black);
        
        [Tooltip("The color of the characters superimposed onto the background.")]
        public ColorParameter characterColor = new ColorParameter(Color.green, true, true, true);

        public bool IsActive()
        {
            return characterAtlas.value != null && active;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}
