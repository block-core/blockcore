namespace Blockcore.Connection.Broadcasting
{
    public enum TransactionBroadcastState
    {
        FailedBroadcast,
        ReadyToBroadcast,
        Broadcasted,
        Propagated
    }
}