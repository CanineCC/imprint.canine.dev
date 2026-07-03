using Imprint.Authoring.Domain;
using Imprint.EventSourcing;

namespace Imprint.Authoring.Features.Pages.AddPreset;

public sealed record AddPreset(PageId PageId, int Index, string PresetKey) : IValidatableCommand
{
    public IEnumerable<string> Validate()
    {
        if (SectionPresets.Find(PresetKey) is null)
        {
            yield return $"There is no '{PresetKey}' section preset.";
        }
    }
}
