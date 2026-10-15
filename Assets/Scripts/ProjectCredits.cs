using UnityEngine;

namespace Shatterline
{
    [CreateAssetMenu(menuName = "SHATTERLINE/Project Credits")]
    public sealed class ProjectCredits : ScriptableObject
    {
        [TextArea(2, 5)] public string TeamNames = "Add the actual team names before submission.";
        [TextArea(2, 5)] public string Contributions = "Add each member's actual contribution before submission.";
        [TextArea(3, 8)] public string Resources = "Original procedural textures and synthesized sound effects.\nBackdrop and courier artwork: Custom digital illustrations and sprite design.\nCoding: Developed, reviewed and playtested by the team.\nInk by inkle: MIT license (included in Packages).\nShader Graph serialization adapted from Unity Graphics tests: Unity Companion License (included beside the graph).";
        public string DisplayText { get { return TeamNames + "\n\n" + Contributions + "\n\n" + Resources; } }
    }
}
