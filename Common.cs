#region Assembly GSF.Core, Version=2.4.153.0, Culture=neutral, PublicKeyToken=null
// D:\Projects\openHistorian_reference\Source\Dependencies\GSF\GSF.Core.dll
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using Gemstone;
using Gemstone.Collections.CollectionExtensions;
using Gemstone.Console;
using Gemstone.IO;
using Gemstone.Reflection;
using Gemstone.StringExtensions;
using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SnapDB;

//
// Summary:
//     Defines common global functions.
public static class Common
{
    private static ApplicationType? s_applicationType;

    private static string s_osPlatformName;

    private static PlatformID s_osPlatformID = PlatformID.Win32S;

    //
    // Summary:
    //     Determines if the current system is a POSIX style environment.
    //
    // Remarks:
    //     Since a .NET application compiled under Mono can run under both Windows and Unix
    //     style platforms, you can use this property to easily determine the current operating
    //     environment.
    //     This property will return true for both MacOSX and Unix environments. Use the
    //     Platform property of the System.Environment.OSVersion to determine more specific
    //     platform type, e.g., MacOSX or Unix. Note that all flavors of Linux will show
    //     up as System.PlatformID.Unix.
    public static readonly bool IsPosixEnvironment = Path.DirectorySeparatorChar == '/';

    //
    // Summary:
    //     Determines if the code base is currently running under Mono.
    //
    // Remarks:
    //     This property can be used to make a run-time determination if Windows or Mono
    //     based .NET is being used. However, it is highly recommended to use the MONO compiler
    //     directive wherever possible instead of determining this at run-time.
    public static bool IsMono = (object)Type.GetType("Mono.Runtime") != null;

    //
    // Summary:
    //     Gets a high-resolution number of seconds, including fractional seconds, that
    //     have elapsed since 12:00:00 midnight, January 1, 0001.
    public static double SystemTimer => Ticks.ToSeconds(DateTime.UtcNow.Ticks);

