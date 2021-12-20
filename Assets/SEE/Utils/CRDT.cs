using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using static SEE.Game.UI.CodeWindow.CodeWindow;

// Based on the explenaitions and code from https://digitalfreepen.com/2017/10/06/simple-real-time-collaborative-text-editor.html
namespace SEE.Utils
{
    public class CRDT
    {
        public class RemoteDeleteNotPossibleException : Exception
        {
            public RemoteDeleteNotPossibleException(string v) : base(v)
            { }
        }
        public class DeleteNotPossibleException : Exception
        {
            public DeleteNotPossibleException(string v) : base(v)
            { }
        }

        public class Identifier
        {
            private int digit;
            private string site;
            public Identifier(int digit, string site)
            {
                this.digit = digit;
                this.site = site;
            }
            public override string ToString()
            {
                return "(" + digit + ", " + site + ")";
            }

            public int GetDigit()
            {
                return digit;
            }
            public void SetDigit(int digit)
            {
                this.digit = digit;
            }
            public string GetSite()
            {
                return site;
            }
        }
        public class CharObj
        {
            private Identifier[] position;
            private char value;
            public CharObj(char value, Identifier[] position)
            {
                this.value = value;
                this.position = position;
            }

            public char GetValue()
            {
                return value;
            }
            public Identifier[] GetIdentifier()
            {
                return position;
            }
            public override string ToString()
            {
                string result = value + " [";
                bool fst = true;
                foreach (Identifier i in position)
                {
                    if (!fst)
                    {
                        result += ", ";
                    }
                    else
                    {
                        fst = false;
                    }
                    if (i != null)
                    {
                        result += i.ToString();
                    }
                }
                return result + "] ";
            }

        }
        /// <summary>
        /// The crdt history contains all local add and delete char operations, as well as the remote deleteChar operations, because that can blockade a inverse operation of a local add char
        /// It contains a CharObj[] (char + position) and a operationType (add or delete or remoteDelete).
        /// </summary>

        private Stack<(CharObj[], operationType)> undoStack = new Stack<(CharObj[], operationType)>();
        private Stack<(CharObj[], operationType)> redoStack = new Stack<(CharObj[], operationType)>();

        /// <summary>
        /// A buffer to reduce the initial network traffic than oppening a file
        /// </summary>
        //public List<(char, Identifier[], Identifier[], string)> networkbuffer = new List<(char, Identifier[], Identifier[], string)>();

        public string networkbuffer = "";

        /// <summary>
        /// The ID 
        /// </summary>
        private string siteID;

        /// <summary>
        /// The name of the file that is managed with this CRDT
        /// </summary>
        private string filename;

        /// <summary>
        /// The size of the CRDT at the start
        /// </summary>
        private int size;


        //private LinkedList<CharObj> crdt = new LinkedList<CharObj>();

        /// <summary>
        /// Broadcasts if there is a change in the CRDT via the network.
        /// </summary>
        public UnityEvent<char, int, operationType> changeEvent = new UnityEvent<char, int, operationType>();

        /// <summary>
        /// Constructs a CRDT
        /// </summary>
        /// <param name="siteID"></param>
        public CRDT(string siteID, string filename, int size = 5000)
        {
            this.siteID = siteID;
            this.filename = filename;
            this.size = size;
        }

        public string getId()
        {
            return siteID;
        }

        public void setId(string id)
        {
            siteID = id;
        }

        /// <summary>
        /// The chars of the CRDT with their positions
        /// </summary>
        private List<CharObj> crdt = new List<CharObj>(capacity: 5000);

        public List<CharObj> getCRDT()
        {
            return crdt;
        }

