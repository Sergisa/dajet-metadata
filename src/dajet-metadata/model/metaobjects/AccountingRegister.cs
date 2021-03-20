﻿namespace DaJet.Metadata.Model
{
    public sealed class AccountingRegister : MetadataObject
    {

    }
    public sealed class AccountingRegisterPropertyFactory : MetadataPropertyFactory
    {
        protected override void InitializePropertyNameLookup()
        {
            PropertyNameLookup.Add("_period", "Период");
            PropertyNameLookup.Add("_recorder", "Регистратор");
            // TODO: добавить остальные свойства
        }
    }
}