    //
    // Summary:
    //     Gets the type of the currently executing application.
    //
    // Returns:
    //     One of the GSF.ApplicationType values.
    public static ApplicationType GetApplicationType()
    {
        if (s_applicationType.HasValue)
        {
            return s_applicationType.Value;
        }

        if (HostingEnvironment.get_IsHosted())
        {
            s_applicationType = ApplicationType.Web;
        }
        else
        {
            try
            {
                FileStream fileStream = new FileStream(AssemblyInfo.EntryAssembly.Location, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                byte[] array = new byte[64];
                byte[] array2 = new byte[248];
                byte[] array3 = new byte[2];
                fileStream.Read(array, 0, array.Length);
                fileStream.Seek(BitConverter.ToInt16(array, 60), SeekOrigin.Begin);
                fileStream.Read(array2, 0, array2.Length);
                fileStream.Close();
                Buffer.BlockCopy(array2, 92, array3, 0, 2);
                s_applicationType = (ApplicationType)LittleEndian.ToInt16(array3, 0);
            }
            catch
            {
                s_applicationType = ApplicationType.Unknown;
            }
        }

        return s_applicationType.Value;
    }

    //
    // Summary:
    //     Gets the operating system System.PlatformID
    //
    // Returns:
    //     The operating system System.PlatformID.
    //
    // Remarks:
    //     This function will properly detect the platform ID, even if running on Mac.
    public static PlatformID GetOSPlatformID()
    {
        if (s_osPlatformID != 0)
        {
            return s_osPlatformID;
        }

        s_osPlatformID = Environment.OSVersion.Platform;
        if (s_osPlatformID == PlatformID.Unix)
        {
            try
            {
                s_osPlatformID = (Command.Execute("uname").StandardOutput.StartsWith("Darwin", StringComparison.OrdinalIgnoreCase) ? PlatformID.MacOSX : PlatformID.Unix);
            }
            catch
            {
                if (Directory.Exists("/Applications") && Directory.Exists("/System") && Directory.Exists("/Users") && Directory.Exists("/Volumes"))
                {
                    s_osPlatformID = PlatformID.MacOSX;
                }
            }
        }

        return s_osPlatformID;
    }

    //
    // Summary:
    //     Gets the operating system product name.
    //
    // Returns:
    //     Operating system product name.
    public static string GetOSProductName()
    {
        if (s_osPlatformName != null)
        {
            return s_osPlatformName;
        }

        PlatformID oSPlatformID = GetOSPlatformID();
        if (oSPlatformID == PlatformID.Unix || oSPlatformID == PlatformID.MacOSX)
        {
            try
            {
                Dictionary<string, string> dictionary = Command.Execute("sw_vers").StandardOutput.ParseKeyValuePairs('\n', ':');
                if (dictionary.Count > 0)
                {
                    s_osPlatformName = dictionary.Values.Select((string val) => val.Trim()).ToDelimitedString(" ");
                }
            }
            catch
            {
                s_osPlatformName = null;
            }

            if (string.IsNullOrEmpty(s_osPlatformName))
            {
                try
                {
                    string[] fileList = FilePath.GetFileList("/etc/*release*");
                    for (int i = 0; i < fileList.Length; i++)
                    {
                        using (StreamReader streamReader = new StreamReader(fileList[i]))
                        {
                            for (string text = streamReader.ReadLine(); text != null; text = streamReader.ReadLine())
                            {
                                if (text.StartsWith("PRETTY_NAME", StringComparison.OrdinalIgnoreCase) && !text.Contains('#'))
                                {
                                    string[] array = text.Split('=');
                                    if (array.Length == 2)
                                    {
                                        s_osPlatformName = array[1].Replace("\"", "");
                                        break;
                                    }
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(s_osPlatformName))
                        {
                            break;
                        }
                    }
                }
                catch
                {
                    try
                    {
                        if (Command.Execute("lsb_release", "-a").StandardOutput.ParseKeyValuePairs('\n', ':').TryGetValue("Description", out s_osPlatformName) && !string.IsNullOrEmpty(s_osPlatformName))
                        {
                            s_osPlatformName = s_osPlatformName.Trim();
                        }
                    }
                    catch
                    {
                        s_osPlatformName = null;
                    }
                }
            }
        }
        else
        {
            try
            {
                s_osPlatformName = Registry.GetValue("HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion", "ProductName", null).ToString();
            }
            catch
            {
                s_osPlatformName = null;
            }
        }

        if (string.IsNullOrWhiteSpace(s_osPlatformName))
        {
            s_osPlatformName = GetOSPlatformID().ToString();
        }

        if (IsMono)
        {
            s_osPlatformName += " using Mono";
        }

        return s_osPlatformName;
    }

    //
    // Summary:
    //     Gets the memory usage by the current process.
    //
    // Returns:
    //     Memory usage by the current process.
    public static long GetProcessMemory()
    {
        long workingSet = Environment.WorkingSet;
        if (workingSet == 0L && IsPosixEnvironment)
        {
            try
            {
                ulong totalPhysicalMemory = GetTotalPhysicalMemory();
                string[] array = Command.Execute("ps", $"-p {Process.GetCurrentProcess().Id} -o %mem").StandardOutput.Split('\n');
                if (array.Length <= 1)
                {
                    return workingSet;
                }

                if (double.TryParse(array[1].Trim(), out var result))
                {
                    return (long)Math.Round(result / 100.0 * (double)totalPhysicalMemory);
                }

                return workingSet;
            }
            catch
            {
                return -1L;
            }
        }

        return workingSet;
    }

    //
    // Summary:
    //     Gets the total physical system memory.
    //
    // Returns:
    //     Total physical system memory.
    public static ulong GetTotalPhysicalMemory()
    {
        if (IsPosixEnvironment)
        {
            return ulong.Parse(Command.Execute("awk", "'/MemTotal/ {print $2}' /proc/meminfo").StandardOutput) * 1024;
        }

        WindowsApi.MEMORYSTATUSEX mEMORYSTATUSEX = new WindowsApi.MEMORYSTATUSEX();
        if (!WindowsApi.GlobalMemoryStatusEx(mEMORYSTATUSEX))
        {
            return 0uL;
        }

        return mEMORYSTATUSEX.ullTotalPhys;
    }

    //
    // Summary:
    //     Gets the available physical system memory.
    //
    // Returns:
    //     Total available system memory.
    public static ulong GetAvailablePhysicalMemory()
    {
        if (IsPosixEnvironment)
        {
            return ulong.Parse(Command.Execute("awk", "'/MemAvailable/ {print $2}' /proc/meminfo").StandardOutput) * 1024;
        }

        WindowsApi.MEMORYSTATUSEX mEMORYSTATUSEX = new WindowsApi.MEMORYSTATUSEX();
        if (!WindowsApi.GlobalMemoryStatusEx(mEMORYSTATUSEX))
        {
            return 0uL;
        }

        return mEMORYSTATUSEX.ullAvailPhys;
    }

    //
    // Summary:
    //     Returns one of two strongly-typed objects.
    //
    // Parameters:
    //   expression:
    //     The expression you want to evaluate.
    //
    //   truePart:
    //     Returned if expression evaluates to True.
    //
    //   falsePart:
    //     Returned if expression evaluates to False.
    //
    // Type parameters:
    //   T:
    //     Return type used for immediate expression
    //
    // Returns:
    //     One of two objects, depending on the evaluation of given expression.
    //
    // Remarks:
    //     This function acts as a strongly-typed immediate if (a.k.a. inline if).
    //     It is expected that this function will only be used in languages that do not
    //     support ?: conditional operations, e.g., Visual Basic.NET. In Visual Basic this
    //     function can be used as a strongly-typed IIf replacement by specifying "Imports
    //     GSF.Common".
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T IIf<T>(bool expression, T truePart, T falsePart)
    {
        if (!expression)
        {
            return falsePart;
        }

        return truePart;
    }

    //
    // Summary:
    //     Creates a strongly-typed Array.
    //
    // Parameters:
    //   length:
    //     Desired length of new array.
    //
    // Type parameters:
    //   T:
    //     Return type for new array.
    //
    // Returns:
    //     New array of specified type.
    //
    // Remarks:
    //     It is expected that this function will only be used in Visual Basic.NET.
    //     The Array.CreateInstance provides better performance and more direct CLR access
    //     for array creation (not to mention less confusion on the matter of array lengths)
    //     in VB.NET, however the returned System.Array is not typed properly. This function
    //     properly casts the return array based on the type specification helping when
    //     Option Strict is enabled.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CreateArray<T>(int length)
    {
        return (T[])Array.CreateInstance(typeof(T), length);
    }

    //
    // Summary:
    //     Creates a strongly-typed Array with an initial value parameter.
    //
    // Parameters:
    //   length:
    //     Desired length of new array.
    //
    //   initialValue:
    //     Value used to initialize all array elements.
    //
    // Type parameters:
    //   T:
    //     Return type for new array.
    //
    // Returns:
    //     New array of specified type.
    //
    // Remarks:
    //     It is expected that this function will only be used in Visual Basic.NET.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T[] CreateArray<T>(int length, T initialValue)
    {
        T[] array = CreateArray<T>(length);
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = initialValue;
        }

        return array;
    }

    //
    // Summary:
    //     Converts value to string; null objects (or DBNull objects) will return an empty
    //     string ("").
    //
    // Parameters:
    //   value:
    //     Value to convert to string.
    //
    // Type parameters:
    //   T:
    //     System.Type of System.Object to convert to string.
    //
    // Returns:
    //     value as a string; if value is null, empty string ("") will be returned.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToNonNullString<T>(this T value) where T : class
    {
        if (value != null && !(value is DBNull))
        {
            return value.ToString();
        }

        return "";
    }

    //
    // Summary:
    //     Converts value to string; null objects (or DBNull objects) will return specified
    //     nonNullValue.
    //
    // Parameters:
    //   value:
    //     Value to convert to string.
    //
    //   nonNullValue:
    //     System.String to return if value is null.
    //
    // Type parameters:
    //   T:
    //     System.Type of System.Object to convert to string.
    //
    // Returns:
    //     value as a string; if value is null, nonNullValue will be returned.
    //
    // Exceptions:
    //   T:System.ArgumentNullException:
    //     nonNullValue cannot be null.
    public static string ToNonNullString<T>(this T value, string nonNullValue) where T : class
    {
        if (nonNullValue == null)
        {
            throw new ArgumentNullException("nonNullValue");
        }

        if (value != null && !(value is DBNull))
        {
            return value.ToString();
        }

        return nonNullValue;
    }

    //
    // Summary:
    //     Makes sure returned string value is not null; if this string is null, empty string
    //     ("") will be returned.
    //
    // Parameters:
    //   value:
    //     System.String to verify is not null.
    //
    // Returns:
    //     System.String value; if value is null, empty string ("") will be returned.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ToNonNullString(this string value)
    {
        if (value != null)
        {
            return value;
        }

        return "";
    }

    //
    // Summary:
    //     Converts value to string; null objects, DBNull objects or empty strings will
    //     return specified nonNullNorEmptyValue.
    //
    // Parameters:
    //   value:
    //     Value to convert to string.
    //
    //   nonNullNorEmptyValue:
    //     System.String to return if value is null.
    //
    // Type parameters:
    //   T:
    //     System.Type of System.Object to convert to string.
    //
    // Returns:
    //     value as a string; if value is null, DBNull or an empty string nonNullNorEmptyValue
    //     will be returned.
    //
    // Exceptions:
    //   T:System.ArgumentException:
    //     nonNullNorEmptyValue must not be null or an empty string.
    public static string ToNonNullNorEmptyString<T>(this T value, string nonNullNorEmptyValue = " ") where T : class
    {
        if (string.IsNullOrEmpty(nonNullNorEmptyValue))
        {
            throw new ArgumentException("Must not be null or an empty string", "nonNullNorEmptyValue");
        }

        if (value == null || value is DBNull)
        {
            return nonNullNorEmptyValue;
        }

        string text = value.ToString();
        if (!string.IsNullOrEmpty(text))
        {
            return text;
        }

        return nonNullNorEmptyValue;
    }

    //
    // Summary:
    //     Converts value to string; null objects, DBNull objects, empty strings or all
    //     white space strings will return specified nonNullNorWhiteSpaceValue.
    //
    // Parameters:
    //   value:
    //     Value to convert to string.
    //
    //   nonNullNorWhiteSpaceValue:
    //     System.String to return if value is null.
    //
    // Type parameters:
    //   T:
    //     System.Type of System.Object to convert to string.
    //
    // Returns:
    //     value as a string; if value is null, DBNull, empty or all white space, nonNullNorWhiteSpaceValue
    //     will be returned.
    //
    // Exceptions:
    //   T:System.ArgumentException:
    //     nonNullNorWhiteSpaceValue must not be null, an empty string or white space.
    public static string ToNonNullNorWhiteSpace<T>(this T value, string nonNullNorWhiteSpaceValue = "_") where T : class
    {
        if (string.IsNullOrWhiteSpace(nonNullNorWhiteSpaceValue))
        {
            throw new ArgumentException("Must not be null, an empty string or white space", "nonNullNorWhiteSpaceValue");
        }

        if (value == null || value is DBNull)
        {
            return nonNullNorWhiteSpaceValue;
        }

        string text = value.ToString();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return nonNullNorWhiteSpaceValue;
    }

    //
    // Summary:
    //     Converts value to a System.String using an appropriate System.ComponentModel.TypeConverter.
    //
    // Parameters:
    //   value:
    //     Value to convert to a System.String.
    //
    // Returns:
    //     value converted to a System.String.
    //
    // Remarks:
    //     If System.ComponentModel.TypeConverter fails, the value's ToString() value will
    //     be returned. Returned value will never be null, if no value exists an empty string
    //     ("") will be returned.
    //     You can use the GSF.StringExtensions.ConvertToType``1(System.String) string extension
    //     method or GSF.Common.TypeConvertFromString(System.String,System.Type) to convert
    //     the string back to its original System.Type.
    public static string TypeConvertToString(object value)
    {
        return TypeConvertToString(value, null);
    }

    //
    // Summary:
    //     Converts value to a System.String using an appropriate System.ComponentModel.TypeConverter.
    //
    // Parameters:
    //   value:
    //     Value to convert to a System.String.
    //
    //   culture:
    //     System.Globalization.CultureInfo to use for the conversion.
    //
    // Returns:
    //     value converted to a System.String.
    //
    // Remarks:
    //     If System.ComponentModel.TypeConverter fails, the value's ToString() value will
    //     be returned. Returned value will never be null, if no value exists an empty string
    //     ("") will be returned.
    //     You can use the GSF.StringExtensions.ConvertToType``1(System.String,System.Globalization.CultureInfo)
    //     string extension method or GSF.Common.TypeConvertFromString(System.String,System.Type,System.Globalization.CultureInfo)
    //     to convert the string back to its original System.Type.
    public static string TypeConvertToString(object value, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        string text = value as string;
        if (text != null)
        {
            return text;
        }

        if (culture == null)
        {
            culture = CultureInfo.InvariantCulture;
        }

        try
        {
            return TypeDescriptor.GetConverter(value).ConvertToString(null, culture, value).ToNonNullString();
        }
        catch
        {
            return value.ToNonNullString();
        }
    }

    //
    // Summary:
    //     Converts this string into the specified type.
    //
    // Parameters:
    //   value:
    //     Source string to convert to type.
    //
    //   type:
    //     System.Type to convert string to.
    //
    // Returns:
    //     System.String converted to specified System.Type; default value of specified
    //     type if conversion fails.
    //
    // Remarks:
    //     This function makes use of a System.ComponentModel.TypeConverter to convert value
    //     to the specified type, the best way to make sure value can be converted back
    //     to its original type is to use the same System.ComponentModel.TypeConverter to
    //     convert the original object to a System.String; see the GSF.Common.TypeConvertToString(System.Object)
    //     method for an easy way to do this.
    //     This function varies from GSF.StringExtensions.ConvertToType``1(System.String)
    //     in that it will use the default value for the type parameter if value is empty
    //     or null.
    public static object TypeConvertFromString(string value, Type type)
    {
        return TypeConvertFromString(value, type, null);
    }

    //
    // Summary:
    //     Converts this string into the specified type.
    //
    // Parameters:
    //   value:
    //     Source string to convert to type.
    //
    //   type:
    //     System.Type to convert string to.
    //
    //   culture:
    //     System.Globalization.CultureInfo to use for the conversion.
    //
    // Returns:
    //     System.String converted to specified System.Type; default value of specified
    //     type if conversion fails.
    //
    // Remarks:
    //     This function makes use of a System.ComponentModel.TypeConverter to convert value
    //     to the specified type, the best way to make sure value can be converted back
    //     to its original type is to use the same System.ComponentModel.TypeConverter to
    //     convert the original object to a System.String; see the GSF.Common.TypeConvertToString(System.Object)
    //     method for an easy way to do this.
    //     This function varies from GSF.StringExtensions.ConvertToType``1(System.String,System.Globalization.CultureInfo)
    //     in that it will use the default value for the type parameter if value is empty
    //     or null.
    public static object TypeConvertFromString(string value, Type type, CultureInfo culture)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            value = Activator.CreateInstance(type).ToString();
        }

        return value.ConvertToType(type, culture);
    }

