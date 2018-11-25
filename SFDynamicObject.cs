using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// Blendmode of the bone to use with its sprite
    /// </summary>
    public enum BlendModeType
    {
        /// <summary>
        /// Default blendmode.
        /// </summary>
        BLEND_ALPHA,

        /// <summary>
        /// Additive blendmode.
        /// </summary>
        BLEND_ADD,

        /// <summary>
        /// Multiplicative blendmode.
        /// </summary>
        BLEND_MULT,

        /// <summary>
        /// Substractive blendmode.
        /// </summary>
        BLEND_SUB
    }

    /// <summary>
    /// An animation. contains all the key of the bones to animate.
    /// </summary>
    public class Animation
    {
        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public Animation()
        {
            Name = null;
            Bones = new List<Couple<Bone, List<Key>>>();
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// A double array of all the keys of the animation, sorted by bones.
        /// </summary>
        public List<Couple<Bone, List<Key>>> Bones { get; set; }

        /// <summary>
        /// The total duration of the animation. Once the chronometer reach the duration, it will reset.
        /// </summary>
        public Time Duration { get; set; }

        /// <summary>
        /// Universal identifier of the animation.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// The name of the animation. Needed when loading an animation.
        /// </summary>
        public string Name { get; set; }

        #endregion Public Properties

        #region Public Classes

        /// <summary>
        /// A key. a key is a transformation at the right moment in the timeline. The dynamic object
        /// will make interpolations between the keys.
        /// </summary>
        public class Key : IComparable<Key>, IComparable
        {
            #region Public Constructors

            /// <summary>
            /// Constructor.
            /// </summary>
            public Key()
            {
                OutlineColor = Color.White;
                OutlineThickness = 0;
                Color = Color.White;
                Transform = new Transformable();
                PosFunction = Fct.LINEAR;
                PosFctCoeff = 1;
                OriginFunction = Fct.LINEAR;
                OriginFctCoeff = 1;
                ScaleFunction = Fct.LINEAR;
                ScaleFctCoeff = 1;
                RotFunction = Fct.LINEAR;
                RotFctCoeff = 1;
                OpacityFunction = Fct.LINEAR;
                OpacityFctCoeff = 1;
                ColorFunction = Fct.LINEAR;
                ColorFctCoeff = 1;
                OutlineColorFunction = Fct.LINEAR;
                OutlineColorFctCoeff = 1;
                OutlineThicknessFunction = Fct.LINEAR;
                OutlineThicknessFctCoeff = 1;
                Opacity = 255;
            }

            #endregion Public Constructors

            #region Public Enums

            /// <summary>
            /// Type of the interpolation function
            /// </summary>
            public enum Fct
            {
                LINEAR,
                POWER,
                ROOT,
                GAUSS,
                BINARY
            }

            #endregion Public Enums

            #region Public Properties

            /// <summary>
            /// The color of the sprite.
            /// </summary>
            public Color Color { get; set; }

            /// <summary>
            /// Coefficient of the color function.
            /// </summary>
            public float ColorFctCoeff { get; set; }

            /// <summary>
            /// How the color will be calculated.
            /// </summary>
            public Fct ColorFunction { get; set; }

            /// <summary>
            /// The opacity of the bone. Doesn't inherit from its parent.
            /// </summary>
            public byte Opacity { get; set; }

            /// <summary>
            /// Coefficient of the opacity function.
            /// </summary>
            public float OpacityFctCoeff { get; set; }

            /// <summary>
            /// How the opacity will be calculated.
            /// </summary>
            public Fct OpacityFunction { get; set; }

            /// <summary>
            /// Coefficient of the origin function.
            /// </summary>
            public float OriginFctCoeff { get; set; }

            /// <summary>
            /// How the origin will be calculated.
            /// </summary>
            public Fct OriginFunction { get; set; }

            /// <summary>
            /// The outline color of the sprite.
            /// </summary>
            public Color OutlineColor { get; set; }

            /// <summary>
            /// Coefficient of the outline color function.
            /// </summary>
            public float OutlineColorFctCoeff { get; set; }

            /// <summary>
            /// How the outline color will be calculated.
            /// </summary>
            public Fct OutlineColorFunction { get; set; }

            /// <summary>
            /// The outline thickness of the sprite.
            /// </summary>
            public float OutlineThickness { get; set; }

            /// <summary>
            /// Coefficient of the outline thickness function.
            /// </summary>
            public float OutlineThicknessFctCoeff { get; set; }

            /// <summary>
            /// How the outline thickness will be calculated.
            /// </summary>
            public Fct OutlineThicknessFunction { get; set; }

            /// <summary>
            /// Coefficient of the position function.
            /// </summary>
            public float PosFctCoeff { get; set; }

            /// <summary>
            /// How the position will be calculated.
            /// </summary>
            public Fct PosFunction { get; set; }

            /// <summary>
            /// The position in time of the key in the timeline.
            /// </summary>
            public Time Position { get; set; }

            /// <summary>
            /// Coefficient of the rotation function.
            /// </summary>
            public float RotFctCoeff { get; set; }

            /// <summary>
            /// How the rotation will be calculated.
            /// </summary>
            public Fct RotFunction { get; set; }

            /// <summary>
            /// Coefficient of the scale function.
            /// </summary>
            public float ScaleFctCoeff { get; set; }

            /// <summary>
            /// How the scale will be calculated.
            /// </summary>
            public Fct ScaleFunction { get; set; }

            /// <summary>
            /// The transformations to add (or multiply in the case of scaling) to the bone.
            /// </summary>
            public Transformable Transform { get; set; }

            #endregion Public Properties

            #region Public Methods

            public int CompareTo(Key other)
            {
                return Position.AsMicroseconds().CompareTo(other.Position.AsMicroseconds());
            }

            public int CompareTo(object obj)
            {
                if (obj is Key)
                    return CompareTo((Key)obj);
                throw new InvalidOperationException("Invalid type :" + obj.GetType());
            }

            #endregion Public Methods
        }

        #endregion Public Classes
    }

    /// <summary>
    /// A basic bone.
    /// </summary>
    public class Bone : Transformable, IEquatable<Bone>
    {
        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public Bone()
        {
            ID = Guid.NewGuid();
            SpriteChrono = null;
            DrawTempSpritesFirst = false;
            TemporarySprites = new List<Drawable>();
            Children = new List<Bone>();
            AttachedSprite = null;
            Name = null;
            Opacity = 255;
            Color = Color.White;
            OutlineColor = Color.White;
            OutlineThickness = 0;
            BlendMode = BlendModeType.BLEND_ALPHA;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// The list of sprites affected by the changes of the bone. Be careful of the order (the
        /// order of drawing). The string is the name of the texture in the texture manager.
        /// </summary>
        public DynamicSprite AttachedSprite { get; set; }

        /// <summary>
        /// BlendMode used to draw this bone.
        /// </summary>
        public BlendModeType BlendMode { get; set; }

        /// <summary>
        /// The childs of the bone. They will be relative to their parent.
        /// </summary>
        public List<Bone> Children { get; set; }

        /// <summary>
        /// Color of the bone. Can only be changed using keys and animations.
        /// </summary>
        public Color Color { get; internal set; }

        /// <summary>
        /// The absolute transforms of the bone. For internal uses only.
        /// </summary>
        public Transform ComputedTransform { get; internal set; }

        /// <summary>
        /// This bone should draw its temporary sprites before its attached sprites ?
        /// </summary>
        public bool DrawTempSpritesFirst { get; set; }

        /// <summary>
        /// Universal identifier of the bone.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// The name of the bone. Needed for animations.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Opacity of the bone. Can only be changed using keys and animations.
        /// </summary>
        public byte Opacity { get; internal set; }

        /// <summary>
        /// OutlineColor of the bone. Can only be changed using keys and animations.
        /// </summary>
        public Color OutlineColor { get; internal set; }

        /// <summary>
        /// Outline thickness of the bone. Can only be changed using keys and animations.
        /// </summary>
        public float OutlineThickness { get; internal set; }

        /// <summary>
        /// The temporary sprites are not saved but they are drawn at the same time as the bone. They
        /// are usually used for small details that change often in a game.
        /// </summary>
        public List<Drawable> TemporarySprites { get; set; }

        #endregion Public Properties

        #region Internal Properties

        internal Chronometer SpriteChrono { get; set; }

        #endregion Internal Properties

        #region Public Methods

        public bool Equals(Bone other) => ID.Equals(other.ID);

        public override bool Equals(object obj) => Equals((Bone)obj);

        public override int GetHashCode() => ID.GetHashCode();

        #endregion Public Methods
    }

    public class Couple<T, U> : IEquatable<Couple<T, U>> where T : IEquatable<T>
    {
        #region Public Constructors

        public Couple()
        {
        }

        public Couple(T key, U value)
        {
            Key = key;
            Value = value;
        }

        #endregion Public Constructors

        #region Public Properties

        public T Key { get; set; }
        public U Value { get; set; }

        #endregion Public Properties

        #region Public Methods

        public bool Equals(Couple<T, U> other)
        {
            return Key.Equals(other.Key);
        }

        #endregion Public Methods
    }

    /// <summary>
    /// The dynamic sprite is used to link a Resource to a RectangleShape.
    /// </summary>
    public class DynamicSprite
    {
        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public DynamicSprite()
        {
            InternalRect = new RectangleShape();
            Resource = null;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="internalRect">Rectangle of the sprite.</param>
        /// <param name="resource">Resource of the rectangle.</param>
        public DynamicSprite(RectangleShape internalRect, Resource resource)
        {
            InternalRect = internalRect;
            Resource = resource;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Rectangle of the sprite.
        /// </summary>
        public RectangleShape InternalRect { get; set; }

        /// <summary>
        /// Resource for the rectangle.
        /// </summary>
        public Resource Resource { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Updates the sprite to match its texture.
        /// </summary>
        /// <param name="timer">Current time for the texture.</param>
        public void Update(Time timer)
        {
            if (InternalRect != null && Resource != null)
                InternalRect.Texture = Resource.GetTexture(timer);
            else if (InternalRect != null)
                InternalRect.Texture = null;
        }

        #endregion Public Methods
    }

    /// <summary>
    /// Dynamic object. Supports multi sprite, bones and animations.
    /// </summary>
    public class SFDynamicObject : Transformable, Drawable
    {
        public void LoadOldVersion_1_1_0_1_to_2_0(System.IO.Stream input, convert.ResourceManager manager)
        {
            string content;
            {
                var sr = new System.IO.StreamReader(input);
                content = sr.ReadToEnd();
            }
            Version = CurrentVersion;
            oldAnimState = null;
            TransitionTime = Time.Zero;
            buffer = new Queue<Animation>();
            BonesHierarchy = new List<Bone>();
            MasterBones = new List<Bone>();
            Animations = new List<Animation>();
            UsedResources = new List<Resource>();
            currentAnim = null;
            ResetAnimation();
            foreach (var item in manager)
            {
                var res = new Resource();
                res.ChangeBaseImage(((Texture)item.Value.Data).CopyToImage());
                res.AdaptFrameSize();
                res.Name = item.Key;
                UsedResources.Add(res);
            }
            convert.FormatJSON fjs = Newtonsoft.Json.JsonConvert.DeserializeObject<convert.FormatJSON>(content);
            BonesHierarchy = fjs.Hierarchy.Select((bone) =>
            {
                var tmp = new Bone();
                tmp.AttachedSprite = new DynamicSprite();
                if (bone.Sprites != null && bone.Sprites.Length > 0)
                {
                    var sprite = bone.Sprites[0];
                    tmp.AttachedSprite.InternalRect.TextureRect = sprite.TextureRect;
                    tmp.AttachedSprite.InternalRect.Size = sprite.Size;
                    tmp.AttachedSprite.InternalRect.Position = sprite.Transform.Position;
                    tmp.AttachedSprite.InternalRect.Origin = sprite.Transform.Origin;
                    tmp.AttachedSprite.InternalRect.Scale = sprite.Transform.Scale;
                    tmp.AttachedSprite.InternalRect.Rotation = sprite.Transform.Rotation;
                    if (sprite.TextureID != null && sprite.TextureID != "")
                        tmp.AttachedSprite.Resource = UsedResources.Find((res) => res.Name == sprite.TextureID);
                }
                tmp.BlendMode = bone.BlendMode;
                tmp.Name = bone.Name;
                tmp.Position = bone.Transform.Position;
                tmp.Origin = bone.Transform.Origin;
                tmp.Scale = bone.Transform.Scale;
                tmp.Rotation = bone.Transform.Rotation;

                return tmp;
            }).ToList();
            foreach (var b in fjs.Hierarchy)
            {
                var bone = BonesHierarchy.Find((other) => other.Name == b.Name);
                foreach (var child in b.Children)
                {
                    bone.Children.Add(BonesHierarchy.Find((other) => other.Name == child));
                }
            }
        }

        #region Public Fields

        /// <summary>
        /// Version of the current SFDynamicObject encoder/decoder.
        /// </summary>
        public static readonly Version CurrentVersion = new Version(2, 0, 0, 0);

        #endregion Public Fields

        #region Private Fields

        private Queue<Animation> buffer;

        private Chronometer chronometer;

        private Animation currentAnim;

        private Chronometer fadeChrono;

        private Chronometer mainChrono;

        private Dictionary<Bone, Transformable> oldAnimState;

        private Dictionary<Bone, Transformable> transforms;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public SFDynamicObject()
        {
            Version = CurrentVersion;
            oldAnimState = null;
            TransitionTime = Time.Zero;
            buffer = new Queue<Animation>();
            BonesHierarchy = new List<Bone>();
            MasterBones = new List<Bone>();
            Animations = new List<Animation>();
            UsedResources = new List<Resource>();
            currentAnim = null;
            mainChrono = null;
            ResetAnimation();
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Animations available for the bones.
        /// </summary>
        public List<Animation> Animations { get; set; }

        /// <summary>
        /// The hierarchy of the bones. All bones must be here. The order in the hierarchy will be
        /// the order of drawing the sprites from the bones.
        /// </summary>
        public List<Bone> BonesHierarchy { get; set; }

        /// <summary>
        /// Chronometer of the animation. Need to be set to animate. Create a relative chronometer
        /// internaly, so it should never change its time or speed to a lower value than 0. Also, it
        /// should be set once, at the begginning.
        /// </summary>
        public Chronometer Chronometer
        {
            private get => chronometer;
            set
            {
                mainChrono = value;
                chronometer = new Chronometer(value);
                fadeChrono = new Chronometer(value);
            }
        }

        /// <summary>
        /// The current time of the internal chronometer.
        /// </summary>
        public Time CurrentTime
        {
            get => chronometer.ElapsedTime;
            set => chronometer.ElapsedTime = value;
        }

        /// <summary>
        /// The list of the master bones. All child bones must NOT be referenced here.
        /// </summary>
        public List<Bone> MasterBones { get; set; }

        /// <summary>
        /// Time between animations to smooth the transition.
        /// </summary>
        public Time TransitionTime { get; set; }

        /// <summary>
        /// Resources used by this object. Should contains ALL resources used. Unused ones can be
        /// added too, tho.
        /// </summary>
        public List<Resource> UsedResources { get; set; }

        /// <summary>
        /// Version of the created object.
        /// </summary>
        public Version Version { get; internal set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Cleans the resources by removing all the unused ones.
        /// </summary>
        public void CleanResources()
        {
            var newList = new List<Resource>();
            foreach (var item in BonesHierarchy)
            {
                if (item.AttachedSprite != null && item.AttachedSprite.Resource != null && !newList.Contains(item.AttachedSprite.Resource, new ResComparer()))
                    newList.Add(item.AttachedSprite.Resource);
            }
            UsedResources = newList;
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform *= Transform;
            foreach (var bone in BonesHierarchy)
            {
                if (bone.DrawTempSpritesFirst && bone.TemporarySprites != null)
                {
                    foreach (var sprite in bone.TemporarySprites)
                    {
                        RenderStates st = new RenderStates(states);
                        st.Transform *= bone.ComputedTransform;
                        target.Draw(sprite, st);
                    }
                }
                if (bone.AttachedSprite != null)
                {
                    var sprite = bone.AttachedSprite;
                    RenderStates st = new RenderStates(states);
                    st.Transform *= bone.ComputedTransform;
                    switch (bone.BlendMode)
                    {
                        case BlendModeType.BLEND_ALPHA:
                            st.BlendMode = BlendMode.Alpha;
                            break;

                        case BlendModeType.BLEND_ADD:
                            st.BlendMode = BlendMode.Add;
                            break;

                        case BlendModeType.BLEND_MULT:
                            st.BlendMode = BlendMode.Multiply;
                            break;

                        case BlendModeType.BLEND_SUB:
                            st.BlendMode = new BlendMode(BlendMode.Factor.OneMinusDstColor, BlendMode.Factor.OneMinusSrcColor);
                            break;
                    }
                    target.Draw(sprite.InternalRect, st);
                }
                if (!bone.DrawTempSpritesFirst && bone.TemporarySprites != null)
                {
                    foreach (var sprite in bone.TemporarySprites)
                    {
                        RenderStates st = new RenderStates(states);
                        st.Transform *= bone.ComputedTransform;
                        target.Draw(sprite, st);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the global bounding box.
        /// </summary>
        /// <returns></returns>
        public FloatRect GetGlobalBounds() => Transform.TransformRect(GetLocalBounds());

        /// <summary>
        /// Returns the local bounding box.
        /// </summary>
        /// <returns></returns>
        public FloatRect GetLocalBounds()
        {
            FloatRect result = new FloatRect();
            foreach (var bone in BonesHierarchy)
            {
                var tr = bone.ComputedTransform;
                if (bone.AttachedSprite != null)
                {
                    var sprite = bone.AttachedSprite;
                    var rect = tr.TransformRect(sprite.InternalRect.GetGlobalBounds());
                    rect.Width += rect.Left;
                    rect.Height += rect.Top;

                    result.Left = Utilities.Min(result.Left, rect.Left);
                    result.Top = Utilities.Min(result.Top, rect.Top);
                    result.Width = Utilities.Max(result.Width, rect.Width);
                    result.Height = Utilities.Max(result.Height, rect.Height);
                }
            }
            result.Width -= result.Left;
            result.Height -= result.Top;
            return result;
        }

        /// <summary>
        /// Loads an animation. If a chronometer is set, it will reset.
        /// </summary>
        /// <param name="anim">The name of the animation to load.</param>
        /// <param name="reset">Reset the chronometer.</param>
        /// <param name="queue">
        /// Queue containing the following animations to play once the current is finished.
        /// </param>
        public void LoadAnimation(string anim, bool reset = true, params string[] queue) => LoadAnimation(Animations.Find((a) => a.Name == anim), reset, queue.Select((n) => Animations.Find((a) => a.Name == n)).ToArray());

        /// <summary>
        /// Loads an animation. If a chronometer is set, it will reset.
        /// </summary>
        /// <param name="anim">The ID of the animation to load.</param>
        /// <param name="reset">Reset the chronometer.</param>
        /// <param name="queue">
        /// Queue containing the following animations to play once the current is finished.
        /// </param>
        public void LoadAnimation(Guid anim, bool reset = true, params Guid[] queue) => LoadAnimation(Animations.Find((a) => a.ID == anim), reset, queue.Select((id) => Animations.Find((a) => a.ID == id)).ToArray());

        /// <summary>
        /// Loads an animation. If a chronometer is set, it will reset.
        /// </summary>
        /// <param name="anim">The animation to load.</param>
        /// <param name="reset">Reset the chronometer.</param>
        /// <param name="queue">
        /// Queue containing the following animations to play once the current is finished.
        /// </param>
        public void LoadAnimation(Animation anim, bool reset = true, params Animation[] queue)
        {
            if (currentAnim != null)
                oldAnimState = new Dictionary<Bone, Transformable>(transforms);
            if (Animations == null)
                throw new Exception("No animations provided");
            if (queue != null)
                buffer = new Queue<Animation>(queue);
            else
                buffer.Clear();
            if (anim == null)
                currentAnim = null;
            else
            {
                currentAnim = anim;
                transforms = new Dictionary<Bone, Transformable>();
                if (currentAnim.Bones != null)
                {
                    foreach (var statList in currentAnim.Bones)
                    {
                        if (statList.Value != null)
                        {
                            statList.Value.Sort();
                        }
                    }
                }
                foreach (var bone in BonesHierarchy)
                    transforms.Add(bone, new Transformable());
                if (Chronometer != null && reset)
                {
                    Chronometer.Restart();
                    fadeChrono.Restart();
                }
                return;
                throw new Exception("No animation named \"" + anim + "\"");
            }
        }

        /// <summary>
        /// Resets the positon of the object, making it in the default position
        /// </summary>
        public void ResetAnimation()
        {
            transforms = new Dictionary<Bone, Transformable>();
            foreach (var item in BonesHierarchy)
            {
                item.Color = Color.White;
                item.OutlineColor = Color.White;
                item.OutlineThickness = 0;
                item.Opacity = 255;
            }
            currentAnim = null;
        }

        /// <summary>
        /// Saves the object to a stream as a template. This template can then be loaded by the
        /// DynamicObjectBuilder to create copies of it.
        /// </summary>
        /// <param name="stream">Stream on which to save</param>
        public void SaveAsTemplate(System.IO.Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new Exception("Can't write in the stream");
            try
            {
                FormatData result = new FormatData();
                result.Version = CurrentVersion.ToString();
                result.Hierarchy = BonesHierarchy.Select((bone) =>
                {
                    var tmp = new BoneData();
                    tmp.BlendMode = bone.BlendMode;
                    tmp.Name = bone.Name;
                    tmp.ID = bone.ID.ToByteArray();
                    tmp.Transform = new TransformData();
                    tmp.Transform.Position = bone.Position;
                    tmp.Transform.Scale = bone.Scale;
                    tmp.Transform.Origin = bone.Origin;
                    tmp.Transform.Rotation = bone.Rotation;
                    tmp.Children = bone.Children.Select((b) => b.ID.ToByteArray()).ToArray();
                    tmp.Sprite = new SpriteData();
                    if (bone.AttachedSprite != null)
                    {
                        if (bone.AttachedSprite.Resource != null)
                            tmp.Sprite.TextureID = bone.AttachedSprite.Resource.ID.ToByteArray();
                        else
                            tmp.Sprite.TextureID = null;
                        tmp.Sprite.TextureRect = bone.AttachedSprite.InternalRect.TextureRect;
                        tmp.Sprite.Size = bone.AttachedSprite.InternalRect.Size;
                        tmp.Sprite.Color = bone.AttachedSprite.InternalRect.FillColor;
                        tmp.Sprite.OutlineColor = bone.AttachedSprite.InternalRect.OutlineColor;
                        tmp.Sprite.OutlineThickness = bone.AttachedSprite.InternalRect.OutlineThickness;
                    }
                    else
                        tmp.Sprite = null;
                    return tmp;
                }).ToArray();
                result.Masters = MasterBones.Select((bone) => bone.ID.ToByteArray()).ToArray();
                result.Animations = Animations.Select((anim) =>
                {
                    var tmp = new AnimationData();
                    tmp.ID = anim.ID.ToByteArray();
                    tmp.Name = anim.Name;
                    tmp.Bones = anim.Bones.Select((bone) =>
                    {
                        var tmp2 = new AnimatedBoneData();
                        tmp2.BoneID = bone.Key.ID.ToByteArray();
                        tmp2.Keys = bone.Value.Select((key) =>
                        {
                            var tmp3 = new KeyData();
                            tmp3.Position = key.Position.Microseconds;
                            tmp3.Transform = new TransformData();
                            tmp3.Transform.Position = key.Transform.Position;
                            tmp3.Transform.Scale = key.Transform.Scale;
                            tmp3.Transform.Origin = key.Transform.Origin;
                            tmp3.Transform.Rotation = key.Transform.Rotation;
                            tmp3.Opacity = key.Opacity;
                            tmp3.Color = key.Color;
                            tmp3.OutlineColor = key.OutlineColor;
                            tmp3.OutlineThickness = key.OutlineThickness;
                            tmp3.PosFunction = key.PosFunction;
                            tmp3.PosCoeff = key.PosFctCoeff;
                            tmp3.OriFunction = key.OriginFunction;
                            tmp3.OriCoeff = key.OriginFctCoeff;
                            tmp3.ScaFunction = key.ScaleFunction;
                            tmp3.ScaCoeff = key.ScaleFctCoeff;
                            tmp3.RotFunction = key.RotFunction;
                            tmp3.RotCoeff = key.RotFctCoeff;
                            tmp3.OpaFunction = key.OpacityFunction;
                            tmp3.OpaCoeff = key.OpacityFctCoeff;
                            tmp3.OCoFunction = key.OutlineColorFunction;
                            tmp3.OCoCoeff = key.OutlineColorFctCoeff;
                            tmp3.ColFunction = key.ColorFunction;
                            tmp3.ColCoeff = key.ColorFctCoeff;
                            tmp3.OThFunction = key.OutlineThicknessFunction;
                            tmp3.OThCoeff = key.OutlineThicknessFctCoeff;

                            return tmp3;
                        }).ToArray();

                        return tmp2;
                    }).ToArray();
                    tmp.Duration = anim.Duration.Microseconds;

                    return tmp;
                }).ToArray();
                result.Resources = UsedResources.ToArray();
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(stream, result);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to save the object to the stream", e);
            }
        }

        /// <summary>
        /// Updates the display of the object by adjusting the bones to match the animations. Won't
        /// have any effect if there are no chronometer set or no animation loaded.
        /// </summary>
        public void Update()
        {
            if (mainChrono != null)
            {
                foreach (var bone in BonesHierarchy)
                {
                    if (bone.SpriteChrono == null)
                        bone.SpriteChrono = new Chronometer(mainChrono);
                    if (bone.AttachedSprite != null)
                        bone.AttachedSprite.Update(bone.SpriteChrono.ElapsedTime);
                }
            }
            if (currentAnim != null && Chronometer != null)
            {
                if (Chronometer.ElapsedTime > currentAnim.Duration)
                {
                    if (buffer.Count > 0)
                        LoadAnimation(buffer.Dequeue());
                    else
                        Chronometer.Restart();
                }
                Time currentTime = Chronometer.ElapsedTime;
                Time currentFadeTime = fadeChrono.ElapsedTime;

                foreach (var bone in BonesHierarchy)
                {
                    bone.Opacity = 255;
                    bone.Color = Color.White;
                    bone.OutlineColor = Color.White;
                    bone.OutlineThickness = 0;
                    if (currentAnim.Bones != null && currentAnim.Bones.Contains(new Couple<Bone, List<Animation.Key>>() { Key = bone }))
                    {
                        if (currentAnim.Bones.First((b) => b.Key == bone).Value != null && currentAnim.Bones.First((b) => b.Key == bone).Value.Count() == 0)
                        {
                            transforms[bone] = new Transformable();
                            continue;
                        }
                        List<Animation.Key> states = null;
                        try
                        {
                            var tmp = currentAnim.Bones.First((b) => b.Key.Equals(bone));
                            if (tmp == null)
                                throw new KeyNotFoundException();
                            states = tmp.Value;
                        }
                        catch (KeyNotFoundException)
                        {
                            throw new Exception("No bone named \"" + bone.Name + "\" in  the animation \"" + currentAnim.Name + "\"");
                        }
                        if (states != null)
                        {
                            Animation.Key first = states.First();
                            Animation.Key second = states.Last();
                            foreach (var state in states)
                            {
                                if (first.Position <= state.Position && state.Position <= currentTime)
                                    first = state;
                                if (second.Position >= state.Position && state.Position >= currentTime)
                                    second = state;
                            }
                            Transformable tr = new Transformable();
                            float posPerc = 0;
                            float oriPerc = 0;
                            float scaPerc = 0;
                            float rotPerc = 0;
                            float opaPerc = 0;
                            float ColorPerc = 0;
                            float OColorPerc = 0;
                            float OTPerc = 0;
                            if (second.PosFunction == Animation.Key.Fct.LINEAR)
                                posPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.PosFunction == Animation.Key.Fct.ROOT)
                                posPerc = new PowFunction(1f / second.PosFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.PosFunction == Animation.Key.Fct.POWER)
                                posPerc = new PowFunction(second.PosFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.PosFunction == Animation.Key.Fct.GAUSS)
                                posPerc = new ProgressiveFunction(second.PosFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.OriginFunction == Animation.Key.Fct.LINEAR)
                                oriPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.OriginFunction == Animation.Key.Fct.ROOT)
                                oriPerc = new PowFunction(1f / second.OriginFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OriginFunction == Animation.Key.Fct.POWER)
                                oriPerc = new PowFunction(second.OriginFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OriginFunction == Animation.Key.Fct.GAUSS)
                                oriPerc = new ProgressiveFunction(second.OriginFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.ScaleFunction == Animation.Key.Fct.LINEAR)
                                scaPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.ScaleFunction == Animation.Key.Fct.ROOT)
                                scaPerc = new PowFunction(1f / second.ScaleFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.ScaleFunction == Animation.Key.Fct.POWER)
                                scaPerc = new PowFunction(second.ScaleFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.ScaleFunction == Animation.Key.Fct.GAUSS)
                                scaPerc = new ProgressiveFunction(second.ScaleFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.RotFunction == Animation.Key.Fct.LINEAR)
                                rotPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.RotFunction == Animation.Key.Fct.ROOT)
                                rotPerc = new PowFunction(1f / second.RotFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.RotFunction == Animation.Key.Fct.POWER)
                                rotPerc = new PowFunction(second.RotFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.RotFunction == Animation.Key.Fct.GAUSS)
                                rotPerc = new ProgressiveFunction(second.RotFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.OpacityFunction == Animation.Key.Fct.LINEAR)
                                opaPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.OpacityFunction == Animation.Key.Fct.ROOT)
                                opaPerc = new PowFunction(1f / second.OpacityFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OpacityFunction == Animation.Key.Fct.POWER)
                                opaPerc = new PowFunction(second.OpacityFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OpacityFunction == Animation.Key.Fct.GAUSS)
                                opaPerc = new ProgressiveFunction(second.OpacityFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.ColorFunction == Animation.Key.Fct.LINEAR)
                                ColorPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.ColorFunction == Animation.Key.Fct.ROOT)
                                ColorPerc = new PowFunction(1f / second.ColorFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.ColorFunction == Animation.Key.Fct.POWER)
                                ColorPerc = new PowFunction(second.ColorFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.ColorFunction == Animation.Key.Fct.GAUSS)
                                ColorPerc = new ProgressiveFunction(second.ColorFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.OutlineColorFunction == Animation.Key.Fct.LINEAR)
                                OColorPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.OutlineColorFunction == Animation.Key.Fct.ROOT)
                                OColorPerc = new PowFunction(1f / second.OutlineColorFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OutlineColorFunction == Animation.Key.Fct.POWER)
                                OColorPerc = new PowFunction(second.OutlineColorFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OutlineColorFunction == Animation.Key.Fct.GAUSS)
                                OColorPerc = new ProgressiveFunction(second.OutlineColorFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            if (second.OutlineThicknessFunction == Animation.Key.Fct.LINEAR)
                                OTPerc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            else if (second.OutlineThicknessFunction == Animation.Key.Fct.ROOT)
                                OTPerc = new PowFunction(1f / second.OutlineThicknessFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OutlineThicknessFunction == Animation.Key.Fct.POWER)
                                OTPerc = new PowFunction(second.OutlineThicknessFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);
                            else if (second.OutlineThicknessFunction == Animation.Key.Fct.GAUSS)
                                OTPerc = new ProgressiveFunction(second.OutlineThicknessFctCoeff).Interpolation(Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds()), 0f, 1);

                            tr.Position = Utilities.Interpolation(posPerc, first.Transform.Position, second.Transform.Position);
                            tr.Scale = Utilities.Interpolation(scaPerc, first.Transform.Scale, second.Transform.Scale);
                            tr.Rotation = Utilities.Interpolation(rotPerc, first.Transform.Rotation, second.Transform.Rotation);
                            tr.Origin = Utilities.Interpolation(oriPerc, first.Transform.Origin, second.Transform.Origin);
                            bone.Opacity = (byte)Utilities.Interpolation(opaPerc, (float)first.Opacity, second.Opacity);
                            bone.OutlineThickness = Utilities.Interpolation(OTPerc, first.OutlineThickness, second.OutlineThickness);
                            bone.Color = new Color(
                                (byte)Utilities.Interpolation(ColorPerc, (float)first.Color.R, second.Color.R),
                                (byte)Utilities.Interpolation(ColorPerc, (float)first.Color.G, second.Color.G),
                                (byte)Utilities.Interpolation(ColorPerc, (float)first.Color.B, second.Color.B)
                                );
                            bone.OutlineColor = new Color(
                                (byte)Utilities.Interpolation(OColorPerc, (float)first.OutlineColor.R, second.OutlineColor.R),
                                (byte)Utilities.Interpolation(OColorPerc, (float)first.OutlineColor.G, second.OutlineColor.G),
                                (byte)Utilities.Interpolation(OColorPerc, (float)first.OutlineColor.B, second.OutlineColor.B)
                                );
                            if (currentFadeTime < TransitionTime && oldAnimState != null)
                            {
                                float perc2 = Utilities.Percent(currentFadeTime.AsSeconds(), 0, TransitionTime.AsSeconds());
                                tr.Position = Utilities.Interpolation(perc2, oldAnimState[bone].Position, tr.Position);
                                tr.Scale = Utilities.Interpolation(perc2, oldAnimState[bone].Scale, tr.Scale);
                                tr.Rotation = Utilities.Interpolation(perc2, oldAnimState[bone].Rotation, tr.Rotation);
                                tr.Origin = Utilities.Interpolation(perc2, oldAnimState[bone].Origin, tr.Origin);
                            }
                            transforms[bone] = tr;
                        }
                    }
                    else
                        transforms[bone] = new Transformable();
                }
                foreach (var bone in MasterBones)
                {
                    ComputeBone(bone, null);
                }
            }
            else
            {
                foreach (var bone in BonesHierarchy)
                {
                    bone.Opacity = 255;
                    transforms[bone] = new Transformable();
                    continue;
                }
                foreach (var bone in MasterBones)
                {
                    ComputeBone(bone, null);
                }
            }
        }

        #endregion Public Methods

        #region Private Methods

        private void ComputeBone(Bone bone, Bone parent)
        {
            Transformable added = transforms[bone];
            Transformable final = new Transformable(bone);
            final.Position += added.Position;
            final.Origin += added.Origin;
            final.Rotation += added.Rotation;
            final.Scale = new Vector2f(bone.Scale.X * added.Scale.X, bone.Scale.Y * added.Scale.Y);
            if (parent != null)
                bone.ComputedTransform = parent.ComputedTransform * final.Transform;
            else
                bone.ComputedTransform = final.Transform;
            if (bone.AttachedSprite != null)
            {
                var sprite = bone.AttachedSprite;
                var c = bone.Color;
                c.A = bone.Opacity;
                var oc = bone.OutlineColor;
                oc.A = bone.Opacity;
                sprite.InternalRect.FillColor = c;
                sprite.InternalRect.OutlineColor = oc;
                sprite.InternalRect.OutlineThickness = bone.OutlineThickness;
            }
            if (bone.Children != null)
            {
                foreach (var child in bone.Children)
                {
                    ComputeBone(child, bone);
                }
            }
        }

        #endregion Private Methods

        #region Private Classes

        private class ResComparer : IEqualityComparer<Resource>
        {
            #region Public Methods

            public bool Equals(Resource x, Resource y) => x.ID.Equals(y.ID);

            public int GetHashCode(Resource obj) => obj.ID.GetHashCode();

            #endregion Public Methods
        }

        #endregion Private Classes
    }
}