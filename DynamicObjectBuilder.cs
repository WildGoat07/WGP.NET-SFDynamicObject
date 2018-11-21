﻿using SFML.Graphics;
using SFML.System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace WGP.SFDynamicObject
{
    /// <summary>
    /// Class used to load templates and creates them.
    /// </summary>
    public class DynamicObjectBuilder
    {
        private List<Resource> resources;
        private Dictionary<string, FormatData> templates;
        /// <summary>
        /// Constructor.
        /// </summary>
        public DynamicObjectBuilder()
        {
            templates = new Dictionary<string, FormatData>();
            resources = new List<Resource>();
        }
        /// <summary>
        /// Create an instance from a template.
        /// </summary>
        /// <param name="name">Template's name</param>
        /// <returns>Dynamic object based by the template</returns>
        public SFDynamicObject CreateObject(string name)
        {
            FormatData copyFrom;
            try
            {
                copyFrom = templates[name];
            }
            catch (Exception)
            {
                throw new Exception("No template named " + name + " found");
            }
            try
            {
                var result = new SFDynamicObject();
                result.Version = new Version(copyFrom.Version);
                result.BonesHierarchy.AddRange(copyFrom.Hierarchy.Select((bone) =>
                {
                    var tmp = new Bone();
                    tmp.BlendMode = bone.BlendMode;
                    tmp.Name = bone.Name;
                    tmp.ID = new Guid(bone.ID);
                    tmp.Position = bone.Transform.Position;
                    tmp.Scale = bone.Transform.Scale;
                    tmp.Origin = bone.Transform.Origin;
                    tmp.Rotation = bone.Transform.Rotation;
                    if (bone.Sprite != null)
                    {
                        tmp.AttachedSprite = new DynamicSprite();
                        tmp.AttachedSprite.InternalRect = new RectangleShape()
                        {
                            TextureRect = bone.Sprite.TextureRect,
                            Size = bone.Sprite.Size,
                            FillColor = bone.Sprite.Color,
                            OutlineColor = bone.Sprite.OutlineColor,
                            OutlineThickness = bone.Sprite.OutlineThickness
                        };
                        if (bone.Sprite.TextureID != null)
                            tmp.AttachedSprite.Resource = resources.Find((res) => res.ID == new Guid(bone.Sprite.TextureID));
                    }

                    return tmp;
                }));
                for (int i = 0; i < copyFrom.Hierarchy.Length; i++)
                    result.BonesHierarchy[i].Children.AddRange(copyFrom.Hierarchy[i].Children.Select((id) => result.BonesHierarchy.Find((bone) => bone.ID == new Guid(id))));
                result.MasterBones.AddRange(copyFrom.Masters.Select((id) => result.BonesHierarchy.Find((bone) => bone.ID == new Guid(id))));
                result.Animations.AddRange(copyFrom.Animations.Select((anim) =>
                {
                    var tmp1 = new Animation();
                    tmp1.ID = new Guid(anim.ID);
                    tmp1.Name = anim.Name;
                    tmp1.Bones.AddRange(anim.Bones.Select((bone) =>
                    {
                        var tmp2 = new List<Animation.Key>();
                        var selectedBone = result.BonesHierarchy.Find((b) => b.ID == new Guid(bone.BoneID));
                        tmp2.AddRange(bone.Keys.Select((key) =>
                        {
                            var tmp3 = new Animation.Key();
                            tmp3.Position = Time.FromMicroseconds(key.Position);

                            tmp3.Transform = new Transformable();
                            tmp3.Transform.Position = key.Transform.Position;
                            tmp3.Transform.Scale = key.Transform.Scale;
                            tmp3.Transform.Origin = key.Transform.Origin;
                            tmp3.Transform.Rotation = key.Transform.Rotation;

                            tmp3.Opacity = key.Opacity;
                            tmp3.Color = key.Color;
                            tmp3.OutlineColor = key.OutlineColor;
                            tmp3.OutlineThickness = key.OutlineThickness;
                            tmp3.PosFunction = key.PosFunction;
                            tmp3.PosFctCoeff = key.PosCoeff;
                            tmp3.OriginFunction = key.OriFunction;
                            tmp3.OriginFctCoeff = key.OriCoeff;
                            tmp3.ScaleFunction = key.ScaFunction;
                            tmp3.ScaleFctCoeff = key.ScaCoeff;
                            tmp3.RotFunction = key.RotFunction;
                            tmp3.RotFctCoeff = key.RotCoeff;
                            tmp3.OpacityFunction = key.OpaFunction;
                            tmp3.OpacityFctCoeff = key.OpaCoeff;
                            tmp3.OutlineColorFunction = key.OCoFunction;
                            tmp3.OutlineColorFctCoeff = key.OCoCoeff;
                            tmp3.ColorFunction = key.ColFunction;
                            tmp3.ColorFctCoeff = key.ColCoeff;
                            tmp3.OutlineThicknessFunction = key.OThFunction;
                            tmp3.OutlineThicknessFctCoeff = key.OThCoeff;

                            return tmp3;
                        }));
                        return new Couple<Bone, List<Animation.Key>>(selectedBone, tmp2);
                    }));
                    tmp1.Duration = Time.FromMicroseconds(anim.Duration);

                    return tmp1;
                }));
                result.UsedResources = copyFrom.Resources.ToList();

                return result;
            }
            catch (Exception e)
            {
                throw new Exception("An Error occurenced", e);
            }
        }
        /// <summary>
        /// Adds a template to the builder's dictionnary.
        /// </summary>
        /// <param name="name">Template's name</param>
        /// <param name="inputStream">Stream containing the template</param>
        public void LoadObjectTemplate(string name, Stream inputStream)
        {
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            var tmp = (FormatData)formatter.Deserialize(inputStream);
            resources.AddRange(tmp.Resources);
            templates.Add(name, tmp);
        }
        /// <summary>
        /// Remove a template from the builder's dictionnary.
        /// </summary>
        /// <param name="name"></param>
        public void RemoveObjectTemplate(string name)
        {
            try
            {
                var template = templates[name];
                foreach (var item in template.Hierarchy)
                {
                    if (item.Sprite != null && item.Sprite.TextureID != null)
                        resources.Remove(resources.Find((res) => res.ID == new Guid(item.Sprite.TextureID)));
                }
                templates.Remove(name);
            }
            catch (Exception)
            { }
        }
    }
}