﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MagicFlatIndex
{
    public class FlatFile<T> : List<T>, IDisposable where T : BaseFlatRecord
    {
        private const int INDEX_ENTRY_SIZE = 8;

        private readonly string DataFileName;
        private readonly string IndexFileName;

        private FileStream DataFile;

        private bool IndexChanged = false;
        private SortedDictionary<int, int> Index;

        private readonly byte[] Zero = BitConverter.GetBytes(0);

        private bool Loaded = false;

        #region T members
        private int RecordSize { get; }
        private MethodInfo FromBytesMethod { get; }

        private T? FromBytes(byte[] data)
        {
            return FromBytesMethod.Invoke(null, new object[] { data }) as T;
        }
        #endregion

        #region List<T> members
        public new void Add(T item)
        {
            Load();
            if (item.Id == 0)
            {
                if (Count > 0)
                {
                    item.Id = this.Max(a => a.Id) + 1;
                }
                else
                {
                    item.Id = 1;
                }
            }
            Insert(item);
            base.Add(item);
        }

        public new void AddRange(IEnumerable<T> collection)
        {
            Load();
            foreach (T item in collection)
            {
                base.Add(item);
            }
        }

        public new void RemoveAt(int index)
        {
            Load();
            Delete(this[index].Id);
            base.RemoveAt(index);
        }
        #endregion

        public FlatFile(string tablename)
        {
            Index = new SortedDictionary<int, int>();

            // Reflection do gather T members
            object? oRecordSize = typeof(T).GetMethod("GetSize")!.Invoke(null, null);
            if (oRecordSize is not null)
            {
                RecordSize = (int)oRecordSize;
            }
            FromBytesMethod = typeof(T).GetMethod("FromBytes")!;

            // Data file access : we open it and keep it open as we would do many accesses
            DataFileName = $"{tablename}.dat";
            DataFile = File.Open(DataFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            // Load index : we open then close the file immediately. We will store updated data when disposing the object
            IndexFileName = $"{tablename}.idx";
            if (!File.Exists(IndexFileName))
            {
                RebuildIndex();
            }
            else
            {
                using FileStream IndexFile = File.Open(IndexFileName, FileMode.OpenOrCreate, FileAccess.Read, FileShare.None);
                IndexFile.Seek(0, SeekOrigin.Begin);
                byte[] buffer = new byte[INDEX_ENTRY_SIZE];
                while (IndexFile.Read(buffer, 0, INDEX_ENTRY_SIZE) > 0)
                {
                    Index.Add(BitConverter.ToInt32(buffer, 0), BitConverter.ToInt32(buffer, 4));
                }
                IndexFile.Close();
            }
        }

        public void Load()
        {
            if (Loaded) return;
            base.AddRange(SelectAll());
            Loaded = true;
        }

        /// <summary>
        /// Truncate all file data. TAKE CARE!
        /// </summary>
        public void Truncate()
        {
            if (DataFile != null)
            {
                DataFile.Close();
            }
            File.Delete(DataFileName);
            DataFile = File.Open(DataFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

            if (File.Exists(IndexFileName))
            {
                File.Delete(IndexFileName);
            }
            Index = new SortedDictionary<int, int>();
        }

        /// <summary>
        /// Reorders the records in the file to fill holes, sort them by ID then reduce file size by shrinking empty space at the end
        /// </summary>
        /// <param name="reorder">If true, the program will sort all records, else it will just fill the hole then free the empty space</param>
        public void Shrink(bool reorder)
        {
            if (!reorder)
            {
                int theoricalSize = Index.Count * RecordSize;
                if (DataFile.Length == theoricalSize)
                {
                    // There is no hole in the file
                    return;
                }

                int pos = 0;
                DataFile.Seek(pos, SeekOrigin.Begin);
                byte[] buffer = new byte[RecordSize];
                List<int> holes = new();
                while (pos < theoricalSize)
                {
                    DataFile.Read(buffer, 0, RecordSize);
                    int id = BitConverter.ToInt32(buffer, 0);
                    if (id == 0)
                    {
                        holes.Add(pos / RecordSize);
                    }
                    pos += RecordSize;
                }

                foreach (int hole in holes)
                {
                    // Fill the holes
                    int id = 0;
                    while (id == 0)
                    {
                        DataFile.Read(buffer, 0, RecordSize);
                        id = BitConverter.ToInt32(buffer, 0);
                        if (id > 0)
                        {
                            Index[id] = hole;
                            // Delete the record
                            DataFile.Seek(-RecordSize, SeekOrigin.Current);
                            DataFile.Write(Zero, 0, 4);
                            // Move the record
                            DataFile.Seek(hole * RecordSize, SeekOrigin.Begin);
                            DataFile.Write(buffer, 0, RecordSize);
                            // Get back to the current position
                            DataFile.Seek(pos + RecordSize, SeekOrigin.Begin);
                        }
                        pos += RecordSize;
                    }
                }

                // Shrink the file to is size without hole
                DataFile.SetLength(theoricalSize);
                DataFile.Flush();
            }
            else
            {
                // We create a new datafile and move the records following the index order
                using (FileStream NewDataFile = File.Open($"{DataFileName}.tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                {

                    byte[] buffer = new byte[RecordSize];
                    foreach (KeyValuePair<int, int> entry in Index)
                    {
                        // Locate current record
                        DataFile.Seek(entry.Value * RecordSize, SeekOrigin.Begin);
                        // Load it
                        DataFile.Read(buffer, 0, RecordSize);
                        // Copy it to the new data file
                        NewDataFile.Write(buffer, 0, RecordSize);
                    }

                    // Replace old datafile by the new one
                    DataFile.Close();
                    NewDataFile.Close();
                }
                File.Delete(DataFileName);
                File.Move($"{DataFileName}.tmp", DataFileName);

                // Reopen the new datafile
                DataFile = File.Open(DataFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

                // Now we rebuild the index
                RebuildIndex();
            }

        }
        
        /// <summary>
        /// Rebuild the index from the data file. This can be usefull after a crash or when the index file is missing
        /// </summary>
        public void RebuildIndex()
        {
            if (File.Exists(IndexFileName))
            {
                File.Delete(IndexFileName);
            }
            Index = new SortedDictionary<int, int>();

            DataFile.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[RecordSize];
            int position = 0;
            while (DataFile.Read(buffer, 0, RecordSize) > 0)
            {
                int id = BitConverter.ToInt32(buffer, 0);
                if (id != 0)
                {
                    Index.Add(id, position);
                }
                position++;
            }
        }

        /// <summary>
        /// Append record to the end of the datafile. This method will update the index.
        /// </summary>
        /// <param name="record">Record to be inserted</param>
        /// <returns>true if record was added, otherwise false</returns>
        public bool Insert(T record)
        {
            if (record.Id == 0)
            {
                // We can't insert a deleted record
                return false;
            }
            else if (Index.ContainsKey(record.Id))
            {
                /// TODO: Consider throwing an exception instead
                // We can't insert a duplicate record
                return false;
            }
            else
            {
                byte[] buffer = record.ToBytes();
                DataFile.Seek(0, SeekOrigin.End);
                Index.Add(record.Id, (int)DataFile.Position / RecordSize);
                IndexChanged = true;
                DataFile.Write(buffer, 0, RecordSize);
                return true;
            }
        }

        /// <summary>
        /// This method will check the records we are inserting are not empty, duplicated or already existing
        /// </summary>
        /// <param name="records">Records to insert</param>
        /// <returns>true if the records to insert are not valid</returns>
        private bool CheckRecords(ref T[] records)
        {
            // We won't insert deleted records.
            records = records.Where(record => record.Id != 0).ToArray();

            // We must check there is no duplicated Ids.
            if (records.GroupBy(x => x.Id).Any(g => g.Count() > 1))
            {
                /// TODO: Consider throwing an exception instead
                // We can't insert the same record several times and we don't know which one to keep
                return false;
            }
            else
            {
                for (int i = 0, length = records.Length; i < length; i++)
                {
                    if (Index.ContainsKey(records[i].Id))
                    {
                        /// TODO: Consider throwing an exception instead
                        // We can't insert a duplicate record
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// Append records to the end of the datafile. This method will update the index.
        /// </summary>
        /// <param name="records">Records to be inserted</param>
        /// <returns>number of records added</returns>
        public int InsertMany(T[] records)
        {
            if (records == null || records.Length == 0)
            {
                // We can't insert records that does not exist.
                return 0;
            }
            else
            {
                if (CheckRecords(ref records))
                {
                    byte[] buffer = new byte[records.Length * RecordSize];
                    DataFile.Seek(0, SeekOrigin.End);
                    for (int i = 0, length = records.Length; i < length; i++)
                    {
                        records[i].ToBytes().CopyTo(buffer, i * RecordSize);
                        Index.Add(records[i].Id, ((int)DataFile.Position / RecordSize) + i);
                    }
                    IndexChanged = true;
                    DataFile.Write(buffer, 0, buffer.Length);
                    return records.Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Find the selected record. This method uses the index for better performances.
        /// </summary>
        /// <param name="id">Record's ID</param>
        /// <returns>Found record or null is not found</returns>
        public T? Select(int id)
        {
            if (Index.TryGetValue(id, out int position))
            {
                byte[] buffer = new byte[RecordSize];
                DataFile.Seek(position * RecordSize, SeekOrigin.Begin);
                DataFile.Read(buffer, 0, RecordSize);
                return FromBytes(buffer)!;
            }
            return null;
        }

        /// <summary>
        /// Load all the records from the file. This method processes a full scan of the datafile and doesn't use the index.
        /// </summary>
        /// <returns>An array containing all the records from the file but not the records marked as deleted</returns>
        public T[] SelectAll()
        {
            List<T> records = new();
            DataFile.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[RecordSize];
            while (DataFile.Read(buffer, 0, RecordSize) > 0)
            {
                T record = FromBytes(buffer)!;
                if (record.Id != 0)
                {
                    records.Add(record);
                }
            }
            return records.ToArray();
        }

        /// <summary>
        /// Update the record. This method uses the index for better performances.
        /// </summary>
        /// <param name="record">New record values. The ID can't be changed</param>
        /// <returns>The record was found and changed</returns>
        public bool Update(T record)
        {
            // Update the record
            bool found = false;
            if (Index.TryGetValue(record.Id, out int position))
            {
                DataFile.Seek(position * RecordSize, SeekOrigin.Begin);
                DataFile.Write(record.ToBytes(), 0, RecordSize);
                found = true;
            }
            return found;
        }
        /// <summary>
        /// Delete the record. This method uses the index for better performances.
        /// </summary>
        /// <param name="id">ID of the record</param>
        /// <returns>Record was found and marked as deleted</returns>
        public bool Delete(int id)
        {
            bool found = false;
            if (Index.TryGetValue(id, out int position))
            {
                DataFile.Seek(position * RecordSize, SeekOrigin.Begin);
                DataFile.Write(BitConverter.GetBytes(0), 0, 4);
                Index.Remove(id);
                IndexChanged = true;
                found = true;
            }
            return found;
        }

        /// <summary>
        /// Get the total records count
        /// </summary>
        /// <returns>Number of valid records in the data file</returns>
        public int CountRecords()
        {
            return Index.Count;
        }

        private void SaveIndex()
        {
            // Let's delete the existing index file...
            if (File.Exists(IndexFileName))
            {
                File.Delete(IndexFileName);
            }

            // Then create a new one
            using FileStream IndexFile = File.Open(IndexFileName, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            IndexFile.Seek(0, SeekOrigin.Begin);
            byte[] buffer = new byte[INDEX_ENTRY_SIZE];
            foreach (KeyValuePair<int, int> entry in Index)
            {
                BitConverter.GetBytes(entry.Key).CopyTo(buffer, 0);
                BitConverter.GetBytes(entry.Value).CopyTo(buffer, 4);
                IndexFile.Write(buffer, 0, INDEX_ENTRY_SIZE);
            }
            IndexFile.Close();
        }

        #region Disposable pattern
        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    if (DataFile != null)
                    {
                        DataFile.Close();
                        DataFile.Dispose();
                    }

                    // We save the index only if it was actually modified
                    if (IndexChanged)
                    {
                        SaveIndex();
                    }
                }

                disposed = true;
            }
        }

        ~FlatFile()
        {
            Dispose(false);
        }
        #endregion
    }
}