    //
    // Summary:
    //     Determines if given item is equal to its default value (e.g., null or 0.0).
    //
    // Parameters:
    //   item:
    //     Object to evaluate.
    //
    // Returns:
    //     Result of evaluation as a System.Boolean.
    //
    // Remarks:
    //     Native types default to zero, not null, therefore this can be used to evaluate
    //     if an item is its default (i.e., uninitialized) value.
    public static bool IsDefaultValue(object item)
    {
        if (item == null)
        {
            return true;
        }

        Type type = item.GetType();
        if (!type.IsValueType)
        {
            return false;
        }

        IConvertible convertible = item as IConvertible;
        if (convertible != null)
        {
            try
            {
                switch (convertible.GetTypeCode())
                {
                    case TypeCode.Boolean:
                        return !(bool)item;
                    case TypeCode.SByte:
                        return (sbyte)item == 0;
                    case TypeCode.Byte:
                        return (byte)item == 0;
                    case TypeCode.Int16:
                        return (short)item == 0;
                    case TypeCode.UInt16:
                        return (ushort)item == 0;
                    case TypeCode.Int32:
                        return (int)item == 0;
                    case TypeCode.UInt32:
                        return (uint)item == 0;
                    case TypeCode.Int64:
                        return (long)item == 0;
                    case TypeCode.UInt64:
                        return (ulong)item == 0;
                    case TypeCode.Single:
                        return (float)item == 0f;
                    case TypeCode.Double:
                        return (double)item == 0.0;
                    case TypeCode.Decimal:
                        return (decimal)item == 0m;
                    case TypeCode.Char:
                        return (char)item == '\0';
                    case TypeCode.DateTime:
                        return (DateTime)item == default(DateTime);
                }
            }
            catch (InvalidCastException)
            {
            }
        }

        return ((ValueType)item).Equals(Activator.CreateInstance(type));
    }

