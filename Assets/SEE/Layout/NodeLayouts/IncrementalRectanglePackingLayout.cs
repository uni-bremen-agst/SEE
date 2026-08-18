using LibGit2Sharp;
using OpenAI.Chat;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Game.HolisticMetrics.Metrics;
using SEE.Layout.NodeLayouts.CirclePacking;
using SEE.Layout.NodeLayouts.RectanglePacking;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.ContentSizeFitter;

using MoreLinq;
using SEE.DataModel.DG;
using SEE.Game.CityRendering;
using SEE.Layout.NodeLayouts;
using SEE.Layout.NodeLayouts.RectanglePacking;
using SEE.Utils;
using System;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

using System;
using System.Collections.Generic;
using System.Globalization;
using ClosedXML.Excel; // NuGet-Paket 'ClosedXML' wird benötigt


using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Xml.Linq;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using Random = UnityEngine.Random;

namespace SEE.Layout.NodeLayouts
{
  /// <summary>
  /// Simple rectangle layout that places nodes in a line
  /// and sorts them descending by Z inside the rectangle.
  /// </summary>
  public class IncrementalRectanglePackingLayout : NodeLayout, IIncrementalNodeLayout
  {
    static IncrementalRectanglePackingLayout()
    {
      Name = "Incremental Rectangle Packing Layout";
    }

