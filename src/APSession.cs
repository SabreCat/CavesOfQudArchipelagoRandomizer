using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using XRL;

public static class APSession
{
    private static readonly Assembly _assembly = Assembly.LoadFrom(
        DataManager.SavePath(
            @"Mods/CavesOfQudArchipelagoRandomizer/thirdparty/Archipelago.MultiClient.Net.dll"
        )
    );

    private static string _name;
    private static string _password;

    private static object _session;
    private static object _socket;
    private static object _items;
    private static object _locations;
    private static object _messageLog;
    private static object _roomState;
    private static object _dataStorage;
    private static object _deathLink;

    public static bool Connect(
        string host,
        string name,
        string password,
        out Dictionary<string, object> slotData,
        out string[] errors
    )
    {
        _name = name;
        _password = password;

        var sessionFactory = _assembly.GetType(
            "Archipelago.MultiClient.Net.ArchipelagoSessionFactory"
        );
        _session = sessionFactory
            .GetMethod("CreateSession", new Type[] { typeof(string), typeof(int) })
            .Invoke(null, new[] { host, null });
        _socket = _session.GetType().GetProperty("Socket").GetValue(_session);
        _items = _session.GetType().GetProperty("Items").GetValue(_session);
        _locations = _session.GetType().GetProperty("Locations").GetValue(_session);
        _messageLog = _session.GetType().GetProperty("MessageLog").GetValue(_session);
        _roomState = _session.GetType().GetProperty("RoomState").GetValue(_session);
        _dataStorage = _session.GetType().GetProperty("DataStorage").GetValue(_session);

        var deathLinkProvider = _assembly.GetType(
            "Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLinkProvider"
        );
        _deathLink = deathLinkProvider.GetMethod("CreateDeathLinkService").Invoke(null, new object[] { _session });

        return Reconnect(out slotData, out errors);
    }

    public static bool Connected
    {
        get
        {
            if (_socket != null)
            {
                return (bool)_socket.GetType().GetProperty("Connected").GetValue(_socket);
            }

            return false;
        }
    }

    public static string Seed
    {
        get { return (string)_roomState.GetType().GetProperty("Seed").GetValue(_roomState); }
    }

    public static IEnumerable<(long, string)> CheckedLocations
    {
        get
        {
            var allLocationsChecked =
                (IEnumerable<long>)
                    _locations.GetType().GetProperty("AllLocationsChecked").GetValue(_locations);
            return allLocationsChecked.Select(id =>
            {
                var name = (string)
                    _locations
                        .GetType()
                        .GetMethod("GetLocationNameFromId")
                        .Invoke(_locations, new object[] { id, null });
                return (id, name);
            });
        }
    }

    public static IEnumerable<(long, string)> MissingLocations
    {
        get
        {
            var allLocationsChecked =
                (IEnumerable<long>)
                    _locations.GetType().GetProperty("AllMissingLocations").GetValue(_locations);
            return allLocationsChecked.Select(id =>
            {
                var name = (string)
                    _locations
                        .GetType()
                        .GetMethod("GetLocationNameFromId")
                        .Invoke(_locations, new object[] { id, null });
                return (id, name);
            });
        }
    }

    public static IEnumerable<(long, string)> ReceivedItems
    {
        get
        {
            var items =
                (IEnumerable<object>)
                    _items.GetType().GetProperty("AllItemsReceived").GetValue(_items);

            return items.Select(item =>
            {
                var name = (string)item.GetType().GetProperty("ItemName").GetValue(item);
                var id = (long)item.GetType().GetProperty("ItemId").GetValue(item);

                return (id, name);
            });
        }
    }

    public static bool Reconnect(out Dictionary<string, object> slotData, out string[] errors)
    {
        Disconnect();
        AddEventHandlers();

        var loginResult = _session
            .GetType()
            .GetMethod("TryConnectAndLogin")
            .Invoke(
                _session,
                new object[] { "Caves of Qud", _name, 7, null, null, null, _password, true }
            );

        if (loginResult.GetType().Name == "LoginFailure")
        {
            slotData = null;
            errors = (string[])loginResult.GetType().GetProperty("Errors").GetValue(loginResult);
            return false;
        }
        else
        {
            slotData =
                (Dictionary<string, object>)
                    loginResult.GetType().GetProperty("SlotData").GetValue(loginResult);
            errors = null;
            return true;
        }
    }

