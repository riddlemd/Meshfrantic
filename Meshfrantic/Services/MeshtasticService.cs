using Meshtastic.Connections;
using Meshtastic.Data;
using Meshtastic.Data.MessageFactories;
using Meshtastic.Protobufs;
using Meshfrantic.Models;

namespace Meshfrantic.Services;

public class MeshtasticService : IDisposable
{
    private readonly ILogger<MeshtasticService> _logger;
    private SerialConnection? _connection;
    private CancellationTokenSource? _readCts;
    private Task? _readTask;
    private readonly List<ChatMessage> _messages = [];
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public event Action? StateChanged;

    public bool IsConnected { get; private set; }
    public string? ConnectedPort { get; private set; }
    public DeviceStateContainer Container { get; private set; } = new();
    public IReadOnlyList<string> AvailablePorts { get; private set; } = [];
    public IReadOnlyList<ChatMessage> Messages => _messages;

    public MeshtasticService(ILogger<MeshtasticService> logger)
    {
        _logger = logger;
        RefreshPorts();
    }

    public void RefreshPorts()
    {
        AvailablePorts = SerialConnection.ListPorts().ToList();
        _logger.LogDebug("Refreshed available ports: {PortCount} ports found", AvailablePorts.Count);
        if (AvailablePorts.Count > 0)
            _logger.LogDebug("Available ports: {Ports}", string.Join(", ", AvailablePorts));
        StateChanged?.Invoke();
    }

    public async Task ConnectAsync(string port)
    {
        if (IsConnected) await DisconnectAsync();

        try
        {
            Container = new DeviceStateContainer();
            _connection = new SerialConnection(_logger, port, Container);
            _messages.Clear();
            ConnectedPort = port;
            IsConnected = true;

            // Start the read loop first — ReadFromRadio opens the serial port's BaseStream.
            // WantConfig is sent from inside the loop once the port is open.
            _readCts = new CancellationTokenSource();
            _readTask = Task.Run(() => RunReadLoopAsync(_readCts.Token));

            StateChanged?.Invoke();
            _logger.LogInformation("Connected to Meshtastic device on {Port}", port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Port}", port);
            _connection = null;
            IsConnected = false;
            ConnectedPort = null;
            StateChanged?.Invoke();
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        _readCts = null;

        if (_readTask != null)
        {
            try { await _readTask.WaitAsync(TimeSpan.FromSeconds(3)); } catch { /* ignore */ }
            _readTask = null;
        }

        if (_connection != null)
        {
            try { _connection.Disconnect(); } catch { /* ignore */ }
            _connection = null;
        }

        IsConnected = false;
        ConnectedPort = null;
        StateChanged?.Invoke();

        _logger.LogInformation("Disconnected from Meshtastic device");
    }

    public async Task SendTextMessageAsync(string text, uint? destNodeId = null)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        await _sendLock.WaitAsync();
        try
        {
            var factory = new TextMessageFactory(Container, destNodeId);
            var packet = factory.CreateTextMessagePacket(text);
            var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
            await _connection.WriteToRadio(toRadio);

            var ownNodeNum = Container.MyNodeInfo?.MyNodeNum ?? 0;
            _logger.LogInformation("Message sent from node {NodeId} to {DestNode}: {MessageLength} bytes",
                ownNodeNum, destNodeId ?? 0, text.Length);

            _messages.Add(new ChatMessage
            {
                Text = text,
                FromNodeId = ownNodeNum,
                FromNodeName = Container.GetNodeDisplayName(ownNodeNum),
                IsOwn = true
            });

            StateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to node {DestNode}", destNodeId ?? 0);
            throw;
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task RebootAsync(int delaySeconds = 5)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        _logger.LogWarning("Device reboot requested with {DelaySeconds}s delay", delaySeconds);
        var factory = new AdminMessageFactory(Container);
        var packet = factory.CreateRebootMessage(delaySeconds, isOta: false);
        var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
        await _connection.WriteToRadio(toRadio);
    }

    public async Task FactoryResetAsync()
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        _logger.LogWarning("Device factory reset requested");
        var factory = new AdminMessageFactory(Container);
        var packet = factory.CreateFactoryResetMessage();
        var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
        await _connection.WriteToRadio(toRadio);
    }

