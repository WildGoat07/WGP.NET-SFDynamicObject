using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WGP.SFDynamicObject
{
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
            if (InternalRect != null && Resource != null && !Resource.Disposed)
                InternalRect.Texture = Resource.GetTexture(timer);
            else if (InternalRect != null)
                InternalRect.Texture = null;
        }

        #endregion Public Methods
    }
}