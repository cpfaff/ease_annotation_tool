﻿
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace CAFE.Web.Configuration
{
    public class ComplexSearchFiltersConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("ComplexFiltersScopes", IsRequired = true)]
        public ConfigElementsCollection ComplexFiltersScopes
        {
            get
            {
                return base["ComplexFiltersScopes"] as ConfigElementsCollection;
            }
        }
        public class ComplexFilterScope : ConfigurationElement
        {

            [ConfigurationProperty("type", IsKey = true, IsRequired = true)]
            public string Type
            {
                get
                {
                    return base["type"] as string;
                }
                set
                {
                    base["type"] = value;
                }
            }

            [ConfigurationProperty("basePath", IsKey = true, IsRequired = true)]
            public string BasePath
            {
                get
                {
                    return base["basePath"] as string;
                }
                set
                {
                    base["basePath"] = value;
                }
            }

            [ConfigurationProperty("tooltip", IsKey = true, IsRequired = false)]
            public string Tooltip
            {
                get
                {
                    return base["tooltip"] as string;
                }
                set
                {
                    base["tooltip"] = value;
                }
            }

            [ConfigurationProperty("ComplexFiltersCollection")]
            public ConfigSubElementsCollection ComplexFiltersCollection
            {
                get
                {
                    return base["ComplexFiltersCollection"] as ConfigSubElementsCollection;
                }
            }

        }

        [ConfigurationCollection(typeof(ComplexFilterScope), AddItemName = "ComplexFilterScope")]
        public class ConfigElementsCollection : ConfigurationElementCollection, IEnumerable<ComplexFilterScope>
        {

            protected override ConfigurationElement CreateNewElement()
            {
                return new ComplexFilterScope();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                var l_configElement = element as ComplexFilterScope;
                if (l_configElement != null)
                    return l_configElement.Type;
                else
                    return null;
            }

            public ComplexFilterScope this[int index]
            {
                get
                {
                    return BaseGet(index) as ComplexFilterScope;
                }
            }

            #region IEnumerable<ComplexFilterScope> Members

            IEnumerator<ComplexFilterScope> IEnumerable<ComplexFilterScope>.GetEnumerator()
            {
                return (from i in Enumerable.Range(0, this.Count)
                        select this[i])
                        .GetEnumerator();
            }

            #endregion
        }

        [ConfigurationCollection(typeof(FilterElement), AddItemName = "FilterElement")]
        public class ConfigSubElementsCollection : ConfigurationElementCollection, IEnumerable<FilterElement>
        {

            protected override ConfigurationElement CreateNewElement()
            {
                return new FilterElement();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                var l_configElement = element as FilterElement;
                if (l_configElement != null)
                    return l_configElement.Property;
                else
                    return null;
            }

            public FilterElement this[int index]
            {
                get
                {
                    return BaseGet(index) as FilterElement;
                }
            }

            #region IEnumerable<FilterElement> Members

            IEnumerator<FilterElement> IEnumerable<FilterElement>.GetEnumerator()
            {
                return (from i in Enumerable.Range(0, this.Count)
                        select this[i])
                        .GetEnumerator();
            }

            #endregion
        }

        public class FilterElement : ConfigurationElement
        {

            [ConfigurationProperty("property", IsKey = true, IsRequired = true)]
            public string Property
            {
                get
                {
                    return base["property"] as string;
                }
                set
                {
                    base["property"] = value;
                }
            }

            [ConfigurationProperty("type", IsKey = true, IsRequired = true, DefaultValue = "Simple")]
            public string Type
            {
                get
                {
                    return base["type"] as string;
                }
                set
                {
                    base["type"] = value;
                }
            }

            [ConfigurationProperty("description", IsKey = true, IsRequired = false, DefaultValue = "")]
            public string Description
            {
                get
                {
                    return base["description"] as string;
                }
                set
                {
                    base["description"] = value;
                }
            }
        }
    }
}