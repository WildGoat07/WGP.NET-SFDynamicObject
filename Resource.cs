using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// The Resource generate one or multiple texture(s) from one image, to create one texture or an animation by cropping the image.
    /// </summary>
    [Serializable]
    public class Resource : ISerializable
    {
        private bool _repeated;
        private bool _smooth;
        private Texture[] textures;
        /// <summary>
        /// Constructor.
        /// </summary>
        public Resource()
        {
            ID = Guid.NewGuid();
            BaseImage = null;
            textures = null;
            FramesPerSecond = 0;
            FrameSize = default;
            Repeated = false;
            Smooth = false;
        }

        public Resource(SerializationInfo info, StreamingContext context) : this()
        {
            Name = info.GetString("Name");
            ID = new Guid((byte[])info.GetValue("ID", typeof(byte[])));
            Repeated = info.GetBoolean("Repeated");
            Smooth = info.GetBoolean("Smooth");
            {
                var bitmap = (System.Drawing.Bitmap)info.GetValue("Image", typeof(System.Drawing.Bitmap));
                if (bitmap != null)
                    BaseImage = new Image(bitmap);
                FramesPerSecond = info.GetInt32("FramesPerSecond");
                FrameSize = (Vector2i)info.GetValue("FrameSize", typeof(Vector2i));
                FramesPosition = (Vector2i[])info.GetValue("FramesPosition", typeof(Vector2i[]));
            }
            Update();
        }
        /// <summary>
        /// Duration of the animation. It is approximative, since the fps are saved and not the actual duration.
        /// </summary>
        public Time ApproxDuration
        {
            get
            {
                if (textures == null) return Time.Zero;
                else return Time.FromSeconds((float)textures.Length / FramesPerSecond);
            }
            set
            {
                if (textures != null)
                {
                    FramesPerSecond = (int)(textures.Length / value.AsSeconds());
                }
            }
        }
        /// <summary>
        /// Image to create the texture(s) from.
        /// </summary>
        public Image BaseImage { get; private set; }
        /// <summary>
        /// Size of the texture(s).
        /// </summary>
        public Vector2i FrameSize { get; private set; }
        /// <summary>
        /// Frames per second. Not much to say.
        /// </summary>
        public int FramesPerSecond { get; set; }
        /// <summary>
        /// The position of the frames in the image. Every position will create one texture. Most of the time, it will be in some kind of grid.
        /// </summary>
        public Vector2i[] FramesPosition { get; private set; }
        /// <summary>
        /// The internal ID of the resource. For saving purpose.
        /// </summary>
        public Guid ID { get; internal set; }
        /// <summary>
        /// The name of the resource. Works as a description.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Repeated textures, like SFML.Graphics.Texture.Repeated
        /// </summary>
        public bool Repeated
        {
            get => _repeated;
            set
            {
                _repeated = value;
                if (textures != null)
                {
                    foreach (var item in textures)
                    {
                        item.Repeated = _repeated;
                    }
                }
            }
        }
        /// <summary>
        /// Smooth textures, like SFML.Graphics.Texture.Smooth
        /// </summary>
        public bool Smooth
        {
            get => _smooth;
            set
            {
                _smooth = value;
                if (textures != null)
                {
                    foreach (var item in textures)
                    {
                        item.Smooth = _smooth;
                    }
                }
            }
        }
        /// <summary>
        /// Changes FrameSize and FramesPosition to make only one texture covering the whole image.
        /// </summary>
        public void AdaptFrameSize()
        {
            if (BaseImage != null)
                ChangeFrames((Vector2i)BaseImage.Size, new Vector2i());
            else
                ChangeFrames(default, new Vector2i());
        }
        /// <summary>
        /// Used to change the image.
        /// </summary>
        /// <param name="img">New image.</param>
        public void ChangeBaseImage(Image img)
        {
            BaseImage = img;
            Update();
        }
        /// <summary>
        /// Used to change the frames options.
        /// </summary>
        /// <param name="size">FrameSize</param>
        /// <param name="positions">FramesPosition</param>
        public void ChangeFrames(Vector2i size, params Vector2i[] positions) => ChangeFrames(size, positions.AsEnumerable());
        /// <summary>
        /// Used to change the frames options.
        /// </summary>
        /// <param name="size">FrameSize</param>
        /// <param name="positions">FramesPosition</param>
        public void ChangeFrames(Vector2i size, IEnumerable<Vector2i> positions)
        {
            FrameSize = size;
            FramesPosition = positions.ToArray();
            Update();
        }
        /// <summary>
        /// Generate mipmap for the textures, like SFML.Graphics.Texture.GenerateMipmap()
        /// </summary>
        public void GenerateMipmap()
        {
            if (textures != null)
            {
                foreach (var item in textures)
                {
                    item.GenerateMipmap();
                }
            }
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("ID", ID.ToByteArray());
            info.AddValue("Repeated", Repeated);
            info.AddValue("Smooth", Smooth);
            if (BaseImage != null)
            {
                info.AddValue("Image", (System.Drawing.Bitmap)BaseImage);
            }
            else
                info.AddValue("Image", null);
            info.AddValue("FramesPerSecond", FramesPerSecond);
            info.AddValue("FrameSize", FrameSize);
            info.AddValue("FramesPosition", FramesPosition);
        }
        /// <summary>
        /// Returns the first texture of the animation, or most of the time the only one texture.
        /// </summary>
        /// <returns>First texture</returns>
        public Texture GetTexture() => GetTexture(Time.Zero);
        /// <summary>
        /// Returns a texture of the animation at a specified time.
        /// </summary>
        /// <param name="time">Current time of the animation</param>
        /// <returns>Texture of the animation</returns>
        public Texture GetTexture(Time time)
        {
            if (textures == null)
                return null;
            var duration = Time.FromSeconds((float)textures.Length / FramesPerSecond);
            time %= duration;
            int index = (int)(time.AsSeconds() * FramesPerSecond);
            return textures[index];
        }
        private void Update()
        {
            if (BaseImage != null
                && FrameSize != default
                && FramesPosition != null
                && FramesPosition.Length > 0)
            {
                textures = FramesPosition.Select((pos) => new Texture(BaseImage, new IntRect(pos, FrameSize)) { Smooth = Smooth, Repeated = Repeated }).ToArray();
            }
        }
    }
}