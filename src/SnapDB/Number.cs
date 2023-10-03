using System.Globalization;

namespace SnapDB;

//Got code ideas from http://code.google.com/p/stringencoders/
//specifically from: http://code.google.com/p/stringencoders/source/browse/trunk/src/modp_numtoa.c

/// <summary>
/// Provides utility methods for working with numbers.
/// </summary>
public static class Number
{
    private static readonly double[] s_powersOf10d = new double[] { 1f, 10f, 100f, 1000f, 10000f, 100000f, 1000000f, 10000000f, 100000000f, 1000000000f, 10000000000f, 100000000000f };

    /// <summary>
    /// Writes a floating-point value to a character array.
    /// </summary>
    /// <param name="value">The float value to be written.</param>
    /// <param name="str">The character array where the value will be written.</param>
    /// <param name="position">The starting position in the character array.</param>
    /// <returns>The number of characters written to the array.</returns>
    public static int WriteToChars(this float value, char[] str, int position)
    {
        int pos = position;
        if (str.Length - position < 32)
            throw new Exception("Insufficient buffer space");

        if (Single.IsNaN(value))
        {
            str[0] = 'n';
            str[1] = 'a';
            str[2] = 'n';

            return 3;
        }
        if (Single.IsNegativeInfinity(value))
        {
            str[0] = 'n';
            str[1] = 'a';
            str[2] = 'n';

            return 3;
        }
        if (Single.IsPositiveInfinity(value))
        {
            str[0] = 'n';
            str[1] = 'a';
            str[2] = 'n';

            return 3;
        }

        if (value == 0)
        {
            str[0] = '0';
            
            return 1;
        }


        // Any number outside of this range will take the exponent form,
        // and I'd rather not have to deal with this.
        const float maxValue = 9999999f;
        const float minValue = -9999999f;
        const float zeroMax = 0.0001f;
        const float zeroMin = -0.0001f;

        if (value > maxValue || value < minValue || value < zeroMax && value > zeroMin)
        {
            // Not worth coding for this case.
            string T = value.ToString(CultureInfo.InvariantCulture);
            for (int x = 0; x < T.Length; x++)
            {
                str[pos + x] = T[pos + x];
            }

            return T.Length;
        }

        if (value < 0)
        {
            str[pos] = '-';
            value = -value;
            pos++;
        }

        int r = value >= 999999.5f ? 7 : value >= 99999.95f ? 6 : value >= 9999.995f ? 5 :
            value >= 999.9995f ? 4 : value >= 99.99995f ? 3 : value >= 9.999995f ? 2 :
            value >= 0.9999995f ? 1 : value >= 0.09999995f ? 0 : value >= 0.009999995f ? -1 :
            value >= 0.0009999995f ? -2 : -3;

        int wholePrecision = r;
        int fracPrecision = 7 - r;

        double scaled = value * s_powersOf10d[fracPrecision];
        uint number = (uint)scaled;

        // Do the rounding
        double fraction = scaled - number;

        if (fraction >= 0.5)
        {
            // Round
            number++;
        }

        // Write the number
        ulong bcd = BinToReverseBcd(number);

        if (wholePrecision <= 0)
        {
            str[pos++] = '0';
            str[pos++] = '.';

            while (wholePrecision < 0)
            {
                str[pos++] = '0';
                wholePrecision++;
            }
        }
        else
        {
            while (wholePrecision > 0)
            {
                wholePrecision--;
                str[pos++] = (char)(48 + (bcd & 0xf));
                bcd >>= 4;
            }

            if (bcd == 0)
                return pos - position;

            str[pos++] = '.';
        }

        while (bcd != 0)
        {
            str[pos++] = (char)(48 + (bcd & 0xf));
            bcd >>= 4;
        }
        return pos - position;

    }

