// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Server._Erida.Administration;
using Content.Shared._Erida.Sponsor;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Erida.Sponsor;

public sealed class SponsorManagerSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly EridaServerApi _eridaServerApi = default!;

    private Dictionary<NetUserId, SponsorStatus> _cachedPlayers = [];

    public override void Initialize()
    {
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;

        base.Initialize();
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        switch (args.NewStatus)
        {
            case SessionStatus.Connected:
                await RefreshChachedPlayerStatus(args.Session);
                break;
            case SessionStatus.Disconnected:
                _cachedPlayers.Remove(args.Session.UserId);
                break;
        }
    }

    private async Task RefreshChachedPlayerStatus(ICommonSession session)
    {
        var status = await _eridaServerApi.GetSponsorUserDataOrNull(session.UserId);
        if (status != null)
            _cachedPlayers[session.UserId] = status;
    }

    public bool IsSponsor(NetUserId userId)
    {
        return _cachedPlayers.ContainsKey(userId);
    }

    public bool GetCachedUserInfo(NetUserId userId, [NotNullWhen(true)] out SponsorStatus? status)
    {
        status = null;
        if (!IsSponsor(userId))
            return false;

        status = _cachedPlayers[userId];
        return true;
    }
}
