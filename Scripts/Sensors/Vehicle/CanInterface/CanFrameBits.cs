using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text;

namespace Autonoma
{

public class CanFrameBits
{
    // 20 + 64 + 28 = 112 , 14 bytes 
    public static int MaxLength = 14;
    public static int IdStartBit = 1;
    public static int IdFieldLength = 11;
    public static int RtrStartBit = 13;
    public static int IdExtensionStartBit = 14;
    public static int DlcStartBit = 16;
    public static int DlcFieldLength = 4;
    public static int DataFieldStartBit = 20;
    public static int DataFieldLength = 64;
    public static int CrcStartBit = 84;
    public static int CrcFieldLength = 15;
    public byte[] data = new byte[MaxLength];

    public void set_bits(int target_start_bit, ulong source_data, int source_length, bool lsb)
    {
        if(lsb)
        {
            source_data = reverse_bit_order(source_data, source_length);
        }
        int target_byte_index = target_start_bit / 8;
        int first_block_size = 8 - (target_start_bit % 8); // The 
        int last_block_size = ((target_start_bit + source_length) % 8);

        //Console.WriteLine("Setting from {0} to {1}", target_start_bit, source_length);
        //PrintBits.Long("with bits", source_data);

        if(source_length < 9 && (target_start_bit % 8 == 0))
        {
            first_block_size = source_length;
            last_block_size = 0;
        }
        int pos = first_block_size;

        if(first_block_size > 0)
        {
            byte mask = (byte)~((((byte)1 << first_block_size) - 1) << first_block_size);
            int source_shift = (source_length - first_block_size);
            //Console.WriteLine("Source shift is {0}", source_shift);
            // need to override current data, not just |=
            data[target_byte_index] &= (byte)~mask;
            data[target_byte_index++] |= (byte)((byte)(source_data >> source_shift) & mask);

            print();
        }

        while(source_length - pos > 7)
        {
            //Console.WriteLine("handling middle byte {0}", target_byte_index);
            int source_shift = (source_length - pos) - 8;
            //Console.WriteLine("Source shift is {0}", source_shift);
            data[target_byte_index++] = (byte)(source_data >> (source_shift));
            pos += 8;

            print();
        }

        if(last_block_size > 0)
        {
           //Console.WriteLine("setting last bock of size {0} in byte {1}", last_block_size, target_byte_index);
            byte mask = (byte)~(((byte)1 << 8 - last_block_size) - 1);

            //PrintBits.Byte("mask", mask);
            //PrintBits.Byte("pre", data[target_byte_index]);

            data[target_byte_index] &= (byte)~mask;

            //PrintBits.Byte("cleaned", data[target_byte_index]);

            data[target_byte_index] |= (byte)(source_data << 8 - last_block_size);
            //PrintBits.Byte("post", data[target_byte_index]);

            print();
        }
    }

