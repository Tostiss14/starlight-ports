using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Lust.Thermite.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class DissolvableComponent : Component
{
    [DataField]
    public DamageSpecifier Damage = new();

    [DataField]
    public float MaximumStacks = 10f;

    [DataField]
    public float MinimumStacks;

    [DataField]
    public float StackFade = -0.1f;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float Stacks;

    [AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public bool IsBurning;

    public TimeSpan NextUpdate;
    public EntityUid? FireEffect;
}
