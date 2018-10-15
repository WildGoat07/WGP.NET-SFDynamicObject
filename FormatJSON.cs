using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;

namespace WGP.SFDynamicObject
{
    internal class TransformJSON
    {
        public Vector2f Position;
        public Vector2f Scale;
        public Vector2f Origin;
        public float Rotation;
    }
    internal class BoneJSON
    {
        public BlendModeType BlendMode;
        public string Name;
        public TransformJSON Transform;
        public string[] Children;
        public SpriteJSON[] Sprites;
    }
    internal class SpriteJSON
    {
        public string TextureID;
        public IntRect TextureRect;
        public Vector2f Size;
        public TransformJSON Transform;
        public Color OutlineColor;
        public float OutlineThickness;
    }
    internal class FormatJSON
    {
        public string Version = "1.0.0.0";
        public BoneJSON[] Hierarchy;
        public string[] Masters;
        public AnimationJSON[] Animations;
    }
    internal class AnimationJSON
    {
        public string Name;
        public AnimatedBoneJSON[] Bones;
        public long Duration;
    }
    internal class KeyJSON
    {
        public long Position;
        public TransformJSON Transform;
        public byte Opacity = 255;
        public Color Color = Color.White;
        public Color OutlineColor = Color.White;
        public float OutlineThickness = 0;
        public int PosFunction;
        public float PosCoeff;
        public int OriFunction;
        public float OriCoeff;
        public int ScaFunction;
        public float ScaCoeff;
        public int RotFunction;
        public float RotCoeff;
        public int OpaFunction;
        public float OpaCoeff;
        public int OCoFunction;
        public float OCoCoeff;
        public int ColFunction;
        public float ColCoeff;
        public int OThFunction;
        public float OThCoeff;
    }
    internal class AnimatedBoneJSON
    {
        public string BoneName;
        public KeyJSON[] Keys;
    }
}
