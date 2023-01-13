using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using LogicApps.Portals.Helpers;

namespace LogicApps.Portals.CustomEntity
{
    internal static class CustomEntityExtensions
    {
        public static EntityCollection ToCustomEntityCollection(this EntityCollection entityCollection, List<OptionSetOptionResponseParams> optionsList = null)
        {
            string jsonResult;
            var entities = entityCollection.Entities;
            var customEntities = new List<CustomEntity>();
            foreach (var entity in entities)
            {
                var customEntity = new CustomEntity()
                {
                    Id = entity.Id,
                    LogicalName = entity.LogicalName
                };

                var attributes = new List<Field>();

                foreach (KeyValuePair<string, object> attribute in entity.Attributes)
                {
                    attributes = ParseAttribute(attributes, entity, attribute, optionsList);
                }
                customEntity.Attributes = attributes;
                customEntities.Add(customEntity);
            }
            CustomEntityCollection customEntityCollection = new CustomEntityCollection()
            {
                TotalCount = entityCollection.TotalRecordCount,
                Entities = customEntities,
                Count = customEntities.Count()
            };
            try
            {
                jsonResult = JsonConvert.SerializeObject(customEntityCollection);
            }
            catch (Exception ex)
            {
                jsonResult = JsonHelper.CustomJsonErrorResponse(ex.Message);
            }
            return jsonResult.ToCustomEntityCollection();
        }

        public static EntityCollection ToCustomEntityCollection(this string jsonResult)
        {
            var responseEntity = new Entity(GlobalConfiguration.EntityName) { [GlobalConfiguration.ResultField] = jsonResult };

            var resultEntityCollection = new EntityCollection(new List<Entity>() { responseEntity });

            return resultEntityCollection;
        }

        private static List<Field> ParseAttribute(List<Field> attributes, Entity entity, KeyValuePair<string, object> attribute, List<OptionSetOptionResponseParams> optionsList = null)
        {
            if (attribute.Value is AliasedValue aliasedValue)
            {
                attributes = ParseAttribute(attributes, entity, new KeyValuePair<string, object>(attribute.Key, aliasedValue.Value), optionsList);
            }
            else
            {
                object value;
                if (attribute.Value is OptionSetValue optionSet)
                {
                    var customOptionSet = new CustomOptionSet();

                    customOptionSet.Value = optionSet.Value;

                    if (entity.FormattedValues.Contains(attribute.Key))
                    {
                        customOptionSet.Label = entity.FormattedValues[attribute.Key];
                        var tagTypeOptions = optionsList?.Find(x => x.Value == customOptionSet.Value);
                        if (tagTypeOptions != null)
                        {
                            customOptionSet.Color = tagTypeOptions.Color;
                        }
                    }
                    value = customOptionSet;
                }
                else if (entity.FormattedValues.Contains(attribute.Key) && attribute.Value.GetType() != typeof(EntityReference) && attribute.Value.GetType() != typeof(Money))
                {
                    value = entity.FormattedValues[attribute.Key];
                }
                else
                {
                    value = attribute.Value;
                }
                attributes.Add(new Field(attribute.Key, value));
            }
            return attributes;
        }
    }
}
