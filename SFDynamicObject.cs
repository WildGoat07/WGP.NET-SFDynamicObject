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
                foreach(var sprite in bone.AttachedSprites)
                {
                    RenderStates st = new RenderStates(states);
                    st.Transform *= bone.ComputedTransform;
                    target.Draw(sprite, st);
                }
            }
        }
        private Dictionary<Bone, Transformable> transforms;
        /// <summary>
        /// The hierarchy of the bones. All bones must be here. The order in the hierarchy will be the order of drawing the sprites from the bones.
        /// </summary>
        public ICollection<Bone> BonesHierarchy { get; set; }
        /// <summary>
        /// The list of the master bones. All child bones must NOT be referenced here.
        /// </summary>
        public ICollection<Bone> MasterBones { get; set; }
        /// <summary>
        /// Animations available for the bones.
        /// </summary>
        public ICollection<Animation> Animations { get; set; }
        private Animation currentAnim;
        /// <summary>
        /// Constructor.
        /// </summary>
        public SFDynamicObject()
        {
            transforms = null;
            BonesHierarchy = null;
            MasterBones = null;
            Animations = null;
            currentAnim = null;
        }
        /// <summary>
        /// Loads an animation. If a chronometer is set, it will reset.
        /// </summary>
        /// <param name="animName">Name of the animation to load.</param>
        public void LoadAnimation(string animName)
        {
            if (Animations == null)
                throw new Exception("No animations provided");
            foreach (var item in Animations)
            {
                if (item.Name == animName)
                {
                    currentAnim = item;
                    transforms = new Dictionary<Bone, Transformable>();
                    foreach (var statList in currentAnim.Bones)
                        Array.Sort(statList.Value);
                    foreach (var bone in BonesHierarchy)
                        transforms.Add(bone, default);
                    if (Chronometer != null)
                        Chronometer.Restart();
                    return;
                }
            }
            throw new Exception("No animation named \"" + animName + "\"");
        }
        /// <summary>
        /// Updates the display of the object by adjusting the bones to match the animations. Won't have any effect if there are no chronometer set or no animation loaded.
        /// </summary>
        public void Update()
        {
            if (currentAnim != null && Chronometer != null)
            {
                if (Chronometer.ElapsedTime > currentAnim.Duration)
                    Chronometer.Restart();
                Time currentTime = Chronometer.ElapsedTime;

                foreach (var bone in BonesHierarchy)
                {
                    if (currentAnim.Bones.ContainsKey(bone.Name))
                    {
                        if (currentAnim.Bones[bone.Name].Count() == 0)
                        {
                            transforms[bone] = new Transformable();
                            continue;
                        }
                        ICollection<Animation.Key> states = currentAnim.Bones[bone.Name];
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
                        float perc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                        tr.Position = Utilities.Interpolation(perc, first.Transform.Position, second.Transform.Position);
                        tr.Scale = Utilities.Interpolation(perc, first.Transform.Scale, second.Transform.Scale);
                        tr.Rotation = Utilities.Interpolation(perc, first.Transform.Rotation, second.Transform.Rotation);
                        tr.Origin = Utilities.Interpolation(perc, first.Transform.Origin, second.Transform.Origin);
                        transforms[bone] = tr;
                    }
                    else
                        transforms[bone] = new Transformable();
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
        public Chronometer Chronometer { get; set; }
        public void LoadFromFile(string path)
        {
            var stream = new System.IO.FileStream(path, System.IO.FileMode.Open);
            LoadFromStream(stream);
        }
        public void LoadFromStream(System.IO.Stream stream)
        {
        }
        public void LoadFromMemory(byte[] buffer)
        {
            var stream = new System.IO.MemoryStream(buffer);
            LoadFromStream(stream);
        }
        public void SaveToFile(string path)
        {
            try
            {
                var stream = new System.IO.FileStream(path, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.Write);

                stream.Write(new byte[] { (byte)'W', (byte)'G', (byte)'D', (byte)'O' }, 0, 4);

                {
                    var bytes = BitConverter.GetBytes((int)BonesHierarchy.Count);
                    stream.Write(bytes, 0, bytes.Length);
                }
                foreach (var bone in BonesHierarchy)
                {
                    var nameB = StringToByte(bone.Name);
                    var sizeB = BitConverter.GetBytes((int)nameB.Length);
                    stream.Write(sizeB, 0, sizeB.Length);
                    stream.Write(nameB, 0, nameB.Length);

                    {
                        var vecX = BitConverter.GetBytes((float)bone.Position.X);
                        var vecY = BitConverter.GetBytes((float)bone.Position.Y);
                        stream.Write(vecX, 0, vecX.Length);
                        stream.Write(vecY, 0, vecY.Length);
                    }
                    {
                        var vecX = BitConverter.GetBytes((float)bone.Origin.X);
                        var vecY = BitConverter.GetBytes((float)bone.Origin.Y);
                        stream.Write(vecX, 0, vecX.Length);
                        stream.Write(vecY, 0, vecY.Length);
                    }
                    {
                        var vecX = BitConverter.GetBytes((float)bone.Scale.X);
                        var vecY = BitConverter.GetBytes((float)bone.Scale.Y);
                        stream.Write(vecX, 0, vecX.Length);
                        stream.Write(vecY, 0, vecY.Length);
                    }
                    {
                        var rot = BitConverter.GetBytes((float)bone.Rotation);
                        stream.Write(rot, 0, rot.Length);
                    }

                    var childNumB = BitConverter.GetBytes((int)bone.ChildBones.Count);
                    stream.Write(childNumB, 0, childNumB.Length);

                    foreach (var child in bone.ChildBones)
                    {
                        var childNameB = StringToByte(bone.Name);
                        var childSizeB = BitConverter.GetBytes((int)nameB.Length);
                        stream.Write(sizeB, 0, childSizeB.Length);
                        stream.Write(nameB, 0, childNameB.Length);
                    }
                }
                {
                    var bytes = BitConverter.GetBytes((int)MasterBones.Count);
                    stream.Write(bytes, 0, bytes.Length);
                }
                foreach (var bone in MasterBones)
                {
                    var nameB = StringToByte(bone.Name);
                    var sizeB = BitConverter.GetBytes((int)nameB.Length);
                    stream.Write(sizeB, 0, sizeB.Length);
                    stream.Write(nameB, 0, nameB.Length);
                }
                {
                    var bytes = BitConverter.GetBytes((int)Animations.Count);
                    stream.Write(bytes, 0, bytes.Length);
                }
                foreach (var animation in Animations)
                {
                    {
                        var nameB = StringToByte(animation.Name);
                        var sizeB = BitConverter.GetBytes((int)nameB.Length);
                        stream.Write(sizeB, 0, sizeB.Length);
                        stream.Write(nameB, 0, nameB.Length);
                    }
                    {
                        var bytes = BitConverter.GetBytes((long)animation.Duration.AsMicroseconds());
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    {
                        var bytes = BitConverter.GetBytes((int)animation.Bones.Count);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    foreach (var boneKeys in animation.Bones)
                    {
                        {
                            var nameB = StringToByte(boneKeys.Key);
                            var sizeB = BitConverter.GetBytes((int)nameB.Length);
                            stream.Write(sizeB, 0, sizeB.Length);
                            stream.Write(nameB, 0, nameB.Length);
                        }
                        {
                            var bytes = BitConverter.GetBytes((int)boneKeys.Value.Length);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                        foreach (var key in boneKeys.Value)
                        {
                            {
                                var bytes = BitConverter.GetBytes((long)key.Position.AsMicroseconds());
                                stream.Write(bytes, 0, bytes.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)key.Transform.Position.X);
                                var vecY = BitConverter.GetBytes((float)key.Transform.Position.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)key.Transform.Origin.X);
                                var vecY = BitConverter.GetBytes((float)key.Transform.Origin.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)key.Transform.Scale.X);
                                var vecY = BitConverter.GetBytes((float)key.Transform.Scale.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var rot = BitConverter.GetBytes((float)key.Transform.Rotation);
                                stream.Write(rot, 0, rot.Length);
                            }
                        }
                    }
                    foreach (var bone in BonesHierarchy)
                    {

                    }
                }


                stream.Close();
            }
            catch(Exception e)
            {
                throw new Exception("Failed to save the object to the file", e);
            }
        }
        static internal byte[] StringToByte(string s)
        {
            var result = new byte[s.Length * sizeof(char)];
            int index = 0;
            foreach (var character in s)
            {
                var charB = BitConverter.GetBytes(character);
                foreach (var oct in charB)
                {
                    result[index] = oct;
                    index++;
                }
            }
            return result;
        }
        static internal string ByteToString(byte[] buffer, int count, int startIndex = 0)
        {
            string result = "";
            for (int index = 0;index < count;index+=2)
            {
                var character = BitConverter.ToChar(buffer, startIndex + index);
                result += character;
            }
            return result;
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
        }
        /// <summary>
        /// A double array of all the keys of the animation, sorted by bones.
        /// </summary>
        public Dictionary<string, Key[]> Bones { get; set; }
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
            Bones = null;
        }
    }
    /// <summary>
    /// A basic bone.
    /// </summary>
    public class Bone : Transformable
    {
        /// <summary>
        /// The childs of the bone. They will be relative to their parent.
        /// </summary>
        public ICollection<Bone> ChildBones { get; set; }
        internal Transform ComputedTransform { get; set; }
        /// <summary>
        /// The list of sprites affected by the changes of the bone. Be careful of the order (the order of drawing).
        /// </summary>
        public ICollection<RectangleShape> AttachedSprites { get; set; }
        /// <summary>
        /// The name of the bone. Needed for animations.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Constructor.
        /// </summary>
        public Bone()
        {
            ChildBones = null;
            AttachedSprites = null;
            Name = null;
        }
    }
}
