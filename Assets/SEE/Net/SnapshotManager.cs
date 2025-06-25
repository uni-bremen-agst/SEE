using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using OpenAI.Files;
using SEE.Net;
using SEE.Net.Util;
using SEE.Utils;
using UnityEngine;
using UnityEngine.Networking;

public class SnapshotManager : MonoBehaviour
{
    /// <summary>
    /// Path to the directory where snapshots are stored before they are sent to the server.
    /// </summary>
    private static readonly string SnapshotPath = Path.Combine(Path.GetTempPath(), "see_snapshots");

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InvokeRepeating(nameof(OnCreateServerSnapshotAsync), 0f, 60f);
    }

    private async UniTaskVoid OnCreateServerSnapshotAsync()
    {
        IEnumerable<ICodeCityPersitance> cityPersitances = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ICodeCityPersitance>();

        IList<SeeCitySnapshot> snapshots = new List<SeeCitySnapshot>();

        foreach (ICodeCityPersitance city in cityPersitances)
        {
            SeeCitySnapshot snapshot = city.CreateSnapshot();
            if (snapshot == null)
            {
                Debug.LogWarning("City snapshot is null, skipping.");
                continue;
            }
            snapshot.CityType = city.GetCityType();
            snapshots.Add(snapshot);
        }

        if (snapshots.Count == 0)
        {
            Debug.LogWarning("No cities found, skip saving snapshot.");
            // Don't send snapshot to backend of no cities exist.
            return;
        }

        ServerSnapshot serverSnapshot = new ServerSnapshot
        {
            CitySnapshots = snapshots,
        };

        await SaveSnapshot(serverSnapshot);
    }

    public static async UniTask SaveSnapshot(ServerSnapshot snapshot)
    {
        //string snapshotJson = JsonUtility.ToJson(snapshot);

        string snapshotJson = JsonConvert.SerializeObject(snapshot,
        new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });

        string fileName = Path.Combine(SnapshotPath, $"server_snapshot-{Guid.NewGuid()}.json.xz");
        Directory.CreateDirectory(SnapshotPath);

        Compressor.Save(fileName,
         new MemoryStream(Encoding.UTF8.GetBytes(snapshotJson)));

        if (await BackendSyncUtil.LogInAsync())
        {
            string url = Network.ClientRestAPI + "server/snapshots?serverId=" + Network.ServerId;

            List<IMultipartFormSection> formSections = new List<IMultipartFormSection>();

            // Create a MultipartForm section with the snapshot file.
            var bytes = File.ReadAllBytes(fileName);
            MultipartFormFileSection fileFormSection = new MultipartFormFileSection("file", bytes, fileName, "application/octet-stream");
            formSections.Add(fileFormSection);

            byte[] boundary = UnityWebRequest.GenerateBoundary();
            using (UnityWebRequest request = UnityWebRequest.Post(url, formSections, boundary))
            {
                await request.SendWebRequest().ToUniTask();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to upload snapshot: {request.error}");
                }
                else
                {
                    Debug.Log("Snapshot uploaded successfully.");

                    try
                    {
                        File.Delete(fileName);
                    }
                    catch (Exception)
                    {
                        Debug.LogError($"Failed to delete local snapshot after upload: {request.error}");
                    }
                }
            }
        }



    }

}
