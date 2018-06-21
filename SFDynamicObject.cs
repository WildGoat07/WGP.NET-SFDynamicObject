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
                if (bone.AttachedSprites != null)
                {
                    foreach (var sprite in bone.AttachedSprites)
                    {
                        RenderStates st = new RenderStates(states);
                        st.Transform *= bone.ComputedTransform;
                        target.Draw(sprite.Value, st);
                    }
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
            BonesHierarchy = null;
            MasterBones = null;
            Animations = null;
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
        public void LoadAnimation(string animName, bool reset = true)
        {
            if (Animations == null)
                throw new Exception("No animations provided");
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
                                    var tmp = statList.Value.ToArray();
                                    Array.Sort(tmp);
                                    statList.Value.Clear();
                                    statList.Value.AddRange(tmp);
                                }
                            }
                        }
                        foreach (var bone in BonesHierarchy)
                            transforms.Add(bone, default);
                        if (Chronometer != null && reset)
                            Chronometer.Restart();
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
                    Chronometer.Restart();
                Time currentTime = Chronometer.ElapsedTime;

                foreach (var bone in BonesHierarchy)
                {
                    if (currentAnim.Bones != null && currentAnim.Bones.Contains(new Animation.Couple<string, List<Animation.Key>>() { Key = bone.Name }))
                    {
                        if (currentAnim.Bones.First((b) => b.Key == bone.Name).Value != null && currentAnim.Bones.First((b) => b.Key == bone.Name).Value.Count() == 0)
                        {
                            transforms[bone] = new Transformable();
                            continue;
                        }
                        List<Animation.Key> states;
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
                            float perc = Utilities.Percent(currentTime.AsSeconds(), first.Position.AsSeconds(), second.Position.AsSeconds());
                            tr.Position = Utilities.Interpolation(perc, first.Transform.Position, second.Transform.Position);
                            tr.Scale = Utilities.Interpolation(perc, first.Transform.Scale, second.Transform.Scale);
                            tr.Rotation = Utilities.Interpolation(perc, first.Transform.Rotation, second.Transform.Rotation);
                            tr.Origin = Utilities.Interpolation(perc, first.Transform.Origin, second.Transform.Origin);
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
        public Chronometer Chronometer { get; set; }
        /// <summary>
        /// Loads an object from a file.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        /// <param name="manager">Used manager.</param>
        public void LoadFromFile(string path, ResourceManager manager = null)
        {
            var stream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            try
            {
                LoadFromStream(stream, manager);
            }
            catch(Exception e)
            {
                stream.Close();
                throw new Exception("Unable to load from the file", e);
            }
            stream.Close();
        }
        /// <summary>
        /// Loads an object from a stream.
        /// </summary>
        /// <param name="stream">stream.</param>
        /// <param name="manager">Used manager.</param>
        public void LoadFromStream(System.IO.Stream stream, ResourceManager manager = null)
        {
            const string WrongFile = "Wrong data type or corrupted data";
            try
            {
                {
                    var bytes = new byte[4];
                    stream.Read(bytes, 0, 4);
                    if (bytes[0] != 'W' ||
                        bytes[1] != 'G' ||
                        bytes[2] != 'D' ||
                        bytes[3] != 'O')
                        throw new Exception(WrongFile);
                }
                int numBones;
                {
                    var bytes = new byte[sizeof(int)];
                    if (stream.Read(bytes, 0, bytes.Length) == 0)
                        throw new Exception(WrongFile);
                    numBones = BitConverter.ToInt32(bytes, 0);
                }
                if (numBones > 0)
                    BonesHierarchy = new List<Bone>();
                for (int i = 0; i < numBones; i++)
                {
                    Bone bone = new Bone();
                    BonesHierarchy.Add(bone);
                    int sizeName;
                    var sizeB = new byte[sizeof(int)];
                    if (stream.Read(sizeB, 0, sizeB.Length) == 0)
                        throw new Exception(WrongFile);
                    sizeName = BitConverter.ToInt32(sizeB, 0);
                    var nameB = new byte[sizeName];
                    if (stream.Read(nameB, 0, nameB.Length) == 0 && sizeName > 0)
                        throw new Exception(WrongFile);
                    bone.Name = ByteToString(nameB, nameB.Length);

                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        bone.Position = vec;
                    }
                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        bone.Origin = vec;
                    }
                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        bone.Scale = vec;
                    }
                    {
                        float rot;
                        var rotB = new byte[sizeof(float)];
                        if (stream.Read(rotB, 0, rotB.Length) == 0)
                            throw new Exception(WrongFile);
                        rot = BitConverter.ToSingle(rotB, 0);
                        bone.Rotation = rot;
                    }
                    int numbChild;
                    {
                        var bytes = new byte[sizeof(int)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        numbChild = BitConverter.ToInt32(bytes, 0);
                    }
                    if (numbChild > 0)
                        bone.ChildBones = new List<Bone>();
                    for (int j = 0;j<numbChild;j++)
                    {
                        var tmp = new Bone();
                        int sizeNameChild;
                        var bytes = new byte[sizeof(int)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        sizeNameChild = BitConverter.ToInt32(bytes, 0);
                        bytes = new byte[sizeNameChild];
                        if (stream.Read(bytes, 0, bytes.Length) == 0 && sizeNameChild > 0)
                            throw new Exception(WrongFile);
                        tmp.Name = ByteToString(bytes, bytes.Length);
                        bone.ChildBones.Add(tmp);
                    }
                }
                foreach (var bone in BonesHierarchy)
                {
                    var listChild = new List<Bone>();
                    if (bone.ChildBones != null)
                    {
                        foreach (var child in bone.ChildBones)
                        {
                            Bone match = null;
                            foreach (var originalBone in BonesHierarchy)
                            {
                                if (originalBone.Name == child.Name)
                                {
                                    match = originalBone;
                                    break;
                                }
                            }
                            if (match == null)
                                throw new Exception("No bone named \"" + child.Name + "\" as child of the bone named \"" + bone.Name + "\"");
                            listChild.Add(match);
                        }
                    }
                    bone.ChildBones = listChild;
                }
                int numMaster;
                {
                    var bytes = new byte[sizeof(int)];
                    if (stream.Read(bytes, 0, bytes.Length) == 0)
                        throw new Exception(WrongFile);
                    numMaster = BitConverter.ToInt32(bytes, 0);
                }
                if (numMaster > 0)
                    MasterBones = new List<Bone>();
                for (int i = 0; i < numMaster; i++)
                {
                    int sizeName;
                    var bytes = new byte[sizeof(int)];
                    if (stream.Read(bytes, 0, bytes.Length) == 0)
                        throw new Exception(WrongFile);
                    sizeName = BitConverter.ToInt32(bytes, 0);
                    string name;
                    bytes = new byte[sizeName];
                    if (stream.Read(bytes, 0, bytes.Length) == 0 && sizeName > 0)
                        throw new Exception(WrongFile);
                    name = ByteToString(bytes, bytes.Length);
                    Bone match = null;
                    foreach (var originalBone in BonesHierarchy)
                    {
                        if (originalBone.Name == name)
                        {
                            match = originalBone;
                            break;
                        }
                    }
                    if (match == null)
                        throw new Exception("No bone named \"" + name + "\" as master bone");
                    MasterBones.Add(match);
                }
                int numAnim;
                {
                    var bytes = new byte[sizeof(int)];
                    if (stream.Read(bytes, 0, bytes.Length) == 0)
                        throw new Exception(WrongFile);
                    numAnim = BitConverter.ToInt32(bytes, 0);
                }
                if (numAnim > 0)
                    Animations = new List<Animation>();
                for (int i = 0; i < numAnim; i++)
                {
                    Animation tmp = new Animation();
                    {
                        int sizeName;
                        var bytes = new byte[sizeof(int)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        sizeName = BitConverter.ToInt32(bytes, 0);
                        bytes = new byte[sizeName];
                        if (stream.Read(bytes, 0, bytes.Length) == 0 && sizeName > 0)
                            throw new Exception(WrongFile);
                        tmp.Name = ByteToString(bytes, bytes.Length);
                    }
                    {
                        var bytes = new byte[sizeof(long)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        tmp.Duration = Time.FromMicroseconds(BitConverter.ToInt64(bytes, 0));
                    }
                    {
                        int numBone;
                        var bytes = new byte[sizeof(int)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        numBone = BitConverter.ToInt32(bytes, 0);
                        if (numBone > 0)
                            tmp.Bones = new List<Animation.Couple<string, List<Animation.Key>>>();
                        for (int j = 0;j<numBone;j++)
                        {
                            string boneName;
                            {
                                int sizeName;
                                bytes = new byte[sizeof(int)];
                                if (stream.Read(bytes, 0, bytes.Length) == 0)
                                    throw new Exception(WrongFile);
                                sizeName = BitConverter.ToInt32(bytes, 0);
                                bytes = new byte[sizeName];
                                if (stream.Read(bytes, 0, bytes.Length) == 0 && sizeName > 0)
                                    throw new Exception(WrongFile);
                                boneName = ByteToString(bytes, bytes.Length);
                            }
                            int numKeys;
                            {
                                bytes = new byte[sizeof(int)];
                                if (stream.Read(bytes, 0, bytes.Length) == 0)
                                    throw new Exception(WrongFile);
                                numKeys = BitConverter.ToInt32(bytes, 0);
                            }
                            var keys = new List<Animation.Key>();
                            for (int k = 0;k<numKeys;k++)
                            {
                                Animation.Key tmpKey = new Animation.Key();
                                tmpKey.Transform = new Transformable();
                                {
                                    bytes = new byte[sizeof(long)];
                                    if (stream.Read(bytes, 0, bytes.Length) == 0)
                                        throw new Exception(WrongFile);
                                    tmpKey.Position = Time.FromMicroseconds(BitConverter.ToInt64(bytes, 0));
                                }
                                {
                                    var vec = new Vector2f();
                                    var vecX = new byte[sizeof(float)];
                                    var vecY = new byte[sizeof(float)];
                                    if (stream.Read(vecX, 0, vecX.Length) == 0)
                                        throw new Exception(WrongFile);
                                    if (stream.Read(vecY, 0, vecY.Length) == 0)
                                        throw new Exception(WrongFile);
                                    vec.X = BitConverter.ToSingle(vecX, 0);
                                    vec.Y = BitConverter.ToSingle(vecY, 0);
                                    tmpKey.Transform.Position = vec;
                                }
                                {
                                    var vec = new Vector2f();
                                    var vecX = new byte[sizeof(float)];
                                    var vecY = new byte[sizeof(float)];
                                    if (stream.Read(vecX, 0, vecX.Length) == 0)
                                        throw new Exception(WrongFile);
                                    if (stream.Read(vecY, 0, vecY.Length) == 0)
                                        throw new Exception(WrongFile);
                                    vec.X = BitConverter.ToSingle(vecX, 0);
                                    vec.Y = BitConverter.ToSingle(vecY, 0);
                                    tmpKey.Transform.Origin = vec;
                                }
                                {
                                    var vec = new Vector2f();
                                    var vecX = new byte[sizeof(float)];
                                    var vecY = new byte[sizeof(float)];
                                    if (stream.Read(vecX, 0, vecX.Length) == 0)
                                        throw new Exception(WrongFile);
                                    if (stream.Read(vecY, 0, vecY.Length) == 0)
                                        throw new Exception(WrongFile);
                                    vec.X = BitConverter.ToSingle(vecX, 0);
                                    vec.Y = BitConverter.ToSingle(vecY, 0);
                                    tmpKey.Transform.Scale = vec;
                                }
                                {
                                    float rot;
                                    var rotB = new byte[sizeof(float)];
                                    if (stream.Read(rotB, 0, rotB.Length) == 0)
                                        throw new Exception(WrongFile);
                                    rot = BitConverter.ToSingle(rotB, 0);
                                    tmpKey.Transform.Rotation = rot;
                                }
                                keys.Add(tmpKey);
                            }
                            tmp.Bones.Add(new Animation.Couple<string, List<Animation.Key>>() { Key = boneName, Value = keys });
                        }
                    }
                    Animations.Add(tmp);
                }
                int numSprite;
                {
                    var bytes = new byte[sizeof(int)];
                    if (stream.Read(bytes, 0, bytes.Length) == 0)
                        throw new Exception(WrongFile);
                    numSprite = BitConverter.ToInt32(bytes, 0);
                }
                for (int i = 0;i<numSprite;i++)
                {
                    string boneName;
                    string textureName;
                    Bone affectedBone = null;
                    RectangleShape shape = new RectangleShape();
                    {
                        int sizeName;
                        var bytes = new byte[sizeof(int)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        sizeName = BitConverter.ToInt32(bytes, 0);
                        bytes = new byte[sizeName];
                        if (stream.Read(bytes, 0, bytes.Length) == 0 && sizeName > 0)
                            throw new Exception(WrongFile);
                        boneName = ByteToString(bytes, bytes.Length);
                    }
                    {
                        int sizeName;
                        var bytes = new byte[sizeof(int)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        sizeName = BitConverter.ToInt32(bytes, 0);
                        bytes = new byte[sizeName];
                        if (stream.Read(bytes, 0, bytes.Length) == 0 && sizeName > 0)
                            throw new Exception(WrongFile);
                        textureName = ByteToString(bytes, bytes.Length);
                    }
                    foreach (var bone in BonesHierarchy)
                    {
                        if (bone.Name == boneName)
                        {
                            affectedBone = bone;
                            break;
                        }
                    }
                    if (affectedBone == null)
                        throw new Exception("No bone named \"" + boneName + "\" found for the sprite using the texture \"" + textureName + "\"");
                    if (affectedBone.AttachedSprites == null)
                        affectedBone.AttachedSprites = new List<KeyValuePair<string, RectangleShape>>();
                    if (manager != null && textureName.Length > 0)
                    {
                        if (manager[textureName].Data is Texture)
                            shape.Texture = (Texture)manager[textureName].Data;
                        else
                            throw new Exception("\"" + textureName + "\"is a " + textureName.GetType() + ", not a Texture");
                    }
                    else if (manager == null && textureName.Length > 0)
                        throw new Exception("The provided manager doesn't contain \"" + textureName + "\" or is null");
                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        shape.Position = vec;
                    }
                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        shape.Origin = vec;
                    }
                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        shape.Scale = vec;
                    }
                    {
                        float rot;
                        var rotB = new byte[sizeof(float)];
                        if (stream.Read(rotB, 0, rotB.Length) == 0)
                            throw new Exception(WrongFile);
                        rot = BitConverter.ToSingle(rotB, 0);
                        shape.Rotation = rot;
                    }
                    {
                        var vec = new Vector2f();
                        var vecX = new byte[sizeof(float)];
                        var vecY = new byte[sizeof(float)];
                        if (stream.Read(vecX, 0, vecX.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(vecY, 0, vecY.Length) == 0)
                            throw new Exception(WrongFile);
                        vec.X = BitConverter.ToSingle(vecX, 0);
                        vec.Y = BitConverter.ToSingle(vecY, 0);
                        shape.Size = vec;
                    }
                    {
                        var rect = new IntRect();
                        var l = new byte[sizeof(int)];
                        var t = new byte[sizeof(int)];
                        var w = new byte[sizeof(int)];
                        var h = new byte[sizeof(int)];
                        if (stream.Read(l, 0, l.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(t, 0, t.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(w, 0, w.Length) == 0)
                            throw new Exception(WrongFile);
                        if (stream.Read(h, 0, h.Length) == 0)
                            throw new Exception(WrongFile);
                        rect.Left = BitConverter.ToInt32(l, 0);
                        rect.Top = BitConverter.ToInt32(t, 0);
                        rect.Width = BitConverter.ToInt32(w, 0);
                        rect.Height = BitConverter.ToInt32(h, 0);
                        shape.TextureRect = rect;
                    }
                    {
                        var color = new Color();
                        var bytes = new byte[4 * sizeof(byte)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        color.R = bytes[0];
                        color.G = bytes[1];
                        color.B = bytes[2];
                        color.A = bytes[3];
                        shape.FillColor = color;
                    }
                    {
                        var color = new Color();
                        var bytes = new byte[4 * sizeof(byte)];
                        if (stream.Read(bytes, 0, bytes.Length) == 0)
                            throw new Exception(WrongFile);
                        color.R = bytes[0];
                        color.G = bytes[1];
                        color.B = bytes[2];
                        color.A = bytes[3];
                        shape.OutlineColor = color;
                    }
                    {
                        float oT;
                        var oTB = new byte[sizeof(float)];
                        if (stream.Read(oTB, 0, oTB.Length) == 0)
                            throw new Exception(WrongFile);
                        oT = BitConverter.ToSingle(oTB, 0);
                        shape.OutlineThickness = oT;
                    }
                    affectedBone.AttachedSprites.Add(new KeyValuePair<string, RectangleShape>(textureName, shape));
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to load " + this, e);
            }
        }
        /// <summary>
        /// Loads an object from the memory.
        /// </summary>
        /// <param name="buffer">bytes in the memory.</param>
        /// <param name="manager">Used manager.</param>
        public void LoadFromMemory(byte[] buffer, ResourceManager manager = null)
        {
            var stream = new System.IO.MemoryStream(buffer);
            try
            {
                LoadFromStream(stream, manager);
            }
            catch(Exception e)
            {
                stream.Close();
                throw new Exception("Unable to load from the memory", e);
            }
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

                    byte[] childNumB;
                    if (bone.ChildBones != null)
                        childNumB = BitConverter.GetBytes((int)bone.ChildBones.Count);
                    else
                        childNumB = BitConverter.GetBytes((int)0);
                    stream.Write(childNumB, 0, childNumB.Length);

                    if (bone.ChildBones != null)
                    {
                        foreach (var child in bone.ChildBones)
                        {
                            var childNameB = StringToByte(child.Name);
                            var childSizeB = BitConverter.GetBytes((int)childNameB.Length);
                            stream.Write(childSizeB, 0, childSizeB.Length);
                            stream.Write(childNameB, 0, childNameB.Length);
                        }
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
                if (Animations != null)
                {
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
                                var bytes = BitConverter.GetBytes((int)boneKeys.Value.Count);
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
                    }
                }
                else
                {
                    var bytes = BitConverter.GetBytes((int)0);
                    stream.Write(bytes, 0, bytes.Length);
                }
                int numSprite = 0;
                foreach (var bone in BonesHierarchy)
                {
                    if (bone.AttachedSprites != null)
                        numSprite += bone.AttachedSprites.Count;
                }
                {
                    var bytes = BitConverter.GetBytes((int)numSprite);
                    stream.Write(bytes, 0, bytes.Length);
                }
                foreach (var bone in BonesHierarchy)
                {
                    if (bone.AttachedSprites != null)
                    {
                        foreach (var sprite in bone.AttachedSprites)
                        {
                            {
                                var nameB = StringToByte(bone.Name);
                                var sizeB = BitConverter.GetBytes((int)nameB.Length);
                                stream.Write(sizeB, 0, sizeB.Length);
                                stream.Write(nameB, 0, nameB.Length);
                            }
                            if (sprite.Key != null && sprite.Key != "")
                            {
                                var nameB = StringToByte(sprite.Key);
                                var sizeB = BitConverter.GetBytes((int)nameB.Length);
                                stream.Write(sizeB, 0, sizeB.Length);
                                stream.Write(nameB, 0, nameB.Length);
                            }
                            else
                            {
                                var bytes = BitConverter.GetBytes((int)0);
                                stream.Write(bytes, 0, bytes.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)sprite.Value.Position.X);
                                var vecY = BitConverter.GetBytes((float)sprite.Value.Position.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)sprite.Value.Origin.X);
                                var vecY = BitConverter.GetBytes((float)sprite.Value.Origin.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)sprite.Value.Scale.X);
                                var vecY = BitConverter.GetBytes((float)sprite.Value.Scale.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var rot = BitConverter.GetBytes((float)sprite.Value.Rotation);
                                stream.Write(rot, 0, rot.Length);
                            }
                            {
                                var vecX = BitConverter.GetBytes((float)sprite.Value.Size.X);
                                var vecY = BitConverter.GetBytes((float)sprite.Value.Size.Y);
                                stream.Write(vecX, 0, vecX.Length);
                                stream.Write(vecY, 0, vecY.Length);
                            }
                            {
                                var vecL = BitConverter.GetBytes((int)sprite.Value.TextureRect.Left);
                                var vecT = BitConverter.GetBytes((int)sprite.Value.TextureRect.Top);
                                var vecW = BitConverter.GetBytes((int)sprite.Value.TextureRect.Width);
                                var vecH = BitConverter.GetBytes((int)sprite.Value.TextureRect.Height);
                                stream.Write(vecL, 0, vecL.Length);
                                stream.Write(vecT, 0, vecT.Length);
                                stream.Write(vecW, 0, vecW.Length);
                                stream.Write(vecH, 0, vecH.Length);
                            }
                            {
                                var color = new byte[] { sprite.Value.FillColor.R, sprite.Value.FillColor.G, sprite.Value.FillColor.B, sprite.Value.FillColor.A };
                                stream.Write(color, 0, color.Length);
                            }
                            {
                                var color = new byte[] { sprite.Value.OutlineColor.R, sprite.Value.OutlineColor.G, sprite.Value.OutlineColor.B, sprite.Value.OutlineColor.A };
                                stream.Write(color, 0, color.Length);
                            }
                            {
                                var bytes = BitConverter.GetBytes((float)sprite.Value.OutlineThickness);
                                stream.Write(bytes, 0, bytes.Length);
                            }
                        }
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
        public class Couple<T, U>: IEquatable<Couple<T, U>> where T : IEquatable<T>
        {
            public T Key { get; set; }
            public U Value { get; set; }

            public bool Equals(Couple<T, U> other)
            {
                return Key.Equals(other.Key);
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
        /// <summary>
        /// The absolute transforms of the bone. For internal uses only.
        /// </summary>
        public Transform ComputedTransform { get; internal set; }
        /// <summary>
        /// The list of sprites affected by the changes of the bone. Be careful of the order (the order of drawing). The string is the name of the texture in the texture manager.
        /// </summary>
        public ICollection<KeyValuePair<string, RectangleShape>> AttachedSprites { get; set; }
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