        /// <summary>
        /// Expects a string in the following format(| is not in the string only here for the readability): Char|PositionAsString|/|PositionAsString|\n|nextLine
        /// PositionAsString can be null (\0);
        /// Adds the string to the CRDT only for remote use
        /// </summary>
        /// <param name="text"></param>
        /// <param name="file"></param>
        public void RemoteAddString(string text)
        {
            bool CharSet = false;
            char ch = '\0';
            Identifier[] pos1 = null;
            Identifier[] pos2 = null;
            string tmpPos = "";
            foreach (char c in text)
            {
                if (!CharSet)
                {
                    ch = c;
                    CharSet = true;
                }
                else
                {
                    if (c == '/')
                    {
                        pos1 = StringToPosition(tmpPos);
                        tmpPos = "";
                        continue;
                    }
                    if (c == '\n')
                    {
                        pos2 = StringToPosition(tmpPos);
                        tmpPos = "";
                        RemoteAddChar(ch, pos1, pos2);
                        pos1 = null;
                        pos2 = null;
                        ch = '\0';
                        CharSet = false;
                        continue;
                    }
                    tmpPos += c;
                }
            }
        }

        public void SingleRemoteAddChar(char c, Identifier[] position, Identifier[] prePosition)
        {
            if (IsEmpty())
            {
                RemoteAddChar(c, position, prePosition);
            }
        }

        /// <summary>
        /// The Remoteoperation to add a Char from another CRDT/client
        /// </summary>
        /// <param name="c">The Char to add</param>
        /// <param name="position">The position at which the Char should be added</param>
        /// <param name="prePosition">The position befor the Char, can be null if its the Char at index 0</param>
        public void RemoteAddChar(char c, Identifier[] position, Identifier[] prePosition)
        {
            int insertIdx;
            if (prePosition != null && prePosition.Length > 0)
            {
                if (ComparePosition(prePosition, position) < 0)
                {
                    (int, CharObj) found = Find(prePosition);
                    crdt.Insert(found.Item1 + 1, new CharObj(c, position));
                    insertIdx = found.Item1 + 1;
                }
                else
                {
                    ShowNotification.Error("Failure during remote change", "RemoteAddChar failed! Something is wrong with the order of the Chars.");
                    return;
                }

            }
            else
            {
                int idx = FindNextFittingIndex(position, 0);
                if (idx < crdt.Count())
                {
                    crdt.Insert(idx, new CharObj(c, position));
                    insertIdx = idx;
                }
                else
                {
                    crdt.Add(new CharObj(c, position));
                    insertIdx = crdt.Count - 1;
                }

            }
            changeEvent.Invoke(c, insertIdx, operationType.Add);
        }

        /// <summary>
        /// The Remoteoperation to remove a Char from the CRDT
        /// </summary>
        /// <param name="position">The position at which the Char should be deleted</param>
        public void RemoteDeleteChar(Identifier[] position) //TODO Maybe i need a version counter for every action!
        {
            (int, CharObj) found = Find(position);
            if (-1 < found.Item1)
            {
                changeEvent.Invoke(' ', found.Item1, operationType.Delete);
                crdt.RemoveAt(found.Item1);
            }
            else
            {
                throw new RemoteDeleteNotPossibleException("The position is in the CRDT!");
            }
        }

        /// <summary>
        /// Deletes every char between the start and end index (inclusive start and end index)
        /// </summary>
        /// <param name="startIdx">The start index</param>
        /// <param name="endIdx">The end index</param>
        public void DeleteString(int startIdx, int endIdx)
        {
            List<CharObj> charObjs = new List<CharObj>();
            for (int i = endIdx; i >= startIdx; i--)
            {
                charObjs.Add(crdt[i]);
                DeleteChar(i);
            }
            undoStack.Push((charObjs.ToArray(), operationType.Delete));
            redoStack.Clear();
        }

