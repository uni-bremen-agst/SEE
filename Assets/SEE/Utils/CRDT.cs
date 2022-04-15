using Cysharp.Threading.Tasks;
using SEE.Game.UI.Notification;
using SEE.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Events;
using static SEE.Game.UI.CodeWindow.CodeWindow;

namespace SEE.Utils

{
    /// <summary>
    /// A Conflict-Free Replicated Data Type (CRDT) for collaboratively edited text.
    /// Based on the explanation and code from
    /// https://digitalfreepen.com/2017/10/06/simple-real-time-collaborative-text-editor.html
    /// </summary>
    public class CRDT
    {
        /// <summary>
        /// An exception that will be thrown when a failure with the <see cref="crdt"/> happens.
        /// </summary>
        public class CRDTfailureException : Exception
        {
            public CRDTfailureException(string v) : base(v)
            { }
        }
        /// <summary>
        /// An exception that will be thrown when a remoteDelete operation is not possible
        /// because the character that should be deleted could not be found in the <see cref="CRDT"/>.
        /// </summary>
        public class RemoteDeleteNotPossibleException : Exception
        {
            public RemoteDeleteNotPossibleException(string v) : base(v)
            { }
        }

        /// <summary>
        /// An exception that will be thrown when a delete operation is not possible
        /// because the requested index is smaller than zero or greater than the length
        /// of the <see cref="CRDT"/>.
        /// </summary>
        public class DeleteNotPossibleException : Exception
        {
            public DeleteNotPossibleException(string v) : base(v)
            { }
        }

        /// <summary>
        /// An exception that will be thrown when a remoteAdd operation tries to add
        /// a character at a position that already exists in the <see cref="CRDT"/>.
        /// </summary>
        public class PositionAlreadyExistsException : Exception
        {
            public PositionAlreadyExistsException(string v) : base(v)
            { }
        }
        
        /// <summary>
        /// An exception that will be thrown when a remoteAdd operation fails, for example the submited position was empty or no fitting index could be found.
        /// </summary>
        public class RemoteAddCharNotPossibleException : Exception
        {
            public RemoteAddCharNotPossibleException(string v) : base(v)
            { }
        }

        /// <summary>
        /// An exception that will be thrown when a undo operation is impossible.
        /// </summary>
        public class UndoNotPossibleExcpetion : Exception
        {
            public UndoNotPossibleExcpetion(string v) : base(v)
            { }
        }

        /// <summary>
        /// An exception that will be thrown when a redo operation is impossible.
        /// </summary>
        public class RedoNotPossibleException : Exception
        {
            public RedoNotPossibleException(string v) : base(v)
            { }
        }

        /// <summary>
        /// An <see cref="Identifier"/> represents a position inside of the CRDT.
        /// It contains the digit, i.e., the index, and the site ID, i.e., the user that
        /// added the character.
        /// </summary>
        public class Identifier
        {
            /// <summary>
            /// Represents an index, i.e., a position of an insertion to the CRDT.
            /// </summary>
            private int digit;

            /// <summary>
            /// The site ID of a user who contributed the inserted character.
            /// </summary>
            private readonly string site;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="digit">The digit representing an index.</param>
            /// <param name="site">The site ID of a user.</param>
            public Identifier(int digit, string site)
            {
                this.digit = digit;
                this.site = site;
            }

            /// <summary>
            /// Converts the Identifer to a string.
            /// </summary>
            /// <returns>The Identifier represented as string.</returns>
            public override string ToString()
            {
                return "(" + digit + ", " + site + ")";
            }

            /// <summary>
            /// Returns the digit of a Identifier.
            /// </summary>
            /// <returns>The digit.</returns>
            public int GetDigit()
            {
                return digit;
            }

            /// <summary>
            /// Sets the digit of the Identifier.
            /// </summary>
            /// <param name="digit">The digit to set.</param>
            public void SetDigit(int digit)
            {
                this.digit = digit;
            }

            /// <summary>
            /// Returns the SiteID of the Identifier.
            /// </summary>
            /// <returns>The SiteID.</returns>
            public string GetSite()
            {
                return site;
            }
        }

