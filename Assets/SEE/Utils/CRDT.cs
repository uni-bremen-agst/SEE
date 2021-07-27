using OdinSerializer.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Based on the explenaitions and code from https://digitalfreepen.com/2017/10/06/simple-real-time-collaborative-text-editor.html
public class CRDT
{
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
        private string value;

        public override string ToString()
        {
            string result = "[";
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
                result += i.ToString();
            }
            return result + "]";
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
        for (int i = 0; 1 < Mathf.Min(o1.Length, o2.Length); i++)
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

    public Identifier[] GeneratePositionBetween(Identifier[] pos1, Identifier[] pos2, int site)
    {
        Identifier headP1, headP2;
        if (pos1[0] != null)
        {
            headP1 = pos1[0];
        }
        else
        {
            headP1 = new Identifier(0, site);
        }
        if (pos2[0] != null)
        {
            headP2 = pos2[0];
        }
        else
        {
            headP2 = new Identifier(int.MaxValue, site);
        }

        if (headP1.GetDigit() != headP2.GetDigit())
        {
            int[] digitP1 = new int[pos1.Length];
            int[] digitP2 = new int[pos2.Length];
            for (int i = 0; i < pos1.Length; i++)
            {
                digitP1[i] = pos1[i].GetDigit();
            }
            for (int i = 0; i < pos2.Length; i++)
            {
                digitP2[i] = pos2[i].GetDigit();
            }
            int[] delta = CalcDelta(digitP1, digitP2);
            return ToIdentifierList(Increment(digitP1, delta), pos1, pos2, site);
        }
        else if (headP1.GetSite() < headP2.GetSite())
        {
            Identifier[] tmp = { headP1 };
            return (Identifier[])tmp.Concat(GeneratePositionBetween((Identifier[])pos1.Skip(1), null, site));
        } else if(headP1.GetSite() == headP2.GetSite())
        {
            Identifier[] tmp = { headP1 };
            return (Identifier[])tmp.Concat(GeneratePositionBetween((Identifier[])pos1.Skip(1), (Identifier[])pos2.Skip(1), site));
        }


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
        int[] inc = (int[])delta.Take(Array.FindIndex(delta, x => x != 0)).Concat(tmp);
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
        return (Identifier[])newPos.Select((digit, index) =>
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
        });
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

            if (i > o1.Length)
            {
                result[i] = o2[i];
            }
            else if (i > o2.Length)
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
            if (i > o1.Length)
            {
                delta[i] = o2[i];
            }
            else if (i > o2.Length)
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
}
