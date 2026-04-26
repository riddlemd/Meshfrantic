using Meshtastic.Connections;
using Meshtastic.Data;
using Meshtastic.Data.MessageFactories;
using Google.Protobuf;
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
    private readonly Dictionary<uint, ChatMessage> _pendingAcks = [];
    private readonly Dictionary<uint, NodeTelemetry> _nodeTelemetry = [];
    private readonly Dictionary<uint, List<uint>> _tracerouteResults = [];
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public event Action? StateChanged;

    public bool IsConnected { get; private set; }
    public string? ConnectedPort { get; private set; }
    public DeviceStateContainer Container { get; private set; } = new();
    public IReadOnlyList<string> AvailablePorts { get; private set; } = [];
    public IReadOnlyList<ChatMessage> Messages => _messages;
    public IReadOnlyDictionary<uint, NodeTelemetry> NodeTelemetryMap => _nodeTelemetry;
    public IReadOnlyDictionary<uint, List<uint>> TracerouteResults => _tracerouteResults;

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
            _pendingAcks.Clear();
            _nodeTelemetry.Clear();
            _tracerouteResults.Clear();
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

    public async Task SendTextMessageAsync(string text, uint channel = 0, uint? destNodeId = null)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        await _sendLock.WaitAsync();
        try
        {
            var factory = new TextMessageFactory(Container, destNodeId);
            var packet = factory.CreateTextMessagePacket(text, channel);
            var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
            await _connection.WriteToRadio(toRadio);

            var ownNodeNum = Container.MyNodeInfo?.MyNodeNum ?? 0;
            _logger.LogInformation("Message sent from node {NodeId} to {DestNode} on channel {Channel}: {MessageLength} bytes",
                ownNodeNum, destNodeId ?? 0, channel, text.Length);

            var msg = new ChatMessage
            {
                Text = text,
                FromNodeId = ownNodeNum,
                FromNodeName = Container.GetNodeDisplayName(ownNodeNum),
                IsOwn = true,
                ChannelIndex = (int)channel,
                DestNodeId = destNodeId,
                PacketId = packet.Id
            };

            if (packet.Id != 0)
                _pendingAcks[packet.Id] = msg;

            _messages.Add(msg);
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

    public async Task SetChannelAsync(Channel channel)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        await _sendLock.WaitAsync();
        try
        {
            var factory = new AdminMessageFactory(Container);

            var begin = factory.CreateBeginEditSettingsMessage();
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(begin));

            var setChannel = factory.CreateSetChannelMessage(channel);
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(setChannel));

            var commit = factory.CreateCommitEditSettingsMessage();
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(commit));

            _logger.LogInformation("Channel {Index} saved (role={Role})", channel.Index, channel.Role);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task SetModuleConfigAsync(ModuleConfig config)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        await _sendLock.WaitAsync();
        try
        {
            var factory = new AdminMessageFactory(Container);

            var begin = factory.CreateBeginEditSettingsMessage();
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(begin));

            var setConfig = factory.CreateSetModuleConfigMessage(config);
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(setConfig));

            var commit = factory.CreateCommitEditSettingsMessage();
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(commit));

            _logger.LogInformation("Module config ({Type}) saved", config.PayloadVariantCase);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    public async Task SetConfigAsync(Config config)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        await _sendLock.WaitAsync();
        try
        {
            var factory = new AdminMessageFactory(Container);

            var begin = factory.CreateBeginEditSettingsMessage();
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(begin));

            var setConfig = factory.CreateSetConfigMessage(config);
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(setConfig));

            var commit = factory.CreateCommitEditSettingsMessage();
            await _connection.WriteToRadio(_connection.ToRadioFactory.CreateMeshPacketMessage(commit));

            _logger.LogInformation("Config ({Type}) saved", config.PayloadVariantCase);
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

    public async Task RequestTracerouteAsync(uint destNodeNum)
    {
        if (_connection == null || !IsConnected)
            throw new InvalidOperationException("Not connected to a device");

        var rd = new RouteDiscovery();
        var packet = new MeshPacket
        {
            To = destNodeNum,
            WantAck = false,
            Decoded = new Meshtastic.Protobufs.Data
            {
                Portnum = PortNum.TracerouteApp,
                Payload = ByteString.CopyFrom(rd.ToByteArray()),
                WantResponse = true
            },
            Id = (uint)Random.Shared.Next(1, int.MaxValue)
        };

        var toRadio = _connection.ToRadioFactory.CreateMeshPacketMessage(packet);
        await _connection.WriteToRadio(toRadio);
        _logger.LogInformation("Traceroute requested to node {NodeId}", destNodeNum);
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
                    ProcessFromRadio(fromRadio);
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

    private void ProcessFromRadio(FromRadio fromRadio)
    {
        if (fromRadio.PayloadVariantCase != FromRadio.PayloadVariantOneofCase.Packet)
            return;

        var packet = fromRadio.Packet;
        if (packet?.Decoded == null) return;

        switch (packet.Decoded.Portnum)
        {
            case PortNum.TextMessageApp:
                HandleTextMessage(packet);
                break;
            case PortNum.RoutingApp:
                HandleRouting(packet);
                break;
            case PortNum.TelemetryApp:
                HandleTelemetry(packet);
                break;
            case PortNum.TracerouteApp:
                HandleTraceroute(packet);
                break;
        }
    }

    private void HandleTextMessage(MeshPacket packet)
    {
        var text = packet.Decoded.Payload.ToStringUtf8();
        var fromNodeId = packet.From;
        var nodeName = Container.GetNodeDisplayName(fromNodeId);
        var ownNodeNum = Container.MyNodeInfo?.MyNodeNum ?? 0;

        // 0xFFFFFFFF is the broadcast address; anything else is a DM
        uint? destNodeId = packet.To != 0xFFFFFFFF && packet.To != 0 ? packet.To : null;

        _logger.LogDebug("Message received from node {FromNodeId} ({NodeName}) on channel {Channel}: {MessageLength} bytes",
            fromNodeId, nodeName, packet.Channel, text.Length);

        _messages.Add(new ChatMessage
        {
            Text = text,
            FromNodeId = fromNodeId,
            FromNodeName = nodeName,
            IsOwn = fromNodeId == ownNodeNum && ownNodeNum != 0,
            ChannelIndex = (int)packet.Channel,
            DestNodeId = destNodeId,
            PacketId = packet.Id
        });
    }

    private void HandleTraceroute(MeshPacket packet)
    {
        var rd = RouteDiscovery.Parser.ParseFrom(packet.Decoded.Payload);
        // The route list contains the intermediate hops; the destination is packet.From
        var route = new List<uint>(rd.Route) { packet.From };
        _tracerouteResults[packet.From] = route;
        _logger.LogDebug("Traceroute result from {NodeId}: {Hops} hops", packet.From, route.Count);
    }

    private void HandleTelemetry(MeshPacket packet)
    {
        var telemetry = Telemetry.Parser.ParseFrom(packet.Decoded.Payload);
        var nodeId = packet.From;

        if (!_nodeTelemetry.TryGetValue(nodeId, out var record))
        {
            record = new NodeTelemetry();
            _nodeTelemetry[nodeId] = record;
        }

        record.LastUpdated = DateTime.Now;

        if (telemetry.DeviceMetrics is { } dev)
        {
            if (dev.BatteryLevel > 0) record.BatteryLevel = dev.BatteryLevel;
            if (dev.Voltage > 0) record.Voltage = dev.Voltage;
            if (dev.ChannelUtilization > 0) record.ChannelUtilization = dev.ChannelUtilization;
            if (dev.AirUtilTx > 0) record.AirUtilTx = dev.AirUtilTx;
            if (dev.UptimeSeconds > 0) record.UptimeSeconds = dev.UptimeSeconds;
        }

        if (telemetry.EnvironmentMetrics is { } env)
        {
            if (env.Temperature != 0) record.Temperature = env.Temperature;
            if (env.RelativeHumidity != 0) record.RelativeHumidity = env.RelativeHumidity;
            if (env.BarometricPressure != 0) record.BarometricPressure = env.BarometricPressure;
            if (env.WindSpeed != 0) record.WindSpeed = env.WindSpeed;
            if (env.WindDirection != 0) record.WindDirection = env.WindDirection;
            if (env.Lux != 0) record.Lux = env.Lux;
        }

        _logger.LogDebug("Telemetry from node {NodeId}: DevMetrics={HasDev} EnvMetrics={HasEnv}",
            nodeId, telemetry.DeviceMetrics != null, telemetry.EnvironmentMetrics != null);
    }

    private void HandleRouting(MeshPacket packet)
    {
        var requestId = packet.Decoded.RequestId;
        if (requestId == 0 || !_pendingAcks.TryGetValue(requestId, out var pendingMsg))
            return;

        var routing = Routing.Parser.ParseFrom(packet.Decoded.Payload);
        if (routing.ErrorReason == Routing.Types.Error.None)
            pendingMsg.IsAcked = true;
        else
            pendingMsg.IsFailed = true;

        _pendingAcks.Remove(requestId);
        _logger.LogDebug("ACK received for packet {PacketId}: {Error}", requestId, routing.ErrorReason);
    }

    public void Dispose()
    {
        _readCts?.Cancel();
        _readCts?.Dispose();
        _connection?.Disconnect();
        _sendLock.Dispose();
    }
}
