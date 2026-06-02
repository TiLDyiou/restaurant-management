using System.Net.Http;

namespace RestaurantManagementGUI.Helpers
{
    public static class HttpsClientHandlerService
    {
        public static HttpMessageHandler GetPlatformMessageHandler()
        {
#if ANDROID
            // Cấu hình tối ưu cho Android
            var handler = new Xamarin.Android.Net.AndroidMessageHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            return handler;
#else
            // Cấu hình cho Windows Desktop
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            return handler;
#endif
        }
    }
}