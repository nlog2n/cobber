using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Cobber.Core
{
    // Db entry showed in list
    public class CobberDbEntry
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public CobberDbEntry(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }

    // Db table which contains entries
    // Note that it allow entries with same name.
    public class CobberDbTable : List<CobberDbEntry>
    {
        public string Name { get; set; } // table name

        public CobberDbTable(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// CobberDatabaseModule stores obfuscation information for one module
    /// it is a dictionary with key = table name.
    /// </summary>
    public class CobberDbModule : Dictionary<string, CobberDbTable>
    {
        static string GetString(byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public string Name { get; set; } // module name
        public CobberDbModule(string name)
        {
            Name = name;
        }

        public void AddEntry(string table, string name, object value)
        {
            CobberDbTable tbl;
            if (!TryGetValue(table, out tbl))
            {
                // create this table if not exists
                tbl = new CobberDbTable(table);
                this[table] = tbl;
            }

            string valueStr = value.ToString();
            if      (value is byte[])   { valueStr = GetString((byte[])value);}
            else if (value is sbyte)    { valueStr = ((sbyte)value).ToString("X"); }
            else if (value is byte)     { valueStr = ((byte)value).ToString("X"); }
            else if (value is short)    { valueStr = ((short)value).ToString("X"); }
            else if (value is ushort)   { valueStr = ((ushort)value).ToString("X"); }
            else if (value is int)      { valueStr = ((int)value).ToString("X"); }
            else if (value is uint)     { valueStr = ((uint)value).ToString("X"); }
            else if (value is long)     { valueStr = ((long)value).ToString("X"); }
            else if (value is ulong)    { valueStr = ((ulong)value).ToString("X"); }
            else if (value is DateTime) { valueStr = ((DateTime)value).ToString(); }
            else { valueStr = value.ToString(); }

            CobberDbEntry entry = new CobberDbEntry(name, valueStr);
            tbl.Add(entry); // simply add a new entry
        }

        public void Serialize(BinaryWriter wtr)
        {
            wtr.Write(Count);  // number of tables
            foreach (var i in this)
            {
                wtr.Write(i.Key);  // table name
                wtr.Write(i.Value.Count);  // table size
                foreach (var entry in i.Value) // each entry is a name-value pair
                {
                    wtr.Write(entry.Name);  // entry name
                    wtr.Write(entry.Value); // entry value
                }
            }
        }
        public void Deserialize(BinaryReader rdr)
        {
            int count = rdr.ReadInt32();  // number of tables
            for (int i = 0; i < count; i++)
            {
                string tblName = rdr.ReadString(); // table name
                CobberDbTable tbl = new CobberDbTable(tblName);
                int tblSize = rdr.ReadInt32();
                for (int j = 0; j < tblSize; j++)
                {
                    string n = rdr.ReadString();
                    string v = rdr.ReadString();
                    CobberDbEntry entry = new CobberDbEntry(n,v);
                    tbl.Add(entry); // add
                }
                Add(tblName, tbl);
            }
        }
    }

    /// <summary>
    /// CobberDatabase stores obfuscation information for modules.
    /// It is a dictionary with key = module name.
    /// The overall database structure is like (module, table, name-value)
    /// </summary>
    public class CobberDatabase : Dictionary<string, CobberDbModule>
    {
        public string Name { get; set; } // database name

        public CobberDatabase() : base(StringComparer.Ordinal)
        {
        }

        CobberDbModule module; // for current writing
        public void Module(string name)
        {
            if (!TryGetValue(name, out module))
            {
                module = new CobberDbModule(name);
                this[name] = module;
            }
        }

        public void AddEntry(string table, string name, object value)
        {
            module.AddEntry(table, name, value);
        }

        public void Serialize(BinaryWriter wtr)
        {
            wtr.Write(0x434F4252); // write magic number: COBR
            wtr.Write(Count);  // write count
            foreach (var i in this)
            {
                wtr.Write(i.Key);  // write module name
                i.Value.Serialize(wtr);  // write module
            }
        }
        public void Deserialize(BinaryReader rdr)
        {
            if (rdr.ReadInt32() != 0x434F4252)
                throw new InvalidOperationException();
            int count = rdr.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string modName = rdr.ReadString();
                CobberDbModule mod = new CobberDbModule(modName);
                mod.Deserialize(rdr);
                Add(modName, mod);
            }
        }
    }
}
