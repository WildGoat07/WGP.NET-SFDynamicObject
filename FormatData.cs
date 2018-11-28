using SFML.Graphics;
using SFML.System;
using System;
using System.Runtime.Serialization;

namespace WGP.SFDynamicObject
{
    [Serializable]
    internal class AnimatedBoneData : ISerializable
    {
        #region Public Fields

        public byte[] BoneID;
        public KeyData[] Keys;

        #endregion Public Fields

        #region Public Constructors

        public AnimatedBoneData()
        {
        }

        public AnimatedBoneData(SerializationInfo info, StreamingContext context)
        {
            BoneID = (byte[])info.GetValue("BoneID", typeof(byte[]));
            Keys = (KeyData[])info.GetValue("Keys", typeof(KeyData[]));
        }

        #endregion Public Constructors

        #region Public Methods

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("BoneID", BoneID);
            info.AddValue("Keys", Keys);
        }

        #endregion Public Methods
    }

    [Serializable]
    internal class AnimationData : ISerializable
    {
        #region Public Fields

        public AnimatedBoneData[] Bones;
        public long Duration;
        public byte[] ID;
        public string Name;
        public Trigger[] Triggers;

        #endregion Public Fields

        #region Public Constructors

        public AnimationData()
        {
        }

        public AnimationData(SerializationInfo info, StreamingContext context)
        {
            Bones = (AnimatedBoneData[])info.GetValue("Bones", typeof(AnimatedBoneData[]));
            Duration = (long)info.GetValue("Duration", typeof(long));
            ID = (byte[])info.GetValue("ID", typeof(byte[]));
            Name = (string)info.GetValue("Name", typeof(string));
            Triggers = info.TryGetValue<Trigger[]>("Triggers");
        }

        #endregion Public Constructors

        #region Public Methods

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Bones", Bones);
            info.AddValue("Duration", Duration);
            info.AddValue("ID", ID);
            info.AddValue("Name", Name);
            info.AddValue("Triggers", Triggers);
        }

        #endregion Public Methods
    }

    [Serializable]
    internal class BoneData : ISerializable
    {
        #region Public Fields

        public BlendModeType BlendMode;
        public byte[][] Children;
        public byte[] ID;
        public string Name;
        public SpriteData Sprite;
        public TransformData Transform;

        #endregion Public Fields

        #region Public Constructors

        public BoneData()
        {
        }

        public BoneData(SerializationInfo info, StreamingContext context)
        {
            BlendMode = (BlendModeType)info.GetValue("BlendMode", typeof(BlendModeType));
            Children = (byte[][])info.GetValue("Children", typeof(byte[][]));
            ID = (byte[])info.GetValue("ID", typeof(byte[]));
            Name = (string)info.GetValue("Name", typeof(string));
            Sprite = (SpriteData)info.GetValue("Sprite", typeof(SpriteData));
            Transform = (TransformData)info.GetValue("Transform", typeof(TransformData));
        }

        #endregion Public Constructors

        #region Public Methods

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("BlendMode", BlendMode);
            info.AddValue("Children", Children);
            info.AddValue("ID", ID);
            info.AddValue("Name", Name);
            info.AddValue("Sprite", Sprite);
            info.AddValue("Transform", Transform);
        }

        #endregion Public Methods
    }

    [Serializable]
    internal class FormatData : ISerializable
    {
        #region Public Fields

        public AnimationData[] Animations;
        public BoneData[] Hierarchy;
        public byte[][] Masters;
        public Resource[] Resources;
        public string Version;

        #endregion Public Fields

        #region Public Constructors

        public FormatData()
        {
        }

        public FormatData(SerializationInfo info, StreamingContext context)
        {
            Animations = (AnimationData[])info.GetValue("Animations", typeof(AnimationData[]));
            Hierarchy = (BoneData[])info.GetValue("Hierarchy", typeof(BoneData[]));
            Masters = (byte[][])info.GetValue("Masters", typeof(byte[][]));
            Resources = (Resource[])info.GetValue("Resources", typeof(Resource[]));
            Version = (string)info.GetValue("Version", typeof(string));
        }

        #endregion Public Constructors

        #region Public Methods

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Animations", Animations);
            info.AddValue("Hierarchy", Hierarchy);
            info.AddValue("Masters", Masters);
            info.AddValue("Resources", Resources);
            info.AddValue("Version", Version);
        }

        #endregion Public Methods
    }

    [Serializable]
    internal class KeyData
    {
        #region Public Fields

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

        #endregion Public Fields
    }

    [Serializable]
    internal class SpriteData
    {
        #region Public Fields

        public Color Color;
        public Color OutlineColor;
        public float OutlineThickness;
        public Vector2f Size;
        public byte[] TextureID;
        public IntRect TextureRect;

        #endregion Public Fields
    }

    [Serializable]
    internal class TransformData
    {
        #region Public Fields

        public Vector2f Origin;
        public Vector2f Position;
        public float Rotation;
        public Vector2f Scale;

        #endregion Public Fields
    }

    [Serializable]
    internal class Trigger : ISerializable
    {
        #region Public Fields

        public FloatRect Area;
        public byte[] ID;
        public string Name;
        public Time Time;

        #endregion Public Fields

        #region Public Constructors

        public Trigger()
        {
        }

        public Trigger(SerializationInfo info, StreamingContext context)
        {
            ID = (byte[])info.GetValue("ID", typeof(byte[]));
            Area = (FloatRect)info.GetValue("Area", typeof(FloatRect));
            Time = (Time)info.GetValue("Time", typeof(Time));
            Name = (string)info.GetValue("Name", typeof(string));
        }

        #endregion Public Constructors

        #region Public Methods

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("Area", Area);
            info.AddValue("Time", Time);
            info.AddValue("Name", Name);
        }

        #endregion Public Methods
    }
}