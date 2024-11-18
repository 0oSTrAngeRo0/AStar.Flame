#if UNITY_EDITOR

using UnityEditor;

namespace AStar.Flame
{
    public partial class FlameFramesPlayer
    {
        [CustomEditor(typeof(FlameFramesPlayer))]
        public class Editor : UnityEditor.Editor
        {
            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();
                FlameFramesPlayer mono = target as FlameFramesPlayer;
                if(mono == null) return;
                int frame = mono.CurrentFrameIndex;
                frame = EditorGUILayout.IntSlider(frame, 0, (int)mono.m_FlameFrames.FrameCount);
                mono.CurrentFrameIndex = frame;
            }
        }
    }
}

#endif