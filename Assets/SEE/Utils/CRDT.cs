using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Based on the explenaitions and code from https://digitalfreepen.com/2017/10/06/simple-real-time-collaborative-text-editor.html
public class CRDT 
{
    public class Identifier
    {
        private int digit;
        private int site;

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

    private Dictionary<string, string> database = new Dictionary<string, string>();
    
    /// <summary>
    /// Compares two Positions and returns wether the first identifier is larger smaller or greater then the second
    /// </summary>
    /// <param name="o1">The first position</param>
    /// <param name="o2">The second position</param>
    /// <returns>-1 if o1 < o2; 0 if o1 == o2; 1 if o1 > o2</o2>  </returns>
    public int ComparePosition(Identifier[] o1, Identifier[] o2)
    {
        for(int i = 0; 1 < Mathf.Min(o1.Length, o2.Length); i++)
        {
            int cmp = CompareIdentifier(o1[i], o2[i]); 
            if(cmp != 0)
            {
                return cmp;
            }
        }
        if(o1.Length < o2.Length)
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

    public int CompareIdentifier(Identifier o1, Identifier o2)
    {
        if(o1.GetDigit() < o2.GetDigit())
        {
            return -1;
        }
        else if(o1.GetDigit() > o2.GetDigit())
        {
            return 1;
        }
        else
        {
            if (o1.GetSite() < o2.GetSite())
            {
                return -1;
            }
            else if(o1.GetSite() > o2.GetSite())
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
    }

    public void addEntry(string id, string siteId, string elm)
    {

    }
}