    //
    // Summary:
    //     Determines if given item is a reference type.
    //
    // Parameters:
    //   item:
    //     Object to evaluate.
    //
    // Returns:
    //     Result of evaluation as a System.Boolean.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsReference(object item)
    {
        return !(item is ValueType);
    }

    //
    // Summary:
    //     Determines if given item is a reference type but not a string.
    //
    // Parameters:
    //   item:
    //     Object to evaluate.
    //
    // Returns:
    //     Result of evaluation as a System.Boolean.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNonStringReference(object item)
    {
        if (IsReference(item))
        {
            return !(item is string);
        }

        return false;
    }

    //
    // Summary:
    //     Determines if typeCode is a numeric type, i.e., one of: System.TypeCode.Boolean,
    //     System.TypeCode.SByte, System.TypeCode.Byte, System.TypeCode.Int16, System.TypeCode.UInt16,
    //     System.TypeCode.Int32, System.TypeCode.UInt32, System.TypeCode.Int64, System.TypeCode.UInt64
    //     System.TypeCode.Single, System.TypeCode.Double or System.TypeCode.Decimal.
    //
    // Parameters:
    //   typeCode:
    //     System.TypeCode value to check.
    //
    // Returns:
    //     true if typeCode is a numeric type; otherwise, false.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType(TypeCode typeCode)
    {
        if (typeCode == TypeCode.Boolean || (uint)(typeCode - 5) <= 10u)
        {
            return true;
        }

        return false;
    }

