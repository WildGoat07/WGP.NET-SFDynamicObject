using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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
    /// Dynamic object. Supports multi sprite, bones and animations.
    /// </summary>
    public class SFDynamicObject : Transformable, Drawable
    {
        #region Public Fields

        /// <summary>
        /// Version of the current SFDynamicObject encoder/decoder.
        /// </summary>
        public static readonly Version CurrentVersion = new Version(2, 0, 0, 2);

        /// <summary>
        /// The default category of the object.
        /// </summary>
        public readonly Category DefaultCategory;

        #endregion Public Fields

        #region Internal Fields

        internal List<Animation> _animations;

        internal List<Bone> _bonesHierarchy;

        internal List<Category> _customCategories;

        internal List<Bone> _masterBones;

        internal List<Resource> _usedResources;

        #endregion Internal Fields

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
            DefaultCategory = new Category(true);
            DefaultCategory.Owner = this;
            Version = CurrentVersion;
            oldAnimState = null;
            TransitionTime = Time.Zero;
            buffer = new Queue<Animation>();
            _bonesHierarchy = new List<Bone>();
            _masterBones = new List<Bone>();
            _animations = new List<Animation>();
            _usedResources = new List<Resource>();
            currentAnim = null;
            mainChrono = null;
            _customCategories = new List<Category>();
            ResetAnimation();
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// Animations available for the bones.
        /// </summary>
        public ReadOnlyCollection<Animation> Animations => _animations.AsReadOnly();

        /// <summary>
        /// The hierarchy of the bones. All bones must be here. The order in the hierarchy will be
        /// the order of drawing the sprites from the bones.
        /// </summary>
        public ReadOnlyCollection<Bone> BonesHierarchy => _bonesHierarchy.AsReadOnly();

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
        /// Custom categories, to add more control over the object.
        /// </summary>
        public ReadOnlyCollection<Category> CustomCategories => _customCategories.AsReadOnly();

        /// <summary>
        /// The list of the master bones. All child bones must NOT be referenced here.
        /// </summary>
        public ReadOnlyCollection<Bone> MasterBones => _masterBones.AsReadOnly();

        /// <summary>
        /// Time between animations to smooth the transition.
        /// </summary>
        public Time TransitionTime { get; set; }

        /// <summary>
        /// Resources used by this object. Should contains ALL resources used. Unused ones can be
        /// added too, tho.
        /// </summary>
        public ReadOnlyCollection<Resource> UsedResources => _usedResources.AsReadOnly();

        /// <summary>
        /// Version of the created object.
        /// </summary>
        public Version Version { get; internal set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Adds a resource to this object.
        /// </summary>
        /// <param name="res">Resource to add.</param>
        /// <returns>True if successful, false if the resource already exists.</returns>
        public bool AddResource(Resource res)
        {
            if (!_usedResources.Contains(res))
            {
                _usedResources.Add(res);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Cleans the resources by removing all the unused ones.
        /// </summary>
        public void CleanResources()
        {
            var newList = new List<Resource>();
            foreach (var item in _bonesHierarchy)
            {
                if (item.AttachedSprite != null && item.AttachedSprite.Resource != null && !newList.Contains(item.AttachedSprite.Resource, new ResComparer()))
                    newList.Add(item.AttachedSprite.Resource);
            }
            _usedResources = newList;
        }

        /// <summary>
        /// Creates a new animation for this object.
        /// </summary>
        /// <returns>New animation.</returns>
        public Animation CreateAnimation()
        {
            Animation result = new Animation();
            result.Owner = this;
            _animations.Add(result);
            return result;
        }

        /// <summary>
        /// Creates a new bone for this object.
        /// </summary>
        /// <param name="master">True if the bone is a masterbone.</param>
        /// <returns>New bone.</returns>
        public Bone CreateBone(bool master = false)
        {
            Bone result = new Bone();
            result.Owner = this;
            result.Category = DefaultCategory;
            _bonesHierarchy.Add(result);
            if (master)
                _masterBones.Add(result);
            return result;
        }

        /// <summary>
        /// Creates a new category for this object.
        /// </summary>
        /// <returns>New category.</returns>
        public Category CreateCustomCategory()
        {
            Category result = new Category();
            result.Owner = this;
            _customCategories.Add(result);
            return result;
        }

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform *= Transform;
            foreach (var bone in _bonesHierarchy)
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
                if (bone.AttachedSprite != null && bone.Category.Enabled)
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
            foreach (var bone in _bonesHierarchy)
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
        public void LoadAnimation(string anim, bool reset = true, params string[] queue) => LoadAnimation(_animations.Find((a) => a.Name == anim), reset, queue.Select((n) => _animations.Find((a) => a.Name == n)).ToArray());

        /// <summary>
        /// Loads an animation. If a chronometer is set, it will reset.
        /// </summary>
        /// <param name="anim">The ID of the animation to load.</param>
        /// <param name="reset">Reset the chronometer.</param>
        /// <param name="queue">
        /// Queue containing the following animations to play once the current is finished.
        /// </param>
        public void LoadAnimation(Guid anim, bool reset = true, params Guid[] queue) => LoadAnimation(_animations.Find((a) => a.ID == anim), reset, queue.Select((id) => _animations.Find((a) => a.ID == id)).ToArray());

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
            if (_animations == null)
                throw new Exception("No animations provided");
            if (queue != null)
                buffer = new Queue<Animation>(queue);
            else
                buffer.Clear();
            if (anim == null)
                currentAnim = null;
            else
            {
                if (anim._triggers != null)
                {
                    foreach (var item in anim._triggers)
                    {
                        item.triggered = false;
                    }
                }
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
                foreach (var bone in _bonesHierarchy)
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
        /// Moves the bone in the hierarchy.
        /// </summary>
        /// <param name="bone">Bone to move.</param>
        /// <param name="beforeThisBone">
        /// The bone will be moved just before this one. If set to null, the bone will be moved to
        /// the end.
        /// </param>
        /// <returns>True if successful.</returns>
        public bool MoveBone(Bone bone, Bone beforeThisBone)
        {
            if (bone.Owner == this && beforeThisBone == null)
            {
                _bonesHierarchy.Remove(bone);
                _bonesHierarchy.Insert(_bonesHierarchy.Count, bone);
                return true;
            }
            if (bone.Owner == this && beforeThisBone.Owner == this)
            {
                _bonesHierarchy.Remove(bone);
                _bonesHierarchy.Insert(_bonesHierarchy.IndexOf(beforeThisBone), bone);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Removes an animation.
        /// </summary>
        /// <param name="anim">Animation to remove.</param>
        /// <returns>True if successful.</returns>
        public bool RemoveAnimation(Animation anim)
        {
            if (anim == null)
                return false;
            if (anim.Owner == this)
            {
                anim.Owner = null;
                _animations.Remove(anim);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Removes a bone.
        /// </summary>
        /// <param name="bone">Bone to remove.</param>
        /// <returns>True if successful.</returns>
        public bool RemoveBone(Bone bone)
        {
            if (bone == null)
                return false;
            if (bone.Owner == this)
            {
                bone.Owner = null;
                _masterBones.Remove(bone);
                return _bonesHierarchy.Remove(bone);
            }
            else
                return false;
        }

        /// <summary>
        /// Removes a category.
        /// </summary>
        /// <param name="category">Category to remove.</param>
        /// <returns>True if successful.</returns>
        public bool RemoveCustomCategory(Category category)
        {
            if (category == null)
                return false;
            if (category.Owner == this)
            {
                category.Owner = null;
                _customCategories.Remove(category);
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// Removes a bone from the masterbone list.
        /// </summary>
        /// <param name="bone">Bone to remove.</param>
        /// <returns>True if successful.</returns>
        public bool RemoveMasterBone(Bone bone)
        {
            if (bone == null)
                return false;
            if (bone.Owner == this)
                return _masterBones.Remove(bone);
            return false;
        }

        /// <summary>
        /// Removes a resource.
        /// </summary>
        /// <param name="res">Resource to remove.</param>
        /// <returns>True if successful.</returns>
        public bool RemoveResource(Resource res)
        {
            if (res == null)
                return false;
            foreach (var b in _bonesHierarchy)
            {
                if (b.AttachedSprite != null && b.AttachedSprite.Resource != null && b.AttachedSprite.Resource.ID == res.ID)
                    b.AttachedSprite.Resource = null;
            }
            return _usedResources.Remove(res);
        }

        /// <summary>
        /// Resets the positon of the object, making it in the default position
        /// </summary>
        public void ResetAnimation()
        {
            transforms = new Dictionary<Bone, Transformable>();
            foreach (var item in _bonesHierarchy)
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
                result.Hierarchy = _bonesHierarchy.Select((bone) =>
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
                    tmp.Category = bone.Category.ID.ToByteArray();

                    return tmp;
                }).ToArray();
                result.Masters = _masterBones.Select((bone) => bone.ID.ToByteArray()).ToArray();
                result.Categories = _customCategories.Select((categ) =>
                {
                    var tmp = new CategoryData();
                    tmp.Name = categ.Name;
                    tmp.ID = categ.ID.ToByteArray();

                    return tmp;
                }).ToArray();
                result.Animations = _animations.Select((anim) =>
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
                            tmp3.TextureTime = key.TextureTime;

                            return tmp3;
                        }).ToArray();

                        return tmp2;
                    }).ToArray();
                    tmp.Triggers = anim._triggers.Select((t) =>
                    {
                        var tmp2 = new Trigger();
                        tmp2.Area = t.Area;
                        tmp2.ID = t.ID.ToByteArray();
                        tmp2.Name = t.Name;
                        tmp2.Time = t.Time;

                        return tmp2;
                    }).ToArray();
                    tmp.Duration = anim.Duration.Microseconds;

                    return tmp;
                }).ToArray();
                result.Resources = _usedResources.ToArray();
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                formatter.Serialize(stream, result);
            }
            catch (Exception e)
            {
                throw new Exception("Failed to save the object to the stream", e);
            }
        }

        /// <summary>
        /// Sets a bone as masterbone.
        /// </summary>
        /// <param name="bone">Bone to change.</param>
        /// <returns>True if successful.</returns>
        public bool SetMasterBone(Bone bone)
        {
            if (bone.Owner == this)
            {
                if (!_masterBones.Contains(bone))
                {
                    _masterBones.Add(bone);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Updates the display of the object by adjusting the bones to match the animations. Won't
        /// have any effect if there are no chronometer set or no animation loaded.
        /// </summary>
        public void Update()
        {
            if (mainChrono != null)
            {
                foreach (var bone in _bonesHierarchy)
                {
                    if (bone.SpriteChrono == null)
                        bone.SpriteChrono = new Chronometer(mainChrono);
                }
            }
            if (currentAnim != null && Chronometer != null)
            {
                if (Chronometer.ElapsedTime > currentAnim.Duration)
                {
                    if (buffer.Count > 0)
                        LoadAnimation(buffer.Dequeue());
                    else
                    {
                        Chronometer.Restart();
                        foreach (var b in currentAnim.Bones)
                        {
                            foreach (var k in b.Value)
                            {
                                k.reached = false;
                            }
                        }
                        if (currentAnim._triggers != null)
                        {
                            foreach (var item in currentAnim._triggers)
                            {
                                item.triggered = false;
                            }
                        }
                    }
                }
                Time currentTime = Chronometer.ElapsedTime;
                Time currentFadeTime = fadeChrono.ElapsedTime;
                if (currentAnim._triggers != null)
                {
                    foreach (var item in currentAnim._triggers)
                    {
                        if (!item.triggered)
                        {
                            if (currentTime > item.Time)
                            {
                                item.triggered = true;
                                if (item.Trigger != null)
                                    item.Trigger.Invoke();
                            }
                        }
                    }
                }
                foreach (var bone in _bonesHierarchy)
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
                            if (!first.reached)
                            {
                                if (first.TextureTime.HasValue)
                                    bone.SpriteChrono.ElapsedTime = first.TextureTime.Value;
                            }
                            first.reached = true;
                            if (bone.AttachedSprite != null)
                                bone.AttachedSprite.Update(bone.SpriteChrono.ElapsedTime);
                        }
                    }
                    else
                        transforms[bone] = new Transformable();
                }
                foreach (var bone in _masterBones)
                {
                    ComputeBone(bone, null);
                }
            }
            else
            {
                foreach (var bone in _bonesHierarchy)
                {
                    bone.Opacity = 255;
                    transforms[bone] = new Transformable();
                    continue;
                }
                foreach (var bone in _masterBones)
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