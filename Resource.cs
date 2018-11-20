using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.System;
using SFML.Graphics;
using System.Runtime.Serialization;

namespace WGP.SFDynamicObject
{
    [Serializable]
    public class Resource : ISerializable
    {
        public string Name { get; set; }
        public Guid ID { get; internal set; }
        private bool _repeated;
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
        private bool _smooth;
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
        public Image BaseImage { get; private set; }
        private Texture[] textures;
        public int FramesPerSecond { get; set; }
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
        public Texture GetTexture() => GetTexture(Time.Zero);
        public Texture GetTexture(Time time)
        {
            if (textures == null)
                return null;
            var duration = Time.FromSeconds((float)textures.Length / FramesPerSecond);
            time %= duration;
            int index = (int)(time.AsSeconds() * FramesPerSecond);
            return textures[index];
        }
        public Vector2i FrameSize { get; private set; }
        public Vector2i[] FramesPosition { get; private set; }
        public void ChangeFrames(Vector2i size, params Vector2i[] positions) => ChangeFrames(size, positions.AsEnumerable());
        public void ChangeFrames(Vector2i size, IEnumerable<Vector2i> positions)
        {
            FrameSize = size;
            FramesPosition = positions.ToArray();
            Update();
        }
        public void ChangeBaseImage(Image img)
        {
            BaseImage = img;
            Update();
        }
        public void AdaptFrameSize()
        {
            if (BaseImage != null)
                ChangeFrames((Vector2i)BaseImage.Size, new Vector2i());
            else
                ChangeFrames(default, new Vector2i());
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
    }
}
