using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// A basic bone.
    /// </summary>
    public class Bone : Transformable, IEquatable<Bone>, IBaseElement
    {
        public Category Category;

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
            Owner = null;
        }

        #endregion Public Constructors

        #region Public Properties

        /// <summary>
        /// The current owner of the bone.
        /// </summary>
        public SFDynamicObject Owner { get; internal set; }

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
}