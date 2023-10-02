using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using System.Text.RegularExpressions;
using System.Globalization;
using can_msgs.msg;

namespace Autonoma
{

public class CanMessageDef
{
    private static String RgxString = @"BO_ (\d*) ([a-z_A-Z\d]*): (\d) ([a-zA-Z_]*)";
    private static RegexOptions RgxOptions = RegexOptions.Compiled | 
        RegexOptions.Multiline;
    private static Regex Rgx = new Regex(RgxString, RgxOptions);

    public ulong id;
    public String name;
    public byte length;
    public String sender;
    public List<CanSignalDef> signals;

    public CanMessageDef(Match entry_match)
    {
        Match message_match = Rgx.Match(entry_match.Value);
        int i = 1;
        id = ulong.Parse(message_match.Groups[i++].Value);
        name = message_match.Groups[i++].Value;
        length = (byte)ushort.Parse(message_match.Groups[i++].Value);
        sender = message_match.Groups[i++].Value;
        signals = CanSignalDef.Parse(entry_match);

        foreach(CanSignalDef s in signals)
        {
            s.dlc = length;
        }
    }

    public byte[] serialize_frame(List<double> signal_data)
    {
        CanFrameBits cf = new CanFrameBits();
        cf.set_id(id);
        for(int i = 0; i < signals.Count; i++)
        {
            ulong item = signals[i].get_value_bits(signal_data[i]);
            cf.set_bits(CanFrameBits.DataFieldStartBit + signals[i].start_bit_index, 
                item, signals[i].length, signals[i].is_little_endian);
        }
        return cf.data;
    }

    public List<double> deserialize_data(byte[] data)
    {
        List<double> out_values = new List<double>();
        CanFrameBits cf = new CanFrameBits();
        cf.set_data_field(data);

        //Debug.Log("Bits inside data field " + BitConverter.ToString(cf.data), null);
        //Debug.Log(Convert.ToString((long)cf.get_data_field(), 2).PadLeft(64, '0'), null);


        for(int i = 0; i < signals.Count; i++)
        {
            ulong value_bits = cf.get_bits(CanFrameBits.DataFieldStartBit + signals[i].start_bit_index,
                signals[i].length, signals[i].is_little_endian);
            Debug.Log(Convert.ToString((long)value_bits, 2).PadLeft(64, '0'), null);
            out_values.Add(signals[i].get_bits_value(value_bits));
        }

        return out_values;
    }

    public void serialize_data(List<double> signal_data, ref byte[] out_bytes)
    {
        ulong block = serialize_data(signal_data);
        for(int i = 0; i < 8; i++)
        {
            out_bytes[i] = (byte)(block >> 56 - (i * 8));
        }
    }

    public void serialize_data(List<double> signal_data, ref Frame out_frame)
    {
        ulong block = serialize_data(signal_data);
        for(int i = 0; i < 8; i++)
        {
            out_frame.Data[i] = (byte)(block >> 56 - (i * 8));
        }
    }

    public void stream_serialize_data(List<double> signal_data, ref Frame out_frame)
    {
        byte[] data_field = new byte[8];
        for(int i = 0; i < signals.Count; i++)
        {
            byte[] item_bytes = signals[i].value_to_bytes(signal_data[i]);
            BitStream.set_bits(ref data_field, item_bytes, signals[i].start_bit_index, 
                signals[i].length, signals[i].is_little_endian);
        }

        data_field = BitStream.reverse_bit_order(data_field, 64);

        for(int i = 0; i < 8; i++)
        {
            out_frame.Data[i] = data_field[i];
        }
    }

    public List<double> stream_deserialize_data(byte[] data)
    {
        List<double> out_values = new List<double>();
        for(int i = 0; i < signals.Count; i++)
        {
            ulong value_bytes = BitStream.get_ulong(data, signals[i].start_bit_index,
                signals[i].length, signals[i].is_little_endian);
            out_values.Add(signals[i].bytes_to_value(value_bytes));
        }
        return out_values;
    }

    public void ser_data(List<double> signal_data, ref Frame out_frame)
    {
        byte[] data_field = new byte[8];
        for(int i = 0; i < signals.Count; i++)
        {
            CanParser.Pack(ref data_field, signal_data[i], signals[i]);
        }

        //Debug.Log(name + " packed bits: " + BitStream.get_byte_string(data_field), null);

        for(int i = 0; i < 8; i++)
        {
            out_frame.Data[i] = data_field[i];
        }
    }

    public List<double> deser_data(byte[] data)
    {
        List<double> out_values = new List<double>();

        for(int i = 0; i < signals.Count; i++)
        {
            double v = CanParser.Unpack(data, signals[i]);
            Debug.Log(name + $" unpacked: {v}", null);
            out_values.Add(v);
        }

        return out_values;
    }

    public ulong serialize_data(List<double> signal_data)
    {
        CanFrameBits cf = new CanFrameBits();
        for(int i = 0; i < signals.Count; i++)
        {
            ulong item = signals[i].get_value_bits(signal_data[i]);
            cf.set_bits(CanFrameBits.DataFieldStartBit + signals[i].start_bit_index, 
                item, signals[i].length, signals[i].is_little_endian);
        }
        return cf.get_data_field();
    }

    public void print()
    {
        Console.WriteLine("ID: {0}, Name: {1}, LEN: {2}, SENDER {3}",
            id, name, length, sender);
        foreach(CanSignalDef s in signals)
        {
            s.print();
        }
    }

    public int get_little_endian_signal_count()
    {
        int ct = 0;
        foreach(CanSignalDef s in signals)
        {
            if(s.is_little_endian)
            {
                ct++;
            }
        }
        return ct;
    }

    public int get_big_endian_signal_count()
    {
        int ct = 0;
        foreach(CanSignalDef s in signals)
        {
            if(!s.is_little_endian)
            {
                ct++;
            }
        }
        return ct;
    }
}

public class CanDBCMessage : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

}
