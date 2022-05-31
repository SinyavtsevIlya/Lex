namespace Nanory.Lex.Stats
{
    /// <summary>
    /// The Stat-Entity marked with this tag applies the effect 
    /// to the <see cref="StatReceiverTag">Stat-Receiver</see> using the addition/subtraction operation
    /// </summary>
    public struct AdditiveStatTag { }
    /// <summary>
    /// The Stat-Entity marked with this tag applies the effect 
    /// to the <see cref="StatReceiverTag">Stat-Receiver</see> using the multiply/divide operation
    /// </summary>
    public struct MultiplyStatTag { }

    /// <summary>
    /// Event of applying Stat-Entity (to the <see cref="StatReceiverTag">Stat-Receiver</see>) or changing it's value
    /// </summary>
    [OneFrame]
    public struct StatsChangedEvent { }

    /// <summary>
    /// Event of removing Stat-Entity (from the <see cref="StatReceiverTag">Stat-Receiver</see>)
    /// </summary>
    [OneFrame]
    public struct StatsRemovedEvent { }

    /// <summary>
    /// An event that any of the Stat-Entity of <see cref="StatReceiverTag">Stat-Receiver</see> has changed. Always placed on the <see cref="StatReceiverTag">Stat-Receiver</see> entity
    /// </summary>
    [OneFrame]
    public struct StatReceivedEvent<TStatComponent>
    {
    }

    /// <summary>
    /// Reference to the <see cref="StatReceiverTag">Stat-Receiver</see> entity to which this effect will be applied
    /// </summary>
    public struct StatReceiverLink
    {
        public EcsPackedEntity Value;
    }

    /// <summary>
    /// This tag determines the entity as a "Stat-Receiver". This means that the entity 
    /// may accumulate stats values from it's Stat-Context entities. 
    /// As soon as the value of the Stat-Context entity changes, the value of the Stat-Receiver also changes.
    /// </summary>
    public struct StatReceiverTag { }

    /// <summary>
    /// Stat-Context entity - is the entity which holds one or several Stat-Components (e.g. Strength, Max-Health, etc.) 
    /// It persists as a StatElement (dynamic buffer element) of a "Stat-Context" Entity. Stat-Context Entity can be anything, say an item (sword, gun, potion etc.) or a buff, an ability. 
    /// Stat-Context entity also stores a reference
    /// to a Stat-Receiver entity to which this effect will be applied.
    /// </summary>
    public struct Stats : IEcsAutoReset<Stats>
    {
        public Buffer<EcsPackedEntity> Buffer;

        public void AutoReset(ref Stats c)
        {
            c.Buffer.AutoReset(ref c.Buffer);
        }
    }
}
