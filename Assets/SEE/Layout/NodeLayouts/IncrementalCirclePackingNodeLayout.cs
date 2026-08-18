using ClosedXML.Excel;
using SEE.Layout.NodeLayouts.CirclePacking;
using SEE.Layout.NodeLayouts.RectanglePacking;
using SEE.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

namespace SEE.Layout.NodeLayouts
{

  public class IncrementalCirclePackingNodeLayout : NodeLayout, IIncrementalNodeLayout
  {
    static IncrementalCirclePackingNodeLayout()
    {
      Name = "Incremental Circle Packing";
    }

    public IncrementalCirclePackingNodeLayout oldLayout;

    public IIncrementalNodeLayout OldLayout
    {
      set
      {
        if (value is IncrementalCirclePackingNodeLayout layout)
        {
          oldLayout = layout;
        }
        else
        {
          throw new ArgumentException(
              $"Predecessor of {nameof(IncrementalCirclePackingNodeLayout)} was not an {nameof(IncrementalCirclePackingNodeLayout)}.");
        }
      }
    }

    
    public Dictionary<ILayoutNode, NodeTransform> layoutResult;

    public static int Counter = 0;
    public static double totalTimeInMilliSeconds = 0;
    public static List<float> incLayoutDistanceChange;
    public static List<float> incNearestNeighborWithin;
    public static List<float> incRanking;
    public static List<float> incSECC;
    public static List<int> incNewNodesCount;
    public static List<int> incGrownNodesCount;
    public static List<int> incShrunkNodesCount;
    public static List<int> incDeletedNodesCount;
    public static List<int> incAllNodes;

    //********************************************************************************************************************************
    protected override Dictionary<ILayoutNode, NodeTransform> Layout(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {

      Counter += 1;
      Performance p = Performance.Begin("ICP Layout Evaluation");
      FirstScenario(layoutNodes, centerPosition, rectangle);
      p.End();
      totalTimeInMilliSeconds += p.GetTimeInMilliSeconds();

      if (oldLayout == null)
      {
        incLayoutDistanceChange = new List<float>();
        incNearestNeighborWithin = new List<float>();
        incRanking = new List<float>();
        incSECC = new List<float>();
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
        incSECC.Add(CalculateSECC(layoutResult, centerPosition));
        incNewNodesCount.Add(CalculateNewNodesCount(oldLayout.layoutResult, layoutResult));
        incGrownNodesCount.Add(CalculateGrownAndShrunkNodesCount(oldLayout.layoutResult, layoutResult).Item1);
        incShrunkNodesCount.Add(CalculateGrownAndShrunkNodesCount(oldLayout.layoutResult, layoutResult).Item2);
        incDeletedNodesCount.Add(CalculateDeletedNodesCount(oldLayout.layoutResult, layoutResult));
        int n = layoutNodes.Count();
        incAllNodes.Add(n);


        string ldcString = "";
        string nnwString = "";
        string rankingString = "";
        string seccString = "";
        string newNodesString = "";
        string grownNodesString = "";
        string shrunkNodesString = "";
        string deletedNodesString = "";
        string allNodesString = "";

        ldcString += " ICP Layout Distance Changes: " + string.Join(", ", incLayoutDistanceChange) + "\n" + "#####################\n";
        nnwString += " ICP Nearest Neighbor Within: " + string.Join(", ", incNearestNeighborWithin) + "\n" + "#####################\n";
        rankingString += " ICP Ranking: " + string.Join(", ", incRanking) + "\n" + "#####################\n";
        seccString += " ICP SECC: " + string.Join(", ", incSECC) + "\n" + "#####################\n";
        newNodesString += " ICP New Nodes: " + string.Join(", ", incNewNodesCount) + "\n" + "#####################\n";
        grownNodesString += " ICP Grown Nodes: " + string.Join(", ", incGrownNodesCount) + "\n" + "#####################\n";
        shrunkNodesString += " ICP Shrunk Nodes: " + string.Join(", ", incShrunkNodesCount) + "\n" + "#####################\n";
        deletedNodesString += " ICP Deleted Nodes: " + string.Join(", ", incDeletedNodesCount) + "\n" + "#####################\n";
        allNodesString += " ICP All Nodes: " + string.Join(", ", incAllNodes) + "\n" + "#####################\n";

        Debug.Log(ldcString + nnwString + rankingString + seccString + newNodesString + grownNodesString + shrunkNodesString + deletedNodesString + allNodesString);

        if (Counter == 102)
        {
          SchreibeListenInSpalten(new List<List<float>> { incLayoutDistanceChange, incNearestNeighborWithin, incRanking, incSECC });
          SchreibeListenInSpalten(new List<List<int>> { incNewNodesCount, incGrownNodesCount, incShrunkNodesCount, incDeletedNodesCount, incAllNodes }, 5);
          Debug.Log("Total time for ICP layout evaluation: " + Performance.GetElapsedTime(totalTimeInMilliSeconds));
        }

      }

      return layoutResult;

    }

    #region Scenarios

    public float CalculateSECC(Dictionary<ILayoutNode, NodeTransform> layout, Vector3 layoutCenter)
    {
      if (layout.Count == 0) return 0f;

      float totalNodeArea = 0f;
      float maxRadiusFromCenter = 0f;

      foreach (var kvp in layout)
      {
        NodeTransform t = kvp.Value;

        // Der Radius des aktuellen Knotens (Annahme: Scale.x ist der Durchmesser)
        float nodeRadius = t.Scale.x / 2f;

        // 1. Fläche dieses Kreises zur Gesamtsumme addieren (Pi * r^2)
        totalNodeArea += Mathf.PI * nodeRadius * nodeRadius;

        // 2. Distanz des Kreismittelpunkts zum globalen Layout-Zentrum
        // Wir ignorieren die Y-Achse (Höhe), da wir die Grundfläche evaluieren
        Vector3 pos2D = new Vector3(t.CenterPosition.x, 0, t.CenterPosition.z);
        Vector3 center2D = new Vector3(layoutCenter.x, 0, layoutCenter.z);

        float distanceToCenter = Vector3.Distance(pos2D, center2D);

        // 3. Die äußerste Kante dieses Kreises vom Zentrum aus gesehen
        float outerEdgeDistance = distanceToCenter + nodeRadius;

        // Wenn diese Kante weiter außen liegt als alle bisherigen, 
        // haben wir einen neuen maximalen Hüllkreis-Radius gefunden
        if (outerEdgeDistance > maxRadiusFromCenter)
        {
          maxRadiusFromCenter = outerEdgeDistance;
        }
      }

      // Fläche des minimal umschließenden Hüllkreises berechnen (Pi * R^2)
      float enclosingCircleArea = Mathf.PI * maxRadiusFromCenter * maxRadiusFromCenter;

      // Division durch Null abfangen
      if (enclosingCircleArea <= 0f) return 0f;

      // SECC-Formel: 100 * (A_N / A_C)
      return 100f * (totalNodeArea / enclosingCircleArea);
    }
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
      return Path.Combine(Application.persistentDataPath, "ICP.xlsx");
    }
    public void makeTable()
    {
      return;
    }