        /// <summary>
        ///  A CharObj represents a character in the CRDT. It consists of a unique position
        ///  in the CRDT and the inserted character.
        /// </summary>
        public class CharObj
        {
            /// <summary>
            /// An array that represents a position. A position contains mutltiple tuples of (digit, site).
            /// </summary>
            private readonly Identifier[] position;

            /// <summary>
            /// Character inserted to the CRDT.
            /// </summary>
            private readonly char value;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="value">The character inserted to the CRDT.</param>
            /// <param name="position">The unique position in the CRDT where to insert the character.</param>
            public CharObj(char value, Identifier[] position)
            {
                this.value = value;
                this.position = position;
            }

            /// <summary>
            /// Returns the character that was inserted to the CRDT.
            /// </summary>
            /// <returns>The inserted character</returns>
            public char GetValue()
            {
                return value;
            }

            /// <summary>
            /// Gets the Identifier or Position, respectively.
            /// </summary>
            /// <returns>The Identifier[].</returns>
            public Identifier[] GetIdentifier()
            {
                return position;
            }

            /// <summary>
            /// Converts the CharObj into a string.
            /// </summary>
            /// <returns>The CharObj as String.</returns>
            public override string ToString()
            {
                string result = value + " [";
                bool firstIteration = true;
                foreach (Identifier index in position)
                {
                    if (!firstIteration)
                    {
                        result += ", ";
                    }
                    else
                    {
                        firstIteration = false;
                    }
                    if (index != null)
                    {
                        result += index.ToString();
                    }
                }
                return result + "] ";
            }
        }

        /// <summary>
        /// The crdt history contains all local operations adding or deleting a character.
        /// It contains a CharObj[] (char + position) and a operationType (add or delete).
        /// </summary>
        private Stack<(CharObj[], OperationType)> undoStack = new Stack<(CharObj[], OperationType)>();
        /// <summary>
        /// Undone operations that can be redone.
        /// </summary>
        private Stack<(CharObj[], OperationType)> redoStack = new Stack<(CharObj[], OperationType)>();

        /// <summary>
        /// A buffer to reduce the initial network traffic when oppening a file.
        /// </summary>
        public string networkbuffer = "";

        /// <summary>
        /// The site ID, that is, the user that inserted the character.
        /// </summary>
        private string siteID;

        /// <summary>
        /// The name of the file that is managed by this CRDT-
        /// </summary>
        private readonly string filename;

        /// <summary>
        /// Broadcasts if there is a change in the CRDT via the network.
        /// </summary>
        public UnityEvent<char, int, OperationType> changeEvent = new UnityEvent<char, int, OperationType>();

        /// <summary>
        /// Constructs a CRDT.
        /// </summary>
        /// <param name="siteID">the site ID, i.e., the user editing the file</param>
        /// <param name="filename">the name of the file being edited</param>
        public CRDT(string siteID, string filename)
        {
            this.siteID = siteID;
            this.filename = filename;
        }

        /// <summary>
        /// Returns the site ID of the user in the CRDT.
        /// </summary>
        /// <returns>The site ID.</returns>
        public string GetId()
        {
            return siteID;
        }

        /// <summary>
        /// Sets the site ID of the user in the CRDT.
        /// </summary>
        /// <param name="id">The user ID to set.</param>
        public void SetId(string id)
        {
            siteID = id;
        }

        /// <summary>
        /// The characters of the CRDT with their positions.
        /// </summary>
        private readonly List<CharObj> crdt = new List<CharObj>(capacity: 5000);

        /// <summary>
        /// Returns the CRDT.
        /// </summary>
        /// <returns>The CRDT.</returns>
        public List<CharObj> GetCRDT()
        {
            return crdt;
        }

