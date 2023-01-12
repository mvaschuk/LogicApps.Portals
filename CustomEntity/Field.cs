using Microsoft.Xrm.Sdk;

namespace LogicApps.Portals.CustomEntity
{
    public class Field
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public object FieldType { get; set; }
        public CustomOptionSet OptionSetValue { get; set; }
        public CustomEntityReference EntityReferenceValue { get; set; }

        public Field(string name, object value)
        {
            Name = name;
            FieldType = value.GetType().Name;

            if (value is EntityReference entityReference)
            {
                Value = entityReference.Name;
                EntityReferenceValue = new CustomEntityReference()
                {
                    Id = entityReference.Id,
                    Name = entityReference.Name,
                    LogicalName = entityReference.LogicalName
                };
            }
            else if (value is CustomOptionSet customOptionSet)
            {
                Value = customOptionSet.Label;
                OptionSetValue = customOptionSet;
            }

            else if (value is Money money)
                Value = money.Value;
            else
                Value = value;
        }
    }
}
