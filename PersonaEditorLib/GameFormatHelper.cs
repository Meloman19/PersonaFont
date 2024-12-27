using System;
using System.Collections.Generic;
using System.IO;
using AuxiliaryLibraries.Extensions;
using PersonaEditorLib.FileContainer;
using PersonaEditorLib.Other;
using PersonaEditorLib.Sprite;
using PersonaEditorLib.SpriteContainer;
using PersonaEditorLib.Text;

namespace PersonaEditorLib
{
    public static class GameFormatHelper
    {
        private static readonly Dictionary<Type, string> _defaultExtension = new Dictionary<Type, string>
        {
            { typeof(BF) ,      ".bf"   },
            { typeof(BIN),      ".bin"  },
            { typeof(BVP),      ".bvp"  },
            { typeof(PM1),      ".pm1"  },
            { typeof(TBL),      ".tbl"  },
            { typeof(DAT),      ".dat"  },
            { typeof(FNT),      ".fnt"  },
            { typeof(FNT0),     ".fnt0" },
            { typeof(FTD),      ".ftd"  },
            { typeof(DDS),      ".dds"  },
            { typeof(DDSAtlus), ".dds"  },
            { typeof(TMX),      ".tmx"  },
            { typeof(SPD),      ".spd"  },
            { typeof(SPR),      ".spr"  },
            { typeof(BMD),      ".msg"  },
            { typeof(PTP),      ".ptp"  },
        };

        private static readonly Dictionary<string, Type> _extensionToType = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase)
        {
            //Containers
            { ".bin",  typeof(BIN) },
            { ".pak",  typeof(BIN) },
            { ".pac",  typeof(BIN) },
            { ".p00",  typeof(BIN) },
            { ".p01",  typeof(BIN) },
            { ".arc",  typeof(BIN) },
            { ".dds2", typeof(BIN) },
            { ".cpk",  typeof(BIN) },

            { ".bf",  typeof(BF)  },
            { ".pm1", typeof(PM1) },
            { ".bvp", typeof(BVP) },
            { ".tbl", typeof(TBL) },

            { ".ctd", typeof(FTD) },
            { ".ftd", typeof(FTD) },
            { ".ttd", typeof(FTD) },

            //Graphic containers
            { ".spr", typeof(SPR) },
            { ".spd", typeof(SPD) },

            //Graphic
            { ".fnt", typeof(FNT) },
            { ".tmx", typeof(TMX) },
            { ".dds", typeof(DDS) },

            //Text
            { ".bmd", typeof(BMD) },
            { ".msg", typeof(BMD) },
            { ".ptp", typeof(PTP) }
        };

        private static readonly Dictionary<Type, Type> _alternateType = new Dictionary<Type, Type>
        {
            { typeof(TBL), typeof(BIN)      },
            { typeof(DDS), typeof(DDSAtlus) },
        };

        private static readonly Dictionary<Type, Func<string, byte[], IGameData>> _factories = new Dictionary<Type, Func<string, byte[], IGameData>>
        {
            { typeof(BIN),      (name, data) => name.EndsWith(".cpk", StringComparison.InvariantCultureIgnoreCase) ? new BIN(data, true) : new BIN(data, false) },
            { typeof(SPR),      (name, data) => new SPR(data) },
            { typeof(TMX),      (name, data) => new TMX(data) },
            { typeof(BF),       (name, data) => new BF(data, name) },
            { typeof(PM1),      (name, data) => new PM1(data) },
            { typeof(BMD),      (name, data) => new BMD(data) },
            { typeof(PTP),      (name, data) => new PTP(data) },
            { typeof(FNT),      (name, data) => new FNT(data) },
            { typeof(FNT0),     (name, data) => new FNT0(data) },
            { typeof(BVP),      (name, data) => new BVP(name, data) },
            { typeof(TBL),      (name, data) => new TBL(data, name) },
            { typeof(FTD),      (name, data) => new FTD(data) },
            { typeof(DDS),      (name, data) => new DDS(data) },
            { typeof(DDSAtlus), (name, data) => new DDSAtlus(data) },
            { typeof(SPD),      (name, data) => new SPD(data) },
            { typeof(DAT),      (name, data) => new DAT(data) },
        };

        public static string GetDefaultExtension(Type type)
        {
            if (_defaultExtension.TryGetValue(type, out var extension))
                return extension;

            return string.Empty;
        }

        public static string GetDefaultExtension<T>() =>
            GetDefaultExtension(typeof(T));

        /// <summary>
        /// Tries to open in the specified format
        /// </summary>
        public static GameFile TryOpenFile(string name, byte[] data, Type type)
        {
            try
            {
                if (!_factories.TryGetValue(type, out var factory))
                    return null;

                var obj = factory(name, data);
                return new GameFile(name, obj);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Tries to open in the specified format
        /// </summary>
        public static GameFile TryOpenFile<T>(string name, byte[] data) =>
            TryOpenFile(name, data, typeof(T));

        /// <summary>
        /// Tries to determine the format and open it. On failure, it returns by default in DAT format.
        /// </summary>
        public static GameFile OpenUnknownFile(string name, byte[] data)
        {
            var type = TryGetDataType(data)
                ?? TryGetDataType(name)
                ?? typeof(DAT);

            var gf = TryOpenFile(name, data, type);
            if (gf != null)
                return gf;

            if (_alternateType.TryGetValue(type, out var alternateType))
                return TryOpenFile(name, data, alternateType);

            return TryOpenFile<DAT>(name, data);
        }

        public static Type TryGetDataType(string name)
        {
            string ext = Path.GetExtension(name).TrimEnd();
            if (_extensionToType.TryGetValue(ext, out var type))
                return type;

            return null;
        }

        public static Type TryGetDataType(byte[] data)
        {
            if (data.Length >= 0xC)
            {
                byte[] buffer = data.SubArray(0, 4);
                if (buffer.ArrayEquals(new byte[] { 0x46, 0x4E, 0x54, 0x30 }))
                    return typeof(FNT0);

                buffer = data.SubArray(8, 4);
                if (buffer.ArrayEquals(new byte[] { 0x31, 0x47, 0x53, 0x4D }) | buffer.ArrayEquals(new byte[] { 0x4D, 0x53, 0x47, 0x31 }))
                    return typeof(BMD);
                else if (buffer.ArrayEquals(new byte[] { 0x54, 0x4D, 0x58, 0x30 }))
                    return typeof(TMX);
                else if (buffer.ArrayEquals(new byte[] { 0x53, 0x50, 0x52, 0x30 }))
                    return typeof(SPR);
                else if (buffer.ArrayEquals(new byte[] { 0x46, 0x4C, 0x57, 0x30 }))
                    return typeof(BF);
                else if (buffer.ArrayEquals(new byte[] { 0x50, 0x4D, 0x44, 0x31 }))
                    return typeof(PM1);
            }

            return null;
        }
    }
}