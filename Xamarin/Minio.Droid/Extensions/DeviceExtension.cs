namespace Minio.Extensions
{
    using Android.OS;

    internal static class DeviceExtension
    {
        public static string GetArchType()
        {
            return IsEmulator ? "SIMULATOR" : "DEVICE";
        }

        private static bool IsEmulator
        {
            get
            {
                var fing = Build.Fingerprint;
                var isEmulator = false;
                if (fing != null)
                {
                    isEmulator = fing.Contains("vbox") || fing.Contains("generic");
                }

                return isEmulator;
            }
        }
    }
}