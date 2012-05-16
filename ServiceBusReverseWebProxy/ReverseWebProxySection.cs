//
// (c) Microsoft Corporation. All rights reserved.
//

namespace Microsoft.Samples.ServiceBusReverseWebProxy
{
    using System;
    using System.Configuration;

    class ReverseWebProxySection : ConfigurationSection
    {
        [ConfigurationProperty("serviceNamespace", DefaultValue = null, IsRequired = true)]
        public string ServiceNamespace
        {
            get
            {
                return (string)this["serviceNamespace"];
            }
            set
            {
                this["serviceNamespace"] = value;
            }
        }

        [ConfigurationProperty("issuerName", DefaultValue = null, IsRequired = true)]
        public string IssuerName
        {
            get
            {
                return (string)this["issuerName"];
            }
            set
            {
                this["issuerName"] = value;
            }
        }

        [ConfigurationProperty("issuerSecret", DefaultValue = null, IsRequired = true)]
        public string IssuerSecret
        {
            get
            {
                return (string)this["issuerSecret"];
            }
            set
            {
                this["issuerSecret"] = value;
            }
        }

        [ConfigurationProperty("enableSilverlightPolicy", DefaultValue = false, IsRequired = true)]
        public bool EnableSilverlightPolicy
        {
            get
            {
                return (bool)this["enableSilverlightPolicy"];
            }
            set
            {
                this["enableSilverlightPolicy"] = value;
            }
        }

        [ConfigurationProperty("pathMappings", IsDefaultCollection = false)]
        [ConfigurationCollection(typeof(PathMappingCollection),
            AddItemName = "add", ClearItemsName = "clear", RemoveItemName = "remove")]
        public PathMappingCollection PathMappings
        {
            get
            {
                PathMappingCollection pathMappingCollection =
                    (PathMappingCollection)base["pathMappings"];
                return pathMappingCollection;
            }
        }
    }

    public class PathMappingCollection : ConfigurationElementCollection
    {
        public PathMappingCollection()
        {
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get
            {
                return ConfigurationElementCollectionType.AddRemoveClearMap;
            }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new PathMappingElement();
        }

        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((PathMappingElement)element).NamespacePath;
        }

        public PathMappingElement this[int index]
        {
            get
            {
                return (PathMappingElement)BaseGet(index);
            }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public PathMappingElement this[string NamespacePath]
        {
            get
            {
                return (PathMappingElement)BaseGet(NamespacePath);
            }
        }

        public int IndexOf(PathMappingElement pathMapping)
        {
            return BaseIndexOf(pathMapping);
        }

        public void Add(PathMappingElement pathMapping)
        {
            BaseAdd(pathMapping);
        }
        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        public void Remove(PathMappingElement pathMapping)
        {
            if (BaseIndexOf(pathMapping) >= 0)
            {
                BaseRemove(pathMapping.NamespacePath);
            }
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string namespacePath)
        {
            BaseRemove(namespacePath);
        }

        public void Clear()
        {
            BaseClear();
        }
    }

    public class PathMappingElement : ConfigurationElement
    {
        public PathMappingElement(String namespacePath, String localUri)
        {
            this.NamespacePath = namespacePath;
            this.LocalUri = localUri;
        }

        public PathMappingElement()
        {
        }

        [ConfigurationProperty("namespacePath", DefaultValue = null, IsRequired = true, IsKey = true)]
        public string NamespacePath
        {
            get
            {
                return (string)this["namespacePath"];
            }
            set
            {
                this["namespacePath"] = value;
            }
        }

        [ConfigurationProperty("localUri", DefaultValue = null, IsRequired = true)]
        public string LocalUri
        {
            get
            {
                return (string)this["localUri"];
            }
            set
            {
                this["localUri"] = value;
            }
        }
    }
}
