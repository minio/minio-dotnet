namespace Minio.Extensions
{
    using System;
    using System.Runtime.InteropServices;
    using ObjCRuntime;
    using UIKit;

    internal static class DeviceExtension
    {
        public static string GetModelName (this UIDevice device)
        {
            return GetNameByVersion (Version);
        }

        public const string HardwareProperty = "hw.machine";


        [DllImport (Constants.SystemLibrary)]
        internal static extern int sysctlbyname ([MarshalAs (UnmanagedType.LPStr)] string property, // name of the property 
                                                  IntPtr output, // output 
                                                 IntPtr oldLen, // IntPtr.Zero 
                                                IntPtr newp, // IntPtr.Zero 
                                             uint newlen // 0 
                                               );
        public static string Version {
            get {
                try {
                    // get the length of the string that will be returned 
                    var pLen = Marshal.AllocHGlobal (sizeof (int));
                    sysctlbyname (HardwareProperty, IntPtr.Zero, pLen, IntPtr.Zero, 0);

                    var length = Marshal.ReadInt32 (pLen);

                    // check to see if we got a length 
                    if (length == 0) {
                        Marshal.FreeHGlobal (pLen);
                        return "Unknown";
                    }

                    // get the hardware string 
                    var pStr = Marshal.AllocHGlobal (length);
                    sysctlbyname (HardwareProperty, pStr, pLen, IntPtr.Zero, 0);

                    // convert the native string into a C# string 
                    var hardwareStr = Marshal.PtrToStringAnsi (pStr);

                    // cleanup 
                    Marshal.FreeHGlobal (pLen);
                    Marshal.FreeHGlobal (pStr);

                    return hardwareStr;
                } catch (Exception ex) {
                    Console.WriteLine ("DeviceHardware.Version Ex: " + ex.Message);
                }


                return "Unknown";
            }
        }

        static string GetNameByVersion (string hardware)
        {
            // http://support.apple.com/kb/HT3939
            if (hardware.StartsWith ("iPhone")) {
                // ************
                // iPhone
                // ************
                // Model(s): A1203
                // Apple Tech specs: http://support.apple.com/kb/SP2
                if (hardware == "iPhone1,1")
                    return "iPhone";

                // ************
                // iPhone 3G
                // ************
                // Model(s): A1241 & A1324
                // Apple Tech specs: http://support.apple.com/kb/SP495
                if (hardware == "iPhone1,2")
                    return "iPhone 3G";

                // ************
                // iPhone 3GS
                // ************
                // Model(s): A1303 & A1325
                // Apple Tech specs: http://support.apple.com/kb/SP565
                if (hardware == "iPhone2,1")
                    return "iPhone 3GS";

                // ************
                // iPhone 4
                // ************
                // Model(s): A1332
                // Apple Tech specs: http://support.apple.com/kb/SP587
                if (hardware == "iPhone3,1" || hardware == "iPhone3,2")
                    return "iPhone 4 GSM";
                // Model(s): A1349
                if (hardware == "iPhone3,3")
                    return "iPhone 4 CDMA";

                // ************
                // iPhone 4S
                // ************
                // Model(s): A1387 & A1431
                // Apple Tech specs: http://support.apple.com/kb/SP643
                if (hardware == "iPhone4,1")
                    return "iPhone 4S";

                // ************
                // iPhone 5
                // ************
                // Model(s): A1428
                // Apple Tech specs: http://support.apple.com/kb/SP655
                if (hardware == "iPhone5,1")
                    return "iPhone 5 GSM";

                // Model(s): A1429 & A1442
                if (hardware == "iPhone5,2")
                    return "iPhone 5 Global";

                // ************
                // iPhone 5C
                // ************
                // Model(s): A1456 & A1532
                // Apple Tech specs: http://support.apple.com/kb/SP684
                if (hardware == "iPhone5,3")
                    return "iPhone 5C GSM";

                // Model(s): A1507, A1516, A1526 & A1529
                if (hardware == "iPhone5,4")
                    return "iPhone 5C Global";

                // ************
                // iPhone 5S
                // ************
                // Model(s): A1453 & A1533
                // Apple Tech specs: http://support.apple.com/kb/SP685
                if (hardware == "iPhone6,1")
                    return "iPhone 5S GSM";

                // Model(s): A1457, A1518, A1528 & A1530    
                if (hardware == "iPhone6,2")
                    return "iPhone 5S Global";

                // ************
                // iPhone 6
                // ************
                // Model(s): A1549, A1586 & A1589
                // Apple Tech specs: http://support.apple.com/kb/SP705
                if (hardware == "iPhone7,2")
                    return "iPhone 6";

                // ************
                // iPhone 6 Plus
                // ************
                // Model(s): A1522, A1524 & A1593
                // Apple Tech specs: http://support.apple.com/kb/SP706
                if (hardware == "iPhone7,1")
                    return "iPhone 6 Plus";

                // ************
                // iPhone 6S
                // ************
                // Model(s): A1633, A1688 & A1700
                // Apple Tech specs: http://support.apple.com/kb/SP726
                if (hardware == "iPhone8,1")
                    return "iPhone 6S";

                // ************
                // iPhone 6S Plus
                // ************
                // Model(s): A1634, A1687 & A1699
                // Apple Tech specs: http://support.apple.com/kb/SP727
                if (hardware == "iPhone8,2")
                    return "iPhone 6S Plus";

                // ************
                // iPhone SE
                // ************
                // Model(s): A1662 & A1723
                // Apple Tech specs: https://support.apple.com/kb/SP738
                if (hardware == "iPhone8,4")
                    return "iPhone SE";
            }

            if (hardware.StartsWith ("iPod")) {
                // ************
                // iPod touch
                // ************
                // Model(s): A1213
                // Apple Tech specs: http://support.apple.com/kb/SP3
                if (hardware == "iPod1,1")
                    return "iPod touch";

                // ************
                // iPod touch 2G
                // ************
                // Model(s): A1288
                // Apple Tech specs: http://support.apple.com/kb/SP496
                if (hardware == "iPod2,1")
                    return "iPod touch 2G";

                // ************
                // iPod touch 3G
                // ************
                // Model(s): A1318
                // Apple Tech specs: http://support.apple.com/kb/SP570
                if (hardware == "iPod3,1")
                    return "iPod touch 3G";

                // ************
                // iPod touch 4G
                // ************
                // Model(s): A1367
                // Apple Tech specs: http://support.apple.com/kb/SP594
                if (hardware == "iPod4,1")
                    return "iPod touch 4G";

                // ************
                // iPod touch 5G
                // ************
                // Model(s): A1421 & A1509
                // Apple Tech specs: (A1421) http://support.apple.com/kb/SP657 & (A1509) http://support.apple.com/kb/SP675
                if (hardware == "iPod5,1")
                    return "iPod touch 5G";

                // ************
                // iPod touch 6G
                // ************
                // Model(s): A1574
                // Apple Tech specs: (A1574) https://support.apple.com/kb/SP720 
                if (hardware == "iPod7,1")
                    return "iPod touch 6G";
            }

            if (hardware.StartsWith ("iPad")) {
                // ************
                // iPad
                // ************
                // Model(s): A1219 (WiFi) & A1337 (GSM)
                // Apple Tech specs: http://support.apple.com/kb/SP580
                if (hardware == "iPad1,1")
                    return "iPad";

                // ************
                // iPad 2
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP622
                // Model(s): A1395
                if (hardware == "iPad2,1")
                    return "iPad 2 WiFi";
                // Model(s): A1396
                if (hardware == "iPad2,2")
                    return "iPad 2 GSM";
                // Model(s): A1397
                if (hardware == "iPad2,3")
                    return "iPad 2 CDMA";
                // Model(s): A1395
                if (hardware == "iPad2,4")
                    return "iPad 2 Wifi";

                // ************
                // iPad 3
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP647
                // Model(s): A1416
                if (hardware == "iPad3,1")
                    return "iPad 3 WiFi";
                // Model(s): A1403
                if (hardware == "iPad3,2")
                    return "iPad 3 Wi-Fi + Cellular (VZ)";
                // Model(s): A1430
                if (hardware == "iPad3,3")
                    return "iPad 3 Wi-Fi + Cellular";

                // ************
                // iPad 4
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP662
                // Model(s): A1458
                if (hardware == "iPad3,4")
                    return "iPad 4 Wifi";
                // Model(s): A1459
                if (hardware == "iPad3,5")
                    return "iPad 4 Wi-Fi + Cellular";
                // Model(s): A1460
                if (hardware == "iPad3,6")
                    return "iPad 4 Wi-Fi + Cellular (MM)";

                // ************
                // iPad Air
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP692
                // Model(s): A1474
                if (hardware == "iPad4,1")
                    return "iPad Air Wifi";
                // Model(s): A1475
                if (hardware == "iPad4,2")
                    return "iPad Air Wi-Fi + Cellular";
                // Model(s): A1476
                if (hardware == "iPad4,3")
                    return "iPad Air Wi-Fi + Cellular (TD-LTE)";

                // ************
                // iPad Air 2
                // ************
                // Apple Tech specs: 
                // Model(s): A1566
                if (hardware == "iPad5,3")
                    return "iPad Air 2";
                // Model(s): A1567
                if (hardware == "iPad5,4")
                    return "iPad Air 2";

                // ************
                // iPad Pro
                // ************
                // Apple Tech specs: https://support.apple.com/kb/SP723
                // Model(s): A1584 (Wi-Fi) 
                if (hardware == "iPad6,7")
                    return "iPad Pro";
                // Model(s): A1652 (Wi-Fi + Cellular)
                if (hardware == "iPad6,8")
                    return "iPad Pro Wi-Fi + Cellular";

                // ************
                // iPad Pro (9.7-inch)
                // ************
                // Apple Tech specs: https://support.apple.com/kb/SP739
                // Model(s): A1673
                if (hardware == "iPad6,3")
                    return "iPad Pro (9.7-inch)";
                // Model(s): A1674, A1675 (Wi-Fi + Cellular)
                if (hardware == "iPad6,8")
                    return "iPad Pro (9.7-inch) Wi-Fi + Cellular";

                // ************
                // iPad mini
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP661
                // Model(s): A1432
                if (hardware == "iPad2,5")
                    return "iPad mini Wifi";
                // Model(s): A1454
                if (hardware == "iPad2,6")
                    return "iPad mini Wi-Fi + Cellular";
                // Model(s): A1455
                if (hardware == "iPad2,7")
                    return "iPad mini Wi-Fi + Cellular (MM)";

                // ************
                // iPad mini 2
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP693
                // Model(s): A1489
                if (hardware == "iPad4,4")
                    return "iPad mini 2 Wifi";
                // Model(s): A1490
                if (hardware == "iPad4,5")
                    return "iPad mini 2 Wi-Fi + Cellular";
                // Model(s): A1491
                if (hardware == "iPad4,6")
                    return "iPad mini 2 Wi-Fi + Cellular (TD-LTE)";

                // ************
                // iPad mini 3
                // ************
                // Apple Tech specs: 
                // Model(s): A1599
                if (hardware == "iPad4,7")
                    return "iPad mini 3 Wifi";
                // Model(s): A1600
                if (hardware == "iPad4,8")
                    return "iPad mini 3 Wi-Fi + Cellular";
                // Model(s): A1601
                if (hardware == "iPad4,9")
                    return "iPad mini 3 Wi-Fi + Cellular (TD-LTE)";

                // ************
                // iPad mini 4
                // ************
                // Apple Tech specs: http://support.apple.com/kb/SP725
                // Model(s): A1538
                if (hardware == "iPad5,1")
                    return "iPad mini 4";
                // Model(s): A1550
                if (hardware == "iPad5,2")
                    return " iPad mini 4 Wi-Fi + Cellular";
            }

            if (hardware == "i386" || hardware == "x86_64")
                return "Simulator";

            return (hardware == "" ? "Unknown" : hardware);
        }

    }
}
