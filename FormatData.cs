using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML.System;

namespace WGP.SFDynamicObject
{
    [Serializable]
    internal class TransformData
    {
        public Vector2f Position;
        public Vector2f Scale;
        public Vector2f Origin;
        public float Rotation;
    }
    [Serializable]
    internal class BoneData
    {
        public BlendModeType BlendMode;
        public string Name;
        public Guid ID;
        public TransformData Transform;
        public Guid[] Children;
        public SpriteData Sprite;
    }
    [Serializable]
    internal class SpriteData
    {
        public Guid TextureID;
        public IntRect TextureRect;
        public Vector2f Size;
        public Color OutlineColor;
        public float OutlineThickness;
    }
    [Serializable]
    internal class FormatData
    {
        public string Version;
        public BoneData[] Hierarchy;
        public Guid[] Masters;
        public AnimationData[] Animations;
        public Resource[] Resources;
    }
    [Serializable]
    internal class AnimationData
    {
        public Guid ID;
        public string Name;
        public AnimatedBoneData[] Bones;
        public long Duration;
    }
    [Serializable]
    internal class KeyData
    {
        public long Position;
        public TransformData Transform;
        public byte Opacity;
        public Color Color;
        public Color OutlineColor;
        public float OutlineThickness;
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
    [Serializable]
    internal class AnimatedBoneData
    {
        public Guid BoneID;
        public KeyData[] Keys;
    }
}
