using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class ColumnCoursePrompt : MonoBehaviour
    {
        private bool announced;
        private void OnTriggerEnter(Collider other)
        {
            if (announced || other.GetComponent<Runner>() == null) return;
            announced = true;
            int act = RunGame.Current.Level;
            RunGame.Current.Flash(act == 1 ?
                "PARKOUR / JUMP BETWEEN COLUMNS; HOLD R TO RECOVER" : act == 2 ?
                "CLOCKWORK / TIME THE MOVING LIFTS; DOUBLE JUMP BETWEEN THEM" :
                "CORE / RIDE THE LIFT, REACH THE HIGH BRIDGE; HOLD R TO CLIMB", 3.5f);
        }
    }
}
