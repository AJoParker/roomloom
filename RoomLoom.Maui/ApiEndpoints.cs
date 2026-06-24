using Microsoft.Maui.Devices;

namespace RoomLoom.Maui;

public static class ApiEndpoints
{
    public static string ApiBaseUrl =>
        DeviceInfo.Platform == DevicePlatform.Android
            ? "http://10.0.2.2:5150"
            : "http://localhost:5150";

    public static string HubUrl => $"{ApiBaseUrl}/hubs/session";
}
