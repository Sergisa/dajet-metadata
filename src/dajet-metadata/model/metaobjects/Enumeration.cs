﻿namespace DaJet.Metadata.Model
{
    public sealed class Enumeration : ApplicationObject
    {

    }
    public sealed class EnumerationPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_idrref", "Ссылка");
            PropertyNameLookup.Add("_enumorder", "Порядок");
        }
    }
}