        /// <summary>
        /// Expects a string in the following format (| is not in the string; only here for the readability):
        /// Char|PositionAsString|/|PositionAsString|\n|nextLine
        /// PositionAsString can be null (\0);
        /// Adds the string to the CRDT only for remote use.
        /// </summary>
        /// <param name="text">The remote changes as a string.</param>
        public void RemoteAddString(string text)
        {
            bool CharSet = false;
            char ch = '\0';
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
                    if (c == '\n')
                    {
                        RemoteAddChar(ch, StringToPosition(tmpPos));
                        tmpPos = "";
                        ch = '\0';
                        CharSet = false;
                        continue;
                    }
                    tmpPos += c;
                }
            }
        }

        /// <summary>
        /// For the later connection of a client into an existing session.
        /// TODO: Actually it doesn't work correctly now; so it needs to be reworked.
        /// </summary>
        /// <param name="c"></param>
        /// <param name="position"></param>
        public void SingleRemoteAddChar(char c, Identifier[] position)
        {
            if (IsEmpty())
            {
                RemoteAddChar(c, position);
            }
        }

        /// <summary>
        /// The Remoteoperation to add a Char from another CRDT/client
        /// </summary>
        /// <param name="c">The Char to add</param>
        /// <param name="position">The position at which the Char should be added</param>
        /// <exception cref="RemoteAddCharNotPossibleException">Throws an Exception if no fitting index for the change could be found or the position is null.</exception>
        public void RemoteAddChar(char c, Identifier[] position)
        {
            if (position != null && position.Length > 0)
            {
                if (crdt.Count == 0)
                {
                    crdt.Add(new CharObj(c, position));
                    changeEvent.Invoke(c, crdt.Count - 1, OperationType.Add);
                    return;
                }
                int insertIdx = FindFittingIndex(position);
                if (insertIdx == -1)
                {
                    throw new RemoteAddCharNotPossibleException("A requested remote change could not be inserted, because no fitting position was found!");
                }
                if (insertIdx >= crdt.Count)
                {
                    crdt.Add(new CharObj(c, position));
                    changeEvent.Invoke(c, insertIdx, OperationType.Add);
                    return;
                }
                crdt.Insert(insertIdx, new CharObj(c, position));
                changeEvent.Invoke(c, insertIdx, OperationType.Add);
            }
            else
            {
                throw new RemoteAddCharNotPossibleException("RemoteAddChar failed! The position was empty.");
            }
        }

        /// <summary>
        /// The remote operation to remove a character from the <see cref="crdt"/>.
        /// </summary>
        /// <param name="position">The position at which the character should be deleted</param>
        /// <exception cref="RemoteDeleteNotPossibleException">Throws the exception when the requested position is not contained by the <see cref="crdt"/></exception>
        public void RemoteDeleteChar(Identifier[] position) //TODO Maybe I need a version counter for every action!
        {
            (int, CharObj) found = Find(position);
            if (-1 < found.Item1)
            {
                changeEvent.Invoke(' ', found.Item1, OperationType.Delete);
                crdt.RemoveAt(found.Item1);
            }
            else
            {
                throw new RemoteDeleteNotPossibleException("The position is not in the CRDT!");
            }
        }

        /// <summary>
        /// Deletes every character between <paramref name="startIdx"/> and <paramref name="endIdx"/> (inclusive).
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
            undoStack.Push((charObjs.ToArray(), OperationType.Delete));
            redoStack.Clear();
        }

        /// <summary>
        /// Removes a character from the CRDT at given <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index at which the character should be removed</param>
        /// <exception cref="DeleteNotPossibleException">Throws an exception than the requested index is out of range.</exception>
        public void DeleteChar(int index)
        {
            if (0 <= index && index < crdt.Count())
            {
                new NetCRDT().DeleteChar(crdt[index].GetIdentifier(), filename);
                crdt.RemoveAt(index);
            }
            else
            {
                throw new DeleteNotPossibleException($"Index {index} is out of range!");
            }
        }

        /// <summary>
        /// Adds the string <paramref name="s"/> to the CRDT at the <paramref name="startIdx"/>
        /// | for the small data changes.
        /// </summary>
        /// <param name="s">The string that should be added</param>
        /// <param name="startIdx">The start index of the string in the file</param>
        /// <param name="disableUndo">At the start up we do not want to be able of using undo so we should disable it</param>
        public void AddString(string s, int startIdx, bool disableUndo = false)
        {
            List<CharObj> charObjs = new List<CharObj>(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                AddChar(s[i], i + startIdx);
                charObjs.Add(crdt[i + startIdx]);
            }
            if (!disableUndo)
            {
                CharObj[] charArr = charObjs.ToArray();
                undoStack.Push((charArr, OperationType.Add));
                redoStack.Clear();
            }
        }

        /// <summary>
        /// Adds <paramref name="addedString"/> to the CRDT at the <paramref name="startIdx"/>.
        /// This method is intended for a large datastream in async mode.
        /// </summary>
        /// <param name="addedString">The string that should be added</param>
        /// <param name="startIdx">The start index of the string in the file</param>
        /// <param name="startUp">Is this the start-up from a CRDT or CodeWindow, respectively</param>
        public async UniTask AsyncAddString(string addedString, int startIdx, bool startUp = false)
        {
            await UniTask.SwitchToThreadPool();
            List<CharObj> charObjs = new List<CharObj>(addedString.Length);
            if (!startUp)
            {
                for (int i = 0; i < addedString.Length; i++)
                {
                    AddChar(addedString[i], i + startIdx, startUp);
                    charObjs.Add(crdt[i + startIdx]);
                }
            }
            else
            {
                for (int i = 0; i < addedString.Length; i++)
                {
                    AddChar(addedString[i], i + startIdx, startUp);
                }
            }
            await UniTask.SwitchToMainThread();
            if (!startUp)
            {
                CharObj[] charArr = charObjs.ToArray();
                undoStack.Push((charArr, OperationType.Add));
                redoStack.Clear();
            }
            else
            {
                new NetCRDT().AddString(networkbuffer, filename); ;
            }
        }

        /// <summary>
        /// Adds <paramref name="addition"/>  to the CRDT at the <paramref name="index"/>.
        /// </summary>
        /// <param name="addition">The character to add</param>
        /// <param name="index">The index in the local string</param>
        /// <param name="startUp">Is this the start-up from a CRDT or CodeWindow, respectively</param>
        public void AddChar(char addition, int index, bool startUp = false)
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
                crdt.Insert(index, new CharObj(addition, position));
            }
            else
            {
                crdt.Add(new CharObj(addition, position));
            }

            if (startUp)
            {
                networkbuffer += addition + PositionToString(position) + "\n";
            }
            else
            {
                new NetCRDT().AddChar(addition, position, filename);
            }
        }

        /// <summary>
        /// Returns the index for the given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position</param>
        /// <returns>The index of <paramref name="position"/> or -1 if <paramref name="position"/>
        /// is not in the crdt</returns>
        public int GetIndexByPosition(Identifier[] position)
        {
            return BinarySearch(position, 0, crdt.Count - 1);
        }

        /// <summary>
        /// Compares <paramref name="first"/> and <paramref name="second"/>.
        /// </summary>
        /// <param name="first">The first position</param>
        /// <param name="second">The second position</param>
        /// <returns>-1 if <paramref name="first"/> < <paramref name="second"/>;
        /// 0 if <paramref name="first"/> == <paramref name="second"/>;
        /// 1 if <paramref name="first"/> > <paramref name="second"/></returns>
        public int ComparePosition(Identifier[] first, Identifier[] second)
        {
            for (int i = 0; i < Mathf.Min(first.Length, second.Length); i++)
            {
                int cmp = CompareIdentifier(first[i], second[i]);
                if (cmp != 0)
                {
                    return cmp;
                }
            }
            if (first.Length < second.Length)
            {
                return -1;
            }
            else if (first.Length > second.Length)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// A comparator for <see cref="Identifier"/>s.
        /// </summary>
        /// <param name="first">Identifier one</param>
        /// <param name="second">Identifier two</param>
        /// <returns>-1 if <paramref name="first"/> < <paramref name="second"/>;
        /// 0 if <paramref name="first"/> == <paramref name="second"/>;
        /// 1 if <paramref name="first"/> > <paramref name="second"/></returns>
        public int CompareIdentifier(Identifier first, Identifier second)
        {
            if (first.GetDigit() < second.GetDigit())
            {
                return -1;
            }
            else if (first.GetDigit() > second.GetDigit())
            {
                return 1;
            }
            else
            {
                if (String.Compare(first.GetSite(), second.GetSite()) < 0)
                {
                    return -1;
                }
                else if (String.Compare(first.GetSite(), second.GetSite()) > 0)
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
        /// Generates a new position between two old positions, or a complete new one
        /// if no or only one position is given.
        /// </summary>
        /// <param name="before">Before position</param>
        /// <param name="after">After position</param>
        /// <param name="site">The site ID of the requester</param>
        /// <returns>A new position</returns>
        /// <exception cref="CRDTfailureException">Throws an exception than the <see cref="crdt"/> has the wrong order.</exception>
        public Identifier[] GeneratePositionBetween(Identifier[] before, Identifier[] after, string site)
        {
            Identifier headP1, headP2;
            int pos1Length, pos2Length;
            if (before != null && before.Any())
            {
                headP1 = before[0];
                pos1Length = before.Length;
            }
            else
            {
                headP1 = new Identifier(0, site);
                pos1Length = 1;
                before = new Identifier[1];
                before[0] = headP1;
            }
            if (after != null && after.Any())
            {
                headP2 = after[0];
                pos2Length = after.Length;
            }
            else
            {
                headP2 = new Identifier(int.MaxValue, site);
                pos2Length = 1;
                after = new Identifier[1];
                after[0] = headP2;
            }

            if (headP1.GetDigit() != headP2.GetDigit())
            {
                int[] digitP1 = new int[pos1Length];
                int[] digitP2 = new int[pos2Length];
                for (int i = 0; i < pos1Length; i++)
                {
                    digitP1[i] = before[i].GetDigit();
                }
                for (int i = 0; i < pos2Length; i++)
                {
                    digitP2[i] = after[i].GetDigit();
                }
                int[] delta = CalcDelta(digitP1, digitP2);
                return ToIdentifierList(Increment(digitP1, delta), before, after, site);
            }
            else if (String.Compare(headP1.GetSite(), headP2.GetSite()) < 0)
            {
                Identifier[] tmp = { headP1 };
                return FromIEnumToIdentifier(tmp.Concat(GeneratePositionBetween(FromIEnumToIdentifier(before.Skip(1)), null, site)));
            }
            else if (headP1.GetSite() == headP2.GetSite())
            {
                Identifier[] tmp = { headP1 };
                return FromIEnumToIdentifier(tmp.Concat(GeneratePositionBetween(FromIEnumToIdentifier(before.Skip(1)),
                                                                                FromIEnumToIdentifier(after.Skip(1)),
                                                                                site)));
            }
            else
            {
                throw new CRDTfailureException("The CRDT has a wrong order.");
            }
        }

        /// <summary>
        /// Finds a CharObj in the CRDT at the <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position of the CharObj to find</param>
        /// <returns>A tuple of the index and the CharObj or(-1, null) if the
        /// position is not in the CRDT</returns>
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
        /// Finds the fitting index for a given <paramref name="position"/>.
        /// </summary>
        /// <param name="position">The position that needs an index</param>
        /// <returns>The index</returns>
        public int FindFittingIndex(Identifier[] position)
        {
            return BinaryIndexFinder(position, 0, crdt.Count - 1);
        }

        /// <summary>
        /// Finds the next fitting index for a position by recursively testing if
        /// the <paramref name="startIdx"/> fits.
        /// </summary>
        /// <param name="position">The position that needs an index</param>
        /// <param name="startIdx">The index wich should be tested first</param>
        /// <returns>The index at which the position should be placed.
        /// WARNING CAN BE GREATER THAN THE SIZE OF THE CRDT!</returns>
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
        /// Converts an IEnumerable to an Identifier[].
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
        /// Increments the value a little less than the delta size.
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
        /// Creates a new identifier list (position identifier).
        /// </summary>
        /// <param name="newPos">The new position to insert</param>
        /// <param name="before">The position before the new one</param>
        /// <param name="after">The position after the new one</param>
        /// <param name="site">The site id from the user</param>
        /// <returns>A list of identifiers representing a position</returns>
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
        /// Adds up two arrays element by element.
        /// </summary>
        /// <param name="first">The first array</param>
        /// <param name="second">The second array</param>
        /// <returns>The array that contains the sum of <paramref name="first"/> and <paramref name="second"/></returns>
        private int[] Add(int[] first, int[] second)
        {
            int[] result = new int[(Math.Max(first.Length, second.Length))];
            for (int i = 0; i < result.Length; i++)
            {
                if (i >= first.Length)
                {
                    result[i] = second[i];
                }
                else if (i >= second.Length)
                {
                    result[i] = first[i];
                }
                else
                {
                    result[i] = first[i] + second[i];
                }
            }
            return result;
        }

        /// <summary>
        /// Calculates the difference delta between each value in the given arrays.
        /// </summary>
        /// <param name="first">The first array</param>
        /// <param name="second">The second array</param>
        /// <returns>An array with the delta values</returns>
        private int[] CalcDelta(int[] first, int[] second)
        {
            int[] delta = new int[(Mathf.Max(first.Length, second.Length))];
            for (int i = 0; i < delta.Length; i++)
            {
                if (first.Length <= i)
                {
                    delta[i] = second[i];
                }
                else if (second.Length <= i)
                {
                    delta[i] = first.Length;
                }
                else
                {
                    delta[i] = Mathf.Abs(first[i] - second[i]);
                }
            }
            return delta;
        }

        /// <summary>
        /// Finds the next position where the given position fits in between the
        /// <paramref name="start"/> and <paramref name="end"/> using binary search.
        /// </summary>
        /// <param name="position">The position that should be fit in</param>
        /// <param name="start">The start of the search space</param>
        /// <param name="end">The end of the search space</param>
        /// <returns>The fitting index</returns>
        /// <exception cref="PositionAlreadyExistsException">Throws the exception when the position that should be added in the <see cref="crdt"/> already is represent in the crdt</exception>
        private int BinaryIndexFinder(Identifier[] position, int start, int end)
        {
            if (start < end)
            {
                int mid = (end - start + 1) / 2 + start;
                if (ComparePosition(crdt[mid].GetIdentifier(), position) < 0)
                {
                    return BinaryIndexFinder(position, mid + 1, end);
                }
                else if (ComparePosition(crdt[mid].GetIdentifier(), position) > 0)
                {
                    return BinaryIndexFinder(position, start, mid - 1);
                }
                else
                {
                    throw new PositionAlreadyExistsException("The searched position exists in the CRDT already!");
                }
            }
            if (end < crdt.Count && ComparePosition(crdt[end].GetIdentifier(), position) == 0)
            {
                throw new PositionAlreadyExistsException("The searched position exists in the CRDT already!");
            }
            else if (end < crdt.Count && ComparePosition(crdt[end].GetIdentifier(), position) == 1)
            {
                return end;
            }
            else if (end < crdt.Count && ComparePosition(crdt[end].GetIdentifier(), position) == -1)
            {
                return end + 1;
            }
            return -1;
        }

        /// <summary>
        /// Searches the CRDT for <paramref name="position"/> in the range from <paramref name="start"/>
        /// to <paramref name="end"/>.
        /// </summary>
        /// <param name="position">The position that is searched</param>
        /// <param name="start">The start index, usually 0 if the complete CRDT should be searched</param>
        /// <param name="end">The end index, usually crdt.Count if the complete CRDT should be searched</param>
        /// <returns>The index at which the position is placed or -1 if the position is not contained in the CRDT</returns>
        private int BinarySearch(Identifier[] position, int start, int end)
        {
            if (start < end)
            {
                int length = end - start + 1;
                int mid = length / 2 + start;
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

        /// <summary>
        /// Undoes the last edit operation.
        /// </summary>
        /// <exception cref="UndoNotPossibleExcpetion">Throws an exception when the undo could not be perfomrt like the undo stack is empty or the operation can´t be undone</exception>
        public void Undo()
        {
            if (undoStack.Count < 1)
            {
                throw new UndoNotPossibleExcpetion("Undo Stack is empty!");
            }
            (CharObj[], OperationType) lastOperation = undoStack.Pop();
            switch (lastOperation.Item2)
            {
                case OperationType.Add:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        int idx = GetIndexByPosition(c.GetIdentifier());
                        if (idx > -1)
                        {
                            changeEvent.Invoke(' ', idx, OperationType.Delete);
                            DeleteChar(idx);
                        }
                        else
                        {
                            throw new UndoNotPossibleExcpetion("Undo not possible, Char is already deleted!");
                        }
                    }
                    redoStack.Push((lastOperation.Item1, OperationType.Delete));
                    break;
                case OperationType.Delete:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        InternalAddCharObj(c);
                    }
                    redoStack.Push((lastOperation.Item1, OperationType.Add));
                    break;
            }
        }

        /// <summary>
        /// Redoes the last undone edit operation.
        /// </summary>
        /// <exception cref="RedoNotPossibleException">Throws an exception than the redo is impossible because of an empty redo stack or the action could not be redone.</exception>
        public void Redo()
        {
            if (redoStack.Count < 1)
            {
                throw new RedoNotPossibleException("Redo Stack is empty!");
            }
            (CharObj[], OperationType) lastOperation = redoStack.Pop();
            switch (lastOperation.Item2)
            {
                case OperationType.Add:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        int idx = GetIndexByPosition(c.GetIdentifier());
                        if (idx > -1)
                        {
                            changeEvent.Invoke(' ', idx, OperationType.Delete);
                            DeleteChar(idx);
                        }
                        else
                        {
                            throw new RedoNotPossibleException("Redo not possible, Char is already deleted!");
                        }
                    }
                    undoStack.Push((lastOperation.Item1, OperationType.Delete));
                    break;
                case OperationType.Delete:
                    foreach (CharObj c in lastOperation.Item1)
                    {
                        InternalAddCharObj(c);
                    }
                    undoStack.Push((lastOperation.Item1, OperationType.Add));
                    break;
            }
        }

        /// <summary>
        /// Re-adds an existing CharObj into the crdt, used for undo and redo
        /// </summary>
        /// <param name="c">The CharObj that should be added</param>
        private void InternalAddCharObj(CharObj c)
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

            new NetCRDT().AddChar(c.GetValue(), c.GetIdentifier(), filename);
            changeEvent.Invoke(c.GetValue(), index, OperationType.Add);
        }

        /// <summary>
        /// For a new client later in the network to synchronize it to the current state.
        /// </summary>
        /// <param name="recipient"></param>
        public void SyncCodeWindows(IPEndPoint[] recipient)
        {
            int idx = 0;
            foreach (CharObj c in crdt)
            {
                if (idx != 0)
                {
                    new NetCRDT().AddChar(c.GetValue(), c.GetIdentifier(), filename);
                }
                else
                {
                    new NetCRDT().SingleAddChar(c.GetValue(), c.GetIdentifier(), filename, recipient);
                }
                idx++;
            }
        }

        /// <summary>
        /// Transforms a string into a position-
        /// </summary>
        /// <param name="s">The string contianing the position</param>
        /// <returns>a position - Identifier[]</returns>
        public Identifier[] StringToPosition(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return null;
            }
            List<Identifier> ret = new List<Identifier>();
            string digit = "";
            string siteID = "";
            bool isDigit = false;
            bool isSiteID = false;
            bool next = false;
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
            // The last element in the string hasn't a comma behind it so we need to insert it here!
            if (digit != "" && siteID != "")
            {
                ret.Add(new Identifier(int.Parse(digit), siteID));
            }

            return ret.ToArray();
        }

        /// <summary>
        /// Converts a position into a string.
        /// </summary>
        /// <param name="position">The position that should be converted to a string</param>
        /// <returns>A string of a position</returns>
        public string PositionToString(Identifier[] position)
        {
            if (position == null)
            {
                return null;
            }
            string result = "";
            bool first = true;
            foreach (Identifier identifier in position)
            {
                if (!first)
                {
                    result += ", ";
                }
                else
                {
                    first = false;
                }
                result += identifier.ToString();
            }
            return result;
        }

        /// <summary>
        /// Converts the CRDT values to a string
        /// </summary>
        /// <returns>The readable string of the values in the CRDT</returns>
        public string PrintString()
        {
            string result = "";
            foreach (CharObj element in crdt)
            {
                result += element.GetValue();
            }
            return result;
        }

        /// <summary>
        /// Converts the CRDT to a string.
        /// </summary>
        /// <returns>A string representing the CRDT</returns>
        public override string ToString()
        {
            if (crdt == null)
            {
                return "CRDT EMPTY!";
            }
            string result = "";
            foreach (CharObj element in crdt)
            {
                if (element != null)
                {
                    result += element.ToString();
                }
            }
            return result;
        }

        /// <summary>
        /// Tests if the CRDT is empty.
        /// </summary>
        /// <returns>True if the CRDT is empty, false if not</returns>
        public bool IsEmpty()
        {
            return crdt.Count < 1;
        }
    }
}
