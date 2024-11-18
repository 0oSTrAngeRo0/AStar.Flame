namespace AStar.Flame
{
    public static class Constants
    {
        public static class Bone
        {
            public static class Name
            {
                public const string ROOT = "Root";
                public const string NECK = "Neck";
                public const string JAW = "Jaw";
                public const string RIGHT_EYE = "RightEye";
                public const string LEFT_EYE = "LeftEye";

                public static readonly string[] ARRAY = { ROOT, NECK, JAW, RIGHT_EYE, LEFT_EYE };
            }

            public static class Index
            {
                public const int ROOT = 0;
                public const int NECK = 1;
                public const int JAW = 2;
                public const int RIGHT_EYE = 3;
                public const int LEFT_EYE = 4;
            }
        }

        public static class MeshBs
        {
            public const int SHAPE_COUNT = 300;
            public const int EXPRESSION_COUNT = 100;
            public const int POSE_COUNT = 36;
            public const int POSE_START_INDEX = 0;
            public const int EXPRESSION_START_INDEX = 36;
        }

        public static class Frames
        {
            public const string ROOT_POSITION = "translation";
            public const string ROOT_ROTATION = "rotation";
            public const string NECK_ROTATION = "neck_pose";
            public const string JAW_ROTATION = "jaw_pose";
            public const string EYE_ROTATION = "eyes_pose";
            public const string EXPRESSIONS = "expr";
        }
    }
}