    //
    // Summary:
    //     Determines if type is a numeric type, i.e., has a System.TypeCode that is one
    //     of: System.TypeCode.Boolean, System.TypeCode.SByte, System.TypeCode.Byte, System.TypeCode.Int16,
    //     System.TypeCode.UInt16, System.TypeCode.Int32, System.TypeCode.UInt32, System.TypeCode.Int64,
    //     System.TypeCode.UInt64 System.TypeCode.Single, System.TypeCode.Double or System.TypeCode.Decimal.
    //
    // Parameters:
    //   type:
    //     System.Type to check.
    //
    // Returns:
    //     true if type is a numeric type; otherwise, false.
    public static bool IsNumericType(Type type)
    {
        return IsNumericType(Type.GetTypeCode(type));
    }

    //
    // Summary:
    //     Determines if T is a numeric type, i.e., has a System.TypeCode that is one of:
    //     System.TypeCode.Boolean, System.TypeCode.SByte, System.TypeCode.Byte, System.TypeCode.Int16,
    //     System.TypeCode.UInt16, System.TypeCode.Int32, System.TypeCode.UInt32, System.TypeCode.Int64,
    //     System.TypeCode.UInt64 System.TypeCode.Single, System.TypeCode.Double or System.TypeCode.Decimal.
    //
    // Type parameters:
    //   T:
    //     System.Type to check.
    //
    // Returns:
    //     true if T is a numeric type; otherwise, false.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType<T>()
    {
        return IsNumericType(Type.GetTypeCode(typeof(T)));
    }