    public ulong get_bits(int start_bit, int length, bool lsb)
    {
        ulong result = 0;
        int target_byte_index = start_bit / 8;
        int first_block_size = 8 - (start_bit % 8); // The 
        int last_block_size = ((start_bit + length) % 8);

        // doesn't work for non byte aligned with len < 9, last block size may have size...
        if(length < 9 && (start_bit % 8 == 0))
        {
            first_block_size = length;
            last_block_size = 0;
        }

        int pos = first_block_size;

        Console.WriteLine("Get length: {0}, start index (64 - length = {1})", length, 64 - length);
        Console.WriteLine("Start Bit: {0}, LBS {1}", start_bit, last_block_size);
        //PrintBits.Long("edge", (ulong)(((ulong)1 << (64 - length))));

        // the name "pos" is misleading..

        if(first_block_size > 0)
        {
            //Console.WriteLine("FBS: {0}, Length from pos: {1}", first_block_size, length - pos);
            ulong mask = (((ulong)1 << first_block_size) - 1) << length - pos;
            ulong raw_source_block = (ulong)data[target_byte_index++] << (length - pos);

            //PrintBits.Long("mask", mask);
            //PrintBits.Long("raw", raw_source_block);
            //PrintBits.Long("masked", raw_source_block & mask);
            //PrintBits.Long("pre-or-result", result);

            result |= raw_source_block & mask;
            //PrintBits.Long("post-or-result", result);
            //pos += first_block_size;
        }

        Console.WriteLine("");

        pos += 8;

        while(length - pos > -1)
        {
            //Console.WriteLine("Length from pos: {0}", length - pos);
            ulong mask = (ulong)((((ulong)1 << 8) - 1) << length - pos);
            ulong raw_source_block = (ulong)data[target_byte_index++] << length - pos;

           // PrintBits.Long("mask", mask);
            //PrintBits.Long("raw", raw_source_block);
            //PrintBits.Long("pre-or-result", result);

            result |= raw_source_block & mask;

            //PrintBits.Long("post-or-result", result);

            pos += 8;
        }

        Console.WriteLine("");

        if(last_block_size > 0)
        {
            //Console.WriteLine("LBS {0}", last_block_size);
            //Console.WriteLine("doing last block for get size: {0}, byte index {1}", last_block_size, target_byte_index);
            //Console.WriteLine("len - pos is {0}", length - pos);

            ulong mask = (ulong)((((ulong)1 << last_block_size) - 1));
            ulong raw_source_block = (ulong)data[target_byte_index] >> 8 - last_block_size;

            //PrintBits.Long("mask", mask);
           // PrintBits.Long("raw", raw_source_block);
            //PrintBits.Long("masked", raw_source_block & mask);
            //PrintBits.Long("pre-or-result", result);

            result |= raw_source_block & mask;
        }

        //PrintBits.Long("final result", result);

        //return result;
        return lsb ? reverse_bit_order(result, length) : result;
    }

    public static ulong reverse_bit_order(ulong data, int length)
    {
        ulong result = 0;

        for(int pos = 0; pos < length; pos++)
        {
            result <<= 1;
            result |= data & 1;
            data >>= 1;
        }

        return result;
    }

    public static ulong reverse_endianness(ulong data, int length)
    {
        ulong result = 0;

        // if lsb, count by 8 starting at left, else count by 8 starting at right
        // one needs to do remainder up front and the other does at the end

        for(int pos = 0; pos < length; pos += 8)
        {
            result <<= 8;
            result |= data & 0xFF;
            data >>= 8;
        }

        return result;
    }

    public void set_id(ulong id)
    {
        set_bits(IdStartBit, id, IdFieldLength, true);
    }

    public ulong get_id()
    {
        return get_bits(IdStartBit, IdFieldLength, true);
    }

    public void set_data_field(byte[] data)
    {
        for(int i = 0; i < 8; i++)
        {
            set_bits(DataFieldStartBit + i * 8, (ulong)data[i], 8, true);
        }
    }

    public void set_data_field(ulong data)
    {
        set_bits(DataFieldStartBit, data, DataFieldLength, true);
    }

    public ulong get_data_field()
    {
        return get_bits(DataFieldStartBit, DataFieldLength, true);
    }

    public void set_rtr(bool dominant)
    {
        set_bits(RtrStartBit, (ulong)(dominant ? 0x00 : 0x01), 1, true);
    }

    public bool get_rtr()
    {
        return get_bits(RtrStartBit, 1, true) == 0x00;
    }

    public void print()
    {
        Console.WriteLine("data hex {0}", BitConverter.ToString(data));
        for(int i = 1; i < CanFrameBits.MaxLength; i+=2)
        {
            Console.WriteLine("b {0} {1} {2}", Convert.ToString(i - 1).PadLeft(3, '0'), 
                Convert.ToString(data[i - 1], 2).PadLeft(8, '0'),
                Convert.ToString(data[i], 2).PadLeft(8, '0'));
        }
    }

    public static String ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);
        foreach (byte b in ba)
        {
            hex.AppendFormat("{0:x2}", b);
        }
        return hex.ToString();
    }
}
}