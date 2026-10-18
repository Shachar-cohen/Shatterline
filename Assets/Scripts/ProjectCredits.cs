using UnityEngine;

namespace Shatterline
{
    [CreateAssetMenu(menuName = "SHATTERLINE/Project Credits")]
    public sealed class ProjectCredits : ScriptableObject
    {
        [TextArea(2, 5)] public string TeamNames = "Shachar Cohen, Shachar Levy, Elad Abuganim, Tamir Ben Ovadia";
        [TextArea(2, 5)] public string Contributions = "Game Design, Programming, Level Design, Art & Sound";
        [TextArea(3, 8)] public string Resources = "Original procedural textures, artwork, and synthesized sound effects.\nDeveloped and playtested by the team.\nInk by inkle: MIT license (included in Packages).\nShader Graph serialization adapted from Unity Graphics tests: Unity Companion License (included beside the graph).";
        public string DisplayText { get { return TeamNames + "\n\n" + Contributions + "\n\n" + Resources; } }
    }
}
