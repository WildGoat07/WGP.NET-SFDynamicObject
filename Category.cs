using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// A category is used to sort bones with tags and to select certains drawing mode.
    /// </summary>
    public class Category : IBaseElement, IEquatable<Category>
    {
        #region Private Fields

        private string _name;

        #endregion Private Fields

        #region Public Constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        public Category()
        {
            Enabled = true;
            ID = Guid.NewGuid();
            Name = "";
        }

        #endregion Public Constructors

        #region Private Constructors

        internal Category(bool b)
        {
            Enabled = true;
            ID = new Guid();
            Name = "Default";
        }

        #endregion Private Constructors

        #region Public Properties

        /// <summary>
        /// When the category is enabled, its bone will be displayed.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Universial identifier of the category.
        /// </summary>
        public Guid ID { get; internal set; }

        /// <summary>
        /// Name of the category.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value ?? throw new NullReferenceException();
        }

        public override int GetHashCode() => ID.GetHashCode();

        public override bool Equals(object obj) => Equals((Category)obj);

        public bool Equals(Category other) => ID.Equals(other.ID);

        #endregion Public Properties
    }
}