    private static int MeasureDigits(uint value)
    {
        const uint digits2 = 10;
        const uint digits3 = 100;
        const uint digits4 = 1000;
        const uint digits5 = 10000;
        const uint digits6 = 100000;
        const uint digits7 = 1000000;
        const uint digits8 = 10000000;
        const uint digits9 = 100000000;
        const uint digits10 = 1000000000;

        if (value >= digits5)
        {
            if (value >= digits8)
            {
                if (value >= digits10)
                    return 10;

                if (value >= digits9)
                    return 9;

                return 8;
            }

            if (value >= digits7)
                return 7;

            if (value >= digits6)
                return 6;

            return 5;
        }

        if (value >= digits3)
        {
            if (value >= digits4)
                return 4;
            
            return 3;
        }

        if (value >= digits2)
            return 2;

        return 1;
    }

    /// <summary>
    /// Writes an unsigned integer value to a character array.
    /// </summary>
    /// <param name="value">The uint value to be written.</param>
    /// <param name="destination">The character array where the value will be written.</param>
    /// <param name="position">The starting position in the character array.</param>
    /// <returns>The number of characters written to the array.</returns>
        public static unsafe int WriteToChars2(this uint value, char[] destination, int position)
    {
        uint temp;
        int digits;

        const uint digits2 = 10;
        const uint digits3 = 100;
        const uint digits4 = 1000;
        const uint digits5 = 10000;
        const uint digits6 = 100000;
        const uint digits7 = 1000000;
        const uint digits8 = 10000000;
        const uint digits9 = 100000000;
        const uint digits10 = 1000000000;

        if (value >= digits5)
        {
            if (value >= digits8)
            {
                if (value >= digits10)
                    digits = 10;

                else if (value >= digits9)
                    digits = 9;

                else
                    digits = 8;
            }
            else
            {
                if (value >= digits7)
                    digits = 7;

                else if (value >= digits6)
                    digits = 6;

                else
                    digits = 5;
            }
        }
        else
        {
            if (value >= digits3)
            {
                if (value >= digits4)
                    digits = 4;

                else
                    digits = 3;
            }
            else
            {
                if (value >= digits2)
                    digits = 2;

                else
                    digits = 1;
            }
        }

        if (destination.Length - position < digits)
            throw new Exception("Insufficient buffer space");

        fixed (char* str = &destination[position + digits - 1])
        {
            char* wstr = str;

            do
            {
                temp = value / 10u;
                *wstr = (char)(48u + (value - temp * 10u));
                wstr--;
                value = temp;

            } 
            while (value != 0);

            return digits;
        }
    }

    private static unsafe void Strreverse(char* begin, char* end)
    {
        char aux;
        while (end > begin)
        {
            aux = *end;
            *end-- = *begin;
            *begin++ = aux;
        }
    }