    public async Task ResetNodeDbAsync()
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        _logger.LogWarning("Device node database reset requested");
        var factory = new AdminMessageFactory(Container);
        var packet = factory.CreateNodeDbResetMessage();
        var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
        await _connection.WriteToRadio(toRadio);
    }

    public async Task RemoveNodeAsync(uint nodeNum)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        _logger.LogInformation("Removing node {NodeId}", nodeNum);
        var factory = new AdminMessageFactory(Container);
        var packet = factory.CreateRemoveByNodenumMessage(nodeNum);
        var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
        await _connection.WriteToRadio(toRadio);

        Container.Nodes.RemoveAll(n => n.Num == nodeNum);
        StateChanged?.Invoke();
    }

    public async Task RequestConfigAsync()
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        var wantConfig = _connection.ToRadioFactory.CreateWantConfigMessage();
        await _connection.WriteToRadio(wantConfig);
    }

    public async Task SetOwnerAsync(string longName, string shortName)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        _logger.LogInformation("Setting device owner: {LongName} ({ShortName})", longName, shortName);
        var factory = new AdminMessageFactory(Container);
        var user = new User { LongName = longName, ShortName = shortName };
        var packet = factory.CreateSetOwnerMessage(user);
        var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
        await _connection.WriteToRadio(toRadio);
    }

    internal async Task RunReadLoopAsync(CancellationToken ct)
    {
        if (_connection == null) return;
        _logger.LogInformation("Read loop started");

        // Only WriteToRadio(packet, callback) opens the serial port.
        // WriteToRadio(packet) and ReadFromRadio both assume the port is already open.
        // So we use the callback overload with WantConfig as the entry point:
        //   1. It waits 1s (library-imposed settle delay), opens the port, sends WantConfig
        //   2. It then drives ReadFromRadio internally, calling our callback per packet
        //   3. The port stays open, so the simple WriteToRadio used for sends works fine
        try
        {
            var wantConfig = _connection.ToRadioFactory.CreateWantConfigMessage();
            _logger.LogInformation("Opening port and sending WantConfig...");

            await _connection.WriteToRadio(
                wantConfig,
                async (FromRadio fromRadio, DeviceStateContainer container) =>
                {
                    ExtractTextMessages(fromRadio);
                    StateChanged?.Invoke();
                    return ct.IsCancellationRequested;
                }
            );
        }
        catch (OperationCanceledException) { /* normal shutdown */ }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Serial read error — disconnecting");
            _ = Task.Run(async () => await DisconnectAsync());
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Unexpected error in read loop — disconnecting");
            _ = Task.Run(async () => await DisconnectAsync());
        }

        _logger.LogInformation("Read loop ended");
    }

    private void ExtractTextMessages(FromRadio fromRadio)
    {
        if (fromRadio.PayloadVariantCase != FromRadio.PayloadVariantOneofCase.Packet)
            return;

        var packet = fromRadio.Packet;
        if (packet?.Decoded?.Portnum != PortNum.TextMessageApp)
            return;

        var text = packet.Decoded.Payload.ToStringUtf8();
        var fromNodeId = packet.From;
        var nodeName = Container.GetNodeDisplayName(fromNodeId);
        var ownNodeNum = Container.MyNodeInfo?.MyNodeNum ?? 0;

        _logger.LogDebug("Message received from node {FromNodeId} ({NodeName}): {MessageLength} bytes",
            fromNodeId, nodeName, text.Length);

        _messages.Add(new ChatMessage
        {
            Text = text,
            FromNodeId = fromNodeId,
            FromNodeName = nodeName,
            IsOwn = fromNodeId == ownNodeNum && ownNodeNum != 0
        });
    }

    public void Dispose()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        _connection?.Disconnect();
        _sendLock.Dispose();
    }
}