    //
    // Summary:
    //     Determines if System.Type of item is a numeric type, i.e., item is System.IConvertible
    //     and has a System.TypeCode that is one of: System.TypeCode.Boolean, System.TypeCode.SByte,
    //     System.TypeCode.Byte, System.TypeCode.Int16, System.TypeCode.UInt16, System.TypeCode.Int32,
    //     System.TypeCode.UInt32, System.TypeCode.Int64, System.TypeCode.UInt64 System.TypeCode.Single,
    //     System.TypeCode.Double or System.TypeCode.Decimal.
    //
    // Parameters:
    //   item:
    //     Object to evaluate.
    //
    // Returns:
    //     true if item is a numeric type; otherwise, false.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumericType(object item)
    {
        IConvertible convertible = item as IConvertible;
        if (convertible != null)
        {
            return IsNumericType(convertible.GetTypeCode());
        }

        return false;
    }

    //
    // Summary:
    //     Determines if given item is or can be interpreted as numeric.
    //
    // Parameters:
    //   item:
    //     Object to evaluate.
    //
    // Returns:
    //     true if item is or can be interpreted as numeric; otherwise, false.
    //
    // Remarks:
    //     If type of item is a System.Char or a System.String, then if value can be parsed
    //     as a numeric value, result will be true.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumeric(object item)
    {
        if (IsNumericType(item))
        {
            return true;
        }

        decimal result;
        if (item is char || item is string)
        {
            return decimal.TryParse(item.ToString(), out result);
        }

        return false;
    }

