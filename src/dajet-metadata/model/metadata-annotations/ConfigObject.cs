﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace DaJet.Metadata.Model
{
    public class ConfigObject
    {
        public List<object> Values { get; }

        public ConfigObject(int capacity)
        {
            Values = new List<object>(capacity);
        }

        public ConfigObject()
        {
            Values = new List<object>(2000);
        }

        private int[] parseSequence(string sequence)
        {
            List<int> intSequence = new();
            foreach (string s in sequence.Split(","))
            {
                intSequence.Add(Int32.Parse(s.Trim()));
            }

            return intSequence.ToArray();
        }

        public int GetInt32(string sq)
        {
            return GetInt32(parseSequence(sq));
        }

        public long GetInt64(params int[] path)
        {
            return Int64.Parse(GetString(path));
        }

        public int GetInt32(params int[] path)
        {
            return int.Parse(GetString(path));
        }

        public Guid GetUuid(string path)
        {
            return GetUuid(parseSequence(path));
        }

        public Guid GetUuid(params int[] path)
        {
            return new Guid(GetString(path));
        }

        public string GetString(string path)
        {
            return GetString(parseSequence(path));
        }

        public string GetString(params int[] path)
        {
            if (path.Length == 1)
            {
                return (string)Values[path[0]];
            }

            int i = 0;
            List<object> values = Values;
            do
            {
                values = ((ConfigObject)values[path[i]]).Values;
                i++;
            } while (i < path.Length - 1);

            return (string)values[path[i]];
        }

        public ConfigObject GetObject(string path)
        {
            return GetObject(parseSequence(path));
        }

        public T GetObject<T>(params int[] path)
        {
            if (path.Length == 1)
            {
                return (T)Values[path[0]];
            }

            int i = 0;
            List<object> values = Values;
            do
            {
                values = ((ConfigObject)values[path[i]]).Values;
                i++;
            } while (i < path.Length - 1);

            return (T)values[path[i]];
        }

        public ConfigObject GetObject(params int[] path)
        {
            return GetObject<ConfigObject>(path);
        }

        public DiffObject CompareTo(ConfigObject target)
        {
            DiffObject diff = new DiffObject()
            {
                Path = string.Empty,
                SourceValue = this,
                TargetValue = target,
                DiffKind = DiffKind.None
            };

            CompareObjects(this, target, diff);

            return diff;
        }

        private void CompareObjects(ConfigObject source, ConfigObject target, DiffObject diff)
        {
            int source_count = source.Values.Count;
            int target_count = target.Values.Count;
            int count = Math.Min(source_count, target_count);

            ConfigObject mdSource;
            ConfigObject mdTarget;
            for (int i = 0; i < count; i++) // update
            {
                mdSource = source.Values[i] as ConfigObject;
                mdTarget = target.Values[i] as ConfigObject;

                if (mdSource != null && mdTarget != null)
                {
                    DiffObject newDiff = CreateDiff(diff, DiffKind.Update, mdSource, mdTarget, i);
                    CompareObjects(mdSource, mdTarget, newDiff);
                    if (newDiff.DiffObjects.Count > 0)
                    {
                        diff.DiffObjects.Add(newDiff);
                    }
                }
                else if (mdSource != null || mdTarget != null)
                {
                    diff.DiffObjects.Add(
                        CreateDiff(
                            diff, DiffKind.Update, source.Values[i], target.Values[i], i));
                }
                else
                {
                    if ((string)source.Values[i] != (string)target.Values[i])
                    {
                        diff.DiffObjects.Add(
                            CreateDiff(
                                diff, DiffKind.Update, source.Values[i], target.Values[i], i));
                    }
                }
            }

            if (source_count > target_count) // delete
            {
                for (int i = count; i < source_count; i++)
                {
                    diff.DiffObjects.Add(
                        CreateDiff(
                            diff, DiffKind.Delete, source.Values[i], null, i));
                }
            }
            else if (target_count > source_count) // insert
            {
                for (int i = count; i < target_count; i++)
                {
                    diff.DiffObjects.Add(
                        CreateDiff(
                            diff, DiffKind.Insert, null, target.Values[i], i));
                }
            }
        }

        private DiffObject CreateDiff(DiffObject parent, DiffKind kind, object source, object target, int path)
        {
            return new DiffObject()
            {
                Path = parent.Path + (string.IsNullOrEmpty(parent.Path) ? string.Empty : ".") + path.ToString(),
                SourceValue = source,
                TargetValue = target,
                DiffKind = kind
            };
        }

        public override string ToString()
        {
            return "{" + String.Join(", ", Values.ToArray()) + "}";
        }
    }
}