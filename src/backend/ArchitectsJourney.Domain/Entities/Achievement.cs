using ArchitectsJourney.Domain.Common;
using ArchitectsJourney.Domain.ValueObjects;

namespace ArchitectsJourney.Domain.Entities;

public sealed class Achievement : Entity<string>
{
    public Achievement(string id, string name, string description, string category, int experienceReward, bool hidden, IEnumerable<AchievementCondition> conditions) : base(id)
    {
        Name = name;
        Description = description;
        Category = category;
        ExperienceReward = experienceReward;
        Hidden = hidden;
        Conditions = conditions.ToList().AsReadOnly();
    }

    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Category { get; private set; }
    public int ExperienceReward { get; private set; }
    public bool Hidden { get; private set; }
    public IReadOnlyList<AchievementCondition> Conditions { get; private set; }
}
