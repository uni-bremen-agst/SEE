using SEE.Controls;
using SEE.Net;
using SEE.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SEE.Utils.CRDT;

/// <summary>
/// Inserts and Deletes characters in an existing code window on every client to keep the files synct
/// </summary>
public class SyncCodeWindowInput : AbstractAction
{
    /// <summary>
    /// The position as a String
    /// </summary>
    public string position;

    /// <summary>
    /// The File name as a String
    /// </summary>
    public string file;

    /// <summary>
    /// The character that should be added
    /// </summary>
    public char c;

    /// <summary>
    /// Enum to control wether a new char should be added or an existing deleted
    /// </summary>
    public enum Mode
    {
        /// <summary>
        /// The initial mode: do nothing
        /// </summary>
        init,
        /// <summary>
        /// Inserts a new character
        /// </summary>
        insert,
        /// <summary>
        /// Deletes an existing character
        /// </summary>
        delete
    };
    /// <summary>
    /// Controlls the mode if a char should be deleted or added
    /// </summary>
    public Mode mode = Mode.init;
    public SyncCodeWindowInput() : base()
    {
    }

    protected override void ExecuteOnServer()
    {
        // Intentionally left blank.
    }

    /// <summary>
    ///
    /// </summary>
    protected override void ExecuteOnClient()
    {
        if (!IsRequester())
        {
            switch (mode)
            {
                case Mode.insert:
                    if (CodeSpaceManager.ManagerInstance)
                    {

                        int index = ICRDT.GetIndexByPosition(ICRDT.StringToPosition(position, file), file);
                        if (index == -1)
                        {
                            return;
                        }
                        Debug.Log("NETZWERK LÖPT!" + file + " " + c + " " + index + " " + RequesterIPAddress);
                        CodeSpaceManager.ManagerInstance.InsertChar(RequesterIPAddress, file, c, index);

                    }
                    break;

                case Mode.delete:
                    if (CodeSpaceManager.ManagerInstance)
                    {
                        int index = ICRDT.GetIndexByPosition(ICRDT.StringToPosition(position, file), file);
                        if (index == -1)
                        {
                            return;
                        }

                        CodeSpaceManager.ManagerInstance.DeleteChar(RequesterIPAddress, file, index);
                    }
                    break;
            }
            mode = Mode.init;

        }
    }

    /// <summary>
    /// Inserts a char in the code window on every client
    /// </summary>
    /// <param name="c">The character that should be inserted</param>
    /// <param name="position">The position on which it should be inserted</param>
    /// <param name="file">The title of the file in which it should be inserted</param>
    public void InsertChar(char c, Identifier[] position, string file)
    {
        this.c = c;
        this.position = ICRDT.PositionToString(position, file);
        this.file = file;
        mode = Mode.insert;
        Execute(null);
    }

    /// <summary>
    /// Deletes a character on the code window on each client
    /// </summary>
    /// <param name="position">The position at which the character should be deleted</param>
    /// <param name="file">The title of the file in which it should be deleted</param>
    public void DeleteChar(Identifier[] position, string file)
    {
        this.position = ICRDT.PositionToString(position, file);
        this.file = file;
        mode = Mode.delete;
        Execute(null);
    }
}
