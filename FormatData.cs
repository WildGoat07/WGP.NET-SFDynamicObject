using SFML.Graphics;
using SFML.System;
using System;

namespace WGP.SFDynamicObject
{
    [Serializable]
    internal class AnimatedBoneData
    {
        public byte[] BoneID;
        public KeyData[] Keys;
    }

    [Serializable]
    internal class AnimationData
    {
        public AnimatedBoneData[] Bones;
        public long Duration;
        public byte[] ID;
        public string Name;
    }

    [Serializable]
    internal class BoneData
    {
        public BlendModeType BlendMode;
        public byte[][] Children;
        public byte[] ID;
        public string Name;
        public SpriteData Sprite;
        public TransformData Transform;
    }

    [Serializable]
    internal class FormatData
    {
        public AnimationData[] Animations;
        public BoneData[] Hierarchy;
        public byte[][] Masters;
        public Resource[] Resources;
        public string Version;
    }

    [Serializable]
    internal class KeyData
    {
        public float ColCoeff;
        public Animation.Key.Fct ColFunction;
        public Color Color;
        public float OCoCoeff;
        public Animation.Key.Fct OCoFunction;
        public byte Opacity;
        public float OpaCoeff;
        public Animation.Key.Fct OpaFunction;
        public float OriCoeff;
        public Animation.Key.Fct OriFunction;
        public float OThCoeff;
        public Animation.Key.Fct OThFunction;
        public Color OutlineColor;
        public float OutlineThickness;
        public float PosCoeff;
        public Animation.Key.Fct PosFunction;
        public long Position;
        public float RotCoeff;
        public Animation.Key.Fct RotFunction;
        public float ScaCoeff;
        public Animation.Key.Fct ScaFunction;
        public TransformData Transform;
    }

    [Serializable]
    internal class SpriteData
    {
        public Color Color;
        public Color OutlineColor;
        public float OutlineThickness;
        public Vector2f Size;
        public byte[] TextureID;
        public IntRect TextureRect;
    }

    [Serializable]
    internal class TransformData
    {
        public Vector2f Origin;
        public Vector2f Position;
        public float Rotation;
        public Vector2f Scale;
    }
}