    public static void Disconnect()
    {
        RemoveEventHandlers();

        if (Connected)
        {
            // TODO unregister helpers here
            ((Task)_socket.GetType().GetMethod("DisconnectAsync").Invoke(_socket, null)).Wait();
        }
    }

    public static void CheckLocations(params long[] ids)
    {
        _locations
            .GetType()
            .GetMethod("CompleteLocationChecks")
            .Invoke(_locations, new object[] { ids });
    }

    public static void ScoutLocations(params long[] ids)
    {
        var hintPolicyType = _assembly.GetType("Archipelago.MultiClient.Net.Enums.HintCreationPolicy");

        _locations
            .GetType()
            .GetMethod("ScoutLocationsAsync", new Type[] { hintPolicyType, typeof(long[]) })
            .Invoke(_locations, new object[] { 2, ids });
    }

    public static void SetGoalAchieved()
    {
        _session.GetType().GetMethod("SetGoalAchieved").Invoke(_session, null);
    }

    public static void EnableDeathLink()
    {
        _deathLink.GetType().GetMethod("EnableDeathLink").Invoke(_deathLink, null);
    }

    public static void DisableDeathLink()
    {
        _deathLink.GetType().GetMethod("DisableDeathLink").Invoke(_deathLink, null);
    }

    public static void SendDeathLink(string player, string cause)
    {
        var deathLinkType = _assembly.GetType("Archipelago.MultiClient.Net.BounceFeatures.DeathLink.DeathLink");
        object deathLink = deathLinkType.GetConstructor(new Type[] { typeof(string), typeof(string) }).Invoke(new object[] { player, cause });

        _deathLink.GetType().GetMethod("SendDeathLink").Invoke(_deathLink, new object[] { deathLink });
    }

    public static void SetDataStorageEntry<T>(string key, T value)
    {
        var scopeType = _assembly.GetType("Archipelago.MultiClient.Net.Enums.Scope");
        var dseType = _assembly.GetType("Archipelago.MultiClient.Net.Models.DataStorageElement");

        // Conversion to a DataStorageElement using one of the implicit operator definitions
        var opMethod = dseType.GetMethod(
            "op_Implicit",
            BindingFlags.Static | BindingFlags.Public,
            null,
            new[] { value.GetType() },
            null
        );
        object dseValue = opMethod.Invoke(null, new object[] { value });

        var setMethod = _dataStorage
            .GetType()
            .GetProperty("Item", new Type[] { scopeType, typeof(string) })
            .GetSetMethod();
        // scope 3 means this slot only
        setMethod.Invoke(_dataStorage, new object[] { 3, key, dseValue });
    }

    public static T GetDataStorageEntry<T>(string key)
    {
        var scopeType = _assembly.GetType("Archipelago.MultiClient.Net.Enums.Scope");

        var getMethod = _dataStorage
            .GetType()
            .GetProperty("Item", new Type[] { scopeType, typeof(string) })
            .GetGetMethod();
        // scope 3 means this slot only
        var dseValue = getMethod.Invoke(_dataStorage, new object[] { 3, key });

        var toMethodGeneric = dseValue.GetType().GetMethod("To");
        var toMethodSpecific = toMethodGeneric.MakeGenericMethod(typeof(T));
        object value = toMethodSpecific.Invoke(dseValue, new object[] { });

        return (T)value;
    }

    // private static Delegate _packetReceivedDelegate;
    private static Delegate _onMessageReceivedDelegate;
    private static Delegate _itemReceivedDelegate;
    private static Delegate _onDeathLinkReceivedDelegate;
    private static Delegate _errorReceivedDelegate;

