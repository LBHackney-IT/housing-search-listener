namespace HousingSearchListener.V1.Boundary
{
    public static class EventTypes
    {
        public const string PersonCreatedEvent = "PersonCreatedEvent";
        public const string PersonUpdatedEvent = "PersonUpdatedEvent";

        public const string TenureCreatedEvent = "TenureCreatedEvent";
        public const string TenureUpdatedEvent = "TenureUpdatedEvent";
        public const string PersonAddedToTenureEvent = "PersonAddedToTenureEvent";
        public const string PersonRemovedFromTenureEvent = "PersonRemovedFromTenureEvent";

        public const string AccountCreatedEvent = "AccountCreatedEvent";
        public const string AccountUpdatedEvent = "AccountUpdatedEvent";
        public const string TransactionCreatedEvent = "TransactionCreatedEvent";

        public const string AssetCreatedEvent = "AssetCreatedEvent";
        public const string AssetUpdatedEvent = "AssetUpdatedEvent";

        public const string ProcessStartedEvent = "ProcessStartedEvent";
        public const string ProcessUpdatedEvent = "ProcessUpdatedEvent";
        public const string ProcessClosedEvent = "ProcessClosedEvent";
        public const string ProcessCompletedEvent = "ProcessCompletedEvent";

        public const string ContractCreatedEvent = "ContractCreatedEvent";
        public const string ContractUpdatedEvent = "ContractUpdatedEvent";
    }
}
