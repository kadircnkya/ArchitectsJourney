using ArchitectsJourney.Domain.Common;

namespace ArchitectsJourney.Domain.Entities;

public enum ObjectiveState
{
    Pending,
    Completed,
    Failed
}

public sealed class MissionObjective : Entity<string>
{
    public MissionObjective(string id) : base(id)
    {
        State = ObjectiveState.Pending;
    }

    public ObjectiveState State { get; private set; }

    public void Complete()
    {
        if (State != ObjectiveState.Failed)
        {
            State = ObjectiveState.Completed;
        }
    }

    public void Fail()
    {
        State = ObjectiveState.Failed;
    }
    
    public void Reset()
    {
        State = ObjectiveState.Pending;
    }
    
    // For restoration
    public void SetState(ObjectiveState state)
    {
        State = state;
    }
}
