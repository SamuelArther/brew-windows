using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WinTools;

// SwitchAudioSource (switchaudio-osx): list audio endpoints and switch the default playback/recording
// device or mute it. Endpoints come from the Core Audio IMMDeviceEnumerator; the default is set through
// IPolicyConfig, the interface the Sound control panel itself uses (Windows has no public API for it).
// "system" (macOS's alert-sound device) maps to Windows' default communications device.
static unsafe partial class SwitchaudioOsx
{
    [ModuleInitializer]
    internal static void Register() => Commands.Add("SwitchAudioSource", "Change the default audio device from the command-line", Run);

    sealed class Device
    {
        public string Name, Uid, Type;
        public uint Id;
    }

    static int Run(string[] args)
    {
        bool all = false, current = false, next = false;
        string type = null, format = "human", setName = null, setUid = null, mute = null;
        uint? setId = null;
        for (var i = 0; i < args.Length; i++)
        {
            string Value() => ++i < args.Length ? args[i] : throw new UsageException($"{args[i - 1]} needs a value");
            switch (args[i])
            {
                case "-h" or "--help":
                    Console.WriteLine("""
                        Usage: SwitchAudioSource [-a] [-c] [-t type] [-n] -s device_name | -i device_id | -u device_uid
                          -a             : shows all devices
                          -c             : shows current device
                          -f format      : output format (cli/human/json). Defaults to human.
                          -t type        : device type (input/output/system/all).  Defaults to output.
                          -m mute        : sets the mute status (mute/unmute/toggle).  For input/output only.
                          -n             : cycles the audio device to the next one
                          -i device_id   : sets the audio device to the given device by id
                          -u device_uid  : sets the audio device to the given device by uid or a substring of the uid
                          -s device_name : sets the audio device to the given device by name
                        On Windows, "system" is the default communications device.
                        """);
                    return 0;
                case "-a": all = true; break;
                case "-c": current = true; break;
                case "-n": next = true; break;
                case "-t": type = Value(); break;
                case "-f": format = Value(); break;
                case "-s": setName = Value(); break;
                case "-u": setUid = Value(); break;
                case "-i": setId = uint.TryParse(Value(), out var id) ? id : throw new UsageException("-i needs a numeric device id"); break;
                case "-m": mute = Value(); break;
                default: throw new UsageException($"unknown option {args[i]}");
            }
        }
        if (type != null && type is not ("input" or "output" or "system" or "all")) throw new UsageException($"invalid device type '{type}'");
        if (format is not ("human" or "cli" or "json")) throw new UsageException($"invalid format '{format}'");
        if (mute != null && mute is not ("mute" or "unmute" or "toggle")) throw new UsageException($"invalid mute action '{mute}'");
        if (!all && !current && !next && setName == null && setUid == null && setId == null && mute == null)
            throw new UsageException("nothing to do (see SwitchAudioSource -h)");

        try
        {
            var enumerator = Enumerator();
            if (all)
            {
                foreach (var d in Devices(enumerator, type ?? "all")) Console.WriteLine(Format(d, format));
                return 0;
            }
            var t = type ?? "output";
            if (t == "all") throw new UsageException("-t all only works with -a");
            if (current)
            {
                var d = Default(enumerator, t) ?? throw new InvalidOperationException($"there is no default {t} device");
                Console.WriteLine(format == "human" ? d.Name : Format(d, format));
                return 0;
            }
            if (mute != null)
            {
                if (t == "system") throw new UsageException("-m works with input or output devices");
                var d = Default(enumerator, t) ?? throw new InvalidOperationException($"there is no default {t} device");
                return Mute(enumerator, d, mute);
            }
            var devices = Devices(enumerator, t);
            Device target;
            if (next)
            {
                if (devices.Count == 0) throw new InvalidOperationException($"no {t} devices");
                var cur = Default(enumerator, t);
                var index = cur == null ? -1 : devices.FindIndex(d => d.Uid == cur.Uid);
                target = devices[(index + 1) % devices.Count];
            }
            else
            {
                target = setName != null ? devices.FirstOrDefault(d => d.Name == setName) ?? devices.FirstOrDefault(d => d.Name.Equals(setName, StringComparison.OrdinalIgnoreCase))
                       : setUid != null ? devices.FirstOrDefault(d => d.Uid.Equals(setUid, StringComparison.OrdinalIgnoreCase)) ?? devices.FirstOrDefault(d => d.Uid.Contains(setUid, StringComparison.OrdinalIgnoreCase))
                       : devices.FirstOrDefault(d => d.Id == setId);
                if (target == null)
                {
                    Console.Error.WriteLine($"Could not find an audio device {(setName != null ? $"named \"{setName}\"" : setUid != null ? $"with uid \"{setUid}\"" : $"with id {setId}")} of type {t}.  Nothing was changed.");
                    return 1;
                }
            }
            SetDefault(target, t);
            Console.WriteLine($"{t} audio device set to \"{target.Name}\"");
            return 0;
        }
        catch (COMException e) { throw new InvalidOperationException($"the Windows audio service failed (0x{e.HResult:x8}) - is the Windows Audio service running?"); }
    }

    static string Format(Device d, string format) => format switch
    {
        "cli" => $"{d.Name},{d.Type},{d.Id},{d.Uid}",
        "json" => $"{{\"name\": {MediaControl.JsonString(d.Name)}, \"type\": \"{d.Type}\", \"id\": \"{d.Id}\", \"uid\": {MediaControl.JsonString(d.Uid)}}}",
        _ => $"{d.Name} ({d.Type})",
    };

    static List<Device> Devices(IMMDeviceEnumerator enumerator, string type)
    {
        var list = new List<Device>();
        if (type is "output" or "system" or "all") list.AddRange(List(enumerator, eRender, "output"));
        if (type is "input" or "all") list.AddRange(List(enumerator, eCapture, "input"));
        return list;
    }

    static IEnumerable<Device> List(IMMDeviceEnumerator enumerator, int flow, string type)
    {
        var collection = enumerator.EnumAudioEndpoints(flow, DEVICE_STATE_ACTIVE);
        var count = collection.GetCount();
        for (uint i = 0; i < count; i++) yield return Describe(collection.Item(i), type);
    }

    static Device Default(IMMDeviceEnumerator enumerator, string type)
    {
        var hr = enumerator.GetDefaultAudioEndpoint(type == "input" ? eCapture : eRender, type == "system" ? eCommunications : eConsole, out var device);
        return hr == 0 && device != null ? Describe(device, type == "input" ? "input" : "output") : null;   // E_NOTFOUND: none
    }

    static Device Describe(IMMDevice device, string type)
    {
        var uid = device.GetId();
        var store = device.OpenPropertyStore(STGM_READ);
        var key = new PROPERTYKEY { fmtid = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), pid = 14 };   // PKEY_Device_FriendlyName
        string name = null;
        if (store.GetValue(&key, out var value) == 0)
        {
            if (value.vt == VT_LPWSTR) name = Marshal.PtrToStringUni(value.p);
            PropVariantClear(ref value);
        }
        // Stable numeric id (macOS ids are integers): FNV-1a of the endpoint id.
        var hash = 2166136261u;
        foreach (var c in uid) hash = (hash ^ c) * 16777619u;
        return new Device { Name = name ?? uid, Uid = uid, Type = type, Id = hash & 0x7fffffff };
    }

    static void SetDefault(Device device, string type)
    {
        var clsid = new Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9");
        var policy = Create<IPolicyConfig>(clsid, new Guid("f8679f50-850a-41cf-9c72-430f290290c8"));
        // macOS has one default per direction; Windows has console/multimedia/communications roles.
        if (type == "system") policy.SetDefaultEndpoint(device.Uid, eCommunications);
        else
        {
            policy.SetDefaultEndpoint(device.Uid, eConsole);
            policy.SetDefaultEndpoint(device.Uid, eMultimedia);
        }
    }

    static int Mute(IMMDeviceEnumerator enumerator, Device device, string action)
    {
        var iid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        var endpoint = enumerator.GetDevice(device.Uid);
        endpoint.Activate(&iid, CLSCTX_ALL, IntPtr.Zero, out var ptr);
        IAudioEndpointVolume volume;
        try { volume = (IAudioEndpointVolume)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
        var muted = volume.GetMute() != 0;
        var target = action switch { "mute" => true, "unmute" => false, _ => !muted };
        Console.WriteLine($"Setting device {device.Name} to {(target ? "mute" : "unmute")}");
        var context = Guid.Empty;
        volume.SetMute(target ? 1 : 0, &context);
        return 0;
    }

    // ---- COM --------------------------------------------------------------------------------------

    static readonly StrategyBasedComWrappers Wrappers = new();

    static IMMDeviceEnumerator Enumerator() =>
        Create<IMMDeviceEnumerator>(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"), new Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"));

    static T Create<T>(Guid clsid, Guid iid)
    {
        CoInitializeEx(IntPtr.Zero, 0);
        Marshal.ThrowExceptionForHR(CoCreateInstance(in clsid, IntPtr.Zero, CLSCTX_ALL, in iid, out var ptr));
        try { return (T)Wrappers.GetOrCreateObjectForComInstance(ptr, CreateObjectFlags.UniqueInstance); }
        finally { Marshal.Release(ptr); }
    }

    const int eRender = 0, eCapture = 1, eConsole = 0, eMultimedia = 1, eCommunications = 2;
    const uint DEVICE_STATE_ACTIVE = 1, STGM_READ = 0, CLSCTX_ALL = 0x17;
    const ushort VT_LPWSTR = 31;

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPERTYKEY { public Guid fmtid; public uint pid; }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PROPVARIANT { public ushort vt, r1, r2, r3; public IntPtr p, p2; }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    internal partial interface IMMDeviceEnumerator
    {
        IMMDeviceCollection EnumAudioEndpoints(int dataFlow, uint stateMask);
        [PreserveSig] int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice device);
        IMMDevice GetDevice(string id);
        void RegisterEndpointNotificationCallback(IntPtr client);
        void UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [GeneratedComInterface, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    internal partial interface IMMDeviceCollection
    {
        uint GetCount();
        IMMDevice Item(uint index);
    }

    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    internal partial interface IMMDevice
    {
        void Activate(Guid* iid, uint clsCtx, IntPtr activationParams, out IntPtr iface);
        IPropertyStore OpenPropertyStore(uint access);
        string GetId();
        uint GetState();
    }

    [GeneratedComInterface, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    internal partial interface IPropertyStore
    {
        uint GetCount();
        void GetAt(uint index, PROPERTYKEY* key);
        [PreserveSig] int GetValue(PROPERTYKEY* key, out PROPVARIANT value);
    }

    // Undocumented; SetDefaultEndpoint is the 11th method.
    [GeneratedComInterface(StringMarshalling = StringMarshalling.Utf16), Guid("f8679f50-850a-41cf-9c72-430f290290c8")]
    internal partial interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat();
        [PreserveSig] int GetDeviceFormat();
        [PreserveSig] int ResetDeviceFormat();
        [PreserveSig] int SetDeviceFormat();
        [PreserveSig] int GetProcessingPeriod();
        [PreserveSig] int SetProcessingPeriod();
        [PreserveSig] int GetShareMode();
        [PreserveSig] int SetShareMode();
        [PreserveSig] int GetPropertyValue();
        [PreserveSig] int SetPropertyValue();
        void SetDefaultEndpoint(string deviceId, int role);
    }

    [GeneratedComInterface, Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
    internal partial interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr notify);
        void UnregisterControlChangeNotify(IntPtr notify);
        uint GetChannelCount();
        void SetMasterVolumeLevel(float level, Guid* context);
        void SetMasterVolumeLevelScalar(float level, Guid* context);
        float GetMasterVolumeLevel();
        float GetMasterVolumeLevelScalar();
        void SetChannelVolumeLevel(uint channel, float level, Guid* context);
        void SetChannelVolumeLevelScalar(uint channel, float level, Guid* context);
        float GetChannelVolumeLevel(uint channel);
        float GetChannelVolumeLevelScalar(uint channel);
        void SetMute(int mute, Guid* context);
        int GetMute();
    }

    [DllImport("ole32.dll")]
    static extern int CoInitializeEx(IntPtr reserved, uint coinit);

    [DllImport("ole32.dll")]
    static extern int CoCreateInstance(in Guid clsid, IntPtr outer, uint context, in Guid iid, out IntPtr obj);

    [DllImport("ole32.dll")]
    static extern int PropVariantClear(ref PROPVARIANT value);
}
