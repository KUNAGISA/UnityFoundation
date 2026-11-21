namespace Gameplay.Tables
{
    public struct TableReloadedEvent<T>
    {
        public readonly T Table;
        
        public TableReloadedEvent(T table)
        {
            Table = table;
        }
    }
}

