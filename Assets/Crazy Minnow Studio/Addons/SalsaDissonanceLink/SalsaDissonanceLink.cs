using System.Collections;
using UnityEngine;
using Dissonance;

namespace CrazyMinnow.SALSA.DissonanceLink
{
    [AddComponentMenu( "Crazy Minnow Studio/SALSA LipSync/Add-ons/SalsaDissonanceLink" )]
    public class SalsaDissonanceLink : MonoBehaviour
    {
        // RELEASE NOTES & TODO ITEMS:
        //    2.5.1:
        //      ! Add check for DissonancePlayer.PlayerID to prevent null-refs.
        //    2.5.0: Requires SALSA v2.5.0+
        //      + Takes advantage of SALSA's delegate maps to eliminate a local 'update' loop and relies on SALSA to
        //          poll SalsaDissonanceLink for the analysis value when it needs it.
        //      ~ Implemented a cached PlayerState flag to avoid constant !null checks on the PlayerState object.
        //    2.0.0-BETA : Initial release for SALSA LipSync v2.
        // ==========================================================================
        // PURPOSE: This script connects output streams from Dissonance Voice Chat
        //    to SALSA's ananlysisValue property. NOTE: for Dissonance support,
        //    please contact Placeholder Software. For the latest information
        //    about SalsaDissonanceLink, visit crazyminnowstudio.com.
        // ========================================================================================
        // LOCATION OF FILES:
        //		Assets\Crazy Minnow Studio\Addons\SalsaDissonanceLink
        //		Assets\Crazy Minnow Studio\Examples\Scenes      (if applicable)
        //		Assets\Crazy Minnow Studio\Examples\Scripts     (if applicable)
        // ========================================================================================
        // INSTRUCTIONS:
        //		(visit https://crazyminnowstudio.com/docs/salsa-lip-sync/ for the latest info)
        //		To extend/modify these files, copy their contents to a new set of files and
        //		use a different namespace to ensure there are no scoping conflicts if/when this
        //		add-on is updated.
        // ========================================================================================
        // SUPPORT: Contact assetsupport@crazyminnow.com. Provide:
        //		1) your purchase email and invoice number
        //		2) version numbers (OS, Unity, SALSA, etc.)
        //		3) full details surrounding the problem you are experiencing.
        //		4) relevant information for what you have tried/implemented.
        //		NOTE: Support is only provided for Crazy Minnow Studio products with valid
        //			proof of purchase.
        // ========================================================================================
        // KNOWN ISSUES: none.
        // ==========================================================================
        // DISCLAIMER: While every attempt has been made to ensure the safe content
        //    and operation of these files, they are provided as-is, without
        //    warranty or guarantee of any kind. By downloading and using these
        //    files you are accepting any and all risks associated and release
        //    Crazy Minnow Studio, LLC of any and all liability.
        // ==========================================================================


        public bool isDebug = false;                // display debug messages?
        public bool useLocalLipSync = false;        // process local lipsync if desired
        [Range(0f, 10f)] public float amplifyMultipleExperimental = 1.0f;

        private VoicePlayerState playerState;       // provides SALSA with audio data (via ARV)
        private bool isPlayerStateReady = false;    // more efficient cached flag state for guard-clause, eliminates checking object validity each frame.
        private DissonanceComms dissonanceComms;    // allows reference to spawned player/audio objects
        private IDissonancePlayer dissonancePlayer; // reference to the player for discovery
        private Salsa salsa;                    // link up and feed SALSA instance average data
        private IEnumerator coroAudioSourceLinkage; // coro pointer (best-practice for GC reduction)
        private const float PollTimer = .5f;        // how often the coro rechecks for playerState discovery

        // using OnEnable() since it's probably necessary to re-process if the player is disabled/enabled for any reason
        private void OnEnable()
        {
            // link up required components
            salsa = GetComponent<Salsa>();

            dissonancePlayer = GetComponent<IDissonancePlayer>();
            dissonanceComms = FindObjectOfType<DissonanceComms>();

            if ( !salsa )
                Debug.LogError( "[" + GetType().Name + "] SALSA was not found on the player object." );
            if ( dissonancePlayer == null )
                Debug.LogError( "[" + GetType().Name + "] an IDissonancePlayer component was not found on the player object." );
            if ( dissonanceComms == null )
                Debug.LogError( "[" + GetType().Name + "] the DissonanceComms component was not found in the scene." );

            salsa.getExternalAnalysis = SalsaDissonanceLinkExternalAnalysis;


            // start coroutine to wait for Dissonance to register the network player
            if ( coroAudioSourceLinkage != null )
            {
                StopCoroutine(coroAudioSourceLinkage);
            }
            coroAudioSourceLinkage = WaitSalsaDissonanceLink();
            StartCoroutine(coroAudioSourceLinkage);
        }

        /// <summary>
        /// SALSA will poll this computation according to its normal update delay cycle.
        /// </summary>
        /// <returns></returns>
        private float SalsaDissonanceLinkExternalAnalysis()
        {
            if (!isPlayerStateReady)
                return 0f;

            if ( dissonancePlayer.Type == NetworkPlayerType.Local && !useLocalLipSync )
                return 0f;     // Bail out: local player and lip-sync not desired

            return playerState.Amplitude * amplifyMultipleExperimental;
        }

        // linkage routine - wait for Dissonance to register the player and spawn audio components
        private IEnumerator WaitSalsaDissonanceLink()
        {
            // implement internal timer to avoid WaitForSeconds GC
            var timeCheck = Time.time;

            //Find the playerstate for this playerid
            while ( !isPlayerStateReady )
            {
                if ( Time.time - timeCheck > PollTimer )
                {
                    timeCheck = Time.time;

                    if ( isDebug )
                        Debug.Log("[" + GetType().Name + "] - Looking for PlayerState for player ID:  " + dissonancePlayer.PlayerId);

                    if (dissonancePlayer.PlayerId == null)
                        continue;

                    playerState = dissonanceComms.FindPlayer(dissonancePlayer.PlayerId);
                    if (playerState != null)
                        isPlayerStateReady = true;
                }

                yield return null;
            }

            if ( isDebug )
                Debug.Log("[" + GetType().Name + "] - SALSA and Dissonance are linked.");
        }
    }
}
