using UnityEngine;

namespace Shatterline
{
    [CreateAssetMenu(menuName = "SHATTERLINE/Project Credits")]
    public sealed class ProjectCredits : ScriptableObject
    {
        [TextArea(2, 5)] public string TeamNames = "TODO: REPLACE BEFORE SUBMISSION - Team Member Names";
        [TextArea(2, 5)] public string Contributions = "TODO: REPLACE BEFORE SUBMISSION - Team Member Contributions";
        [TextArea(3, 8)] public string Resources = "Original procedural textures and synthesized sound effects.\nBackdrop and courier artwork: OpenAI image generation.\nCoding: AI-assisted development; reviewed and playtested by the team.\nInk by inkle: MIT license (included in Packages).\nShader Graph serialization adapted from Unity Graphics tests: Unity Companion License (included beside the graph).";
        public string DisplayText { get { return TeamNames + "\n\n" + Contributions + "\n\n" + Resources; } }
    }
}