    //
    // Summary:
    //     Returns the smallest item from a list of parameters.
    //
    // Parameters:
    //   itemList:
    //     A variable number of parameters of the specified type.
    //
    // Type parameters:
    //   T:
    //     Return type System.Type that is the minimum value in the itemList.
    //
    // Returns:
    //     Result is the minimum value of type System.Type in the itemList.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Min<T>(params T[] itemList)
    {
        return itemList.Min();
    }

    //
    // Summary:
    //     Returns the largest item from a list of parameters.
    //
    // Parameters:
    //   itemList:
    //     A variable number of parameters of the specified type .
    //
    // Type parameters:
    //   T:
    //     Return type System.Type that is the maximum value in the itemList.
    //
    // Returns:
    //     Result is the maximum value of type System.Type in the itemList.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Max<T>(params T[] itemList)
    {
        return itemList.Max();
    }

    //
    // Summary:
    //     Returns the value that is neither the largest nor the smallest.
    //
    // Parameters:
    //   value1:
    //     Value 1.
    //
    //   value2:
    //     Value 2.
    //
    //   value3:
    //     Value 3.
    //
    // Type parameters:
    //   T:
    //     System.Type of the objects passed to and returned from this method.
    //
    // Returns:
    //     Result is the value that is neither the largest nor the smallest.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static T Mid<T>(T value1, T value2, T value3) where T : IComparable<T>
    {
        if (value1 == null)
        {
            throw new ArgumentNullException("value1");
        }

        if (value2 == null)
        {
            throw new ArgumentNullException("value2");
        }

        if (value3 == null)
        {
            throw new ArgumentNullException("value3");
        }

        int num = value1.CompareTo(value2);
        int num2 = value1.CompareTo(value3);
        int num3 = value2.CompareTo(value3);
        if (num2 >= 0 && num3 >= 0)
        {
            if (num > 0)
            {
                return value2;
            }

            return value1;
        }

        if (num >= 0 && num3 <= 0)
        {
            if (num2 > 0)
            {
                return value3;
            }

            return value1;
        }

        if (num3 > 0)
        {
            return value3;
        }

        return value2;
    }

    //
    // Summary:
    //     Returns value if not null; otherwise nonNullValue.
    //
    // Parameters:
    //   value:
    //     Value to test.
    //
    //   nonNullValue:
    //     Value to return if primary value is null.
    //
    // Returns:
    //     value if not null; otherwise nonNullValue.
    //
    // Remarks:
    //     This function is useful when using evaluated code parsers based on older versions
    //     of .NET, e.g., the RazorEngine or the ExpressionEvaluator.
    public static object NotNull(object value, object nonNullValue)
    {
        if (nonNullValue == null)
        {
            return new ArgumentNullException("nonNullValue");
        }

        if (value != null && !(value is DBNull))
        {
            return value;
        }

        return nonNullValue;
    }
}
#if false // Decompilation log
'8' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\mscorlib.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.dll'
------------------
Resolve: 'System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.ServiceModel, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Data.Entity.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Data.Entity.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Numerics, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Core.dll'
------------------
Resolve: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\System.Data.dll'
------------------
Resolve: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Security, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Web, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Configuration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'ExpressionEvaluator, Version=2.0.4.0, Culture=neutral, PublicKeyToken=90d9f15d622e2348'
Could not find by name: 'ExpressionEvaluator, Version=2.0.4.0, Culture=neutral, PublicKeyToken=90d9f15d622e2348'
------------------
Resolve: 'System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
Could not find by name: 'System.ComponentModel.DataAnnotations, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35'
------------------
Resolve: 'System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'Novell.Directory.Ldap, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
Could not find by name: 'Novell.Directory.Ldap, Version=4.0.0.0, Culture=neutral, PublicKeyToken=0738eb9f132ed756'
------------------
Resolve: 'System.DirectoryServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.DirectoryServices, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.DirectoryServices.AccountManagement, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Could not find by name: 'System.Management, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
------------------
Resolve: 'System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Runtime.Serialization, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
------------------
Resolve: 'System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Could not find by name: 'System.Data.DataSetExtensions, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
#endif
