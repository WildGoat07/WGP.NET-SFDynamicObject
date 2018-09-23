using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;

namespace WGP.SFDynamicObject
{
    internal struct TransformJSON
    {
        public Vector2f Position;
        public Vector2f Scale;
        public Vector2f Origin;
        public float Rotation;
    }
    internal struct BoneJSON
    {
        public string Name;
        public TransformJSON Transform;
        public string[] Children;
        public SpriteJSON[] Sprites;
    }
    internal struct SpriteJSON
    {
        public string TextureID;
        public IntRect TextureRect;
        public Vector2f Size;
        public TransformJSON Transform;
        public Color FillColor;
        public Color OutlineColor;
        public float OutlineThickness;
    }
    internal struct FormatJSON
    {
        public BoneJSON[] Hierarchy;
        public string[] Masters;
        public AnimationJSON[] Animations;
    }
    internal struct AnimationJSON
    {
        public string Name;
        public AnimatedBoneJSON[] Bones;
        public long Duration;
    }
    internal struct KeyJSON
    {
        public long Position;
        public TransformJSON Transform;
        public byte Opacity;
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
    }
    internal struct AnimatedBoneJSON
    {
        public string BoneName;
        public KeyJSON[] Keys;
    }
}
