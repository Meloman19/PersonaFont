﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using PersonaEditorLib.Extension;

namespace PersonaEditorLib.FileStructure.PM1
{
    class PM1Table
    {
        public class Element
        {
            public int Index { get; set; } = -1;
            public int Size { get; set; } = 0;
            public int Count { get; set; } = 0;
            public int Position { get; set; } = 0;

            public Element(int index, int size, int count, int position)
            {
                Index = index;
                Size = size;
                Count = count;
                Position = position;
            }
        }

        public List<Element> Table { get; private set; } = new List<Element>();

        public PM1Table(int[][] array)
        {
            for (int i = 0; i < array.Length; i++)
                Table.Add(new Element(array[i][0], array[i][1], array[i][2], array[i][3]));
        }

        public PM1Table()
        {

        }

        public int Size
        {
            get
            {
                return Table.Count * 0x10;
            }
        }

        public void Get(BinaryWriter writer)
        {
            foreach (var line in Table)
            {
                writer.Write(line.Index);
                writer.Write(line.Size);
                writer.Write(line.Count);
                writer.Write(line.Position);
            }
        }

        public void Update(List<IPM1Element> List)
        {
            if (Table.Count > 0)
            {
                var temp = List.Find(x => x.Type == (TypeMap)Table[0].Index);
                if (temp != null)
                {
                    Table[0].Size = temp.TableSize;
                    Table[0].Count = temp.TableCount;
                }
            }
            for (int i = 1; i < Table.Count; i++)
            {
                Table[i].Position = Table[i - 1].Position + Table[i - 1].Size * Table[i - 1].Count;

                var temp = List.Find(x => x.Type == (TypeMap)Table[i].Index);
                if (temp != null)
                {
                    Table[i].Size = temp.TableSize;
                    Table[i].Count = temp.TableCount;
                }
            }
        }
    }
}