    #endregion
    //********************************************************************************************************************************
    public Dictionary<ILayoutNode, NodeTransform> FirstScenario(IEnumerable<ILayoutNode> layoutNodes, Vector3 centerPosition, Vector2 rectangle)
    {
      layoutResult = new Dictionary<ILayoutNode, NodeTransform>();

      ICollection<ILayoutNode> roots = LayoutNodes.GetRoots(layoutNodes);
      if (roots.Count == 0)
      {
        throw new System.Exception("Graph has no root node.");
      }
      else if (roots.Count > 1)
      {
        throw new System.Exception("Graph has more than one root node.");
      }
      else
      {
        ILayoutNode root = roots.FirstOrDefault();

        //AddToHistory(layoutResult, layoutNodes.ToList(), rectangle, root.ID);

        // exactly one root
        float outRadius = PlaceNodes(root, layoutResult);
        Vector2 position = Vector2.zero;
        layoutResult[root] = new NodeTransform(position.x, position.y, GetScale(root, outRadius));
        MakeGlobal(layoutResult, position, root.Children());
        //Debug.Log("**************************************************************************************");
        return layoutResult;
      }
    }
    //********************************************************************************************************************************
    private static void MakeGlobal(Dictionary<ILayoutNode, NodeTransform> layoutResult, Vector2 position, ICollection<ILayoutNode> children)
    {
      foreach (ILayoutNode child in children)
      {
        NodeTransform childTransform = layoutResult[child];
        Vector2 childPosition = new Vector2(childTransform.X, childTransform.Z) + position;
        childTransform.MoveTo(childPosition.x, childPosition.y);
        layoutResult[child] = childTransform;
        MakeGlobal(layoutResult, childPosition, child.Children());
      }
    }

    //********************************************************************************************************************************
    public float PlaceNodes(ILayoutNode parent, Dictionary<ILayoutNode, NodeTransform> layout)
    {
      ICollection<ILayoutNode> children = parent.Children();

      if (children.Count == 0)
      {

        return LeafRadius(parent);
      }
      else
      {
        List<Circle1> circles = new(children.Count);

        int i = 0;
        foreach (ILayoutNode child in children)
        {
          float radius = child.IsLeaf ? LeafRadius(child) : PlaceNodes(child, layout);

          float radians = (i / (float)children.Count) * (2.0f * Mathf.PI);
          circles.Add(new Circle1(child, new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius, radius));
          i++;
        }
        
        IncrementalCirclePacker.PackCircles(circles, Vector2.zero, out float outOuterRadius, oldLayout == null, parent.ID);

        if (children.Count == 1 && !children.ElementAt(0).IsLeaf)
        {
          outOuterRadius *= 1.2f;
        }

        foreach (Circle1 circle in circles)
        {

          layout[circle.GameObject]
               = new NodeTransform(circle.Center.x, circle.Center.y,
                                   GetScale(circle.GameObject, circle.Radius));
        }
        return outOuterRadius;
      }
    }


    
    //********************************************************************************************************************************
    private static Vector3 GetScale(ILayoutNode node, float radius)
    {
      return node.IsLeaf ? node.AbsoluteScale
                         : new Vector3(2 * radius, node.AbsoluteScale.y, 2 * radius);
    }

    //********************************************************************************************************************************
    private static float LeafRadius(ILayoutNode block)
    {
      Vector3 extent = block.AbsoluteScale / 2.0f;
      return Mathf.Sqrt(extent.x * extent.x + extent.z * extent.z);
    }
  }
}
