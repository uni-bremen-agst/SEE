using SEE.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            private int site;
            public Identifier(int digit, int site)
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
            public int GetSite()
            {
                return site;
            }
            public void SetSite(int site)
            {
                this.site = site;
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

        int siteID;
        private List<CharObj> crdt = new List<CharObj>();
        public CRDT(int siteID)
        {
            this.siteID = siteID;
        }

        public List<CharObj> getCRDT()
        {
            return crdt;
        }

        /// <summary>
        /// The Remoteoperation to add a Char from another CRDT/client
        /// </summary>
        /// <param name="c">The Char to add</param>
        /// <param name="position">The position at which the Char should be added</param>
        /// <param name="prePosition">The position befor the Char, can be null if its the Char at index 0</param>
        public void RemoteAddChar(char c, Identifier[] position, Identifier[] prePosition)
        {
            
            if (prePosition != null && prePosition.Length > 0)
            {
                Debug.LogWarning("REMOTE ADDD " + PositionToString(position) + " pre " + PositionToString(prePosition) + " char " + c);

                (int, CharObj) found = Find(prePosition);
                Debug.LogWarning("FOUND " + found.Item2.ToString());
                if (ComparePosition(found.Item2.GetIdentifier(), position) < 0)
                {
                    crdt.Insert(found.Item1 + 1, new CharObj(c, position));
                }
                else
                {
                    Debug.LogError("RemoteAddChar fehlgeschlagen! ");
                }

            }
            else
            {
                Debug.LogWarning("REMOTE ADDD " + PositionToString(position) + " char " + c);
                int idx = FindNextFittingIndex(position, 0);
                Debug.LogWarning("idx " + idx + "CRDT S " + crdt.Count());
                if (idx < crdt.Count())
                {
                    crdt.Insert(idx, new CharObj(c, position));
                }
                else
                {
                    crdt.Add(new CharObj(c, position));
                }

            }
        }

        /// <summary>
        /// The Remoteoperation to remove a Char from the CRDT
        /// </summary>
        /// <param name="position">The position at which the Char should be deleted</param>
        public void RemoteDeleteChar(Identifier[] position) //TODO Maybe i need a version counter for every action!
        {
            if (-1 < Find(position).Item1)
            {
                crdt.RemoveAt(Find(position).Item1);
            }
            else
            {
                throw new RemoteDeleteNotPossibleException("The position is in the CRDT!");
            }
        }



        /// <summary>
        /// Removes a Char from the CRDT at a given index
        /// </summary>
        /// <param name="index">The index at which the Char should be removed</param>
        public void DeleteChar(int index)
        {
            if (crdt.Count() > index && index > -1)
            {
                new NetCRDT().DeleteChar(crdt[index].GetIdentifier());
                crdt.RemoveAt(index);
            }
            else
            {
                throw new DeleteNotPossibleException("Index is out of range!");
            }
        }

        /// <summary>
        /// Adds a new Char to the CRDT structure
        /// </summary>
        /// <param name="c">The Char to add</param>
        /// <param name="index">The index in the local string</param>
        public void AddChar(char c, int index)
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
                new NetCRDT().AddChar(c, position, crdt[index - 1].GetIdentifier());
            }
            else
            {
                new NetCRDT().AddChar(c, position, null);
            }
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
                if (o1.GetSite() < o2.GetSite())
                {
                    return -1;
                }
                else if (o1.GetSite() > o2.GetSite())
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
        public Identifier[] GeneratePositionBetween(Identifier[] pos1, Identifier[] pos2, int site)
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
            else if (headP1.GetSite() < headP2.GetSite())
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
            int index = 0;
            foreach (CharObj elm in crdt)
            {
                if (elm.GetIdentifier() == position)
                {
                    return (index, elm);
                }
                index++;
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
            int i = 0;
            foreach (Identifier elm in ienum)
            {
                ret[i] = elm;
                i++;
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
        private Identifier[] ToIdentifierList(int[] newPos, Identifier[] before, Identifier[] after, int site)
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
                else if(c == ',' && !next)
                {
                    isDigit = false;
                    isSiteID = true;
                }
                else if(c == ' ')
                {
                    //do nothing
                }
                else  if(c == ')')
                {
                    isSiteID = false;
                    next = true;
                }
                else if(c == ',' && next)
                {
                    ret.Add(new Identifier(int.Parse(digit), int.Parse(siteID)));
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
                ret.Add(new Identifier(int.Parse(digit), int.Parse(siteID)));
                Debug.LogWarning(PositionToString(ret.ToArray()));
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
            foreach(Identifier i in position)
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
    }
}
