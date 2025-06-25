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
        byte[] jsonBytes = Encoding.UTF8.GetBytes(snapshotJson);

        // using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
        // using (GZipStream compressionStream = new GZipStream(fileStream, System.IO.Compression.CompressionLevel.Optimal))
        // {
        //     await compressionStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
        // }

        Compressor.Save(fileName,
         new MemoryStream(Encoding.UTF8.GetBytes(snapshotJson)));

        if (await BackendSyncUtil.LogInAsync())
        {
            string url = Network.ClientRestAPI + "server/snapshots?serverId=" + Network.ServerId;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerFile(fileName);

                await request.SendWebRequest();

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
                    catch (System.Exception)
                    {

                        Debug.LogError($"Failed to delete local snapshot after upload: {request.error}");
                    }
                }
            }
        }



    }

}
