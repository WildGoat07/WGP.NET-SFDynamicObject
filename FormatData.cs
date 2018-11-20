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
        public byte[] ID;
        public TransformData Transform;
        public byte[][] Children;
        public SpriteData Sprite;
    }
    [Serializable]
    internal class SpriteData
    {
        public byte[] TextureID;
        public IntRect TextureRect;
        public Vector2f Size;
        public Color Color;
        public Color OutlineColor;
        public float OutlineThickness;
    }
    [Serializable]
    internal class FormatData
    {
        public string Version;
        public BoneData[] Hierarchy;
        public byte[][] Masters;
        public AnimationData[] Animations;
        public Resource[] Resources;
    }
    [Serializable]
    internal class AnimationData
    {
        public byte[] ID;
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
        public Animation.Key.Fct PosFunction;
        public float PosCoeff;
        public Animation.Key.Fct OriFunction;
        public float OriCoeff;
        public Animation.Key.Fct ScaFunction;
        public float ScaCoeff;
        public Animation.Key.Fct RotFunction;
        public float RotCoeff;
        public Animation.Key.Fct OpaFunction;
        public float OpaCoeff;
        public Animation.Key.Fct OCoFunction;
        public float OCoCoeff;
        public Animation.Key.Fct ColFunction;
        public float ColCoeff;
        public Animation.Key.Fct OThFunction;
        public float OThCoeff;
    }
    [Serializable]
    internal class AnimatedBoneData
    {
        public byte[] BoneID;
        public KeyData[] Keys;
    }
}
