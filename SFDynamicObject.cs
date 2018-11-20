using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.Graphics;
using SFML;
using SFML.System;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// Dynamic object. Supports multi sprite, bones and animations.
    /// </summary>
    public class SFDynamicObject : Transformable, Drawable
    {
        public IList<Resource> UsedResources { get; set; }
        /// <summary>
        /// Version of the current SFDynamicObject encoder/decoder.
        /// </summary>
        public static readonly Version CurrentVersion = new Version(1, 2, 0, 0);
        /// <summary>
        /// Version of the created object.
        /// </summary>
        public Version Version { get; private set; }
        public class NewerVersionException : Exception
        {
            public Version CurrentVersion { get; }
            public Version RequestedVersion { get; }
            public NewerVersionException(Version FileVersion) : base("The file is in " + FileVersion + " but the API is in " + SFDynamicObject.CurrentVersion)
            {
                CurrentVersion = SFDynamicObject.CurrentVersion;
                RequestedVersion = FileVersion;
            }
        }

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
        private Dictionary<Bone, Transformable> oldAnimState;
        private Chronometer fadeChrono;
        private Dictionary<Bone, Transformable> transforms;
        /// <summary>
        /// The hierarchy of the bones. All bones must be here. The order in the hierarchy will be the order of drawing the sprites from the bones.
        /// </summary>
        public IList<Bone> BonesHierarchy { get; set; }
        /// <summary>
        /// The list of the master bones. All child bones must NOT be referenced here.
        /// </summary>
        public IList<Bone> MasterBones { get; set; }
        /// <summary>
        /// Animations available for the bones.
        /// </summary>
        public IList<Animation> Animations { get; set; }
        /// <summary>
        /// Time between animations to smooth the transition.
        /// </summary>
        public Time TransitionTime { get; set; }
        private Animation currentAnim;
        private Queue<Guid> buffer;
        private Chronometer chronometer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SFDynamicObject()
        {
            Version = CurrentVersion;
            oldAnimState = null;
            TransitionTime = Time.Zero;
            buffer = new Queue<Guid>();
            BonesHierarchy = new List<Bone>();
            MasterBones = new List<Bone>();
            Animations = new List<Animation>();
            UsedResources = new List<Resource>();
            currentAnim = null;
            ResetAnimation();
        }
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
        /// Returns the global bounding box.
        /// </summary>
        /// <returns></returns>
        public FloatRect GetGlobalBounds() => Transform.TransformRect(GetLocalBounds());
        /// <summary>
        /// Loads an animation. If a chronometer is set, it will reset.
        /// </summary>
        /// <param name="animID">ID of the animation to load.</param>
        /// <param name="reset">Reset the chronometer.</param>
        /// <param name="queue">Queue containing the following animations to play once the current is finished.</param>
        public void LoadAnimation(Guid animID, bool reset = true, params Guid[] queue)
        {
            if (currentAnim != null)
                oldAnimState = new Dictionary<Bone, Transformable>(transforms);
            if (Animations == null)
                throw new Exception("No animations provided");
            if (queue != null)
                buffer = new Queue<Guid>(queue);
            else
                buffer.Clear();
            if (animID == null)
                currentAnim = null;
            else
            {
                foreach (var item in Animations)
                {
                    if (item.ID == animID)
                    {
                        currentAnim = item;
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
                    }
                }
                throw new Exception("No animation named \"" + animID + "\"");
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
        /// Updates the display of the object by adjusting the bones to match the animations. Won't have any effect if there are no chronometer set or no animation loaded.
        /// </summary>
        public void Update()
        {
            if (currentAnim != null && Chronometer != null)
            {
                if (Chronometer.ElapsedTime > currentAnim.Duration)
                {
                    if (buffer.Count > 0)
                        LoadAnimation(buffer.Dequeue());
                    Chronometer.Restart();
                }
                Time currentTime = Chronometer.ElapsedTime;
                Time currentFadeTime = fadeChrono.ElapsedTime;

                foreach (var bone in BonesHierarchy)
                {
                    if (bone.SpriteChrono == null)
                        bone.SpriteChrono = new Chronometer(mainChrono);
                    if (bone.AttachedSprite != null)
                        bone.AttachedSprite.Update(bone.SpriteChrono.ElapsedTime);
                    bone.Opacity = 255;
                    bone.Color = Color.White;
                    bone.OutlineColor = Color.White;
                    bone.OutlineThickness = 0;
                    if (currentAnim.Bones != null && currentAnim.Bones.Contains(new Couple<Guid, List<Animation.Key>>() { Key = bone.ID }))
                    {
                        if (currentAnim.Bones.First((b) => b.Key == bone.ID).Value != null && currentAnim.Bones.First((b) => b.Key == bone.ID).Value.Count() == 0)
                        {
                            transforms[bone] = new Transformable();
                            continue;
                        }
                        List<Animation.Key> states = null;
                        try
                        {
                            var tmp = currentAnim.Bones.First((b) => b.Key == bone.ID);
                            if (tmp == null)
                                throw new KeyNotFoundException();
                            states = tmp.Value;
                        }
                        catch (KeyNotFoundException e)
                        {
                            throw new Exception("No bone named \"" + bone.Name + "\" in  the animation \"" + currentAnim.Name + "\"");
                        }
                        if (states != null)
                        {
                            Animation.Key first = states.First();
                            Animation.Key second = states.Last();
                            foreach (var state in states)
                            {
                                if (first.Position < state.Position && state.Position < currentTime)
                                    first = state;
                                if (second.Position > state.Position && state.Position > currentTime)
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
        /// <summary>
        /// Chronometer of the animation. Need to be set to animate. Create a relative chronometer internaly, so it should never change its time or speed to a lower value than 0. Also, it should be set once, at the begginning.
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
        private Chronometer mainChrono;
        /// <summary>
        /// The current time of the internal chronometer.
        /// </summary>
        public Time CurrentTime
        {
            get => chronometer.ElapsedTime;
            set => chronometer.ElapsedTime = value;
        }
        /// <summary>
        /// Saves the object to a stream as a template. This template can then be loaded by the DynamicObjectBuilder to create copies of it.
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
                        tmp2.BoneID = bone.Key.ToByteArray();
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
    }
    public class Couple<T, U> : IEquatable<Couple<T, U>> where T : IEquatable<T>
    {
        public Couple()
        {
        }

        public Couple(T key, U value)
        {
            Key = key;
            Value = value;
        }

        public T Key { get; set; }
        public U Value { get; set; }

        public bool Equals(Couple<T, U> other)
        {
            return Key.Equals(other.Key);
        }
    }
    /// <summary>
    /// An animation. contains all the key of the bones to animate.
    /// </summary>
    public class Animation
    {
        /// <summary>
        /// Universal identifier of the animation.
        /// </summary>
        public Guid ID { get; internal set; }
        /// <summary>
        /// A key. a key is a transformation at the right moment in the timeline. The dynamic object will make interpolations between the keys.
        /// </summary>
        public class Key : IComparable<Key>, IComparable
        {
            public enum Fct
            {
                LINEAR,
                POWER,
                ROOT,
                GAUSS,
                BINARY
            }
            /// <summary>
            /// The opacity of the bone. Doesn't inherit from its parent.
            /// </summary>
            public byte Opacity { get; set; }
            /// <summary>
            /// The color of the sprite.
            /// </summary>
            public Color Color { get; set; }
            /// <summary>
            /// Coefficient of the position function.
            /// </summary>
            public float PosFctCoeff { get; set; }
            /// <summary>
            /// The outline color of the sprite.
            /// </summary>
            public Color OutlineColor { get; set; }
            /// <summary>
            /// The outline thickness of the sprite.
            /// </summary>
            public float OutlineThickness { get; set; }
            /// <summary>
            /// How the position will be calculated.
            /// </summary>
            public Fct PosFunction { get; set; }
            /// <summary>
            /// Coefficient of the origin function.
            /// </summary>
            public float OriginFctCoeff { get; set; }
            /// <summary>
            /// How the origin will be calculated.
            /// </summary>
            public Fct OriginFunction { get; set; }
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
            /// Coefficient of the opacity function.
            /// </summary>
            public float OpacityFctCoeff { get; set; }
            /// <summary>
            /// How the opacity will be calculated.
            /// </summary>
            public Fct OpacityFunction { get; set; }
            /// <summary>
            /// Coefficient of the color function.
            /// </summary>
            public float ColorFctCoeff { get; set; }
            /// <summary>
            /// How the color will be calculated.
            /// </summary>
            public Fct ColorFunction { get; set; }
            /// <summary>
            /// Coefficient of the outline color function.
            /// </summary>
            public float OutlineColorFctCoeff { get; set; }
            /// <summary>
            /// How the outline color will be calculated.
            /// </summary>
            public Fct OutlineColorFunction { get; set; }
            /// <summary>
            /// Coefficient of the outline thickness function.
            /// </summary>
            public float OutlineThicknessFctCoeff { get; set; }
            /// <summary>
            /// How the outline thickness will be calculated.
            /// </summary>
            public Fct OutlineThicknessFunction { get; set; }
            /// <summary>
            /// The transformations to add (or multiply in the case of scaling) to the bone.
            /// </summary>
            public Transformable Transform { get; set; }
            /// <summary>
            /// The position in time of the key in the timeline.
            /// </summary>
            public Time Position { get; set; }
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
        }
        /// <summary>
        /// A double array of all the keys of the animation, sorted by bones.
        /// </summary>
        public List<Couple<Guid, List<Key>>> Bones { get; set; }
        /// <summary>
        /// The name of the animation. Needed when loading an animation.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The total duration of the animation. Once the chronometer reach the duration, it will reset.
        /// </summary>
        public Time Duration { get; set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        public Animation()
        {
            Name = null;
            Bones = new List<Couple<Guid, List<Key>>>();
        }
    }
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
    /// A basic bone.
    /// </summary>
    public class Bone : Transformable
    {
        internal Chronometer SpriteChrono { get; set; }
        /// <summary>
        /// Universal identifier of the bone.
        /// </summary>
        public Guid ID { get; internal set; }
        /// <summary>
        /// BlendMode used to draw this bone.
        /// </summary>
        public BlendModeType BlendMode { get; set; }
        /// <summary>
        /// Opacity of the bone. Can only be changed using keys and animations.
        /// </summary>
        public byte Opacity { get; internal set; }
        /// <summary>
        /// Color of the bone. Can only be changed using keys and animations.
        /// </summary>
        public Color Color { get; internal set; }
        /// <summary>
        /// OutlineColor of the bone. Can only be changed using keys and animations.
        /// </summary>
        public Color OutlineColor { get; internal set; }
        /// <summary>
        /// Outline thickness of the bone. Can only be changed using keys and animations.
        /// </summary>
        public float OutlineThickness { get; internal set; }
        /// <summary>
        /// This bone should draw its temporary sprites before its attached sprites ?
        /// </summary>
        public bool DrawTempSpritesFirst { get; set; }
        /// <summary>
        /// The childs of the bone. They will be relative to their parent.
        /// </summary>
        public List<Bone> Children { get; set; }
        /// <summary>
        /// The absolute transforms of the bone. For internal uses only.
        /// </summary>
        public Transform ComputedTransform { get; internal set; }
        /// <summary>
        /// The list of sprites affected by the changes of the bone. Be careful of the order (the order of drawing). The string is the name of the texture in the texture manager.
        /// </summary>
        public DynamicSprite AttachedSprite { get; set; }
        /// <summary>
        /// The temporary sprites are not saved but they are drawn at the same time as the bone. They are usually used for small details that change often in a game.
        /// </summary>
        public List<Drawable> TemporarySprites { get; set; }
        /// <summary>
        /// The name of the bone. Needed for animations.
        /// </summary>
        public string Name { get; set; }
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
            AttachedSprite = new DynamicSprite();
            Name = null;
            Opacity = 255;
            Color = Color.White;
            OutlineColor = Color.White;
            OutlineThickness = 0;
            BlendMode = BlendModeType.BLEND_ALPHA;
        }
    }
    public class DynamicSprite
    {
        public DynamicSprite()
        {
            InternalRect = null;
            Resource = null;
        }

        public DynamicSprite(RectangleShape internalRect, Resource resource)
        {
            InternalRect = internalRect;
            Resource = resource;
        }

        public RectangleShape InternalRect { get; set; }
        public Resource Resource { get; set; }
        public void Update(Time timer)
        {
            if (InternalRect != null && Resource != null)
                InternalRect.Texture = Resource.GetTexture(timer);
        }
    }
}
