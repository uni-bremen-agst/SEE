using Assets.SEE.DataModel;
using OdinSerializer;
using System;
using UnityEngine;

namespace SEE.Game.Runtime
{
    public class JLGVisualizer : SerializedMonoBehaviour
    {
        /// <summary>
        /// A ParsedJLG object that contains a parsed JLG file. This object contains all information needed for the visualization of a debugging process.
        /// </summary>
        public ParsedJLG parsedJLG;

        /// <summary>
        /// Int value the represents the # of the current active statement.
        /// </summary>
        private int statementCount = 0;

        /// <summary>
        /// Time value in seconds. At this point in time(running time) the next or previous statement will be visualized, depending on the playing direction.
        /// </summary>
        private float nextUpdateTime = 3;

        /// <summary>
        /// Seconds per statement.
        /// </summary>
        private int updateIntervall = 1;

        /// <summary>
        /// true, when visualisation is running. false, when its paused. (Pause visualization by pressing 'p')
        /// </summary>
        private Boolean running = true;

        /// <summary>
        /// Describes the direction, in which the visualisation is running. True for forward, false for backwards.
        /// </summary>
        private Boolean playDirection = true;

        /// Start is called before the first frame update
        void Start()
        {
            if (parsedJLG == null) {
                throw new Exception("Parsed JLG is null!");
            }
        }

        /// Update is called once per frame
        void Update()
        {            
            if (Time.time >= nextUpdateTime)
            {
               if (running)
               {
                    if (playDirection)
                    {
                        NextStatement();
                    }
                    else
                    {
                        PreviousStatement();
                    }
               }
               nextUpdateTime += updateIntervall;               
            }            

            if (Input.GetKeyDown(KeyCode.P))
            {
                running = !running;
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                updateIntervall = 1;
                playDirection = !playDirection;
            }
        }

        private void NextStatement() {
            Debug.Log(parsedJLG.AllStatements[statementCount].Line);
            if (statementCount < parsedJLG.AllStatements.Count)
            {
                statementCount++;
            }
            else
            {
                Debug.Log("End of JLG reached.");
                running = false;
            }
        }
        private void PreviousStatement() {
            Debug.Log(parsedJLG.AllStatements[statementCount].Line);
            if (statementCount > 0)
            {
                statementCount--;
            }
            else
            {
                Debug.Log("Start of JLG reached. Press 'P' to start.");
                running = false;
                playDirection = true;
            }
        }
    }
}
