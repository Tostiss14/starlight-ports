using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Shared.Configuration;

namespace Content.Client.CombatMode;

public sealed partial class CombatModeSystem : SharedCombatModeSystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IEyeManager _eye = default!;
    // Lust added start - храним настройки, чтобы пересоздавать overlay при их изменении
    private bool _showCombatIndicators;
    private bool _showHeatIndicator = true;
    private bool _showJamIndicator = true;
    private bool _showBoltIndicator = true;
    // Lust added end

    /// <summary>
    /// Raised whenever combat mode changes.
    /// </summary>
    public event Action<bool>? LocalPlayerCombatModeUpdated;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CombatModeComponent, AfterAutoHandleStateEvent>(OnHandleState);

        Subs.CVar(_cfg, CCVars.CombatModeIndicatorsPointShow, OnShowCombatIndicatorsChanged, true);
        // Lust added start - обновляем overlay при изменении настроек индикаторов
        Subs.CVar(_cfg, CCVars.HudShowGunHeatIndicator, OnShowHeatIndicatorChanged, true);
        Subs.CVar(_cfg, CCVars.HudShowGunJamIndicator, OnShowJamIndicatorChanged, true);
        Subs.CVar(_cfg, CCVars.HudShowGunBoltIndicator, OnShowBoltIndicatorChanged, true);
        // Lust added end
    }

    private void OnHandleState(EntityUid uid, CombatModeComponent component, ref AfterAutoHandleStateEvent args)
    {
        UpdateHud(uid);
    }

    public override void Shutdown()
    {
        _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();

        base.Shutdown();
    }

    public bool IsInCombatMode()
    {
        var entity = _playerManager.LocalEntity;

        if (entity == null)
            return false;

        return IsInCombatMode(entity.Value);
    }

    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        base.SetInCombatMode(entity, value, component);
        UpdateHud(entity);
    }

    private void UpdateHud(EntityUid entity)
    {
        if (entity != _playerManager.LocalEntity || !Timing.IsFirstTimePredicted)
        {
            return;
        }

        var inCombatMode = IsInCombatMode();
        LocalPlayerCombatModeUpdated?.Invoke(inCombatMode);
    }

    private void OnShowCombatIndicatorsChanged(bool isShow)
    {
        // Lust added start - централизуем создание overlay с текущими настройками
        _showCombatIndicators = isShow;
        UpdateCombatIndicators();
        // Lust added end
    }

    // Lust added start - обновляем прицел при изменении параметров индикаторов
    private void OnShowHeatIndicatorChanged(bool show)
    {
        _showHeatIndicator = show;
        UpdateCombatIndicators();
    }

    private void OnShowJamIndicatorChanged(bool show)
    {
        _showJamIndicator = show;
        UpdateCombatIndicators();
    }

    private void OnShowBoltIndicatorChanged(bool show)
    {
        _showBoltIndicator = show;
        UpdateCombatIndicators();
    }

    private void UpdateCombatIndicators()
    {
        _overlayManager.RemoveOverlay<CombatModeIndicatorsOverlay>();
        if (!_showCombatIndicators)
            return;

        _overlayManager.AddOverlay(new CombatModeIndicatorsOverlay(
            _inputManager,
            EntityManager,
            _eye,
            this,
            EntityManager.System<HandsSystem>(),
            _showHeatIndicator,
            _showJamIndicator,
            _showBoltIndicator));
    }
    // Lust added end
}
