using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace HotFix.Framework.Math.Fix
{
    /// <summary>
    /// 64位定点数
    /// </summary>
    [Serializable]
    public partial struct Fix64
    {
        private readonly long _raw;
        public long Raw => _raw;
        
        private const int FRACTIONAL_BITS = 32;

        private const int NUM_BITS = sizeof(long) * 8;
        private const int INTEGER_BITS = NUM_BITS - FRACTIONAL_BITS;
        private const long ONE = 1L << FRACTIONAL_BITS;
        private const long FRACTION_MASK = ONE - 1;
        private const long INTEGER_MASK = (-1L & ~FRACTION_MASK);
        private const long MAX_VALUE = long.MaxValue;
        private const long MIN_VALUE = long.MinValue;
        
        /// <summary>
        /// 转换精度限制，大于此位数的小数转换时将使用精确转换，反之则使用快速转换
        /// </summary>
        public const int PRECISION = 3;
        private static int PrecisionRange
        {
            get {
                var value = 1;
                for (var i = 0; i < PRECISION; i++) {
                    value *= 10;
                }

                return value;
            }
        }

        public static readonly Fix64 Zero = new Fix64(0L);
        public static readonly Fix64 One = new Fix64(ONE);
        public static readonly Fix64 MinValue = new Fix64(MIN_VALUE);
        public static readonly Fix64 MaxValue = new Fix64(MAX_VALUE);
        
        private const long PI_TIMES_2 = 0x6487ED511;
        private const long PI = 0x3243F6A88;
        private const long PI_OVER_2 = 0x1921FB544;
        private const long LN2 = 0xB17217F7;
        private const long LOG2MAX = 0x1F00000000;
        private const long LOG2MIN = -0x2000000000;
        
        public static readonly Fix64 Pi = new Fix64(PI);
        public static readonly Fix64 PiOver2 = new Fix64(PI_OVER_2);
        public static readonly Fix64 Log2Max = new Fix64(LOG2MAX);
        public static readonly Fix64 Log2Min = new Fix64(LOG2MIN);
        public static readonly Fix64 Ln2 = new Fix64(LN2);
        
        private const int LUT_SIZE = (int)(PI_OVER_2 >> 15);
        private static readonly Fix64 LutInterval = (Fix64)(LUT_SIZE - 1) / PiOver2;
        
        public Fix64(long raw) {
            _raw = raw;
        }

        public static implicit operator Fix64(int value) {
            return new Fix64((long)value << FRACTIONAL_BITS);
        }
        
        public static implicit operator Fix64(long value) {
            return new Fix64(value * ONE);
        }

        public static implicit operator Fix64(string value) {
            var sep = value.Split('.');
            var integer = long.Parse(sep[0]);
            if (sep.Length <= 1) {
                return new Fix64(integer << FRACTIONAL_BITS);
            }
            
            var fractionStr = sep[1];
            var fraction = long.Parse(fractionStr);
            if (fraction == 0) {
                return new Fix64(integer << FRACTIONAL_BITS);
            }
            
            var fractionBi = Convert.ToString(fraction, 2);
            var length = sep[1].Length;
            while (fractionBi.Length > INTEGER_BITS) {
                length--;
                fractionStr = sep[1].Substring(0, length);
                fraction = long.Parse(fractionStr);
                if ((int)sep[1][length] >= 5) {
                    fraction++;
                }
                fractionBi = Convert.ToString(fraction, 2);
            }
            
            fraction <<= FRACTIONAL_BITS;
            for (var i = 0; i < length; i++) {
                fraction /= 10;
            }
            fraction = integer < 0 ? ONE - fraction : fraction;
            
            var raw = (integer << FRACTIONAL_BITS) + fraction;
            return new Fix64(raw);
        }
        
        public static implicit operator Fix64(float value) {
            var remainder = (value * PrecisionRange) % 1;
            if (remainder != 0) {
                return (Fix64)value.ToString("G");
            }
            
            return new Fix64((long)(value * ONE));
        }
        
        public static implicit operator Fix64(double value) {
            var remainder = (value * PrecisionRange) % 1;
            if (remainder != 0) {
                return (Fix64)value.ToString("G");
            }
            
            return new Fix64((long)(value * ONE));
        }
        
        public static implicit operator Fix64(decimal value) {
            var remainder = (value * PrecisionRange) % 1;
            if (remainder != 0) {
                return (Fix64)value.ToString("G");
            }
            
            return new Fix64((long)(value * ONE));
        }
        
        public static explicit operator long(Fix64 value) {
            return value._raw >> FRACTIONAL_BITS;
        }
        
        public static explicit operator int(Fix64 value) {
            return (int)(value._raw >> FRACTIONAL_BITS);
        }
        
        /// <summary>
        /// 快速转换，不保证精度
        /// </summary>
        public static explicit operator float(Fix64 value) {
            return (float)value._raw / ONE;
        }
        
        /// <summary>
        /// 通过字符串精确转换为小数
        /// </summary>
        public static float ToFloat(Fix64 value) {
            return float.Parse(value.ToString());
        }
        
        /// <summary>
        /// 快速转换，不保证精度
        /// </summary>
        public static explicit operator double(Fix64 value) {
            return (double)value._raw / ONE;
        }
        
        /// <summary>
        /// 通过字符串精确转换为小数
        /// </summary>
        public static double ToDouble(Fix64 value) {
            return double.Parse(value.ToString());
        }

        /// <summary>
        /// 快速转换，不保证精度
        /// </summary>
        public static explicit operator decimal(Fix64 value) {
            return (decimal)value._raw / ONE;
        }
        
        /// <summary>
        /// 通过字符串精确转换为小数
        /// </summary>
        public static decimal ToDecimal(Fix64 value) {
            return decimal.Parse(value.ToString());
        }

        public override bool Equals(object obj) {
            if (obj == null) {
                return false;
            }
            return obj is Fix64 && ((Fix64)obj)._raw == _raw;
        }

        public override int GetHashCode() {
            return _raw.GetHashCode();
        }

        public bool Equals(Fix64 other) {
            return _raw == other._raw;
        }

        public int CompareTo(Fix64 other) {
            return _raw.CompareTo(other._raw);
        }

        /// <summary>
        /// 转换成字符串
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var precisionMax = (long.MaxValue / FRACTION_MASK).ToString().Length - 1;
            var sb = new StringBuilder();
            if (_raw < 0) {
                sb.Append(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NegativeSign);
            }
            var integer = _raw >> FRACTIONAL_BITS;
            integer = integer < 0 ? -integer : integer;
            sb.Append(integer.ToString());
            
            ulong fraction = (ulong)(_raw & FRACTION_MASK);
            if (fraction == 0) {
                return sb.ToString();
            }
            
            fraction = _raw < 0 ? ONE - fraction : fraction;
            var multiple = 1UL;
            for (var i = 0; i < precisionMax; i++) {
                multiple *= 10;
            }
            fraction *= multiple;
            fraction += ONE >> 1;
            fraction >>= FRACTIONAL_BITS;

            sb.Append(System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
            var fractionLength = fraction.ToString("D").Length;
            if (fractionLength < precisionMax) {
                var leadingZeroes = precisionMax - fractionLength;
                for (var i = 0; i < leadingZeroes; i++) {
                    sb.Append('0');
                }
            }
            sb.Append(fraction.ToString("D").TrimEnd('0'));
            return sb.ToString();
        }

        /// <summary>
        /// 执行饱和加法，即在溢出的情况下，根据操作数的符号舍入到 MinValue 或 MaxValue。
        /// </summary>
        public static Fix64 operator +(Fix64 x, Fix64 y) {
            var xl = x._raw;
            var yl = y._raw;
            var sum = xl + yl;
            // 如果操作数的符号相同且 sum 和 x 的符号不同，则发生溢出
            if ((~(xl ^ yl) & (xl ^ sum) & MIN_VALUE) != 0) {
                sum = xl > 0 ? MAX_VALUE : MIN_VALUE;
            }

            return new Fix64(sum);
        }

        /// <summary>
        /// 快速加法，不做溢出检查
        /// </summary>
        public static Fix64 FastAdd(Fix64 x, Fix64 y) {
            return new Fix64(x._raw + y._raw);
        }

        /// <summary>
        /// 执行饱和减法，即在溢出的情况下，根据操作数的符号舍入到 MinValue 或 MaxValue。
        /// </summary>
        public static Fix64 operator -(Fix64 x, Fix64 y) {
            var xl = x._raw;
            var yl = y._raw;
            var diff = xl - yl;
            // 如果操作数的符号不同且 sum 和 x 的符号不同
            if (((xl ^ yl) & (xl ^ diff) & MIN_VALUE) != 0) {
                diff = xl < 0 ? MIN_VALUE : MAX_VALUE;
            }

            return new Fix64(diff);
        }

        /// <summary>
        /// 快速减法，不做溢出检查
        /// </summary>
        public static Fix64 FastSub(Fix64 x, Fix64 y) {
            return new Fix64(x._raw - y._raw);
        }
        
        /// <summary>
        /// 判断两个长整数相加是否溢出
        /// </summary>
        /// <returns></returns>
        private static long AddOverflowHelper(long x, long y, ref bool overflow) {
            var sum = x + y;
            // sign(x) ^ sign(y) != sign(sum) 时 x + y 溢出
            overflow |= ((x ^ y ^ sum) & MIN_VALUE) != 0;
            return sum;
        }
        
        public static Fix64 operator *(Fix64 x, Fix64 y) {
            var xRaw = x._raw;
            var yRaw = y._raw;
            
            // 操作数符号是否相等
            bool opSignsEqual = ((xRaw ^ yRaw) & MIN_VALUE) == 0;

            var xFrac = (ulong)(xRaw & FRACTION_MASK);
            var xInt = xRaw >> FRACTIONAL_BITS;
            var yFrac = (ulong)(yRaw & FRACTION_MASK);
            var yInt = yRaw >> FRACTIONAL_BITS;

            var ii = xInt * yInt;
            
            // 如果整数部分相乘结果的前一部分位既不是全0也不是全1，则表示结果溢出。
            var topCarry = ii >> FRACTIONAL_BITS;
            if (topCarry != 0 && topCarry != -1) {
                return opSignsEqual ? MaxValue : MinValue;
            }

            var ffResult = (long)((xFrac * yFrac) >> FRACTIONAL_BITS);
            var midResult1 = (long)xFrac * yInt;
            var midResult2 = xInt * (long)yFrac;
            var iiResult = ii << FRACTIONAL_BITS;

            bool overflow = false;
            var sum = AddOverflowHelper((long)ffResult, midResult1, ref overflow);
            sum = AddOverflowHelper(sum, midResult2, ref overflow);
            sum = AddOverflowHelper(sum, iiResult, ref overflow);

            // 如果操作数的符号相等且结果符号为负，则乘法正溢出，反之亦然
            if (opSignsEqual) {
                if (sum < 0 || (overflow && xRaw > 0)) {
                    return MaxValue;
                }
            }
            else {
                if (sum > 0) {
                    return MinValue;
                }
            }

            // 如果两个操作数符号不同，绝对值都大于1，并且计算结果大于负操作数，则存在负溢出。
            if (!opSignsEqual) {
                long posOp, negOp;
                if (xRaw > yRaw) {
                    posOp = xRaw;
                    negOp = yRaw;
                }
                else {
                    posOp = yRaw;
                    negOp = xRaw;
                }

                if (sum > negOp && negOp < -ONE && posOp > ONE) {
                    return MinValue;
                }
            }

            return new Fix64(sum);
        }

        /// <summary>
        /// 快速乘法，不检查溢出
        /// </summary>
        public static Fix64 FastMul(Fix64 x, Fix64 y) {
            var xRaw = x._raw;
            var yRaw = y._raw;

            var xFrac = (ulong)(xRaw & FRACTION_MASK);
            var xInt = xRaw >> FRACTIONAL_BITS;
            var yFrac = (ulong)(yRaw & FRACTION_MASK);
            var yInt = yRaw >> FRACTIONAL_BITS;
            
            var ffResult = (long)((xFrac * yFrac) >> FRACTIONAL_BITS);
            var midResult1 = (long)xFrac * yInt;
            var midResult2 = xInt * (long)yFrac;
            var iiResult = (xInt * yInt) << FRACTIONAL_BITS;
            
            var sum = ffResult + midResult1 + midResult2 + iiResult;
            return new Fix64(sum);
        }
        
        /// <summary>
        /// 计算无符号长整数的前缀0的个数
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CountLeadingZeroes(ulong x) {
            int result = 0;
            while ((x & 0xF000000000000000) == 0) {
                result += 4;
                x <<= 4;
            }

            while ((x & 0x8000000000000000) == 0) {
                result += 1;
                x <<= 1;
            }

            return result;
        }

        public static Fix64 operator /(Fix64 x, Fix64 y) {
            var xRaw = x._raw;
            var yRaw = y._raw;

            if (xRaw == 0) {
                return Zero;
            }
            if (yRaw == 0) {
                throw new DivideByZeroException();
            }

            var remainder = (ulong)(xRaw >= 0 ? xRaw : -xRaw);
            var divider = (ulong)(yRaw >= 0 ? yRaw : -yRaw);
            var quotient = 0UL;
            var bitPos = NUM_BITS / 2 + 1;

            while ((divider & 0xF) == 0 && bitPos >= 4) {
                divider >>= 4;
                bitPos -= 4;
            }

            while (remainder != 0 && bitPos >= 0) {
                int shift = CountLeadingZeroes(remainder);
                if (shift > bitPos) {
                    shift = bitPos;
                }

                remainder <<= shift;
                bitPos -= shift;

                var div = remainder / divider;
                remainder = remainder % divider;
                quotient += div << bitPos;

                // 检查溢出
                if ((div & ~(0xFFFFFFFFFFFFFFFF >> bitPos)) != 0) {
                    return ((xRaw ^ yRaw) & MIN_VALUE) == 0 ? MaxValue : MinValue;
                }

                remainder <<= 1;
                --bitPos;
            }

            // 四舍五入
            ++quotient;
            var result = (long)(quotient >> 1);
            if (((xRaw ^ yRaw) & MIN_VALUE) != 0) {
                result = -result;
            }

            return new Fix64(result);
        }

        public static Fix64 operator %(Fix64 x, Fix64 y) {
            var raw = (x._raw == MIN_VALUE && y._raw == -1) ? 0 : x._raw % y._raw;
            return new Fix64(raw);
        }

        /// <summary>
        /// 快速取模，当 x == MinValue 且 y == -1 时会溢出
        /// </summary>
        public static Fix64 FastMod(Fix64 x, Fix64 y) {
            return new Fix64(x._raw % y._raw);
        }

        public static Fix64 operator -(Fix64 x) {
            return x._raw == MIN_VALUE ? MaxValue : new Fix64(-x._raw);
        }

        public static Fix64 operator +(Fix64 x) {
            return x;
        }

        public static Fix64 operator ++(Fix64 x) {
            return x + One;
        }

        public static Fix64 operator --(Fix64 x) {
            return x - One;
        }

        public static bool operator ==(Fix64 x, Fix64 y) {
            return x._raw == y._raw;
        }

        public static bool operator !=(Fix64 x, Fix64 y) {
            return x._raw != y._raw;
        }

        public static bool operator >(Fix64 x, Fix64 y) {
            return x._raw > y._raw;
        }

        public static bool operator <(Fix64 x, Fix64 y) {
            return x._raw < y._raw;
        }

        public static bool operator >=(Fix64 x, Fix64 y) {
            return x._raw >= y._raw;
        }

        public static bool operator <=(Fix64 x, Fix64 y) {
            return x._raw <= y._raw;
        }
        
        /// <summary>
        /// 返回Fix64值的符号(正数返回1，负数返回-1，0返回0)
        /// </summary>
        public static int Sign(Fix64 value) {
            return
                value._raw < 0 ? -1 :
                value._raw > 0 ? 1 :
                0;
        }
        
        /// <summary>
        /// 绝对值
        /// 注意：Abs(Fix64.MinValue) = Fix64.MaxValue.
        /// </summary>
        public static Fix64 Abs(Fix64 value) {
            if (value._raw == MIN_VALUE) {
                return MaxValue;
            }

            var mask = value._raw >> (NUM_BITS - 1);
            return new Fix64((value._raw + mask) ^ mask);
        }

        /// <summary>
        /// 快速返回绝对值，不包括 FastAbs(Fix64.MinValue)
        /// </summary>
        public static Fix64 FastAbs(Fix64 value) {
            var mask = value._raw >> (NUM_BITS - 1);
            return new Fix64((value._raw + mask) ^ mask);
        }


        /// <summary>
        /// 向下取整
        /// </summary>
        public static Fix64 Floor(Fix64 value) {
            // 只需将小数部分清零
            return new Fix64(value._raw & INTEGER_MASK);
        }

        /// <summary>
        /// 向上取整
        /// </summary>
        public static Fix64 Ceiling(Fix64 value) {
            var hasFractionalPart = (value._raw & FRACTION_MASK) != 0;
            return hasFractionalPart ? Floor(value) + One : value;
        }

        /// <summary>
        /// 四舍五入到最接近的整数值。如果小数部分正好等于0.5，则返回相邻的整偶数值
        /// </summary>
        public static Fix64 Round(Fix64 value) {
            var fractionalPart = value._raw & FRACTION_MASK;
            var integralPart = Floor(value);
            var half = 1L << (FRACTIONAL_BITS - 1);
            if (fractionalPart < half) {
                return integralPart;
            }

            if (fractionalPart > half) {
                return integralPart + One;
            }

            // 如果小数部分正好等于0.5，则返回相邻的整偶数值
            // 与 System.Math.Round() 规则相同
            return (integralPart._raw & ONE) == 0
                ? integralPart
                : integralPart + One;
        }

        /// <summary>
        /// 舍入到最接近零的整数值
        /// </summary>
        public static Fix64 Truncate(Fix64 value) {
            var integralPart = Floor(value);

            if (Sign(value) < 0) {
                return integralPart + One;
            }
            else {
                return integralPart;
            }
        }

        /// <summary>
        /// 最大值
        /// </summary>
        public static Fix64 Max(Fix64 x, Fix64 y) {
            return x._raw > y._raw ? x : y;
        }
        
        /// <summary>
        /// 最小值
        /// </summary>
        public static Fix64 Min(Fix64 x, Fix64 y) {
            return x._raw < y._raw ? x : y;
        }
        
        /// <summary>
        /// 返回2的指定次幂（精度至少6位小数）
        /// </summary>
        public static Fix64 Pow2(Fix64 x) {
            if (x._raw == 0) {
                return One;
            }

            // exp(-x) = 1/exp(x)
            bool neg = x._raw < 0;
            if (neg) {
                x = -x;
            }

            if (x == One) {
                return neg ? One / (Fix64)2 : (Fix64)2;
            }

            if (x >= Log2Max) {
                return neg ? One / MaxValue : MaxValue;
            }

            if (x <= Log2Min) {
                return neg ? MaxValue : Zero;
            }

            int integerPart = (int)Floor(x);
            // 取指数的小数部分
            x = new Fix64(x._raw & FRACTION_MASK);

            var result = One;
            var term = One;
            int i = 1;
            while (term._raw != 0) {
                term = FastMul(FastMul(x, term), Ln2) / (Fix64)i;
                result += term;
                i++;
            }

            result = new Fix64(result._raw << integerPart);
            if (neg) {
                result = One / result;
            }

            return result;
        }
        
        /// <summary>
        /// 返回指定数字的指定幂（精度约5位数）
        /// </summary>
        /// <exception cref="DivideByZeroException">
        /// 底数为零，指数为负
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 底数为负，指数非零
        /// </exception>
        public static Fix64 Pow(Fix64 b, Fix64 exp) {
            if (b == One) {
                return One;
            }

            if (exp._raw == 0) {
                return One;
            }

            if (b._raw == 0) {
                if (exp._raw < 0) {
                    throw new DivideByZeroException();
                }

                return Zero;
            }

            Fix64 log2 = Log2(b);
            return Pow2(exp * log2);
        }
        
        /// <summary>
        /// 返回指定数字的以2为底的对数
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 参数非正
        /// </exception>
        public static Fix64 Log2(Fix64 x) {
            if (x._raw <= 0) {
                throw new ArgumentOutOfRangeException("Non-positive value passed to Ln", "x");
            }

            long b = 1U << (FRACTIONAL_BITS - 1);
            long y = 0;

            long xRaw = x._raw;
            while (xRaw < ONE) {
                xRaw <<= 1;
                y -= ONE;
            }

            while (xRaw >= (ONE << 1)) {
                xRaw >>= 1;
                y += ONE;
            }

            var z = new Fix64(xRaw);

            for (int i = 0; i < FRACTIONAL_BITS; i++) {
                z = FastMul(z, z);
                if (z._raw >= (ONE << 1)) {
                    z = new Fix64(z._raw >> 1);
                    y += b;
                }

                b >>= 1;
            }

            return new Fix64(y);
        }

        /// <summary>
        /// 返回指定数字的自然对数
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 参数非正
        /// </exception>
        public static Fix64 Ln(Fix64 x) {
            return FastMul(Log2(x), Ln2);
        }

        /// <summary>
        /// 开平方根
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// 负参数
        /// </exception>
        public static Fix64 Sqrt(Fix64 x) {
            var xRaw = x._raw;
            if (xRaw < 0) {
                throw new ArgumentOutOfRangeException("Negative value passed to Sqrt", "x");
            }

            var num = (ulong)xRaw;
            var result = 0UL;

            // 从高位第二位开始
            var bit = 1UL << (NUM_BITS - 2);
            while (bit > num) {
                bit >>= 2;
            }

            // 主要部分执行两次，以避免在计算中使用128位值。
            for (var i = 0; i < 2; ++i) {
                // 首先获取结果的前48位
                while (bit != 0) {
                    if (num >= result + bit) {
                        num -= result + bit;
                        result = (result >> 1) + bit;
                    }
                    else {
                        result = result >> 1;
                    }

                    bit >>= 2;
                }

                if (i == 0) {
                    // 对后16位重复这个过程
                    if (num > (1UL << (NUM_BITS / 2)) - 1) {
                        // num = a - (result + 0.5)^2
                        //       = num + result^2 - (result + 0.5)^2
                        //       = num - result - 0.5
                        num -= result;
                        num = (num << (NUM_BITS / 2)) - 0x80000000UL;
                        result = (result << (NUM_BITS / 2)) + 0x80000000UL;
                    }
                    else {
                        num <<= (NUM_BITS / 2);
                        result <<= (NUM_BITS / 2);
                    }

                    bit = 1UL << (NUM_BITS / 2 - 2);
                }
            }

            // 如果下一位为 1，则将结果向上舍入。
            if (num > result) {
                ++result;
            }

            return new Fix64((long)result);
        }
        
        /// <summary>
        /// 返回x的正弦值。[-2PI, 2PI]中x的相对误差小于1E-10，最坏情况下小于1E-7
        /// </summary>
        public static Fix64 Sin(Fix64 x) {
            var clampedL = ClampSinValue(x._raw, out var flipHorizontal, out var flipVertical);
            var clamped = new Fix64(clampedL);

            // 在 LUT 中找到两个最接近的值并执行线性插值
            var rawIndex = FastMul(clamped, LutInterval);
            var roundedIndex = Round(rawIndex);
            var indexError = FastSub(rawIndex, roundedIndex);

            var nearestValue =
                new Fix64(SinLut[flipHorizontal ? SinLut.Length - 1 - (int)roundedIndex : (int)roundedIndex]);
            var secondNearestValue =
                new Fix64(SinLut[
                    flipHorizontal
                        ? SinLut.Length - 1 - (int)roundedIndex - Sign(indexError)
                        : (int)roundedIndex + Sign(indexError)]);

            var delta = FastMul(indexError, FastAbs(FastSub(nearestValue, secondNearestValue)))._raw;
            var interpolatedValue = nearestValue._raw + (flipHorizontal ? -delta : delta);
            var finalValue = flipVertical ? -interpolatedValue : interpolatedValue;
            return new Fix64(finalValue);
        }

        /// <summary>
        /// 返回x正弦值的粗略近似值。
        /// 至少比x86上的Sin()快3倍，比Math.Sin()略快，
        /// 但是对于足够小的x值，其精度仅限于4-5位小数。
        /// </summary>
        public static Fix64 FastSin(Fix64 x) {
            var clampedL = ClampSinValue(x._raw, out bool flipHorizontal, out bool flipVertical);

            // SinLut表的条目数等于(PI_OVER_2 >> 15)
            var rawIndex = (uint)(clampedL >> 15);
            if (rawIndex >= LUT_SIZE) {
                rawIndex = LUT_SIZE - 1;
            }

            var nearestValue = SinLut[flipHorizontal ? SinLut.Length - 1 - (int)rawIndex : (int)rawIndex];
            return new Fix64(flipVertical ? -nearestValue : nearestValue);
        }
        
        static long ClampSinValue(long angle, out bool flipHorizontal, out bool flipVertical) {
            var largePI = 7244019458077122842;
            // 取自 ((Fix64)1686629713.065252369824872831112M)._raw
            // 这是 (2^29)*PI，其中 29 是最大的 N，使得 (2^N)*PI < MaxValue
            // 这个数字精度比 PI_TIMES_2 更高，且(((x % (2^29*PI)) % (2^28*PI)) % ... (2^1*PI) = x % (2 * PI)

            var clamped2Pi = angle;
            for (int i = 0; i < 29; ++i) {
                clamped2Pi %= (largePI >> i);
            }
            
            if (angle < 0) {
                clamped2Pi += PI_TIMES_2;
            }

            // LUT 包含 0 - PiOver2 的值
            flipVertical = clamped2Pi >= PI;
            // 从 (angle % 2PI) 直接获得 (angle % PI)，这样会比重新取模更快
            var clampedPi = clamped2Pi;
            while (clampedPi >= PI) {
                clampedPi -= PI;
            }

            flipHorizontal = clampedPi >= PI_OVER_2;
            // 从 (angle % PI) 直接获得 (angle % PI_OVER_2)，这样会比重新取模更快
            var clampedPiOver2 = clampedPi;
            if (clampedPiOver2 >= PI_OVER_2) {
                clampedPiOver2 -= PI_OVER_2;
            }

            return clampedPiOver2;
        }

        /// <summary>
        /// 返回x的余弦值。[-2PI, 2PI]中x的相对误差小于1E-10，最坏情况下小于1E-7
        /// </summary>
        public static Fix64 Cos(Fix64 x) {
            var xRaw = x._raw;
            var rawAngle = xRaw + (xRaw > 0 ? -PI - PI_OVER_2 : PI_OVER_2);
            return Sin(new Fix64(rawAngle));
        }

        /// <summary>
        /// 返回x余弦值的粗略近似值（详情同FastSin）
        /// </summary>
        public static Fix64 FastCos(Fix64 x) {
            var xRaw = x._raw;
            var rawAngle = xRaw + (xRaw > 0 ? -PI - PI_OVER_2 : PI_OVER_2);
            return FastSin(new Fix64(rawAngle));
        }

        /// <summary>
        /// 返回x的正切值
        /// </summary>
        /// <remarks>
        /// 未经测试，可能准确度不高
        /// </remarks>
        public static Fix64 Tan(Fix64 x) {
            var clampedPi = x._raw % PI;
            var flip = false;
            if (clampedPi < 0) {
                clampedPi = -clampedPi;
                flip = true;
            }

            if (clampedPi > PI_OVER_2) {
                flip = !flip;
                clampedPi = PI_OVER_2 - (clampedPi - PI_OVER_2);
            }

            var clamped = new Fix64(clampedPi);

            // 在 LUT 中找到两个最接近的值并执行线性插值
            var rawIndex = FastMul(clamped, LutInterval);
            var roundedIndex = Round(rawIndex);
            var indexError = FastSub(rawIndex, roundedIndex);

            var nearestValue = new Fix64(TanLut[(int)roundedIndex]);
            var secondNearestValue = new Fix64(TanLut[(int)roundedIndex + Sign(indexError)]);

            var delta = FastMul(indexError, FastAbs(FastSub(nearestValue, secondNearestValue)))._raw;
            var interpolatedValue = nearestValue._raw + delta;
            var finalValue = flip ? -interpolatedValue : interpolatedValue;
            return new Fix64(finalValue);
        }

        /// <summary>
        /// 使用 Atan 和 Sqrt 计算反余弦（精度至少7位小数）
        /// </summary>
        public static Fix64 Acos(Fix64 x) {
            if (x < -One || x > One) {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (x._raw == 0) {
                return PiOver2;
            }

            var result = Atan(Sqrt(One - x * x) / x);
            return x._raw < 0 ? result + Pi : result;
        }

        /// <summary>
        /// 使用欧拉级数计算反正切（精度至少7位小数）
        /// </summary>
        public static Fix64 Atan(Fix64 z) {
            if (z._raw == 0) {
                return Zero;
            }

            // 强制把参数转化为正值
            // Atan(-z) = -Atan(z).
            var neg = z._raw < 0;
            if (neg) {
                z = -z;
            }

            Fix64 result;
            var two = (Fix64)2;
            var three = (Fix64)3;

            bool invert = z > One;
            if (invert) {
                z = One / z;
            }

            result = One;
            var term = One;

            var zSq = z * z;
            var zSq2 = zSq * two;
            var zSqPlusOne = zSq + One;
            var zSq12 = zSqPlusOne * two;
            var dividend = zSq2;
            var divisor = zSqPlusOne * three;

            for (var i = 2; i < 30; ++i) {
                term *= dividend / divisor;
                result += term;

                dividend += zSq2;
                divisor += zSq12;

                if (term._raw == 0) {
                    break;
                }
            }

            result = result * z / zSqPlusOne;

            if (invert) {
                result = PiOver2 - result;
            }

            if (neg) {
                result = -result;
            }

            return result;
        }

        public static Fix64 Atan2(Fix64 y, Fix64 x) {
            var yl = y._raw;
            var xl = x._raw;
            if (xl == 0) {
                if (yl > 0) {
                    return PiOver2;
                }

                if (yl == 0) {
                    return Zero;
                }

                return -PiOver2;
            }

            Fix64 atan;
            var z = y / x;

            // 处理溢出
            if (One + 0.28M * z * z == MaxValue) {
                return y < Zero ? -PiOver2 : PiOver2;
            }

            if (Abs(z) < One) {
                atan = z / (One + 0.28M * z * z);
                if (xl < 0) {
                    if (yl < 0) {
                        return atan - Pi;
                    }

                    return atan + Pi;
                }
            }
            else {
                atan = PiOver2 - z / (z * z + 0.28M);
                if (yl < 0) {
                    return atan - Pi;
                }
            }

            return atan;
        }
    }
}