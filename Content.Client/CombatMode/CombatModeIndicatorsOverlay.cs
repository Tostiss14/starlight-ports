using System.Numerics;
using Content.Client._Lust.Weapons.Ranged;
using Content.Client.Hands.Systems;
using Content.Shared._Lust.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Utility;

namespace Content.Client.CombatMode;

/// <summary>
///   This shows something like crosshairs for the combat mode next to the mouse cursor.
///   For weapons with the gun class, a crosshair of one type is displayed,
///   while for all other types of weapons and items in hand, as well as for an empty hand,
///   a crosshair of a different type is displayed. These crosshairs simply show the state of combat mode (on|off).
/// </summary>
public sealed class CombatModeIndicatorsOverlay : Overlay
{
    private readonly IInputManager _inputManager;
    private readonly IEntityManager _entMan;
    private readonly IEyeManager _eye;
    private readonly CombatModeSystem _combat;
    private readonly HandsSystem _hands = default!;

    private readonly Texture _gunSight;
    private readonly Texture _gunBoltSight;
    private readonly Texture _meleeSight;
    // Lust added start - управляем индикаторами оружия настройками клиента
    private readonly bool _showHeatIndicator;
    private readonly bool _showJamIndicator;
    private readonly bool _showBoltIndicator;

    private static readonly Color JamIndicatorColor = Color.FromHex("#ff4d4d");
    private static readonly Color BoltIndicatorColor = Color.FromHex("#ffd34d");
    // Lust added end

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public Color MainColor = Color.White.WithAlpha(0.3f);
    public Color StrokeColor = Color.Black.WithAlpha(0.5f);
    public float Scale = 0.6f;  // 1 is a little big

    public CombatModeIndicatorsOverlay(IInputManager input, IEntityManager entMan,
            IEyeManager eye, CombatModeSystem combatSys, HandsSystem hands,
            // Lust added start - передаем состояние пользовательских настроек индикаторов
            bool showHeatIndicator = true, bool showJamIndicator = true, bool showBoltIndicator = true)
    {
        _inputManager = input;
        _entMan = entMan;
        _eye = eye;
        _combat = combatSys;
        _hands = hands;
        _showHeatIndicator = showHeatIndicator;
        _showJamIndicator = showJamIndicator;
        _showBoltIndicator = showBoltIndicator;
        // Lust added end

        var spriteSys = _entMan.EntitySysManager.GetEntitySystem<SpriteSystem>();
        _gunSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"),
            "gun_sight"));
        _gunBoltSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"),
            "gun_bolt_sight"));
        _meleeSight = spriteSys.Frame0(new SpriteSpecifier.Rsi(new ResPath("/Textures/Interface/Misc/crosshair_pointers.rsi"),
             "melee_sight"));
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_combat.IsInCombatMode())
            return false;

        return base.BeforeDraw(in args);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var mouseScreenPosition = _inputManager.MouseScreenPosition;
        var mousePosMap = _eye.PixelToMap(mouseScreenPosition);
        if (mousePosMap.MapId != args.MapId)
            return;

        var handEntity = _hands.GetActiveHandEntity();
        var isHandGunItem = _entMan.HasComponent<GunComponent>(handEntity);
        var isGunBolted = true;
        if (_entMan.TryGetComponent(handEntity, out ChamberMagazineAmmoProviderComponent? chamber))
            isGunBolted = chamber.BoltClosed ?? true;


        var mousePos = mouseScreenPosition.Position;
        var uiScale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var limitedScale = uiScale > 1.25f ? 1.25f : uiScale;

        var sight = isHandGunItem ? (isGunBolted ? _gunSight : _gunBoltSight) : _meleeSight;
        DrawSight(sight, args.ScreenHandle, mousePos, limitedScale * Scale);

        // Lust added start - показываем состояние оружия рядом с прицелом
        if (isHandGunItem && handEntity is { } gun)
            DrawWeaponIndicators(args.ScreenHandle, gun, mousePos + new Vector2(14f, 12f) * limitedScale, limitedScale, !isGunBolted);
        // Lust added end
    }

    // Lust added start - рисуем компактные индикаторы без дополнительных текстур
    private void DrawWeaponIndicators(DrawingHandleScreen screen, EntityUid gun, Vector2 position, float scale, bool boltOpen)
    {
        screen.SetTransform(Matrix3x2.Identity);
        var yOffset = 0f;

        if (_entMan.TryGetComponent<GunHeatComponent>(gun, out var heat))
        {
            if (_showHeatIndicator)
            {
                var (fill, jam) = GunHeatStatusControl.GetFractions(heat);
                if (fill > 0.01f)
                {
                    var size = new Vector2(30f, 5f) * scale;
                    GunHeatStatusControl.DrawGauge(screen, UIBox2.FromDimensions(position, size), fill, jam, MathF.Max(1f, scale));
                    yOffset += size.Y + (3f * scale);
                }
            }

            if (_showJamIndicator && heat.Jammed)
            {
                screen.DrawCircle(position + new Vector2(5f * scale, yOffset + 5f * scale), 4f * scale, JamIndicatorColor);
                yOffset += 10f * scale;
            }
        }

        if (_showBoltIndicator && boltOpen)
        {
            var size = 10f * scale;
            var box = UIBox2.FromDimensions(position + new Vector2(0f, yOffset), new Vector2(size, size));
            screen.DrawRect(box, BoltIndicatorColor);
            screen.DrawRect(UIBox2.FromDimensions(new Vector2(box.Left + size * 0.3f, box.Top + size * 0.3f), new Vector2(size * 0.4f, size * 0.4f)), Color.Black);
        }
    }
    // Lust added end

    private void DrawSight(Texture sight, DrawingHandleScreen screen, Vector2 centerPos, float scale)
    {
        var sightSize = sight.Size * scale;
        var expandedSize = sightSize + new Vector2(7f, 7f);

        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - sightSize * 0.5f, sightSize), StrokeColor);
        screen.DrawTextureRect(sight,
            UIBox2.FromDimensions(centerPos - expandedSize * 0.5f, expandedSize), MainColor);
    }
}
