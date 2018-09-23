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
            if (bone.AttachedSprites != null)
            {
                foreach (var sprite in bone.AttachedSprites)
                {
                    var c = sprite.Value.FillColor;
                    c.A = bone.Opacity;
                    var oc = sprite.Value.OutlineColor;
                    oc.A = bone.Opacity;
                    sprite.Value.FillColor = c;
                    sprite.Value.OutlineColor = oc;
                }
            }
            if (bone.ChildBones != null)
            {
                foreach (var child in bone.ChildBones)
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
                if (bone.AttachedSprites != null)
                {
                    foreach (var sprite in bone.AttachedSprites)
                    {
                        RenderStates st = new RenderStates(states);
                        st.Transform *= bone.ComputedTransform;
                        target.Draw(sprite.Value, st);
                    }
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
        public List<Bone> BonesHierarchy { get; set; }
        /// <summary>
        /// The list of the master bones. All child bones must NOT be referenced here.
        /// </summary>
        public List<Bone> MasterBones { get; set; }
        /// <summary>
        /// Animations available for the bones.
        /// </summary>
        public List<Animation> Animations { get; set; }
        /// <summary>
        /// Time between animations to smooth the transition.
        /// </summary>
        public Time TransitionTime { get; set; }
        /// <summary>
        /// Sets the ResourceManager used by the object. It should contain the necessary resources for the object.
        /// </summary>
        public ResourceManager Manager
        {
            get => _manager;
            set
            {
                _manager = value;
                if (_manager != null)
                {
                    foreach (var bone in BonesHierarchy)
                    {
                        foreach (var sprite in bone.AttachedSprites)
                        {
                            {
                                if (sprite.Key != null)
                                    if (Manager.ContainsKey(sprite.Key))
                                    {
                                        sprite.Value.Texture = (Texture)Manager[sprite.Key].Data;
                                    }
                                    else
                                        sprite.Value.Texture = null;
                            }
                        }
                    }
                }
                else
                {
                    foreach (var bone in BonesHierarchy)
                    {
                        foreach (var sprite in bone.AttachedSprites)
                            sprite.Value.Texture = null;
                    }
                }
            }
        }
        private ResourceManager _manager;
        private Animation currentAnim;
        private Queue<string> buffer;
        private Chronometer chronometer;

        /// <summary>
        /// Constructor.
        /// </summary>
        public SFDynamicObject()
        {
            oldAnimState = null;
            TransitionTime = Time.Zero;
            buffer = new Queue<string>();
            BonesHierarchy = new List<Bone>();
            MasterBones = new List<Bone>();
            Animations = new List<Animation>();
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
                if (bone.AttachedSprites != null)
                {
                    foreach (var sprite in bone.AttachedSprites)
                    {
                        var rect = tr.TransformRect(sprite.Value.GetGlobalBounds());
                        rect.Width += rect.Left;
                        rect.Height += rect.Top;

                        result.Left = Utilities.Min(result.Left, rect.Left);
                        result.Top = Utilities.Min(result.Top, rect.Top);
                        result.Width = Utilities.Max(result.Width, rect.Width);
                        result.Height = Utilities.Max(result.Height, rect.Height);

                    }
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
        /// <param name="animName">Name of the animation to load.</param>
        /// <param name="reset">Reset the chronometer.</param>
        /// <param name="queue">Queue containing the following animations to play once the current is finished.</param>
        public void LoadAnimation(string animName, bool reset = true, params string[] queue)
        {
            if (currentAnim != null)
                oldAnimState = new Dictionary<Bone, Transformable>(transforms);
            if (Animations == null)
                throw new Exception("No animations provided");
            if (queue != null)
                buffer = new Queue<string>(queue);
            else
                buffer.Clear();
            if (animName == null)
                currentAnim = null;
            else
            {
                foreach (var item in Animations)
                {
                    if (item.Name == animName)
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
                throw new Exception("No animation named \"" + animName + "\"");
            }
        }
        /// <summary>
        /// Resets the positon of the object, making it in the default position
        /// </summary>
        public void ResetAnimation()
        {
            transforms = new Dictionary<Bone, Transformable>();
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
                    if (currentAnim.Bones != null && currentAnim.Bones.Contains(new Couple<string, List<Animation.Key>>() { Key = bone.Name }))
                    {
                        if (currentAnim.Bones.First((b) => b.Key == bone.Name).Value != null && currentAnim.Bones.First((b) => b.Key == bone.Name).Value.Count() == 0)
                        {
                            transforms[bone] = new Transformable();
                            continue;
                        }
                        List<Animation.Key> states = null;
                        try
                        {
                            var tmp = currentAnim.Bones.First((b) => b.Key == bone.Name);
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

                            tr.Position = Utilities.Interpolation(posPerc, first.Transform.Position, second.Transform.Position);
                            tr.Scale = Utilities.Interpolation(scaPerc, first.Transform.Scale, second.Transform.Scale);
                            tr.Rotation = Utilities.Interpolation(rotPerc, first.Transform.Rotation, second.Transform.Rotation);
                            tr.Origin = Utilities.Interpolation(oriPerc, first.Transform.Origin, second.Transform.Origin);
                            bone.Opacity = (byte)Utilities.Interpolation(opaPerc, (float)first.Opacity, second.Opacity);
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
        /// Chronometer of the animation. Need to be set to animate.
        /// </summary>
        public Chronometer Chronometer
        {
            private get => chronometer;
            set
            {
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
        /// Loads an object from a file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        public void LoadFromFile(string path)
        {
            var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            try
            {
                LoadFromStream(stream);
            }
            catch (Exception e)
            {
                stream.Close();
                throw new Exception("Unable to load from the file", e);
            }
            stream.Close();
        }
        internal Bone OperateBone(BoneJSON bone)
        {
            Bone result = new Bone();
            result.Name = bone.Name;
            OperateTransform(result, bone.Transform);
            if (bone.Sprites == null)
                result.AttachedSprites = null;
            else
            {
                result.AttachedSprites = new List<Couple<string, RectangleShape>>();
                foreach (var item in bone.Sprites)
                {
                    RectangleShape tmp2 = new RectangleShape()
                    {
                        Size = item.Size,
                        FillColor = item.FillColor,
                        OutlineColor = item.OutlineColor,
                        OutlineThickness = item.OutlineThickness,
                        TextureRect = item.TextureRect
                    };
                    OperateTransform(tmp2, item.Transform);
                    Couple<string, RectangleShape> tmp = new Couple<string, RectangleShape>(item.TextureID, tmp2);
                    result.AttachedSprites.Add(tmp);
                }
            }
            return result;
        }
        internal void OperateTransform(Transformable tr, TransformJSON trjson)
        {
            tr.Position = trjson.Position;
            tr.Origin = trjson.Origin;
            tr.Scale = trjson.Scale;
            tr.Rotation = trjson.Rotation;
        }
        /// <summary>
        /// Reload the manager. If one of his texture which is used in this object has been changed, it will take effect.
        /// </summary>
        public void ReloadManager() => Manager = Manager;
        /// <summary>
        /// Loads an object from a stream.
        /// </summary>
        /// <param name="stream">stream.</param>
        public void LoadFromStream(System.IO.Stream stream)
        {
            const string WrongFile = "Wrong data type or corrupted data";
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanRead)
                throw new Exception("Can't read from the stream");
            BonesHierarchy.Clear();
            MasterBones.Clear();
            Animations.Clear();
            try
            {
                FormatJSON input;
                {
                    var sr = new System.IO.StreamReader(stream, Encoding.Unicode);
                    var deser = new Newtonsoft.Json.JsonSerializer();
                    input = deser.Deserialize<FormatJSON>(new Newtonsoft.Json.JsonTextReader(sr));
                }
                if (input.Hierarchy != null)
                {
                    foreach (var item in input.Hierarchy)
                    {
                        BonesHierarchy.Add(OperateBone(item));
                    }
                    foreach (var item in input.Hierarchy)
                    {
                        if (item.Children != null)
                        {
                            Bone bone = BonesHierarchy.Find((b) => b.Name == item.Name);
                            bone.ChildBones = new List<Bone>();
                            foreach (var child in item.Children)
                            {
                                bone.ChildBones.Add(BonesHierarchy.Find((b) => b.Name == child));
                            }
                        }
                    }
                }
                if (input.Masters != null)
                {
                    foreach (var item in input.Masters)
                    {
                        MasterBones.Add(BonesHierarchy.Find((b) => b.Name == item));
                    }
                }
                if (input.Animations != null)
                {
                    foreach (var item1 in input.Animations)
                    {
                        Animation tmp1 = new Animation();
                        tmp1.Name = item1.Name;
                        tmp1.Duration = Time.FromMicroseconds(item1.Duration);
                        tmp1.Bones = new List<Couple<string, List<Animation.Key>>>();
                        if (item1.Bones != null)
                        {
                            foreach (var item2 in item1.Bones)
                            {
                                Couple<string, List<Animation.Key>> tmp2 = new Couple<string, List<Animation.Key>>();
                                tmp2.Key = item2.BoneName;
                                tmp2.Value = new List<Animation.Key>();
                                if (item2.Keys != null)
                                {
                                    foreach (var item3 in item2.Keys)
                                    {
                                        Animation.Key tmp3 = new Animation.Key();
                                        tmp3.Transform = new Transformable();
                                        tmp3.Position = Time.FromMicroseconds(item3.Position);
                                        OperateTransform(tmp3.Transform, item3.Transform);
                                        tmp3.Opacity = item3.Opacity;
                                        tmp3.PosFctCoeff = item3.PosCoeff;
                                        tmp3.PosFunction = (Animation.Key.Fct)item3.PosFunction;
                                        tmp3.OriginFctCoeff = item3.OriCoeff;
                                        tmp3.OriginFunction = (Animation.Key.Fct)item3.OriFunction;
                                        tmp3.ScaleFctCoeff = item3.ScaCoeff;
                                        tmp3.ScaleFunction = (Animation.Key.Fct)item3.ScaFunction;
                                        tmp3.RotFctCoeff = item3.RotCoeff;
                                        tmp3.RotFunction = (Animation.Key.Fct)item3.RotFunction;
                                        tmp3.OpacityFctCoeff = item3.OpaCoeff;
                                        tmp3.OpacityFunction = (Animation.Key.Fct)item3.OpaFunction;
                                        tmp2.Value.Add(tmp3);
                                    }
                                }
                                tmp1.Bones.Add(tmp2);
                            }
                        }
                        Animations.Add(tmp1);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception(WrongFile, e);
            }
        }
        /// <summary>
        /// Loads an object from the memory.
        /// </summary>
        /// <param name="buffer">bytes in the memory.</param>
        public void LoadFromMemory(byte[] buffer)
        {
            var stream = new System.IO.MemoryStream(buffer);
            try
            {
                LoadFromStream(stream);
            }
            catch (Exception e)
            {
                stream.Close();
                throw new Exception("Unable to load from the memory", e);
            }
        }
        internal BoneJSON OperateBone(Bone bone)
        {
            BoneJSON result = new BoneJSON();
            result.Name = bone.Name;
            result.Transform = OperateTransform(bone);
            if (bone.AttachedSprites == null)
                result.Sprites = null;
            else
            {
                var l = new List<SpriteJSON>();
                foreach (var item in bone.AttachedSprites)
                {
                    SpriteJSON tmp1 = new SpriteJSON();
                    tmp1.FillColor = item.Value.FillColor;
                    tmp1.OutlineColor = item.Value.FillColor;
                    tmp1.OutlineThickness = item.Value.OutlineThickness;
                    tmp1.Size = item.Value.Size;
                    tmp1.TextureID = item.Key;
                    tmp1.TextureRect = item.Value.TextureRect;
                    tmp1.Transform = OperateTransform(item.Value);
                    l.Add(tmp1);
                }
                result.Sprites = l.ToArray();
            }
            if (bone.ChildBones == null)
                result.Children = null;
            else
            {
                var l = new List<string>();
                foreach (var item in bone.ChildBones)
                {
                    l.Add(item.Name);
                }
                result.Children = l.ToArray();
            }
            return result;
        }
        internal TransformJSON OperateTransform(Transformable tr)
        {
            return new TransformJSON
            {
                Position = tr.Position,
                Scale = tr.Scale,
                Origin = tr.Origin,
                Rotation = tr.Rotation
            };
        }
        /// <summary>
        /// Saves the object to a stream.
        /// </summary>
        /// <param name="stream"></param>
        public void SaveToStream(System.IO.Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
            if (!stream.CanWrite)
                throw new Exception("Can't write in the stream");
            try
            {
                FormatJSON result = new FormatJSON();

                if (BonesHierarchy == null)
                    result.Hierarchy = null;
                else
                {
                    var l = new List<BoneJSON>();
                    foreach (var item in BonesHierarchy)
                    {
                        l.Add(OperateBone(item));
                    }
                    result.Hierarchy = l.ToArray();
                }
                if (MasterBones == null)
                    result.Masters = null;
                else
                {
                    var l = new List<string>();
                    foreach (var item in MasterBones)
                    {
                        l.Add(item.Name);
                    }
                    result.Masters = l.ToArray();
                }
                if (Animations == null)
                    result.Animations = null;
                else
                {
                    var l = new List<AnimationJSON>();
                    foreach (var item in Animations)
                    {
                        AnimationJSON tmp1 = new AnimationJSON();
                        tmp1.Name = item.Name;
                        tmp1.Duration = item.Duration.AsMicroseconds();
                        if (item.Bones == null)
                            tmp1.Bones = null;
                        else
                        {
                            var l2 = new List<AnimatedBoneJSON>();
                            foreach (var item2 in item.Bones)
                            {
                                AnimatedBoneJSON tmp2 = new AnimatedBoneJSON();
                                tmp2.BoneName = item2.Key;
                                if (item2.Value == null)
                                    tmp2.Keys = null;
                                else
                                {
                                    var l3 = new List<KeyJSON>();
                                    foreach (var item3 in item2.Value)
                                    {
                                        KeyJSON tmp3 = new KeyJSON();
                                        tmp3.Position = item3.Position.AsMicroseconds();
                                        tmp3.Transform = OperateTransform(item3.Transform);
                                        tmp3.PosCoeff = item3.PosFctCoeff;
                                        tmp3.PosFunction = (int)item3.PosFunction;
                                        tmp3.OriCoeff = item3.OriginFctCoeff;
                                        tmp3.OriFunction = (int)item3.OriginFunction;
                                        tmp3.ScaCoeff = item3.ScaleFctCoeff;
                                        tmp3.ScaFunction = (int)item3.ScaleFunction;
                                        tmp3.RotCoeff = item3.RotFctCoeff;
                                        tmp3.RotFunction = (int)item3.RotFunction;
                                        tmp3.OpaCoeff = item3.OpacityFctCoeff;
                                        tmp3.OpaFunction = (int)item3.OpacityFunction;
                                        tmp3.Opacity = item3.Opacity;
                                        l3.Add(tmp3);
                                    }
                                    tmp2.Keys = l3.ToArray();
                                }
                                l2.Add(tmp2);
                            }
                            tmp1.Bones = l2.ToArray();
                        }
                        l.Add(tmp1);
                    }
                    result.Animations = l.ToArray();
                }
                var serial = new Newtonsoft.Json.JsonSerializer();
                serial.Formatting = Newtonsoft.Json.Formatting.Indented;
                var sw = new System.IO.StreamWriter(stream, Encoding.Unicode);
                serial.Serialize(sw, result);
                sw.Flush();
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
            /// Coefficient of the position function.
            /// </summary>
            public float PosFctCoeff { get; set; }
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
            /// How the position will be calculated.
            /// </summary>
            public Fct OpacityFunction { get; set; }
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
                Opacity = 255;
            }
        }
        /// <summary>
        /// A double array of all the keys of the animation, sorted by bones.
        /// </summary>
        public List<Couple<string, List<Key>>> Bones { get; set; }
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
            Bones = new List<Couple<string, List<Key>>>();
        }
    }
    /// <summary>
    /// A basic bone.
    /// </summary>
    public class Bone : Transformable
    {
        /// <summary>
        /// Opacity of the bone. Can only be changed using keys and animations.
        /// </summary>
        public byte Opacity { get; internal set; }
        /// <summary>
        /// This bone should draw its temporary sprites before its attached sprites ?
        /// </summary>
        public bool DrawTempSpritesFirst { get; set; }
        /// <summary>
        /// The childs of the bone. They will be relative to their parent.
        /// </summary>
        public List<Bone> ChildBones { get; set; }
        /// <summary>
        /// The absolute transforms of the bone. For internal uses only.
        /// </summary>
        public Transform ComputedTransform { get; internal set; }
        /// <summary>
        /// The list of sprites affected by the changes of the bone. Be careful of the order (the order of drawing). The string is the name of the texture in the texture manager.
        /// </summary>
        public List<Couple<string, RectangleShape>> AttachedSprites { get; set; }
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
            DrawTempSpritesFirst = false;
            TemporarySprites = new List<Drawable>();
            ChildBones = new List<Bone>();
            AttachedSprites = new List<Couple<string, RectangleShape>>();
            Name = null;
            Opacity = 255;
        }
    }
}
