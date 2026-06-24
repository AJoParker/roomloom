using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR.Client;
using RoomLoom.Maui.Models;

namespace RoomLoom.Maui.Services;

public sealed class SessionConnection : ISessionConnection, IAsyncDisposable
{
    private readonly HubConnection _connection;
    private ConnectionState _state = ConnectionState.Disconnected;

    public SessionConnection()
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(ApiEndpoints.HubUrl)
            .WithAutomaticReconnect()
            .AddJsonProtocol(o =>
                o.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .Build();

        _connection.On<Participant>("ParticipantJoined", p =>
            ParticipantJoined?.Invoke(this, p));

        _connection.On<Participant>("ParticipantLeft", p =>
            ParticipantLeft?.Invoke(this, p));

        _connection.On<LiveSession>("SessionLive", l =>
            SessionLive?.Invoke(this, l));

        _connection.On<LiveSession>("SessionEnded", l =>
            SessionEnded?.Invoke(this, l));

        _connection.Reconnecting += _ =>
        {
            SetState(ConnectionState.Reconnecting);
            return Task.CompletedTask;
        };

        _connection.Reconnected += _ =>
        {
            SetState(ConnectionState.Connected);
            return Task.CompletedTask;
        };

        _connection.Closed += ex =>
        {
            SetState(ex is null ? ConnectionState.Disconnected : ConnectionState.Faulted);
            return Task.CompletedTask;
        };
    }

    public ConnectionState State => _state;

    public event EventHandler<ConnectionState>? StateChanged;
    public event EventHandler<Participant>? ParticipantJoined;
    public event EventHandler<Participant>? ParticipantLeft;
    public event EventHandler<LiveSession>? SessionLive;
    public event EventHandler<LiveSession>? SessionEnded;

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_state is ConnectionState.Connected or ConnectionState.Connecting)
            return;

        SetState(ConnectionState.Connecting);
        try
        {
            await _connection.StartAsync(ct);
            SetState(ConnectionState.Connected);
        }
        catch
        {
            SetState(ConnectionState.Faulted);
            throw;
        }
    }

    public Task JoinSessionAsync(string sessionId, Participant me, CancellationToken ct = default)
        => _connection.InvokeAsync("JoinSession", sessionId, me, ct);

    public Task LeaveSessionAsync(string sessionId, CancellationToken ct = default)
        => _connection.InvokeAsync("LeaveSession", sessionId, ct);

    public async Task<IReadOnlyList<Participant>> GetParticipantsAsync(string sessionId, CancellationToken ct = default)
    {
        await ConnectAsync(ct);
        return await _connection.InvokeAsync<IReadOnlyList<Participant>>("GetParticipants", sessionId, ct);
    }

    public ValueTask DisposeAsync() => _connection.DisposeAsync();

    private void SetState(ConnectionState next)
    {
        if (_state == next) return;
        _state = next;
        StateChanged?.Invoke(this, next);
    }
}