        /// <summary>
        /// Removes a Char from the CRDT at a given index
        /// </summary>
        /// <param name="index">The index at which the Char should be removed</param>
        public void DeleteChar(int index)
        {
            if (crdt.Count() > index && index > -1)
            {
                new NetCRDT().DeleteChar(crdt[index].GetIdentifier(), filename);
                crdt.RemoveAt(index);
            }
            else
            {
                throw new DeleteNotPossibleException("Index is out of range!");
            }
        }
        /// <summary>
        /// Adds a string to the CRDT at the startIdx | for the small data changes
        /// </summary>
        /// <param name="s">The string that should be added</param>
        /// <param name="startIdx">The start index of the string in the file</param>
        public void AddString(string s, int startIdx, bool disarmUndo = false)
        {
            List<CharObj> charObjs = new List<CharObj>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                AddChar(s[i], i + startIdx);
                charObjs.Add(crdt[i + startIdx]);
            }
            if (!disarmUndo)
            {
                CharObj[] charArr = charObjs.ToArray();
                undoStack.Push((charArr, operationType.Add));
                redoStack.Clear();
            }
            

        }

        /// <summary>
        /// Adds a string to the CRDT at the startIdx this is for huge datastream in async mode
        /// </summary>
        /// <param name="s">The string that should be added</param>
        /// <param name="startIdx">The start index of the string in the file</param>
        /// <param name="dontSyncCodeWindowChars"></param>
        public async UniTask AsyncAddString(string s, int startIdx, bool startUp = false)
        {
            await UniTask.SwitchToThreadPool();
            List<CharObj> charObjs = new List<CharObj>(s.Length);
            if (!startUp)
            {
                
                for (int i = 0; i < s.Length; i++)
                {
                    AddChar(s[i], i + startIdx, startUp);
                    charObjs.Add(crdt[i + startIdx]);
                }
            }
            else
            {
                for (int i = 0; i < s.Length; i++)
                {
                    AddChar(s[i], i + startIdx, startUp);
                }
            }
            await UniTask.SwitchToMainThread();
            if (!startUp)
            {
                CharObj[] charArr = charObjs.ToArray();
                undoStack.Push((charArr, operationType.Add));
                redoStack.Clear();
            }
            else
            {
                new NetCRDT().AddString(networkbuffer, filename); ;
            }
        }

        /// <summary>
        /// Adds a new Char to the CRDT structure
        /// </summary>
        /// <param name="c">The Char to add</param>
        /// <param name="index">The index in the local string</param>
        public void AddChar(char c, int index, bool startUp = false)
        {
            Identifier[] position;
            if (index - 1 >= 0 && crdt.Count > index)
            {
                position = GeneratePositionBetween(crdt[index - 1].GetIdentifier(), crdt[index].GetIdentifier(), siteID);
            }
            else if (index - 1 >= 0 && crdt.Count > index - 1)
            {
                position = GeneratePositionBetween(crdt[index - 1].GetIdentifier(), null, siteID);
            }
            else if (index - 1 < 0 && crdt.Count > index)
            {
                position = GeneratePositionBetween(null, crdt[index].GetIdentifier(), siteID);
            }
            else
            {
                position = GeneratePositionBetween(null, null, siteID);
            }

            if (crdt.Count > index)
            {
                crdt.Insert(index, new CharObj(c, position));
            }
            else
            {
                crdt.Add(new CharObj(c, position));
            }


            if (index - 1 >= 0)
            {
                if (startUp)
                {
                    //networkbuffer.Add((c, position, crdt[index - 1].GetIdentifier(), filename));
                    networkbuffer += c + PositionToString(position) + "/" + PositionToString(crdt[index - 1].GetIdentifier()) + "\n";
                }
                else
                {
                    new NetCRDT().AddChar(c, position, crdt[index - 1].GetIdentifier(), filename);
                }
            }
            else
            {
                if (startUp)
                {
                    //networkbuffer.Add((c, position, null, filename));
                    networkbuffer += c + PositionToString(position) + "/" + null + "\n";

                }
                else
                {
                    new NetCRDT().AddChar(c, position, null, filename);
                }
            }
        }

        /// <summary>
        /// Finds the Index to a Position
        /// </summary>
        /// <param name="position">The position</param>
        /// <returns>The index of the Position or -1 if the position isnt in the crdt</returns>
        public int GetIndexByPosition(Identifier[] position)
        {
            return BinarySearch(position, 0, crdt.Count - 1);
        }

