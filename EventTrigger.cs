using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// An event that calls a function when triggered.
    /// </summary>
    public class EventTrigger : IBaseElement
    {
        #region Internal Fields

        internal bool triggered;

        #endregion Internal Fields

        #region Internal Constructors

        internal EventTrigger()
        {
            ID = Guid.NewGuid();
            Name = null;
            Trigger = null;
            Area = default;
            Time = default;
        }

        #endregion Internal Constructors

        #region Public Properties

        /// <summary>
        /// Animation containing this resource.
        /// </summary>
        public Animation Animation { get; internal set; }

        /// <summary>
        /// Area triggered by the event.
        /// </summary>
        public FloatRect Area { get; set; }

        /// <summary>
        /// Universal identifier of the event.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// Name describing the event.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Dynamic Object containing this resource.
        /// </summary>
        public SFDynamicObject Owner { get; internal set; }

        /// <summary>
        /// Time at which is triggered the event.
        /// </summary>
        public Time Time { get; set; }

        /// <summary>
        /// Function called by the event when triggered.
        /// </summary>
        public Action Trigger { get; set; }

        #endregion Public Properties
    }
}