    /// <summary>
    /// Writes an unsigned integer value to a character array using optimized digit extraction.
    /// </summary>
    /// <param name="value">The uint value to be written.</param>
    /// <param name="destination">The character array where the value will be written.</param>
    /// <param name="position">The starting position in the character array.</param>
    /// <returns>The number of characters written to the array.</returns>
    public static unsafe int WriteToChars(this uint value, char[] destination, int position)
    {
        const uint digits1 = 1;
        const uint digits2 = 10;
        const uint digits3 = 100;
        const uint digits4 = 1000;
        const uint digits5 = 10000;
        const uint digits6 = 100000;
        const uint digits7 = 1000000;
        const uint digits8 = 10000000;
        const uint digits9 = 100000000;
        const uint digits10 = 1000000000;

        byte digit = 48;
        int pos = 0;

        if (destination.Length - position < 10)
            throw new Exception("Insufficient buffer space");

        fixed (char* str = &destination[position])
        {

            if (value >= digits5)
            {
                // 5,6,7,8,9,10

                if (value >= digits8)
                {
                    // 8,9,10
                    if (value >= digits10)
                        goto Digits10;
                    if (value >= digits9)
                        goto Digits9;
                    goto Digits8;

                }

                // 5,6,7
                if (value >= digits7)
                    goto Digits7;
                if (value >= digits6)
                    goto Digits6;
                goto Digits5;
            }

            // 1,2,3,4
            if (value >= digits3)
            {
                // 3 or 4
                if (value >= digits4)
                    goto Digits4;
                goto Digits3;
            }

            // 1 or 2
            if (value >= digits2)
                goto Digits2;
            goto Digits1;



            Digits10:

            if (value >= 4 * digits10) { value -= 4 * digits10; digit += 4; }
            if (value >= 2 * digits10) { value -= 2 * digits10; digit += 2; }
            if (value >= 1 * digits10) { value -= 1 * digits10; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits9:
            if (value >= 8 * digits9) { value -= 8 * digits9; digit += 8; }
            if (value >= 4 * digits9) { value -= 4 * digits9; digit += 4; }
            if (value >= 2 * digits9) { value -= 2 * digits9; digit += 2; }
            if (value >= 1 * digits9) { value -= 1 * digits9; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits8:
            if (value >= 8 * digits8) { value -= 8 * digits8; digit += 8; }
            if (value >= 4 * digits8) { value -= 4 * digits8; digit += 4; }
            if (value >= 2 * digits8) { value -= 2 * digits8; digit += 2; }
            if (value >= 1 * digits8) { value -= 1 * digits8; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits7:
            if (value >= 8 * digits7) { value -= 8 * digits7; digit += 8; }
            if (value >= 4 * digits7) { value -= 4 * digits7; digit += 4; }
            if (value >= 2 * digits7) { value -= 2 * digits7; digit += 2; }
            if (value >= 1 * digits7) { value -= 1 * digits7; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits6:
            if (value >= 8 * digits6) { value -= 8 * digits6; digit += 8; }
            if (value >= 4 * digits6) { value -= 4 * digits6; digit += 4; }
            if (value >= 2 * digits6) { value -= 2 * digits6; digit += 2; }
            if (value >= 1 * digits6) { value -= 1 * digits6; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits5:
            if (value >= 8 * digits5) { value -= 8 * digits5; digit += 8; }
            if (value >= 4 * digits5) { value -= 4 * digits5; digit += 4; }
            if (value >= 2 * digits5) { value -= 2 * digits5; digit += 2; }
            if (value >= 1 * digits5) { value -= 1 * digits5; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits4:
            if (value >= 8 * digits4) { value -= 8 * digits4; digit += 8; }
            if (value >= 4 * digits4) { value -= 4 * digits4; digit += 4; }
            if (value >= 2 * digits4) { value -= 2 * digits4; digit += 2; }
            if (value >= 1 * digits4) { value -= 1 * digits4; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits3:
            if (value >= 8 * digits3) { value -= 8 * digits3; digit += 8; }
            if (value >= 4 * digits3) { value -= 4 * digits3; digit += 4; }
            if (value >= 2 * digits3) { value -= 2 * digits3; digit += 2; }
            if (value >= 1 * digits3) { value -= 1 * digits3; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits2:
            if (value >= 8 * digits2) { value -= 8 * digits2; digit += 8; }
            if (value >= 4 * digits2) { value -= 4 * digits2; digit += 4; }
            if (value >= 2 * digits2) { value -= 2 * digits2; digit += 2; }
            if (value >= 1 * digits2) { value -= 1 * digits2; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

        Digits1:
            if (value >= 8 * digits1) { value -= 8 * digits1; digit += 8; }
            if (value >= 4 * digits1) { value -= 4 * digits1; digit += 4; }
            if (value >= 2 * digits1) { value -= 2 * digits1; digit += 2; }
            if (value >= 1 * digits1) { value -= 1 * digits1; digit += 1; }
            str[pos] = (char)digit; pos += 1; digit = 48;

            return pos;
        }
    }


    /// <summary>
    /// Converts a uint binary value into a BCD value that is encoded in reverse order.
    /// This means what was the Most Significant Digit is now the lease significant digit.
    /// </summary>
    /// <param name="value"></param>
    private static ulong BinToReverseBcd(this uint value)
    {
        ulong result = 0;
        do
        {
            uint temp = value / 10u;
            result = (result << 4) | (byte)(value - temp * 10u);
            value = temp;

        } while (value != 0);

        return result;
    }

}