        /// <summary>
        /// Compares two Positions and returns wether the first identifier is larger smaller or greater then the second
        /// </summary>
        /// <param name="o1">The first position</param>
        /// <param name="o2">The second position</param>
        /// <returns>-1 if o1 < o2; 0 if o1 == o2; 1 if o1 > o2</o2>  </returns>
        public int ComparePosition(Identifier[] o1, Identifier[] o2)
        {
            for (int i = 0; i < Mathf.Min(o1.Length, o2.Length); i++)
            {
                int cmp = CompareIdentifier(o1[i], o2[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            if (o1.Length < o2.Length)
            {
                return -1;
            }
            else if (o1.Length > o2.Length)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// A comperator for Identifiers
        /// </summary>
        /// <param name="o1">Identifier one</param>
        /// <param name="o2">Identifier two</param>
        /// <returns>-1 if o1 < o2; 0 if o1 == o2; 1 if o1 > o2 </returns>
        public int CompareIdentifier(Identifier o1, Identifier o2)
        {
            if (o1.GetDigit() < o2.GetDigit())
            {
                return -1;
            }
            else if (o1.GetDigit() > o2.GetDigit())
            {
                return 1;
            }
            else
            {
                if (String.Compare(o1.GetSite(), o2.GetSite()) < 0)
                {
                    return -1;
                }
                else if (String.Compare(o1.GetSite(), o2.GetSite()) > 0)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Generates a new position between two old positions, or a complete new one if no position given or only one.
        /// </summary>
        /// <param name="pos1">Before position</param>
        /// <param name="pos2">After position</param>
        /// <param name="site">The site ID of the requester</param>
        /// <returns>A new position</returns>
        public Identifier[] GeneratePositionBetween(Identifier[] pos1, Identifier[] pos2, string site)
        {
            Identifier headP1, headP2;
            int pos1Length, pos2Length;
            if (pos1 != null && pos1.Any())
            {
                headP1 = pos1[0];
                pos1Length = pos1.Length;
            }
            else
            {
                headP1 = new Identifier(0, site);
                pos1Length = 1;
                pos1 = new Identifier[1];
                pos1[0] = headP1;
            }
            if (pos2 != null && pos2.Any())
            {
                headP2 = pos2[0];
                pos2Length = pos2.Length;
            }
            else
            {
                headP2 = new Identifier(int.MaxValue, site);
                pos2Length = 1;
                pos2 = new Identifier[1];
                pos2[0] = headP2;
            }

            if (headP1.GetDigit() != headP2.GetDigit())
            {
                int[] digitP1 = new int[pos1Length];
                int[] digitP2 = new int[pos2Length];
                for (int i = 0; i < pos1Length; i++)
                {
                    digitP1[i] = pos1[i].GetDigit();
                }
                for (int i = 0; i < pos2Length; i++)
                {
                    digitP2[i] = pos2[i].GetDigit();
                }
                int[] delta = CalcDelta(digitP1, digitP2);
                return ToIdentifierList(Increment(digitP1, delta), pos1, pos2, site);
            }
            else if (String.Compare(headP1.GetSite(), headP2.GetSite()) < 0)
            {
                Identifier[] tmp = { headP1 };
                return FromIEnumToIdentifier(tmp.Concat(GeneratePositionBetween(FromIEnumToIdentifier(pos1.Skip(1)), null, site)));
            }
            else if (headP1.GetSite() == headP2.GetSite())
            {
                Identifier[] tmp = { headP1 };
                return FromIEnumToIdentifier(tmp.Concat(GeneratePositionBetween(FromIEnumToIdentifier(pos1.Skip(1)), FromIEnumToIdentifier(pos2.Skip(1)), site)));

            }
            else
            {
                throw new Exception("The CRDT has a wrong order");
            }
        }

        /// <summary>
        /// Finds a CharObj in the CRDT by the position
        /// </summary>
        /// <param name="position">The position of the CharObj to find</param>
        /// <returns>A tuple of the index and the CharObj, Returns (-1,null) if the position is not in the CRDT</returns>
        public (int, CharObj) Find(Identifier[] position)
        {
            int find = BinarySearch(position, 0, crdt.Count - 1);
            if (find > -1)
            {
                return (find, crdt[find]);
            }
            return (-1, null);
        }

        /// <summary>
        /// Finds the next fitting index for a position by recursive testing if the startIdx fitts.
        /// </summary>
        /// <param name="position">The position that need an idx</param>
        /// <param name="startIdx">The idx wich should be tested first</param>
        /// <returns>The index at which the position should be placed. WARNING CAN BE GREATER THEN THE SIZE OF THE CRDT!</returns>
        private int FindNextFittingIndex(Identifier[] position, int startIdx)
        {
            if (startIdx < crdt.Count() && ComparePosition(crdt[startIdx].GetIdentifier(), position) < 0)
            {
                return FindNextFittingIndex(position, startIdx + 1);
            }
            else
            {
                return startIdx;
            }
        }

        /// <summary>
        /// Converts a IEnumerable to an Identifier[]
        /// </summary>
        /// <param name="ienum">The enum that should be converted</param>
        /// <returns>The Identifier[]</returns>
        private Identifier[] FromIEnumToIdentifier(IEnumerable<Identifier> ienum)
        {
            Identifier[] ret = new Identifier[ienum.Count()];
            for (int i = 0; i < ienum.Count(); i++)
            {
                ret[i] = ienum.ElementAt(i);
            }

            return ret;
        }

        /// <summary>
        /// Increments the value a little less than the delta size
        /// </summary>
        /// <param name="value">The value that should be incremented</param>
        /// <param name="delta">The referenze size</param>
        /// <returns>The incremented value</returns>
        private int[] Increment(int[] value, int[] delta)
        {
            int[] tmp = { 0, 1 };
            IEnumerable<int> incE = delta.Take(Array.FindIndex(delta, x => x != 0)).Concat(tmp);
            int[] inc = new int[incE.Count()];
            int i = incE.Count() - 1;
            foreach (int elm in incE)
            {
                inc[i] = elm;
                i = i--;

            }

            int[] incValue = Add(value, inc);

            return incValue[incValue.Length - 1] == 0 ? Add(incValue, inc) : incValue;
        }

        /// <summary>
        /// Creates a new identifier list (position identifier) 
        /// </summary>
        /// <param name="newPos">The new position to insert</param>
        /// <param name="before">The position before the new one</param>
        /// <param name="after">The position after the new one</param>
        /// <param name="site">The site id from the user</param>
        /// <returns>A list of identifier, representing a position</returns>
        private Identifier[] ToIdentifierList(int[] newPos, Identifier[] before, Identifier[] after, string site)
        {
            return FromIEnumToIdentifier(newPos.Select((digit, index) =>
           {
               if (index == newPos.Length - 1)
               {
                   return new Identifier(digit, site);
               }
               else if (index < before.Length && digit == before[index].GetDigit())
               {
                   return new Identifier(digit, before[index].GetSite());
               }
               else if (index < after.Length && digit == after[index].GetDigit())
               {
                   return new Identifier(digit, after[index].GetSite());
               }
               else
               {
                   return new Identifier(digit, site);
               }
           }));
        }

        /// <summary>
        /// Adds two arrays elm by elm
        /// </summary>
        /// <param name="o1">The first array</param>
        /// <param name="o2">The second array</param>
        /// <returns>The array that contains the summ of o1 and o2</returns>
        private int[] Add(int[] o1, int[] o2)
        {
            int size = Math.Max(o1.Length, o2.Length);
            int[] result = new int[size];
            for (int i = 0; i < size; i++)
            {

                if (i >= o1.Length)
                {
                    result[i] = o2[i];
                }
                else if (i >= o2.Length)
                {
                    result[i] = o1[i];
                }
                else
                {
                    result[i] = o1[i] + o2[i];
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the difference delta between each value in the arrays
        /// </summary>
        /// <param name="o1">The first array</param>
        /// <param name="o2">The second array</param>
        /// <returns>An array with the delta values</returns>
        private int[] CalcDelta(int[] o1, int[] o2)
        {
            int size = Mathf.Max(o1.Length, o2.Length);
            int[] delta = new int[size];
            for (int i = 0; i < size; i++)
            {
                if (o1.Length <= i)
                {
                    delta[i] = o2[i];
                }
                else if (o2.Length <= i)
                {
                    delta[i] = o1.Length;
                }
                else
                {
                    delta[i] = Mathf.Abs(o1[i] - o2[i]);
                }
            }
            return delta;
        }

        /// <summary>
        /// Binarysearches the CRDT for a given position
        /// </summary>
        /// <param name="position">The position that is searched</param>
        /// <param name="start">The start index, usaly 0 if the full CRDT should be searched</param>
        /// <param name="end">The end index, usaly crdt.Count if the full CRDT should be searched</param>
        /// <returns>The index at which the position is placed or -1 if the position is not contained in the CRDT</returns>
        private int BinarySearch(Identifier[] position, int start, int end)
        {
            if (start < end)
            {
                int length = end - start + 1;
                int mid = 0;
                mid = length / 2 + start;
                if (ComparePosition(crdt[mid].GetIdentifier(), position) == 0)
                {
                    return mid;
                }
                else if (ComparePosition(crdt[mid].GetIdentifier(), position) > 0)
                {
                    return BinarySearch(position, start, mid - 1);
                }
                else
                {
                    return BinarySearch(position, mid + 1, end);
                }
            }
            if (start < crdt.Count && ComparePosition(crdt[start].GetIdentifier(), position) == 0)
            {
                return start;
            }
            return -1;
        }


        public void Undo()
        {
            if (undoStack.Count < 1)
            {
                ShowNotification.Info("Undo Failure", "Undo Stack is empty!");
                return;
            }
            (CharObj[], operationType) lastOperation = undoStack.Pop();
            switch (lastOperation.Item2)
            {
                case operationType.Add:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        int idx = GetIndexByPosition(c.GetIdentifier());
                        if (idx > -1)
                        {
                            changeEvent.Invoke(' ', idx, operationType.Delete);
                            DeleteChar(idx);
                        }
                        else
                        {
                            ShowNotification.Warn("Undo Failure", "Undo not possible, Char is already deleted!");
                        }
                    }
                    redoStack.Push((lastOperation.Item1, operationType.Delete));
                    break;
                case operationType.Delete:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        internalAddCharObj(c);
                    }
                    redoStack.Push((lastOperation.Item1, operationType.Add));
                    break;
            }
        }

        public void Redo()
        {
            if (redoStack.Count < 1)
            {
                ShowNotification.Info("Redo Failure", "Redo Stack is empty!");
                return;
            }
            (CharObj[], operationType) lastOperation = redoStack.Pop();
            switch (lastOperation.Item2)
            {
                case operationType.Add:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        int idx = GetIndexByPosition(c.GetIdentifier());
                        if (idx > -1)
                        {
                            changeEvent.Invoke(' ', idx, operationType.Delete);
                            DeleteChar(idx);
                        }
                        else
                        {
                            ShowNotification.Warn("Redo Failure", "Redo not possible, Char is already deleted!");
                        }
                    }
                    undoStack.Push((lastOperation.Item1, operationType.Delete));
                    break;
                case operationType.Delete:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        internalAddCharObj(c);
                    }
                    undoStack.Push((lastOperation.Item1, operationType.Add));
                    break;
            }
        }

        /// <summary>
        /// ReAdds a existing CharObj into the crdt, used for undo and redo
        /// </summary>
        /// <param name="c">The CharObj that should be added</param>
        private void internalAddCharObj(CharObj c)
        {
            int index = FindNextFittingIndex(c.GetIdentifier(), 0);
            if (index < crdt.Count)
            {
                crdt.Insert(index, c);
            }
            else
            {
                crdt.Add(c);
            }
            
            if (index - 1 >= 0)
            {
                new NetCRDT().AddChar(c.GetValue(), c.GetIdentifier(), crdt[index - 1].GetIdentifier(), filename);
            }
            else
            {
                new NetCRDT().AddChar(c.GetValue(), c.GetIdentifier(), null, filename);
            }
            changeEvent.Invoke(c.GetValue(), index, operationType.Add);
        }

        /// <summary>
        /// For a new client later in the network
        /// </summary>
        /// <param name="recipient"></param>
        public void SyncCodeWindows(IPEndPoint[] recipient)
        {
            int idx = 0;
           foreach(CharObj c in crdt)
            {
                if(idx != 0)
                {
                    new NetCRDT().AddChar(c.GetValue(), c.GetIdentifier(), crdt[idx - 1].GetIdentifier(), filename);
                }
                else
                {
                    new NetCRDT().SingleAddChar(c.GetValue(), c.GetIdentifier(), null, filename, recipient);

                }
                idx++;
            }
           
        }

        /// <summary>
        /// Transforms an string into a position
        /// </summary>
        /// <param name="s">The string contianing the position</param>
        /// <returns>a position - Identifier[]</returns>
        public Identifier[] StringToPosition(string s)
        {
            List<Identifier> ret = new List<Identifier>();
            string digit = "";
            string siteID = "";
            bool isDigit = false;
            bool isSiteID = false;
            bool next = false;
            if (s == null || s == "") return null;
            foreach (char c in s)
            {
                if (c == '(')
                {
                    isDigit = true;
                    next = false;
                }
                else if (c == ',' && !next)
                {
                    isDigit = false;
                    isSiteID = true;
                }
                else if (c == ' ')
                {
                    //do nothing
                }
                else if (c == ')')
                {
                    isSiteID = false;
                    next = true;
                }
                else if (c == ',' && next)
                {
                    ret.Add(new Identifier(int.Parse(digit), siteID));
                    digit = "";
                    siteID = "";
                }
                else
                {
                    if (isDigit)
                    {
                        digit += c;
                    }
                    else if (isSiteID)
                    {
                        siteID += c;
                    }
                }
            }
            //The last element in the string hasnt a comma behind it so we need to insert it here!
            if (digit != "" && siteID != "")
            {
                ret.Add(new Identifier(int.Parse(digit), siteID));
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Converts a position into a string
        /// </summary>
        /// <param name="position">The position that should be converted to a string</param>
        /// <returns>A string of a position</returns>
        public string PositionToString(Identifier[] position)
        {
            string ret = "";
            bool fst = true;
            if (position == null) return null;
            foreach (Identifier i in position)
            {
                if (!fst)
                {
                    ret += ", ";
                }
                else
                {
                    fst = false;
                }
                ret += i.ToString();
            }
            return ret;
        }

        /// <summary>
        /// Converts the CRDT values to a String 
        /// </summary>
        /// <returns>The readable String of the values in the CRDT</returns>
        public string PrintString()
        {
            string ret = "";
            foreach (CharObj elm in crdt)
            {
                ret += elm.GetValue();
            }
            return ret;
        }

        /// <summary>
        /// Converts the CRDT to a String
        /// </summary>
        /// <returns>A string representing the CRDT</returns>
        public override string ToString()
        {
            string ret = "";
            if (crdt == null)
            {
                return "CRDT EMPTY!";
            }
            foreach (CharObj elm in crdt)
            {
                if (elm != null)
                {
                    ret += elm.ToString();
                }
            }
            return ret;
        }

        /// <summary>
        /// Tests if the CRDT is empyt
        /// </summary>
        /// <returns>True if the CRDT is empty, false if not</returns>
        public bool IsEmpty()
        {
            return crdt.Count < 1;
        }
    }
}
