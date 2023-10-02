using System;




public class BitStream
{

	public static double get_double(byte[] source, int lsb_index, int length, bool source_is_little_endian)
	{
		byte[] double_data = get_bits(source, lsb_index, length, source_is_little_endian, 64);
		return BitConverter.ToDouble(double_data, 0);
	}

	public static float get_float(byte[] source, int lsb_index, int length, bool source_is_little_endian)
	{
		byte[] float_data = get_bits(source, lsb_index, length, source_is_little_endian, 32);
		return BitConverter.ToSingle(float_data, 0);
	}

	public static int get_int(byte[] source, int lsb_index, int length, bool source_is_little_endian)
	{
		byte[] int_data = get_bits(source, lsb_index, length, source_is_little_endian, 32);
		return BitConverter.ToInt32(int_data, 0);
	}

	public static uint get_uint(byte[] source, int lsb_index, int length, bool source_is_little_endian)
	{
		byte[] uint_data = get_bits(source, lsb_index, length, source_is_little_endian, 32);
		return BitConverter.ToUInt32(uint_data, 0);
	}

	public static ulong get_ulong(byte[] source, int lsb_index, int length, bool source_is_little_endian)
	{
		byte[] ulong_data = get_bits(source, lsb_index, length, source_is_little_endian, 64);
		return BitConverter.ToUInt32(ulong_data, 0);
	}

	public static void set_double(double source, ref byte[] target, int lsb_index, int length, bool target_is_little_endian)
	{
		byte[] source_data = BitConverter.GetBytes(source);
		set_bits(ref target, source_data, lsb_index, length, target_is_little_endian);
	}

	public static void set_float(float source, ref byte[] target, int lsb_index, int length, bool target_is_little_endian)
	{
		byte[] source_data = BitConverter.GetBytes(source);
		set_bits(ref target, source_data, lsb_index, length, target_is_little_endian);
	}

	public static void set_int(int source, ref byte[] target, int lsb_index, int length, bool target_is_little_endian)
	{
		byte[] source_data = BitConverter.GetBytes(source);
		set_bits(ref target, source_data, lsb_index, length, target_is_little_endian);
	}

	public static void set_bits(ref byte[] target, byte[] source, int lsb_index, 
		int length, bool target_is_little_endian, bool source_is_little_endian = false)
	{
		for(int i = 0; i < length; i++)
		{
			int target_byte_index = target_is_little_endian ? (lsb_index + i) / 8 : 
				(target.Length * 8 - (lsb_index + i) - 1) / 8; // - i for big endian
			int target_bit_index = (lsb_index + i) % 8;

			int source_byte_index = i / 8; // - Len for big endian source
			int source_bit_index = i % 8;

			byte target_mask = (byte)(1 << target_bit_index);
			byte source_mask = (byte)(1 << source_bit_index);

			Console.WriteLine("SET BITS target_byte {0}/{7}, target_bit {1}, source_byte {2}/{6}, source_bit {3}, i {4}/{5}",
				target_byte_index, target_bit_index, source_byte_index, source_bit_index, i, length, source.Length, target.Length);

			//PrintBits.Byte("target mask", target_mask);
			//PrintBits.Byte("source mask", source_mask);

			if((source[source_byte_index] & source_mask) == 0)
			{
				target[target_byte_index] &= (byte)~target_mask;
			}
			else
			{
				target[target_byte_index] &= (byte)~target_mask;
				target[target_byte_index] |= target_mask;
			}
		}
	}

	public static byte[] get_bits(byte[] source, int lsb_index, int length, bool source_is_little_endian, int out_size_override)
	{
		byte[] target = new byte[Math.Max((int)Math.Ceiling(length / 8.0), out_size_override)];

		for(int i = 0; i < length; i++)
		{
			int target_byte_index = i / 8; // - i for little endian target
			int target_bit_index = i % 8;

			int source_byte_index = source_is_little_endian ? (lsb_index + i) / 8 : 
				(source.Length * 8 - (lsb_index + i) - 1) / 8; // - Len for little endian
			int source_bit_index = (lsb_index + i) % 8;

			byte target_mask = (byte)(1 << target_bit_index);
			byte source_mask = (byte)(1 << source_bit_index);

			Console.WriteLine("GET BITS target_byte {0}/{7}, target_bit {1}, source_byte {2}/{6}, source_bit {3}, i {4}/{5}",
				target_byte_index, target_bit_index, source_byte_index, source_bit_index, i, length, source.Length, target.Length);

			//PrintBits.Byte("target mask", target_mask);
			//PrintBits.Byte("source mask", source_mask);

			if((source[source_byte_index] & source_mask) == 0)
			{
				target[target_byte_index] &= (byte)~target_mask;
			}
			else
			{
				target[target_byte_index] &= (byte)~target_mask;
				target[target_byte_index] |= target_mask;
			}
		}

		return target;
	}

	/*
			reverse bits, up to length bits from LSB (ignore 0 MSB bits)
	*/
    public static byte[] reverse_bit_order(byte[] data, int length)
    {
        byte[] result = new byte[(int)Math.Ceiling(length / 8.0)];

        int source_bit_index = length;
        int source_byte_index = source_bit_index / 8;

        int target_bit_index = 0;
        int target_byte_index = target_bit_index / 8;

        for(int i = 0; i < length; i++)
        {
        	target_bit_index = i % 8;
        	target_byte_index = result.Length - 1 - (target_bit_index / 8);
        	source_bit_index = (length - 1 - i) % 8;
        	source_byte_index = source_bit_index / 8;

        	byte target_mask = (byte)(1 << target_bit_index);
        	byte source_mask = (byte)(1 << source_bit_index);

        	if((data[source_byte_index] & source_mask) == 0)
        	{
        		result[target_byte_index] &= (byte)~target_mask;
        	}
        	else
        	{
        		result[target_byte_index] &= (byte)~target_mask;
				result[target_byte_index] |= target_mask;
        	}
        }

        return result;
    }

    public static String get_byte_string(byte[] bytes)
    {
    	String s = "";
    	for(int i = 0; i < bytes.Length; i++)
    	{
			s += Convert.ToString(bytes[i], 2).PadLeft(8, '0') + " ";
    	}
    	return s;
    }
};