    private static void AddEventHandlers()
    {
        // {
        //     var ev = _socket.GetType().GetEvent("PacketReceived");
        //     _packetReceivedDelegate = Delegate.CreateDelegate(ev.EventHandlerType, this,
        //         typeof(APSession).GetMethod("OnPacketReceived",
        //         BindingFlags.Instance | BindingFlags.NonPublic));
        //     ev.AddEventHandler(_socket, _packetReceivedDelegate);
        // }

        {
            var ev = _messageLog.GetType().GetEvent("OnMessageReceived");
            _onMessageReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnMessageReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_messageLog, _onMessageReceivedDelegate);
        }

        {
            var ev = _items.GetType().GetEvent("ItemReceived");
            _itemReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnItemReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_items, _itemReceivedDelegate);
        }

        {
            var ev = _deathLink.GetType().GetEvent("OnDeathLinkReceived");
            _onDeathLinkReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnDeathLinkReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_deathLink, _onDeathLinkReceivedDelegate);
        }

        {
            var ev = _socket.GetType().GetEvent("ErrorReceived");
            _errorReceivedDelegate = Delegate.CreateDelegate(
                ev.EventHandlerType,
                null,
                typeof(APSession).GetMethod(
                    "OnSocketErrorReceived",
                    BindingFlags.Static | BindingFlags.NonPublic
                )
            );
            ev.AddEventHandler(_socket, _errorReceivedDelegate);
        }
    }

    private static void RemoveEventHandlers()
    {
        if (_session == null)
        {
            return;
        }
        // {
        //     var ev = _messageLog.GetType().GetEvent("OnMessageReceived");
        //     ev.RemoveEventHandler(_messageLog, _packetReceivedDelegate);
        // }

        {
            var ev = _messageLog.GetType().GetEvent("OnMessageReceived");
            ev.RemoveEventHandler(_messageLog, _onMessageReceivedDelegate);
        }

        {
            var ev = _items.GetType().GetEvent("ItemReceived");
            ev.RemoveEventHandler(_items, _itemReceivedDelegate);
        }

        {
            var ev = _deathLink.GetType().GetEvent("OnDeathLinkReceived");
            ev.RemoveEventHandler(_deathLink, _onDeathLinkReceivedDelegate);
        }

        {
            var ev = _socket.GetType().GetEvent("ErrorReceived");
            ev.RemoveEventHandler(_socket, _errorReceivedDelegate);
        }
    }

    // private void OnPacketReceived(object data)
    // {
    //     var type = data.GetType().GetProperty("PacketType").GetValue(data);

    //     APEvents.OnPacketReceived(type.ToString());
    // }

    private static void OnMessageReceived(object data)
    {
        var parts = (IEnumerable<object>)data.GetType().GetProperty("Parts").GetValue(data);

        var message = parts.Select(part =>
        {
            var text = (string)part.GetType().GetProperty("Text").GetValue(part);
            var color = (int?)part.GetType().GetProperty("PaletteColor").GetValue(part);
            var isBackground = (bool)part.GetType().GetProperty("IsBackgroundColor").GetValue(part);

            return (text, color, isBackground);
        });

        APEvents.OnMessageReceived(message);
    }

    private static void OnItemReceived(object data)
    {
        var index = (int)data.GetType().GetProperty("Index").GetValue(data);
        var item = data.GetType().GetMethod("DequeueItem").Invoke(data, null);
        var name = (string)item.GetType().GetProperty("ItemName").GetValue(item);
        var id = (long)item.GetType().GetProperty("ItemId").GetValue(item);

        APEvents.OnItemReceived(id, name, index);
    }

    private static void OnDeathLinkReceived(object data)
    {
        var source = (string)data.GetType().GetProperty("Source").GetValue(data);
        var cause = (string)data.GetType().GetProperty("Cause").GetValue(data);

        APEvents.OnDeathLinkReceived(source, cause);
    }

    private static void OnSocketErrorReceived(Exception E, string message)
    {
        APEvents.OnSocketErrorReceived(E, message);
    }
}