    public IncrementalRectanglePackingLayout oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is IncrementalRectanglePackingLayout layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(IncrementalRectanglePackingLayout)} was not an {nameof(IncrementalRectanglePackingLayout)}.");
        }
      }
    }

    //protected override LayoutAnchor Anchor => LayoutAnchor.TopLeft;

    public List<Rec> recs;
    public List<ILayoutNode> leafsNodes;
    public Dictionary<ILayoutNode, NodeTransform> layoutResult;
    LayoutGraphNode rootLayoutNode;
    Graph graph;
    Node rootNode;
    public static Vector2 initialWorstCaseSize;


    public static bool changedOrDeleted = false;
    //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>)>)> history;
    //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
    public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;
    //                       parentID list of (id, position, size) , coverec
    public static Dictionary<string, (List<(string, Vector2, Vector2)>, Vector2)> lastPositions;



    public static int Counter = 0;
    public static double totalTimeInMilliSecondsIncremental = 0;
    public static List<float> incLayoutDistanceChange;
    public static List<float> incNearestNeighborWithin;
    public static List<float> incRanking;
    public static List<float> incSERC;
    public static List<int> incNewNodesCount;
    public static List<int> incGrownNodesCount;
    public static List<int> incShrunkNodesCount;
    public static List<int> incDeletedNodesCount;
    public static List<int> incAllNodes;


    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      Counter += 1;
      layoutResult = new Dictionary<ILayoutNode, NodeTransform>();
      //sleafsNodes = layoutNodes.Where(pn => pn != null && pn.IsLeaf).ToList();

      Performance p = Performance.Begin("incremental Layout Evaluation");
      ThirdScenario(layoutNodes.ToList(), centerPosition, rectangle);
      p.End();
      totalTimeInMilliSecondsIncremental += p.GetTimeInMilliSeconds();



      if (oldLayout == null)
      {
        incLayoutDistanceChange = new List<float>();
        incNearestNeighborWithin = new List<float>();
        incRanking = new List<float>();
        incSERC = new List<float>();
        incNewNodesCount = new List<int>();
        incGrownNodesCount = new List<int>();
        incShrunkNodesCount = new List<int>();
        incDeletedNodesCount = new List<int>();
        incAllNodes = new List<int>();


        incNewNodesCount.Add(layoutNodes.Count());
        incGrownNodesCount.Add(0);
        incShrunkNodesCount.Add(0);
        incDeletedNodesCount.Add(0);
        int n = layoutNodes.Count();
        incAllNodes.Add(n);
      }
      else
      {
        incLayoutDistanceChange.Add(CalculateLayoutDistanceChange(layoutResult, oldLayout.layoutResult));
        incNearestNeighborWithin.Add(CalculateNearestNeighborWithin(layoutResult, oldLayout.layoutResult));
        incRanking.Add(CalculateRanking(layoutResult, oldLayout.layoutResult));
        incSERC.Add(CalculateSERC(layoutResult));
        incNewNodesCount.Add(CalculateNewNodesCount(oldLayout.layoutResult, layoutResult));
        incGrownNodesCount.Add(CalculateGrownAndShrunkNodesCount(oldLayout.layoutResult, layoutResult).Item1);
        incShrunkNodesCount.Add(CalculateGrownAndShrunkNodesCount(oldLayout.layoutResult, layoutResult).Item2);
        incDeletedNodesCount.Add(CalculateDeletedNodesCount(oldLayout.layoutResult, layoutResult));
        int n = layoutNodes.Count();
        incAllNodes.Add(n);


        string ldcString = "";
        string nnwString = "";
        string rankingString = "";
        string sercString = "";
        string newNodesString = "";
        string grownNodesString = "";
        string shrunkNodesString = "";
        string deletedNodesString = "";
        string allNodesString = "";

        ldcString += "Incremental Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange) + "\n" + "#####################\n";
        nnwString += "Incremental Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin) + "\n" + "#####################\n";
        rankingString += "Incremental Ranking: " + string.Join(", ", incRanking) + "\n" + "#####################\n";
        sercString += "Incremental SERC: " + string.Join(", ", incSERC) + "\n" + "#####################\n";
        newNodesString += "Incremental New Nodes: " + string.Join(", ", incNewNodesCount) + "\n" + "#####################\n";
        grownNodesString += "Incremental Grown Nodes: " + string.Join(", ", incGrownNodesCount) + "\n" + "#####################\n";
        shrunkNodesString += "Incremental Shrunk Nodes: " + string.Join(", ", incShrunkNodesCount) + "\n" + "#####################\n";
        deletedNodesString += "Incremental Deleted Nodes: " + string.Join(", ", incDeletedNodesCount) + "\n" + "#####################\n";
        allNodesString += "Incremental All Nodes: " + string.Join(", ", incAllNodes) + "\n" + "#####################\n";

        Debug.Log(ldcString + nnwString + rankingString + sercString + newNodesString + grownNodesString + shrunkNodesString + deletedNodesString + allNodesString);

        if (Counter == 102)
        {
          SchreibeListenInSpalten(new List<List<float>> { incLayoutDistanceChange, incNearestNeighborWithin, incRanking, incSERC });
          SchreibeListenInSpalten(new List<List<int>> { incNewNodesCount, incGrownNodesCount, incShrunkNodesCount, incDeletedNodesCount, incAllNodes }, 5);
          Debug.Log("Total time for incremental layout evaluation: " + Performance.GetElapsedTime(totalTimeInMilliSecondsIncremental));
        }

      }


      return layoutResult;

    }

    #region Scenarios
    public float CalculateLayoutDistanceChange(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
    {
      float totalDistance = 0f;
      int intersectionCount = 0;

      Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
      foreach (var kvp in layout2)
      {
        layout2ById[kvp.Key.ID] = kvp.Value;
      }

      foreach (var kvp in layout1)
      {
        string nodeId = kvp.Key.ID;
        if (layout2ById.TryGetValue(nodeId, out NodeTransform transform2))
        {
          NodeTransform transform1 = kvp.Value;

          // Delta Position (dx, dy)
          // Falls du in Unity die X/Z-Achse nutzt, ändere .y hier zu .z
          float dx = transform1.CenterPosition.x - transform2.CenterPosition.x;
          float dy = transform1.CenterPosition.z - transform2.CenterPosition.z;

          // Delta Dimensionen (dw, dh)
          // Angenommen, 'scale' ist dein Vector3 für die Größe
          float dw = transform1.Scale.x - transform2.Scale.x;
          float dh = transform1.Scale.z - transform2.Scale.z;

          // Formel: Wurzel aus (dx^2 + dy^2 + dw^2 + dh^2)
          float distance = Mathf.Sqrt((dx * dx) + (dy * dy) + (dw * dw) + (dh * dh));

          totalDistance += distance;
          intersectionCount++;
        }
      }

      if (intersectionCount == 0) return 0f;

      // Rückgabe als Durchschnitt (normalisiert durch Knotenanzahl)
      return totalDistance / intersectionCount;
    }
    public float CalculateNearestNeighborWithin(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
    {
      // Schritt 1: Dictionaries für extrem schnellen O(1) ID-Zugriff aufbauen
      Dictionary<string, NodeTransform> l1ById = new Dictionary<string, NodeTransform>();
      Dictionary<string, NodeTransform> l2ById = new Dictionary<string, NodeTransform>();

      foreach (var kvp in layout1) l1ById[kvp.Key.ID] = kvp.Value;
      foreach (var kvp in layout2) l2ById[kvp.Key.ID] = kvp.Value;

      // Schritt 2: Die Schnittmenge (Bestandsknoten) ermitteln
      List<string> commonNodeIds = new List<string>();
      foreach (string id in l1ById.Keys)
      {
        if (l2ById.ContainsKey(id))
        {
          commonNodeIds.Add(id);
        }
      }

      int n = commonNodeIds.Count;

      // Wenn es weniger als 2 Knoten gibt, gibt es keine Nachbarn
      if (n <= 1) return 0f;

      int brokenNeighborhoods = 0;

      // Schritt 3: Für jeden Bestandsknoten den nächsten Nachbarn in BEIDEN Layouts finden
      foreach (string currentId in commonNodeIds)
      {
        string nearestInL1 = GetNearestNeighborId(currentId, commonNodeIds, l1ById);
        string nearestInL2 = GetNearestNeighborId(currentId, commonNodeIds, l2ById);

        // Wenn sich die ID des nächsten Nachbarn geändert hat -> Bruch der Nachbarschaft!
        if (nearestInL1 != nearestInL2)
        {
          brokenNeighborhoods++;
        }
      }

      // Schritt 4: Normalisierung
      // Teilt die Anzahl der Brüche durch die Gesamtanzahl der Bestandsknoten
      return (float)brokenNeighborhoods / n;
    }
    private string GetNearestNeighborId(string targetId, List<string> validIds, Dictionary<string, NodeTransform> layout)
    {
      float minDistance = float.MaxValue;
      string nearestId = null;

      Vector2 targetPos = new Vector2(layout[targetId].CenterPosition.x, layout[targetId].CenterPosition.z);

      foreach (string otherId in validIds)
      {
        // Einen Knoten nicht mit sich selbst vergleichen
        if (otherId == targetId) continue;

        Vector2 otherPos = new Vector2(layout[otherId].CenterPosition.x, layout[otherId].CenterPosition.z);
        float dist = Vector2.Distance(targetPos, otherPos);

        // Neuer nächster Nachbar gefunden?
        if (dist < minDistance)
        {
          minDistance = dist;
          nearestId = otherId;
        }
      }

      return nearestId;
    }
    public float CalculateRanking(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
    {
      // Schritt 1: Schnittmenge (Bestandsknoten) herausfiltern für synchronen Index-Zugriff
      List<NodeTransform> commonNodes1 = new List<NodeTransform>();
      List<NodeTransform> commonNodes2 = new List<NodeTransform>();

      Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
      foreach (var kvp in layout2) layout2ById[kvp.Key.ID] = kvp.Value;

      foreach (var kvp in layout1)
      {
        if (layout2ById.TryGetValue(kvp.Key.ID, out NodeTransform transform2))
        {
          commonNodes1.Add(kvp.Value);
          commonNodes2.Add(transform2);
        }
      }

      int vCount = commonNodes1.Count;

      // Wenn weniger als 2 Knoten existieren, gibt es keine zueinander in Relation stehenden Knoten
      if (vCount <= 1) return 0f;

      // Schritt 2: Upper Bound (UB) berechnen nach der Formel: 1.5 * (|V| - 1)
      float UB = 1.5f * (vCount - 1);

      float totalRankingSum = 0f;

      // Schritt 3: Für jeden Knoten v seinen orthogonalen Rang berechnen
      for (int i = 0; i < vCount; i++)
      {
        Vector2 pos1_i = new Vector2(commonNodes1[i].CenterPosition.x, commonNodes1[i].CenterPosition.z);
        Vector2 pos2_i = new Vector2(commonNodes2[i].CenterPosition.x, commonNodes2[i].CenterPosition.z);

        int rg1 = 0;  // Anzahl der Knoten RECHTS von v in Layout 1
        int abv1 = 0; // Anzahl der Knoten OBERHALB von v in Layout 1

        int rg2 = 0;  // Anzahl der Knoten RECHTS von v in Layout 2
        int abv2 = 0; // Anzahl der Knoten OBERHALB von v in Layout 2

        // Iteriere über alle ANDEREN Knoten, um den Rang zu bestimmen
        for (int j = 0; j < vCount; j++)
        {
          if (i == j) continue; // v nicht mit sich selbst vergleichen

          Vector2 pos1_j = new Vector2(commonNodes1[j].CenterPosition.x, commonNodes1[j].CenterPosition.z);
          Vector2 pos2_j = new Vector2(commonNodes2[j].CenterPosition.x, commonNodes2[j].CenterPosition.z);

          // --- Auswertung Layout 1 ---
          if (pos1_j.x > pos1_i.x) rg1++;

          // Hinweis: Falls dein 2D-Layout in Unity auf dem "Boden" liegt (X/Z-Achse), 
          // musst du hier .y in .z ändern!
          if (pos1_j.y > pos1_i.y) abv1++;

          // --- Auswertung Layout 2 ---
          if (pos2_j.x > pos2_i.x) rg2++;
          if (pos2_j.y > pos2_i.y) abv2++;
        }

        // Schritt 4: Die absolute Differenz der Ränge berechnen
        float diffRg = Mathf.Abs(rg1 - rg2);
        float diffAbv = Mathf.Abs(abv1 - abv2);
        float currentDiff = diffRg + diffAbv;

        // Laut Formel wird die Differenz durch die Upper Bound (UB) gecappt (min-Funktion)
        float cappedDiff = Mathf.Min(currentDiff, UB);

        // Zur Gesamtsumme addieren
        totalRankingSum += cappedDiff;
      }

      // Schritt 5: Gemäß Steinbrückner wird die Summe durch UB normalisiert
      // (bzw. durch UB * vCount, um einen Wert für den "durchschnittlichen" Zerstörungsgrad zu erhalten)
      return totalRankingSum / UB;
    }

    public float CalculateSERC(Dictionary<ILayoutNode, NodeTransform> layout)
    {
      if (layout.Count == 0) return 0f;

      float totalNodeArea = 0f;

      // Variablen für die Extrempunkte des globalen Hüllrechtecks (Bounding Box)
      float minX = float.MaxValue;
      float minZ = float.MaxValue;
      float maxX = float.MinValue;
      float maxZ = float.MinValue;

      foreach (var kvp in layout)
      {
        NodeTransform t = kvp.Value;

        // Ausdehnung des aktuellen Rechtecks
        float width = t.Scale.x;
        float height = t.Scale.z;

        // 1. Fläche des Rechtecks zur Gesamtsumme addieren
        totalNodeArea += (width * height);

        // 2. Die vier Außenkanten dieses Rechtecks berechnen
        // (Annahme: t.CenterPosition ist der Mittelpunkt des Rechtecks)
        float leftEdge = t.CenterPosition.x - (width / 2f);
        float rightEdge = t.CenterPosition.x + (width / 2f);
        float bottomEdge = t.CenterPosition.z - (height / 2f);
        float topEdge = t.CenterPosition.z + (height / 2f);

        // 3. Globale Bounding Box bei Bedarf erweitern
        if (leftEdge < minX) minX = leftEdge;
        if (rightEdge > maxX) maxX = rightEdge;
        if (bottomEdge < minZ) minZ = bottomEdge;
        if (topEdge > maxZ) maxZ = topEdge;
      }

      // Fläche des ermittelten Hüllrechtecks berechnen
      float boundingBoxWidth = maxX - minX;
      float boundingBoxHeight = maxZ - minZ;
      float boundingBoxArea = boundingBoxWidth * boundingBoxHeight;

      // Division durch Null abfangen
      if (boundingBoxArea <= 0f) return 0f;

      // SERC-Formel: 100 * (A_N / A_R)
      return 100f * (totalNodeArea / boundingBoxArea);
    }

    public int CalculateNewNodesCount(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
    {
      int newNodesCount = 0;

      // Schritt 1: Speichere alle IDs der ALTEN Revision in ein HashSet.
      // Das ermöglicht einen O(1) Zugriff.
      HashSet<string> oldLayoutIds = new HashSet<string>();
      foreach (var kvp in layout1)
      {
        oldLayoutIds.Add(kvp.Key.ID);
      }

      // Schritt 2: Iteriere über alle Knoten der NEUEN Revision.
      foreach (var kvp in layout2)
      {
        string newNodeId = kvp.Key.ID;

        // Schritt 3: Prüfe, ob die ID im alten Layout NICHT vorhanden war.
        if (!oldLayoutIds.Contains(newNodeId))
        {
          // Wenn die ID im alten HashSet nicht enthalten ist, ist der Knoten neu.
          newNodesCount++;
        }
      }

      return newNodesCount;
    }


    public (int grownCount, int shrunkCount) CalculateGrownAndShrunkNodesCount(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
    {
      int grownCount = 0;
      int shrunkCount = 0;

      // Toleranzwert für Floating-Point-Vergleiche
      float epsilon = 0.0001f;

      // Schritt 1: Dictionary für den schnellen Zugriff auf die NEUE Revision
      Dictionary<string, NodeTransform> layout2ById = new Dictionary<string, NodeTransform>();
      foreach (var kvp in layout2)
      {
        layout2ById[kvp.Key.ID] = kvp.Value;
      }

      // Schritt 2: Iteriere über die ALTE Revision
      foreach (var kvp in layout1)
      {
        string nodeId = kvp.Key.ID;

        // Schritt 3: Nur Bestandsknoten prüfen
        if (layout2ById.TryGetValue(nodeId, out NodeTransform transform2))
        {
          NodeTransform transform1 = kvp.Value;

          // Schritt 4: Gesamtfläche / Volumen berechnen.
          // HINWEIS: Wenn dein Layout flach in Unity liegt (X- und Z-Achse), 
          // nimm scale.x * scale.z. Wenn es steht, nimm scale.x * scale.y!
          float area1 = transform1.Scale.x * transform1.Scale.z;
          float area2 = transform2.Scale.x * transform2.Scale.z;

          // Schritt 5: Vergleichen mit Epsilon
          if (area2 > area1 + epsilon)
          {
            // Die neue Fläche ist signifikant größer -> Gewachsen
            grownCount++;
          }
          else if (area2 < area1 - epsilon)
          {
            // Die neue Fläche ist signifikant kleiner -> Geschrumpft
            shrunkCount++;
          }
        }
      }

      // Rückgabe beider Werte als Tupel
      return (grownCount, shrunkCount);
    }

    public int CalculateDeletedNodesCount(Dictionary<ILayoutNode, NodeTransform> layout1, Dictionary<ILayoutNode, NodeTransform> layout2)
    {
      int deletedCount = 0;

      // Schritt 1: Speichere alle IDs der NEUEN Revision in ein HashSet.
      // Ein HashSet bietet extrem schnelle Suchzeiten (O(1)).
      HashSet<string> newLayoutIds = new HashSet<string>();
      foreach (var kvp in layout2)
      {
        newLayoutIds.Add(kvp.Key.ID);
      }

      // Schritt 2: Iteriere über alle Knoten der ALTEN Revision.
      foreach (var kvp in layout1)
      {
        string oldNodeId = kvp.Key.ID;

        // Schritt 3: Prüfe, ob die alte ID im neuen Layout fehlt.
        if (!newLayoutIds.Contains(oldNodeId))
        {
          // Wenn die ID nicht gefunden wurde, wurde der Knoten gelöscht.
          deletedCount++;
        }
      }

      return deletedCount;
    }

    /*
        public void SchreibeListenInSpalten(List<List<float>> hauptListe, int startSpalte = 1)
        {
          // 1. Pfad abrufen: Hier rufen wir deine Methode auf, um den Pfad zu bekommen
          string dateipfad = GetFilePath1();

          using (var workbook = new XLWorkbook())
          {
            var worksheet = workbook.Worksheets.Add("Daten");

            // Deutsche Kultur nutzen, damit ToString() automatisch ein Komma setzt 
            CultureInfo deutscheKultur = new CultureInfo("de-DE");

            // 1. Schleife: Geht durch die äußere Liste (bestimmt die Spalten)
            for (int spaltenIndex = 0; spaltenIndex < hauptListe.Count; spaltenIndex++)
            {
              // Die aktuelle Spalte in Excel (startSpalte + aktueller Index)
              int aktuelleExcelSpalte = startSpalte + spaltenIndex;

              // Die innere Liste für diese spezifische Spalte holen
              List<float> spaltenDaten = hauptListe[spaltenIndex];

              // 2. Schleife: Geht durch die innere Liste (bestimmt die Zeilen)
              for (int zeilenIndex = 0; zeilenIndex < spaltenDaten.Count; zeilenIndex++)
              {
                int aktuelleExcelZeile = zeilenIndex + 1; // Excel-Zeilen beginnen bei 1
                float aktuelleZahl = spaltenDaten[zeilenIndex];

                // Zahl in einen Text mit Komma umwandeln (z.B. "16,2938")
                string textMitKomma = aktuelleZahl.ToString(deutscheKultur);

                // Zelle auswählen und den Text eintragen
                var zelle = worksheet.Cell(aktuelleExcelZeile, aktuelleExcelSpalte);

                // SetValue trägt den formatierten String als Text ein
                zelle.SetValue(textMitKomma);
              }
            }

            // 2. Datei speichern: Hier wird nun der Pfad aus GetFilePath1() verwendet
            workbook.SaveAs(dateipfad);

            // Optional: Eine Konsolenausgabe hilft dir in Unity zu sehen, wo die Datei gelandet ist
            Debug.Log("Excel-Datei erfolgreich gespeichert unter: " + dateipfad);
          }
        }
        // Diese Funktion heißt exakt gleich, akzeptiert aber List<List<int>> statt float.
        public void SchreibeListenInSpalten(List<List<int>> hauptListe, int startSpalte = 1)
        {
          // 1. Pfad abrufen: Wir nutzen dieselbe Methode wie bei den floats
          string dateipfad = GetFilePath1();

          using (var workbook = new XLWorkbook())
          {
            var worksheet = workbook.Worksheets.Add("Daten");

            // Deutsche Kultur nutzen (auch wenn ints keine Kommas haben, 
            // ist es gut für eine einheitliche Formatierung großer Zahlen)
            CultureInfo deutscheKultur = new CultureInfo("de-DE");

            // 1. Schleife: Geht durch die äußere Liste (bestimmt die Spalten)
            for (int spaltenIndex = 0; spaltenIndex < hauptListe.Count; spaltenIndex++)
            {
              // Die aktuelle Spalte in Excel (startSpalte + aktueller Index)
              int aktuelleExcelSpalte = startSpalte + spaltenIndex;

              // WICHTIG: Hier holen wir nun eine Liste von ints (Ganzzahlen)
              List<int> spaltenDaten = hauptListe[spaltenIndex];

              // 2. Schleife: Geht durch die innere Liste (bestimmt die Zeilen)
              for (int zeilenIndex = 0; zeilenIndex < spaltenDaten.Count; zeilenIndex++)
              {
                int aktuelleExcelZeile = zeilenIndex + 1; // Excel-Zeilen beginnen bei 1

                // WICHTIG: Hier lesen wir einen int statt eines floats aus
                int aktuelleZahl = spaltenDaten[zeilenIndex];

                // Zahl in Text umwandeln 
                string textMitKomma = aktuelleZahl.ToString(deutscheKultur);

                // Zelle auswählen und den Text eintragen
                var zelle = worksheet.Cell(aktuelleExcelZeile, aktuelleExcelSpalte);

                // SetValue trägt den formatierten String als Text ein
                zelle.SetValue(textMitKomma);
              }
            }

            // 2. Datei speichern
            workbook.SaveAs(dateipfad);

            // Optional: Eine Konsolenausgabe zur Bestätigung in Unity
            Debug.Log("Excel-Datei (mit Integern) erfolgreich gespeichert unter: " + dateipfad);
          }
        }
     */

    // --- Version für Floats (Kommazahlen) ---
    public void SchreibeListenInSpalten(List<List<float>> hauptListe, int startSpalte = 1)
    {
      string dateipfad = GetFilePath1();

      // 1. Wir deklarieren das Workbook außerhalb der if-Abfrage, 
      // damit wir es später im using-Block verwenden können.
      XLWorkbook workbook;

      // 2. Prüfen, ob die Datei bereits existiert
      if (File.Exists(dateipfad))
      {
        // Datei existiert -> Wir laden die bestehende Datei in den Speicher
        workbook = new XLWorkbook(dateipfad);
      }
      else
      {
        // Datei existiert nicht -> Wir erstellen ein neues, leeres Workbook
        workbook = new XLWorkbook();
      }

      // Wir nutzen "using", damit die Datei nach dem Speichern sauber aus dem Arbeitsspeicher entfernt wird
      using (workbook)
      {
        // 3. Das richtige Arbeitsblatt (Worksheet) finden oder erstellen
        IXLWorksheet worksheet;
        if (workbook.Worksheets.Contains("Daten"))
        {
          // Das Blatt "Daten" gibt es schon, wir holen es uns
          worksheet = workbook.Worksheet("Daten");
        }
        else
        {
          // Das Blatt gibt es noch nicht, wir erstellen es
          worksheet = workbook.Worksheets.Add("Daten");
        }

        CultureInfo deutscheKultur = new CultureInfo("de-DE");

        // 4. Daten gezielt in die angegebenen Spalten schreiben (alte Daten in anderen Spalten bleiben erhalten)
        for (int spaltenIndex = 0; spaltenIndex < hauptListe.Count; spaltenIndex++)
        {
          int aktuelleExcelSpalte = startSpalte + spaltenIndex;
          List<float> spaltenDaten = hauptListe[spaltenIndex];

          for (int zeilenIndex = 0; zeilenIndex < spaltenDaten.Count; zeilenIndex++)
          {
            int aktuelleExcelZeile = zeilenIndex + 1;
            float aktuelleZahl = spaltenDaten[zeilenIndex];

            string textMitKomma = aktuelleZahl.ToString(deutscheKultur);

            // Zelle auswählen und den Text eintragen (überschreibt nur exakt diese eine Zelle)
            var zelle = worksheet.Cell(aktuelleExcelZeile, aktuelleExcelSpalte);
            zelle.SetValue(textMitKomma);
          }
        }

        // 5. Änderungen in die Datei zurückspeichern
        workbook.SaveAs(dateipfad);
        Debug.Log("Float-Daten erfolgreich zur Excel-Datei hinzugefügt unter: " + dateipfad);
      }
    }


    // --- Version für Ints (Ganze Zahlen) ---
    // Diese Funktion funktioniert exakt nach dem gleichen Prinzip, aber für ganze Zahlen.
    public void SchreibeListenInSpalten(List<List<int>> hauptListe, int startSpalte = 1)
    {
      string dateipfad = GetFilePath1();

      XLWorkbook workbook;

      if (File.Exists(dateipfad))
      {
        workbook = new XLWorkbook(dateipfad);
      }
      else
      {
        workbook = new XLWorkbook();
      }

      using (workbook)
      {
        IXLWorksheet worksheet;
        if (workbook.Worksheets.Contains("Daten"))
        {
          worksheet = workbook.Worksheet("Daten");
        }
        else
        {
          worksheet = workbook.Worksheets.Add("Daten");
        }

        CultureInfo deutscheKultur = new CultureInfo("de-DE");

        for (int spaltenIndex = 0; spaltenIndex < hauptListe.Count; spaltenIndex++)
        {
          int aktuelleExcelSpalte = startSpalte + spaltenIndex;
          List<int> spaltenDaten = hauptListe[spaltenIndex];

          for (int zeilenIndex = 0; zeilenIndex < spaltenDaten.Count; zeilenIndex++)
          {
            int aktuelleExcelZeile = zeilenIndex + 1;
            int aktuelleZahl = spaltenDaten[zeilenIndex];

            string textMitKomma = aktuelleZahl.ToString(deutscheKultur);

            var zelle = worksheet.Cell(aktuelleExcelZeile, aktuelleExcelSpalte);
            zelle.SetValue(textMitKomma);
          }
        }

        workbook.SaveAs(dateipfad);
        Debug.Log("Int-Daten erfolgreich zur Excel-Datei hinzugefügt unter: " + dateipfad);
      }
    }

    private string GetFilePath1()
    {
      return Path.Combine(Application.persistentDataPath, "IRP.xlsx");
    }
    public void makeTable()
    {
      return;
    }

    #endregion
    public void ThirdScenario(List<ILayoutNode> leafNodes, Vector3 centerPosition, Vector2 rectangle)
    {

      if (oldLayout == null)
      {
        //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize  coverec
        history = new List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)>();
        lastPositions = new Dictionary<string, (List<(string, Vector2, Vector2)>, Vector2)>();
      }

      string rootLayoutNodeID = leafNodes.First().Parent != null ? leafNodes.First().Parent.ID : null;

      IList<ILayoutNode> layoutNodeList = leafNodes.ToList();
      if (layoutNodeList.Count == 1)
      {

        ILayoutNode layoutNode = layoutNodeList.First();
        layoutResult[layoutNode] = new NodeTransform(0, 0, layoutNode.AbsoluteScale);
        return;
      }

      {
        int numberOfLeaves = 0;
        foreach (ILayoutNode node in layoutNodeList)
        {
          if (node.IsLeaf)
          {

            Vector3 scale = node.AbsoluteScale;
            //float padding = Padding(scale.x, scale.z);
            //scale.x += padding;
            //scale.z += padding;
            layoutResult[node] = new NodeTransform(0, 0, scale);
            numberOfLeaves++;
          }
        }
        if (numberOfLeaves == layoutNodeList.Count)
        {
          // There are only leaves.
          Pack(layoutResult, layoutNodeList.Cast<ILayoutNode>().ToList(), GroundLevel);
          //RemovePadding(layoutResult);
          return;
        }
      }


      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(leafNodes);
      if (roots.Count == 1)
      {
        //Debug.Log("only one root");
        ILayoutNode root = roots.FirstOrDefault();
        Vector2 area = PlaceNodes(layoutResult, root, GroundLevel);
        layoutResult[root] = new NodeTransform(0, 0, new Vector3(area.x, root.AbsoluteScale.y, area.y));
        //RemovePadding(layoutResult);
        MakeContained(layoutResult, root);
        return;
      }
      else
      {
        Debug.Log("multiple or zero roots");
        foreach (ILayoutNode leafNode in leafNodes)
        {

          layoutResult[leafNode] = new NodeTransform(
              0,
              0,
              leafNode.AbsoluteScale
          );
        }

        Pack(layoutResult, leafNodes.Cast<ILayoutNode>().ToList(), GroundLevel, rootLayoutNodeID);
        //RemovePadding(layoutResult);
      }
    }
    public Vector2 PlaceNodes(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
    {
      if (node.IsLeaf)
      {
        return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
      }
      else
      {
        ICollection<ILayoutNode> children = node.Children();

        foreach (ILayoutNode child in children)
        {
          if (!child.IsLeaf)
          {
            Vector2 childArea = PlaceNodes(layout, child, groundLevel);
            layout[child] = new NodeTransform(0, 0,
                                              new Vector3(childArea.x, child.AbsoluteScale.y, childArea.y));
            //Debug.Log("Placed node " + child.ID + " with area " + childArea + " in PlaceNodes");
            //Debug.Log("child absolute scale: " + child.AbsoluteScale + " if child.isLeaf " + child.IsLeaf + " : child.Rests() " + child.Children().Count + " : if child.isLeaf " + child.Children().ToList().First().IsLeaf + " : " + child.Children().ToList().First().AbsoluteScale);

          }
          //Debug.Log("Placed node " + node.ID + " with area " + node.AbsoluteScale + " in PlaceNodes");
          //else
          //{
          //  layout[child] = new NodeTransform(0, 0, child.AbsoluteScale);
          //}
          //Debug.Log("Placed node " + child.ID + " with area " + childArea);
        }
        if (children.Count > 0)
        {
          Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel, node.ID);
          //float padding = Padding(area.x, area.y);

          //Debug.Log("Packed node " + node.ID + " with area " + area + " in children.Count");
          //return new Vector2(area.x + padding, area.y + padding);

          return new Vector2(area.x, area.y);
        }
        else
        {
          return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
        }
      }
    }
    //*********************************************************************************************
    private Vector2 PlaceNodes1(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode node, float groundLevel)
    {
      if (node.IsLeaf)
      {
        return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
      }
      else
      {
        ICollection<ILayoutNode> children = node.Children();

        foreach (ILayoutNode child in children)
        {
          if (!child.IsLeaf)
          {
            Vector2 childArea = PlaceNodes1(layout, child, groundLevel);
            layout[child] = new NodeTransform(0, 0,
                                              new Vector3(childArea.x, child.AbsoluteScale.y, childArea.y));
          }
        }
        if (children.Count > 0)
        {
          Vector2 area = Pack(layout, children.Cast<ILayoutNode>().ToList(), groundLevel);
          float padding = Padding(area.x, area.y);
          return new Vector2(area.x + padding, area.y + padding);
        }
        else
        {

          return new Vector2(node.AbsoluteScale.x, node.AbsoluteScale.z);
        }
      }
    }
    //*********************************************************************************************

    private Vector2 Pack(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, float groundLevel, string parent = null)
    {
      /*
       let all initial sizes be in layout with padding added
      if (oldLayout == null)
      {
        FullJournal(layout, nodes);
      }
      JournalNodes(layout, nodes);
       */
      /*
      else { 
        // Adjust the root rectangle to the new worst case size.
        tree.Root.Rectangle.Size = 1.1f * worstCaseSize;
        tree.FreeLeavesAdjust(worstCaseSize);
        tree.Root.Rectangle.Position = Vector2.zero;
      }
       */

      //Debug.Log("Sorted nodes by area size: " + string.Join(", ", nodes.Select(n => n.ID + ": x" + layout[n].Scale.x + " z " + layout[n].Scale.z + "....")));
      //Debug.Log("Sorted nodes by area size: " + string.Join(", ", nodes.Select(n => n.ID + ": x" + n.AbsoluteScale.x + " z " + n.AbsoluteScale.z + "....")));

      /*
       
      Debug.Log("worst case size: " + worstCaseSize);
      Debug.Log("nodes: " + nodes.First().AbsoluteScale);
      Debug.Log("nodes: " + nodes.Last().AbsoluteScale);
      Debug.Log("layout: " + layout[nodes.First()]);
       */


      /*
      if (oldLayout == null)
      {
        tree = new(Vector2.zero, 1.1f * initialWorstCaseSize);
        covrec = Vector2.zero;
      }
      else
      {
        tree = new(Vector2.zero, Vector2.zero);
        covrec = Vector2.zero;
      }
       */

      /*
      Add to history
      perform history 
      set the nodes to last scene

      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);
      PTree tree = new(Vector2.zero, Vector2.zero);
      Vector2 covrec = Vector2.zero;
      string parentID = parent == null ? "dummy" : parent;
      AddToHistory(layout, nodes, worstCaseSize, parentID);
      PerformHistory(ref layout, ref nodes, ref tree, parentID);
      tree.Tighten(tree.Root);
      ResetCoverec(ref tree);
      PlaceNodesInLayout(ref layout, ref nodes, parentID, ref tree);
      return tree.coverec;



      Vector2 coverec =  UsualProcess(layout, nodes);
      return coverec;
       */

      string parentID = parent == null ? "dummy" : parent;
      PTree tree = new(Vector2.zero, Vector2.zero);

      var coverec = PerformHistoryNew(layout, nodes, parentID, ref tree);
      //tree.Tighten(tree.Root);
      ResetCoverec(ref tree);
      PlaceNodesInLayout(ref layout, ref nodes, parent, ref tree);
      return coverec;

    }

    private static void MakeContained(Dictionary<ILayoutNode, NodeTransform> layout, ILayoutNode parent)
    {
      /*
      // The x co-ordinate of the left lower corner of the parent.
      // The z co-ordinate of the left lower corner of the parent.
       */
      NodeTransform parentTransform = layout[parent];
      Vector3 parentExtent = parentTransform.Scale / 2.0f;
      float xCorner = parentTransform.X - parentExtent.x;
      float zCorner = parentTransform.Z - parentExtent.z;

      foreach (ILayoutNode child in parent.Children())
      {
        //Debug.Log("Making contained: " + child.ID);
        layout[child].MoveBy(xCorner, zCorner);
        MakeContained(layout, child);
      }
    }
    public void PlaceNodesInLayout(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, string parent, ref PTree tree)
    {
      /*
       */
      foreach (ILayoutNode el in nodes)
      {
        //Debug.Log(el.Print());
        if (!layout.ContainsKey(el))
        {
          continue;
        }
        PNode fitNode = tree.FindNodeById2(el.ID);

        if (fitNode == null)
        {
          Debug.Log("fitnode is null" + el.ID);
          continue;

        }
        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale, fitNode);

        /*
        {
          Vector2 corner = fitNode.Rectangle.Position + new Vector2(scale.x, scale.z);
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));
          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
        Vector2 coverec = covrec;
        AddCoverecToHistory(coverec, parent);
         */
      }

      //PrintHistory();
      //tree.Print1();
      //Debug.Log("1********************************************************************************************************");
    }

    public void PlaceNodesInPTree(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, Vector2 worstCaseSize, string parent)
    {
      //SortNodesByAreaSize(nodes, layout);
      Vector2 oldWorstCaseSize = tree.Root.Rectangle.Size;
      Vector2 newWorstCaseSize = 1.1f * worstCaseSize;
      /*
      //tree.Root.Rectangle.Size = new Vector2(Mathf.Max(newWorstCaseSize.x,newWorstCaseSize.y), Mathf.Max(newWorstCaseSize.x, newWorstCaseSize.y));
       */
      tree.Root.Rectangle.Size = newWorstCaseSize;
      //tree.FreeLeavesAdjust1(oldWorstCaseSize);
      tree.Root.Rectangle.Position = Vector2.zero;

      Vector2 coverec = tree.coverec; // fix me each node should have its own coverec and tree which is not defined here u cant simply have one coverec for all nodes in the level because they can be in different subtrees of the root and thus have different coverecs and also when you place a node in the tree it can change the coverec of its subtree but not necessarily the coverec of the whole tree so you need to keep track of coverecs on a more granular level and not just one coverec for the whole tree


      foreach ((string newID, Vector2 size) in newNodeIDsSizes)
      {
        Vector2 requiredSize = size;


        Dictionary<PNode, float> preservers = new();
        Dictionary<PNode, float> expanders = new();
        tree.FreeLeaves = tree.FindEmpty(tree.Root, tree.Root.Rests);


        IList<PNode> sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize, oldWorstCaseSize);


        if (sufficientLargeLeaves.Count == 0)
        {
          Debug.Log("--------------------------------------------------------------------------------------------------------------");
          tree.Print1();
          Debug.Log("--------------------------------------------------------------------------------------------------------------");
          if (tree.FreeLeaves.Count == 0) Debug.Log("no free leaves");
          else Debug.Log("free leaves: " + tree.FreeLeaves.Count);
          foreach (PNode freeLeaf in tree.FreeLeaves)
          {
            if (freeLeaf != null) Debug.Log(freeLeaf.ToString1());
            else Debug.Log("free leaf is null");
          }
          Debug.Log("--------------------------------------------------------------------------------------------------------------");

          throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": :" + requiredSize + ": " + tree.coverec + " : " + tree.Root.Rectangle.Size + " : " + worstCaseSize);
        }
        foreach (PNode pnode in sufficientLargeLeaves)
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));

          //Debug.Log(expandedCoveRec + " " + coverec);

          if (PTree.FitsInto(expandedCoveRec, coverec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
            //Debug.Log("added to preservers");
          }
          else
          {
            /*
            float truncatedX = Mathf.Floor(expandedCoveRec.x * 10f) / 10f;
            float truncatedY = Mathf.Floor(expandedCoveRec.y * 10f) / 10f;
            float ratio = truncatedX / truncatedY;

            expanders[pnode] = Mathf.Abs(ratio - 1);
             */
            float ratio = expandedCoveRec.x / expandedCoveRec.y;
            expanders[pnode] = Mathf.Abs(ratio - 1);

            //Debug.Log("added to extenders");
          }

        }
        PNode targetNode = null;
        if (preservers.Count > 0)
        {
          float lowestWaste = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in preservers)
          {
            if (entry.Value < lowestWaste)
            {

              targetNode = entry.Key;
              lowestWaste = entry.Value;
            }
          }
        }
        else
        {

          /*
          float bestRatio = Mathf.Infinity;
          //float smallestArea = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in expanders)
          {
            var area = entry.Key.Rectangle.Size.x * entry.Key.Rectangle.Size.y;
            //if (entry.Value < bestRatio && area < smallestArea)
            if (entry.Value < bestRatio)
            {
              //smallestArea = area;
              targetNode = entry.Key;
              bestRatio = entry.Value;
            }
          }
           */
          // Find the minimum value
          Single minValue = expanders.Values.Min();

          // Filter nodes with that minimum value
          IEnumerable<KeyValuePair<PNode, float>> candidates = expanders
              .Where(kv => kv.Value == minValue);

          // Find the one with the smallest rectangle area
          KeyValuePair<PNode, float>? best = null;

          foreach (KeyValuePair<PNode, float> kv in candidates)
          {
            Single area = kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y;

            if (best == null)
            {
              best = kv;
            }
            else
            {
              Single bestArea = best.Value.Key.Rectangle.Size.x * best.Value.Key.Rectangle.Size.y;

              if (area < bestArea)
              {
                best = kv;
              }
            }
          }

          // Final result
          targetNode = best?.Key;
          /*
           */
          /*
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
           */
        }
        if (targetNode == null)
        {
          Debug.LogError("targetNode is null!");
          continue;
        }
        //PrintPreserverExpanders(preservers, expanders);

        PNode fitNode = new PNode(targetNode.Rectangle.Position, requiredSize, newID);
        tree.Root.Rests.Add(fitNode);
        fitNode.Parent = tree.Root;

        {
          //ResetCoverec(ref tree);

          /*
          coverec = new Vector2( Mathf.Max(
              Mathf.Max(tree.coverec.x, fitNode.Rectangle.Position.x + fitNode.Rectangle.Size.x),
              Mathf.Max(tree.coverec.y, fitNode.Rectangle.Position.y + fitNode.Rectangle.Size.y)
          ), Mathf.Max(
              Mathf.Max(tree.coverec.x, fitNode.Rectangle.Position.x + fitNode.Rectangle.Size.x),
              Mathf.Max(tree.coverec.y, fitNode.Rectangle.Position.y + fitNode.Rectangle.Size.y)
          )) ;
          tree.coverec = coverec;
           */



          Vector2 corner = fitNode.Rectangle.Position + size;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));
          if (!PTree.FitsInto(expandedCoveRec, coverec))
          {
            //coverec = new Vector2(Mathf.Max(expandedCoveRec.x, expandedCoveRec.y), Mathf.Max(expandedCoveRec.x, expandedCoveRec.y));
            coverec = expandedCoveRec;
            tree.coverec = coverec;

            //Debug.Log("coverec changed for a new node " + coverec);
          }
          /*
          Debug.Log("...........................");
          tree.Print();
          Debug.Log("...........................");
           */
        }

        /*
         */

      }
    }

    public void ResizeNodesInPTree1(List<(string, Vector2)> sameIDsNewSizes, ref PTree tree)
    {
      if (sameIDsNewSizes.Count == 0)
        Debug.Log("sameIDsNewSizes is empty.");

      foreach ((string sameID, Vector2 size) in sameIDsNewSizes)
      {
        Vector2 requiredSize = size;
        PNode targetPNode = tree.FindNodeById2(sameID);

        if (targetPNode != null)
        {
          if (targetPNode.Rectangle.Size == requiredSize) continue;
          else
          {
            tree.GrowLeaf2(targetPNode, new Vector3(requiredSize.x, 1, requiredSize.y));

            Vector2 corner = targetPNode.Rectangle.Position + size;
            Vector2 expandedCoveRec = new(Mathf.Max(tree.coverec.x, corner.x), Mathf.Max(tree.coverec.y, corner.y));
            if (!PTree.FitsInto(expandedCoveRec, tree.coverec))
            {
              //tree.coverec = new Vector2(Mathf.Max(expandedCoveRec.x, expandedCoveRec.y), Mathf.Max(expandedCoveRec.x, expandedCoveRec.y));
              tree.coverec = expandedCoveRec;
              //Debug.Log("coverec changed for a new node -------------------- after resize" + tree.coverec);
            }
            //tree.Print1();
            //Debug.Log("--------------------------------------Resized node " + sameID + " to new size " + requiredSize);

          }
        }
        else
        {
          //Debug.LogError("targetPNode is null for sameID " + sameID + " in ResizeNodesInPTree1");
          continue;
        }
      }
    }

    public void PerformHistory(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes, ref PTree tree, string parent)
    {

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        (string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>) getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        // Iterate through all events in the history for this parent
        foreach ((List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2) historyEvent in getLine.Item2)
        {
          List<(string, Vector2)> sameIDsNewSizes = historyEvent.Item1;
          List<(string, Vector2)> newNodeIDsSizes = historyEvent.Item2;
          List<(string, Vector2)> deletedNodeIDsSizes = historyEvent.Item3;
          Vector2 worstCaseSize = historyEvent.Item4;
          if (sameIDsNewSizes.Count == 0 && deletedNodeIDsSizes.Count == 0)
          {
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
          }
          else
          {
            // First, handle deleted nodes
            foreach ((string deletedID, Vector2 size) in deletedNodeIDsSizes)
            {
              tree.DeleteMergeRemainLeaves2(id: deletedID);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
              changedOrDeleted = true;
            }
            // Second, handle resized nodes that are the same
            // set ptree to same nodes with new size
            if (sameIDsNewSizes.Count > 0)
            {
              ResizeNodesInPTree1(sameIDsNewSizes, ref tree);
              //tree.Tighten(tree.Root);
              //ResetCoverec(ref tree);
            }
            if (changedOrDeleted)
            {
              changedOrDeleted = false;
              //tree.Tighten(tree.Root);
            }

            // Next, handle new nodes
            PlaceNodesInPTree(ref layout, ref nodes, newNodeIDsSizes, ref tree, worstCaseSize, parent);
            // Finally, update sizes of same nodes
          }
        }
      }
    }

    /*
     */
    public Vector2 PerformHistoryNew(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, string parent, ref PTree tree)
    {
      //public static List<(string, List<(string, Vector2, Vector2)>)> lastPositions;
      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);
      tree.Root.Rectangle.Size = worstCaseSize * 1.1f;
      tree.Root.Rectangle.Position = Vector2.zero;

      List<(string, Vector2)> newNodeIDsSizes = new List<(string, Vector2)>();
      List<(string, Vector2)> sameIDsNewSizes = new List<(string, Vector2)>();
      List<PNode> rests = new List<PNode>();

      var bufferLastPos = lastPositions.FirstOrDefault(p => p.Key == parent).Value;


      if (bufferLastPos != default)
      {
        tree.coverec = bufferLastPos.Item2;

        foreach (ILayoutNode n in nodes)
        {
          (string, Vector2, Vector2) tupple = bufferLastPos.Item1.FirstOrDefault(l => l.Item1 == n.ID);
          if (tupple != default)
          {
            PNode pn = new PNode(tupple.Item2, tupple.Item3, tupple.Item1);
            //Debug.Log("ID " + pn.Id + "  position " + pn.Rectangle.Position + " size " + pn.Rectangle.Size);
            pn.Parent = tree.Root;

            tree.Root.Rests.Add(pn);
            pn.Occupied = true;
            rests.Add(pn);
          }
        }


        List<ILayoutNode> placedRectangles = nodes.Where(n => rests.Any(r => r.Id == n.ID)).ToList();


        sameIDsNewSizes = placedRectangles.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();

        ResizeNodesInPTree1(sameIDsNewSizes, ref tree);
        tree.Tighten(tree.Root);
        /*
        foreach (PNode pn in tree.Root.Rests)
        {
          ILayoutNode n = nodes.FirstOrDefault(node => node.ID == pn.Id);
          if (layout[n].Scale.x > pn.Width)
          {
            float deltaX = layout[n].Scale.x - pn.Width;
            pn.Width = layout[n].Scale.x;
            List<PNode> siblingsToMove = tree.Root.Rests.Except(new List<PNode>() { pn }).Where(r => r.Rectangle.Position.x >= (pn.Rectangle.Position.x + pn.Rectangle.Size.x - deltaX)).ToList();

            tree.ShiftSubtree1(deltaX, 0f, siblingsToMove);
            tree.Root.Rectangle.Size.x += deltaX;

            
          }
          else if (layout[n].Scale.z > pn.Height)
          {
            float deltaY = layout[n].Scale.z - pn.Height;
            pn.Height = layout[n].Scale.z;
            List<PNode> siblingsToMove = tree.Root.Rests.Except(new List<PNode>() { pn }).Where(r => r.Rectangle.Position.y >= (pn.Rectangle.Position.y + pn.Rectangle.Size.y - deltaY)).ToList();

            tree.ShiftSubtree1(0f, deltaY, siblingsToMove);
            tree.Root.Rectangle.Size.y += deltaY;

            

          }
          else if (layout[n].Scale.x < pn.Width)
          {
            pn.Width = layout[n].Scale.x;


          }
          else if (layout[n].Scale.z < pn.Height)
          {
            pn.Height = layout[n].Scale.z;

            
          }
        }
         */

        List<ILayoutNode> notPlacedRectangles = nodes.Where(n => !rests.Any(r => r.Id == n.ID)).ToList();

        newNodeIDsSizes = notPlacedRectangles.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();

        if (newNodeIDsSizes.Count > 0)
          PlaceNodesInPTreeNew(newNodeIDsSizes, ref tree, parent);

        ResolveAndExpand(tree.Root, tree.Root.Rests);

        List<(string, Vector2, Vector2)> allPlacedRectangles = tree.Root.Rests.Select(n => (n.Id, new Vector2(n.XX, n.YY), new Vector2(n.Width, n.Height))).ToList();
        lastPositions[parent] = (allPlacedRectangles, tree.coverec);

        //tree.Tighten(tree.Root);
        //ResetCoverec(ref tree);
        //PlaceNodesInLayout(ref layout, ref nodes, parent, ref tree);
        return tree.coverec;
      }
      else
      {
        newNodeIDsSizes = nodes.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();
        PlaceNodesInPTreeNew(newNodeIDsSizes, ref tree, parent);
        tree.Tighten(tree.Root);
        ResolveAndExpand(tree.Root, tree.Root.Rests);

        lastPositions[parent] = (tree.Root.Rests.Select(n => (n.Id, n.Position, n.Size)).ToList(), tree.coverec);

        //Debug.Log("6");
        //tree.Print1();
        return tree.coverec;
      }
    }
    public void PlaceNodesInPTreeNew(List<(string, Vector2)> newNodeIDsSizes, ref PTree tree, string parent)
    {

      Vector2 coverec = tree.coverec; // fix me each node should have its own coverec and tree which is not defined here u cant simply have one coverec for all nodes in the level because they can be in different subtrees of the root and thus have different coverecs and also when you place a node in the tree it can change the coverec of its subtree but not necessarily the coverec of the whole tree so you need to keep track of coverecs on a more granular level and not just one coverec for the whole tree

      foreach ((string newID, Vector2 size) in newNodeIDsSizes)
      {
        Vector2 requiredSize = size;


        Dictionary<PNode, float> preservers = new();
        Dictionary<PNode, float> expanders = new();
        tree.FreeLeaves = tree.FindEmpty(tree.Root, tree.Root.Rests);


        IList<PNode> sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize, Vector2.zero);


        if (sufficientLargeLeaves.Count == 0)
        {
          Debug.Log("--------------------------------------------------------------------------------------------------------------");
          tree.Print1();
          Debug.Log("--------------------------------------------------------------------------------------------------------------");
          if (tree.FreeLeaves.Count == 0) Debug.Log("no free leaves");
          else Debug.Log("free leaves: " + tree.FreeLeaves.Count);
          foreach (PNode freeLeaf in tree.FreeLeaves)
          {
            if (freeLeaf != null) Debug.Log(freeLeaf.ToString1());
            else Debug.Log("free leaf is null");
          }
          Debug.Log("--------------------------------------------------------------------------------------------------------------");

          throw new Exception("No sufficiently large free leaf found for size " + " :" + newID + ": :" + requiredSize + ": " + tree.coverec + " : " + tree.Root.Rectangle.Size + " : " + tree.Root.Rectangle.Size);
        }
        foreach (PNode pnode in sufficientLargeLeaves)
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));

          //Debug.Log(expandedCoveRec + " " + coverec);

          if (PTree.FitsInto(expandedCoveRec, coverec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
            //Debug.Log("added to preservers");
          }
          else
          {
            /*
            float truncatedX = Mathf.Floor(expandedCoveRec.x * 10f) / 10f;
            float truncatedY = Mathf.Floor(expandedCoveRec.y * 10f) / 10f;
            float ratio = truncatedX / truncatedY;

            expanders[pnode] = Mathf.Abs(ratio - 1);
             */
            float ratio = expandedCoveRec.x / expandedCoveRec.y;
            expanders[pnode] = Mathf.Abs(ratio - 1);

            //Debug.Log("added to extenders");
          }

        }
        PNode targetNode = null;
        if (preservers.Count > 0)
        {
          float lowestWaste = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in preservers)
          {
            if (entry.Value < lowestWaste)
            {

              targetNode = entry.Key;
              lowestWaste = entry.Value;
            }
          }
        }
        else
        {

          /*
          float bestRatio = Mathf.Infinity;
          //float smallestArea = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in expanders)
          {
            var area = entry.Key.Rectangle.Size.x * entry.Key.Rectangle.Size.y;
            //if (entry.Value < bestRatio && area < smallestArea)
            if (entry.Value < bestRatio)
            {
              //smallestArea = area;
              targetNode = entry.Key;
              bestRatio = entry.Value;
            }
          }
           */
          // Find the minimum value
          Single minValue = expanders.Values.Min();

          // Filter nodes with that minimum value
          IEnumerable<KeyValuePair<PNode, float>> candidates = expanders
              .Where(kv => kv.Value == minValue);

          // Find the one with the smallest rectangle area
          KeyValuePair<PNode, float>? best = null;

          foreach (KeyValuePair<PNode, float> kv in candidates)
          {
            Single area = kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y;

            if (best == null)
            {
              best = kv;
            }
            else
            {
              Single bestArea = best.Value.Key.Rectangle.Size.x * best.Value.Key.Rectangle.Size.y;

              if (area < bestArea)
              {
                best = kv;
              }
            }
          }

          // Final result
          targetNode = best?.Key;
          /*
           */
          /*
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
           */
        }
        if (targetNode == null)
        {
          Debug.LogError("targetNode is null!");
          continue;
        }
        //PrintPreserverExpanders(preservers, expanders);

        PNode fitNode = new PNode(targetNode.Rectangle.Position, requiredSize, newID);
        tree.Root.Rests.Add(fitNode);
        fitNode.Parent = tree.Root;
        fitNode.Occupied = true;

        {
          //ResetCoverec(ref tree);

          /*
          coverec = new Vector2( Mathf.Max(
              Mathf.Max(tree.coverec.x, fitNode.Rectangle.Position.x + fitNode.Rectangle.Size.x),
              Mathf.Max(tree.coverec.y, fitNode.Rectangle.Position.y + fitNode.Rectangle.Size.y)
          ), Mathf.Max(
              Mathf.Max(tree.coverec.x, fitNode.Rectangle.Position.x + fitNode.Rectangle.Size.x),
              Mathf.Max(tree.coverec.y, fitNode.Rectangle.Position.y + fitNode.Rectangle.Size.y)
          )) ;
          tree.coverec = coverec;
           */



          Vector2 corner = fitNode.Rectangle.Position + size;
          Vector2 expandedCoveRec = new(Mathf.Max(coverec.x, corner.x), Mathf.Max(coverec.y, corner.y));
          if (!PTree.FitsInto(expandedCoveRec, coverec))
          {
            //coverec = new Vector2(Mathf.Max(expandedCoveRec.x, expandedCoveRec.y), Mathf.Max(expandedCoveRec.x, expandedCoveRec.y));
            coverec = expandedCoveRec;
            tree.coverec = coverec;

            //Debug.Log("coverec changed for a new node " + coverec);
          }
          /*
          Debug.Log("...........................");
          tree.Print();
          Debug.Log("...........................");
           */
        }

        /*
         */

      }
    }


    public void ResolveAndExpand(PNode parent, List<PNode> nodes, int maxExpansions = 10, int iterationsPerPass = 50)
    {
      float expansionFactor = 1.15f; // Grow the parent by 15% when out of space

      for (int attempt = 0; attempt < maxExpansions; attempt++)
      {
        // 1. Run the separation algorithm within current bounds
        for (int iter = 0; iter < iterationsPerPass; iter++)
        {
          bool movedAny = false;

          // Push overlapping nodes apart
          for (int i = 0; i < nodes.Count; i++)
          {
            for (int j = i + 1; j < nodes.Count; j++)
            {
              PNode a = nodes[i];
              PNode b = nodes[j];

              float aCenterX = a.Position.x + (a.Width / 2f);
              float aCenterY = a.Position.y + (a.Height / 2f);
              float bCenterX = b.Position.x + (b.Width / 2f);
              float bCenterY = b.Position.y + (b.Height / 2f);

              float distX = bCenterX - aCenterX;
              float distY = bCenterY - aCenterY;

              if (distX == 0f && distY == 0f) distX = 0.01f;

              float minX = (a.Width / 2f) + (b.Width / 2f);
              float minY = (a.Height / 2f) + (b.Height / 2f);

              // If overlapping
              if (Mathf.Abs(distX) < minX && Mathf.Abs(distY) < minY)
              {
                float overlapX = distX > 0 ? minX - distX : -minX - distX;
                float overlapY = distY > 0 ? minY - distY : -minY - distY;

                Vector2 posA = a.Position;
                Vector2 posB = b.Position;

                if (Mathf.Abs(overlapX) < Mathf.Abs(overlapY))
                {
                  posA.x -= overlapX / 2f;
                  posB.x += overlapX / 2f;
                }
                else
                {
                  posA.y -= overlapY / 2f;
                  posB.y += overlapY / 2f;
                }

                a.Rectangle.Position = posA;
                b.Rectangle.Position = posB;
                movedAny = true;
              }
            }
          }

          // Clamp to the parent's current bounds
          foreach (var node in nodes)
          {
            Vector2 pos = node.Position;

            if (pos.x < parent.Position.x) pos.x = parent.Position.x;
            if (pos.y < parent.Position.y) pos.y = parent.Position.y;

            if (pos.x + node.Width > parent.Position.x + parent.Width)
              pos.x = parent.Position.x + parent.Width - node.Width;

            if (pos.y + node.Height > parent.Position.y + parent.Height)
              pos.y = parent.Position.y + parent.Height - node.Height;

            node.Rectangle.Position = pos;
          }

          // If nothing had to move, the layout is completely stable!
          if (!movedAny) break;
        }

        // 2. Verify if we successfully separated everything
        if (!HasOverlaps(nodes))
        {
          // Success! Shrink-wrap the parent to tightly fit the final layout to save space.
          TrimParentToFit(parent, nodes);
          return;
        }

        // 3. If overlaps STILL exist, they are jammed. Expand the parent symmetrically!
        ExpandParent(parent, expansionFactor);
      }

      // Final fallback: If we hit max expansions, just wrap whatever state is left.
      TrimParentToFit(parent, nodes);
    }

    private bool HasOverlaps(List<PNode> nodes)
    {
      for (int i = 0; i < nodes.Count; i++)
      {
        for (int j = i + 1; j < nodes.Count; j++)
        {
          PNode a = nodes[i];
          PNode b = nodes[j];

          float distX = (b.Position.x + b.Width / 2f) - (a.Position.x + a.Width / 2f);
          float distY = (b.Position.y + b.Height / 2f) - (a.Position.y + a.Height / 2f);

          float minX = (a.Width / 2f) + (b.Width / 2f);
          float minY = (a.Height / 2f) + (b.Height / 2f);

          if (Mathf.Abs(distX) < minX && Mathf.Abs(distY) < minY)
            return true;
        }
      }
      return false;
    }

    private void ExpandParent(PNode parent, float factor)
    {
      float newWidth = parent.Width * factor;
      float newHeight = parent.Height * factor;

      // Calculate offset to ensure it expands from the center, not just the top-right
      float offsetX = (newWidth - parent.Width) / 2f;
      float offsetY = (newHeight - parent.Height) / 2f;

      parent.Rectangle.Position = new Vector2(parent.Position.x - offsetX, parent.Position.y - offsetY);
      parent.Rectangle.Size = new Vector2(newWidth, newHeight);
    }

    private void TrimParentToFit(PNode parent, List<PNode> nodes)
    {
      if (nodes.Count == 0) return;

      float minX = float.MaxValue, minY = float.MaxValue;
      float maxX = float.MinValue, maxY = float.MinValue;

      // Find the absolute min and max bounds of all child nodes
      foreach (var node in nodes)
      {
        if (node.Position.x < minX) minX = node.Position.x;
        if (node.Position.y < minY) minY = node.Position.y;
        if (node.Position.x + node.Width > maxX) maxX = node.Position.x + node.Width;
        if (node.Position.y + node.Height > maxY) maxY = node.Position.y + node.Height;
      }

      parent.Rectangle.Position = new Vector2(minX, minY);
      parent.Rectangle.Size = new Vector2(maxX - minX, maxY - minY);
    }


    public void ResetCoverec(ref PTree tree)
    {
      List<Vector2> pnodes = tree.Root.Rests
        .Select(n => n.Rectangle.Position + n.Rectangle.Size)
        .ToList();
      Vector2 max = Vector2.zero;
      foreach (Vector2 corner in pnodes)
      {
        max = new Vector2(
            Mathf.Max(max.x, corner.x),
            Mathf.Max(max.y, corner.y)
        );
      }
      //tree.coverec = new Vector2(Mathf.Max(max.x, max.y), Mathf.Max(max.x,max.y));
      tree.coverec = max;

      //Debug.Log("ResetCoverec " + tree.coverec);
    }

    #region untested
    public void AddCoverecToHistory(Vector2 coverec, string parent)
    {
      //parentID          sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      //(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>) getLine = new();

      //    sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      //(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2) lastEvent = new();

      // Find the index of the history entry for the parent
      int historyIdx = history.FindLastIndex(h => h.Item1 == parent || h.Item1 == "dummy");
      if (historyIdx != -1)
      {
        var eventList = history[historyIdx].Item2;
        if (eventList.Count > 0)
        {
          // Get the last event, update its Item5 (coverec), and set it back
          var lastEvent = eventList[eventList.Count - 1];
          lastEvent.Item5 = coverec;
          eventList[eventList.Count - 1] = lastEvent;
        }
      }
    }

    public Vector2 GetCoverecFromHistory(string parent)
    {
      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        var getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");
        var lastEvent = getLine.Item2.LastOrDefault();
        return lastEvent.Item5;
      }
      Debug.LogWarning("No history found for parent: " + parent);
      return Vector2.zero;
    }
    #endregion

    public void AddToHistory(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes, Vector2 worstCaseSize, string parent)
    {
      //                    parentID            sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes  worstCaseSize coverec
      //public static List<(string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>)> history;

      List<string> newNodeIDs = new();
      List<string> sameNodeIDs = new();
      List<string> deletedNodeIDs = new();
      List<string> oldNodeIDs = new();
      List<string> currentNodeIDs = new();

      List<(string, Vector2)> sameIDsNewSizes = new();
      List<(string, Vector2)> newNodeIDsNewSizes = new();
      List<(string, Vector2)> deletedNodeIDsNewSizes = new();
      List<(string, Vector2)> currentNodeIDsNewSizes = new();

      //         sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)> listOfHistory = new();
      List<Vector2> newSizes = new();

      //parentID          sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      (string, List<(List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2)>) getLine = new();

      //    sameIDs newSizes        newIDs  newSizes       deletedIDs  newSizes
      (List<(string, Vector2)>, List<(string, Vector2)>, List<(string, Vector2)>, Vector2, Vector2) lastEvent = new();

      if (history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
      {
        getLine = history.LastOrDefault(h => h.Item1 == parent || h.Item1 == "dummy");

        lastEvent = getLine.Item2.LastOrDefault();

        oldNodeIDs = lastEvent.Item1.Select(x => x.Item1).Concat(lastEvent.Item2.Select(x => x.Item1)).ToList();
        currentNodeIDs = nodes.Select(n => n.ID).ToList();
        sameNodeIDs = oldNodeIDs.Intersect(currentNodeIDs).ToList();
        newNodeIDs = currentNodeIDs.Except(oldNodeIDs).ToList();
        deletedNodeIDs = oldNodeIDs.Except(currentNodeIDs).ToList();

        foreach (ILayoutNode node in nodes)
        {
          if (node != null)
          {
            Vector2 size = new Vector2(layout[node].Scale.x, layout[node].Scale.z);
            currentNodeIDsNewSizes.Add((node.ID, size));
          }
        }
        foreach ((string, Vector2) currentNode in currentNodeIDsNewSizes)
        {
          if (sameNodeIDs.Contains(currentNode.Item1))
          {
            sameIDsNewSizes.Add(currentNode);
          }
          if (newNodeIDs.Contains(currentNode.Item1))
          {
            newNodeIDsNewSizes.Add(currentNode);
          }
        }

        foreach (string deletedID in deletedNodeIDs)
        {
          List<(string, Vector2)> oldTupples = lastEvent.Item1.Concat(lastEvent.Item2).ToList();
          List<(string, Vector2)> deletedTupple = oldTupples.Where(x => x.Item1 == deletedID).ToList();
          deletedNodeIDsNewSizes.AddRange(deletedTupple);
        }

        int idx = history.FindLastIndex(h => h.Item1 == parent || h.Item1 == "dummy");
        if (idx != -1)
        {
          history[idx].Item2.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes, worstCaseSize, Vector2.zero));
        }
        //PrintHistory();
        //Debug.Log("1");
      }
      else
      {
        newNodeIDsNewSizes = nodes.Select(n => (n.ID, new Vector2(layout[n].Scale.x, layout[n].Scale.z))).ToList();
        listOfHistory.Add((sameIDsNewSizes, newNodeIDsNewSizes, deletedNodeIDsNewSizes, worstCaseSize, Vector2.zero));
        if (!history.Any(h => h.Item1 == parent || h.Item1 == "dummy"))
        {
          history.Add((parent, listOfHistory));
        }
        //PrintHistory();
        //Debug.Log("2");
      }

    }


    private Vector2 UsualProcess(Dictionary<ILayoutNode, NodeTransform> layout, List<ILayoutNode> nodes)
    {

      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });


      Vector2 worstCaseSize = Sum(nodes, layout);

      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);

      Vector2 covrec = Vector2.zero;

      Dictionary<PNode, float> preservers = new();

      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {

        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();
        expanders.Clear();

        foreach (PNode pnode in tree.GetSufficientlyLargeLeaves(requiredSize))
        {

          Vector2 corner = pnode.Rectangle.Position + requiredSize;

          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (PTree.FitsInto(expandedCoveRec, covrec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
          }
          else
          {
            float ratio = expandedCoveRec.x / expandedCoveRec.y;
            expanders[pnode] = Mathf.Abs(ratio - 1);
          }
        }

        PNode targetNode = null;
        if (preservers.Count > 0)
        {
          float lowestWaste = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in preservers)
          {
            if (entry.Value < lowestWaste)
            {
              targetNode = entry.Key;
              lowestWaste = entry.Value;
            }
          }
        }
        else
        {
          float bestRatio = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in expanders)
          {
            if (entry.Value < bestRatio)
            {
              targetNode = entry.Key;
              bestRatio = entry.Value;
            }
          }
        }

        PNode fitNode = tree.Split(targetNode, requiredSize);

        Vector3 scale = layout[el].Scale;
        layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                       fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                       scale);

        {
          Vector2 corner = fitNode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (!PTree.FitsInto(expandedCoveRec, covrec))
          {
            covrec = expandedCoveRec;
          }
        }
      }
      Debug.Log("tree.root.size: " + tree.Root.Rectangle.Size);
      //tree.Print();
      return covrec;
    }
    public Vector2 UsualProcess1(ref Dictionary<ILayoutNode, NodeTransform> layout, ref List<ILayoutNode> nodes)
    {
      /*
      */
      SortNodesByAreaSize(nodes, layout);
      Vector2 worstCaseSize = Sum(nodes, layout);
      PTree tree = new(Vector2.zero, 1.1f * worstCaseSize);
      Vector2 covrec = Vector2.zero;

      Dictionary<PNode, float> preservers = new();

      Dictionary<PNode, float> expanders = new();

      foreach (ILayoutNode el in nodes)
      {
        if (!layout.ContainsKey(el))
        {
          Debug.LogWarning("Layout does not contain element************************************** " + el.ID);
          continue;
        }

        Vector2 requiredSize = GetRectangleSize(layout[el]);

        preservers.Clear();

        expanders.Clear();

        var sufficientLargeLeaves = tree.GetSufficientlyLargeLeaves(requiredSize);
        //tree.Print();

        if (sufficientLargeLeaves.Count == 0)
        {
          tree.Print1();
          throw new Exception("No sufficiently large free leaf found for size " + " :" + el.AbsoluteScale + ": " + " :" + RectanglePackingNodeLayout1.globalCallCount + ": ");
        }

        foreach (PNode pnode in sufficientLargeLeaves)
        {
          Vector2 corner = pnode.Rectangle.Position + requiredSize;
          Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

          if (PTree.FitsInto(expandedCoveRec, covrec))
          {
            float waste = pnode.Rectangle.Size.x * pnode.Rectangle.Size.y - requiredSize.x * requiredSize.y;
            preservers[pnode] = waste;
          }
          else
          {
            float ratio = expandedCoveRec.x / expandedCoveRec.y;
            expanders[pnode] = Mathf.Abs(ratio - 1);
          }
        }

        PNode targetNode = null;
        if (preservers.Count > 0)
        {
          float lowestWaste = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in preservers)
          {
            if (entry.Value < lowestWaste)
            {
              targetNode = entry.Key;
              lowestWaste = entry.Value;
            }
          }
        }
        else
        {
          float bestRatio = Mathf.Infinity;
          foreach (KeyValuePair<PNode, float> entry in expanders)
          {
            if (entry.Value < bestRatio)
            {
              targetNode = entry.Key;
              bestRatio = entry.Value;
            }
          }
          /*
          targetNode = expanders
            .Where(kv => kv.Value == expanders.Values.Min())
            .OrderBy(kv => kv.Key.Rectangle.Size.x * kv.Key.Rectangle.Size.y)
            .First()
            .Key;
        }
           */

          if (targetNode == null)
          {
            Debug.LogError("targetNode is null!");
            continue;
          }
          PNode fitNode = tree.Split1(targetNode, requiredSize, el.ID);

          Vector3 scale = layout[el].Scale;
          layout[el] = new NodeTransform(fitNode.Rectangle.Position.x + scale.x / 2.0f,
                                         fitNode.Rectangle.Position.y + scale.z / 2.0f,
                                         scale, fitNode);


          {
            Vector2 corner = fitNode.Rectangle.Position + requiredSize;
            Vector2 expandedCoveRec = new(Mathf.Max(covrec.x, corner.x), Mathf.Max(covrec.y, corner.y));

            if (!PTree.FitsInto(expandedCoveRec, covrec))
            {
              covrec = expandedCoveRec;
            }
          }
        }
        tree.Print1();
        Debug.Log("********************************************************************************************************");
      }
      return covrec;
    }

    public static Vector2 GetRectangleSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return new Vector2(size.x, size.z);
    }


    public static Vector2 Sum(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      Vector2 result = Vector2.zero;
      foreach (ILayoutNode element in nodes)
      {
        if (!layout.ContainsKey(element))
        {
          Debug.LogWarning("Layout does not contain element************************************** " + element.ID);
          continue;
        }

        Vector3 size = layout[element].Scale;
        result.x += size.x;
        result.y += size.z;

      }
      return result;
    }

    private static void SortNodesByAreaSize(List<ILayoutNode> nodes, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      nodes.Sort(delegate (ILayoutNode left, ILayoutNode right)
      { return AreaSize(layout[right]).CompareTo(AreaSize(layout[left])); });
    }

    public static float AreaSize(NodeTransform node)
    {
      Vector3 size = node.Scale;
      return size.x * size.z;
    }
    public void SecondScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      SetRootLayoutNode(rectangle);
      float zStart = centerPosition.z + rectangle.y / 2f;
      float xStart = centerPosition.z + rectangle.y / 2f;

      float zPointer = zStart;
      float xPointer = xStart;

      float zScalePointer = zStart;
      float xScalePointer = xStart;

      float limitZ = -zStart;
      float limitX = -xStart;

      NodeTransform parentTransform = new NodeTransform(
          0,
          0,
          rootLayoutNode.AbsoluteScale
      );

      layoutResult[rootLayoutNode] = parentTransform;

      foreach (var leafNode in leafsNodes)
      {
        Vector3 nodeScale = leafNode.AbsoluteScale;
        if (zPointer + nodeScale.x > limitZ)
        {
          // Move to next row
          zPointer = zStart;
          xPointer -= xScalePointer;
        }
        NodeTransform nodeTransform = new NodeTransform(
            zPointer,
            xPointer,
            nodeScale
        );
        if (nodeScale.z > xScalePointer) xScalePointer = nodeScale.z;
        layoutResult[leafNode] = nodeTransform;
        xPointer -= nodeScale.x + .1f;

        rootLayoutNode.AddChild(leafNode);

      }
    }
    public void PlaceNodesInRecs()
    {
      foreach (var leafNode in leafsNodes)
      {
        Rec nodeRec = new Rec(0, 0, leafNode.AbsoluteScale.x, leafNode.AbsoluteScale.z);
      }
    }
    public void PrintDictionary(Dictionary<ILayoutNode, NodeTransform> dict)
    {
      foreach (var entry in dict)
      {
        Debug.Log($"Node: {entry.Key.Print()}, Transform: {entry.Value}");
      }
    }
    public void FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      ILayoutNode firstNode = layoutNodes.FirstOrDefault(n => n != null && n.IsLeaf);

      SetRootLayoutNode(rectangle);

      Debug.Log(rectangle + " " + centerPosition);

      /*
      for (int i = 0; i < count; i++)
      {
        ILayoutNode node = nodes[i];

        float z = startZ - i * spacing;

      }

      float xx = x - rootLayoutNode.AbsoluteScale.x / 2f;
      float zz = z - rootLayoutNode.AbsoluteScale.z / 2f;
       */
      float x = centerPosition.x - rectangle.x / 2f;
      float z = centerPosition.z + rectangle.y / 2f;


      NodeTransform parentTransform = new NodeTransform(
          0,
          0,
          rootLayoutNode.AbsoluteScale
      );

      NodeTransform firstNodeTransform = new NodeTransform(
          -z,
          -z,
          firstNode.AbsoluteScale
      );

      layoutResult[rootLayoutNode] = parentTransform;
      layoutResult[firstNode] = firstNodeTransform;

    }

    public void SetRootLayoutNode(Vector2 rectangle)
    {
      rootNode.ID = "1";
      graph.AddNode(rootNode);
      rootNode.ItsGraph = graph;
      rootLayoutNode = new LayoutGraphNode(rootNode);
      //rootLayoutNode.AddChild(firstNode);
      rootLayoutNode.Parent = null;

      rootLayoutNode.AbsoluteScale = new Vector3(rectangle.x * 2f, 0, rectangle.y * 2f);
    }

    private void PrintHistory()
    {
      Debug.Log("Printing History:");
      foreach (var line in history)
      {
        Debug.Log($"Parent ID: {line.Item1}");
        foreach (var eventItem in line.Item2)
        {
          Debug.Log("  Event:");
          Debug.Log("    Same IDs and New Sizes:");
          foreach (var (id, size) in eventItem.Item1)
          {
            Debug.Log($"      ID: {id}, Size: {size}");
          }
          Debug.Log("    New IDs and Sizes:");
          foreach (var (id, size) in eventItem.Item2)
          {
            Debug.Log($"      ID: {id}, Size: {size}");
          }
          Debug.Log("    Deleted IDs and Sizes:");
          foreach (var (id, size) in eventItem.Item3)
          {
            Debug.Log($"      ID: {id}, Size: {size}");
          }
        }
      }
    }

    private void PrintPreserverExpanders(Dictionary<PNode, float> preservers, Dictionary<PNode, float> expanders)
    {
      Debug.Log("--------------------------------------------");
      Debug.Log("preservers----------------------------------");
      foreach (var entry in preservers)
      {
        Debug.Log($"PNode ID: {entry.Key.ToString()}, Waste: {entry.Value}");
      }
      Debug.Log("--------------------------------------------");
      Debug.Log("expanders-----------------------------------");
      foreach (var entry in expanders)
      {
        Debug.Log($"PNode ID: {entry.Key.ToString()}, Ratio Difference: {entry.Value}");
      }
      Debug.Log("--------------------------------------------");
    }

    private static void RemovePadding(Dictionary<ILayoutNode, NodeTransform> layout)
    {
      // We use a copy of the keys because we will modify layout during the iteration.
      ICollection<ILayoutNode> layoutNodes = new List<ILayoutNode>(layout.Keys);

      foreach (ILayoutNode layoutNode in layoutNodes)
      {
        // We added padding to both inner nodes and leaves, but we want to
        // restore the original size of the leaves only.
        if (layoutNode.IsLeaf)
        {
          NodeTransform value = layout[layoutNode];
          Vector3 scale = value.Scale;
          float reversePadding = ReversePadding(scale.x, scale.z);
          // We shrink the scale, but the position remains the same since
          // value.Position denotes the center point.
          layout[layoutNode].ExpandBy(-reversePadding, -reversePadding);
        }
      }
    }

  }
  public class Rec
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="x">X co-ordinate at corner.</param>
    /// <param name="z">Z co-ordinate at corner.</param>
    /// <param name="width">Width of the rectangle.</param>
    /// <param name="depth">Depth (breadth) of the rectangle.</param>
    public Rec(float x, float z, float width, float depth)
    {
      X = x;
      Z = z;
      Width = width;
      Depth = depth;
    }
    /// <summary>
    /// X co-ordinate at corner.
    /// </summary>
    public float X;
    /// <summary>
    /// Z co-ordinate at corner.
    /// </summary>
    public float Z;
    /// <summary>
    /// Width of the rectangle.
    /// </summary>
    public float Width;
    /// <summary>
    /// Depth (breadth) of the rectangle.
    /// </summary>
    public float Depth;

    public Vector2 Center()
    {
      return new Vector2(X + Width / 2f, Z + Depth / 2f);
    }

    public Vector2 position
    {
      get { return new Vector2(X, Z); }
    }
  }
  public class NodeSize
  {
    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="gameNode">Layout node this node size corresponds to.</param>
    /// <param name="size">Size of the node.</param>
    public NodeSize(ILayoutNode gameNode, float size)
    {
      GameNode = gameNode;
      Size = size;
    }
    /// <summary>
    /// The layout node this node size corresponds to.
    /// </summary>
    public ILayoutNode GameNode;
    /// <summary>
    /// The size of the node.
    /// </summary>
    public float Size;
  }
}
