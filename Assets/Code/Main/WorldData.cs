using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WorldData {
    /* WorldData is a container that represents a world.
     * World state, name, zone information, etc. are all stored in this class.
     * In the most basic implementation, this class is to be serialized to JSON
     * - for storage in GameData - Our save data file.
     */

    public List<ZoneData> w_Zones; //World Zone List

    public void Generate (ZoneTypesList zoneTypes, Dictionary<ZoneType, ZoneList> mandatoryZoneLibrary, Dictionary<ZoneType, ZoneList> fillerZoneLibrary, Dictionary<ZoneType, ZoneList> uniqueZoneLibrary, int length = 10, int width = 10) { //Make this an IEnumerator later --- (To Add)
        /* Generates a new world and fills the w_Zones property.
         */
        float uniqueZoneChance = 0.75f; //Make this a function parameter later --- (To Add)
        // Create a 2D Array of blank zones, initialize starting points for each zoneType:
        ZoneData[,] newWorldZones = new ZoneData[length, width];
        newWorldZones = initZones(newWorldZones, zoneTypes);

        //Grow each zone type until no more moves can be made:
        newWorldZones = growZones(newWorldZones, zoneTypes);

        //Apply a randomized cutting of zones at the edges of the world to create a continental look:
        newWorldZones = applyContinentFilter(newWorldZones, 1, 0.5f);

        //Assign MandatoryZones in their respective ZoneTypes, and if they do not exist, create them randomly.
        newWorldZones = assignMandatoryZones(newWorldZones, mandatoryZoneLibrary);

        //Fill in the rest of the Zones with FillerZones or UniqueZones based on their ZoneType while assigning their prefabIndex. Finally, convert to a 1D list and update our own w_Zones:
        List<ZoneData> processedZoneList = new List<ZoneData>();
        Dictionary<ZoneType, List<int>> uniqueIndexPool = new Dictionary<ZoneType, List<int>>();
        //Initialize our prefabIndex-by-zoneType pool:
        foreach (ZoneType zt in uniqueZoneLibrary.Keys) {
            List<int> newList = new List<int>();
            for (int i = 0; i < uniqueZoneLibrary[zt].zonePrefabList.Count; i++) {
                newList.Add(i);
            }
            uniqueIndexPool.Add(zt, newList);
        }
        foreach (ZoneData z in newWorldZones) {
            Debug.Log(z.z_ZoneType.name);
            //If it is not a story zone, determine and set whether it is unique or filler --- (To Add)
            if (z.z_ZoneFunction == ZoneFunction.Unset) {
                if (uniqueZoneLibrary.ContainsKey(z.z_ZoneType)) { //Exception may be found here --- (To Review/Foolproof)
                    //Chance for a unique zone
                    if (uniqueIndexPool[z.z_ZoneType].Count > 0) {
                        if (Random.value >= 1 - uniqueZoneChance) {
                            //UniqueZone()
                            z.z_ZoneFunction = ZoneFunction.Unique;
                            z.z_ZonePrefabIndex = (int)Random.Range(0, uniqueZoneLibrary[z.z_ZoneType].zonePrefabList.Count);
                            uniqueIndexPool[z.z_ZoneType].Remove(z.z_ZonePrefabIndex);
                        }
                        else {
                            //Filler zone()
                            z.z_ZoneFunction = ZoneFunction.Filler;
                            if (fillerZoneLibrary.ContainsKey(z.z_ZoneType)) { //Exception may be found here --- (To Review/Foolproof)
                                z.z_ZonePrefabIndex = (int)Random.Range(0, fillerZoneLibrary[z.z_ZoneType].zonePrefabList.Count);
                            }
                        }
                    }
                    else {
                        //Filler zone()
                        z.z_ZoneFunction = ZoneFunction.Filler;
                        if (fillerZoneLibrary.ContainsKey(z.z_ZoneType)) { //Exception may be found here --- (To Review/Foolproof)
                            z.z_ZonePrefabIndex = (int)Random.Range(0, fillerZoneLibrary[z.z_ZoneType].zonePrefabList.Count);
                        }
                    }
                }
                else {
                    //Filler zone()
                    z.z_ZoneFunction = ZoneFunction.Filler;
                    if (fillerZoneLibrary.ContainsKey(z.z_ZoneType)) { //Exception may be found here --- (To Review/Foolproof)
                        z.z_ZonePrefabIndex = (int)Random.Range(0, fillerZoneLibrary[z.z_ZoneType].zonePrefabList.Count);
                    }
                }
            }
            processedZoneList.Add(z); //Perhaps check for empty, deleted zones? --- (To Add/Review)
        }
        w_Zones = processedZoneList;
    }

    #region --- [World-Gen Functions] ---
    private ZoneData[,] initZones (ZoneData[,] zdArray, ZoneTypesList zoneTypes) {
        ZoneData[,] processedZDArray = zdArray;
        for (int yIndex = 0; yIndex < zdArray.GetLength(0); yIndex++) {
            for (int xIndex = 0; xIndex < zdArray.GetLength(1); xIndex++) {
                processedZDArray[yIndex, xIndex] = new ZoneData(new Vector2(xIndex, yIndex));
            }
        }

        // Pick random starting points for each zone type:
        foreach (ZoneType currentType in zoneTypes.zoneTypes) {
            Vector2 chosenPoint = pickRandomUnchosenPoint(processedZDArray);
            processedZDArray[(int)chosenPoint.x, (int)chosenPoint.y].z_ZoneType = currentType;
        }
        return processedZDArray;
    }
    private ZoneData[,] growZones (ZoneData[,] zdArray, ZoneTypesList zoneTypes) {
        ZoneData[,] processedZDArray = zdArray;
        int zonesFinished = zoneTypes.zoneTypes.Count;
        Vector2[] adjacentArray = new Vector2[8] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(-1, -1) };
        while (zonesFinished < zdArray.GetLength(0) * zdArray.GetLength(1)) {
            for (int yIndex = 0; yIndex < zdArray.GetLength(0); yIndex++) {
                for (int xIndex = 0; xIndex < zdArray.GetLength(1); xIndex++) {
                    //For every zone in our newWorldZones array.
                    if (processedZDArray[yIndex, xIndex].z_ZoneType.name != "Unspecified") { //If the zonetype has been set.
                        //Check all adjacent tiles and spread to them with priority zoneType.Priority if possible.
                        foreach (Vector2 adjacentTilePos in adjacentArray) {
                            try {
                                if (processedZDArray[(int)yIndex + (int)adjacentTilePos.x, (int)xIndex + (int)adjacentTilePos.y].isJustSet()) {
                                    processedZDArray[(int)yIndex + (int)adjacentTilePos.x, (int)xIndex + (int)adjacentTilePos.y].unJustSet();
                                    continue;
                                }
                                if (!processedZDArray[(int)yIndex + (int)adjacentTilePos.x, (int)xIndex + (int)adjacentTilePos.y].alreadySet()) {
                                    if (processedZDArray[(int)yIndex + (int)adjacentTilePos.x, (int)xIndex + (int)adjacentTilePos.y].setConversionPercentage(processedZDArray[yIndex, xIndex].z_ZoneType)) {
                                        zonesFinished += 1; // A new zone has been finished
                                    }
                                }
                            }
                            catch (System.IndexOutOfRangeException) {
                                continue;
                            }
                        }
                    }
                }
            }
        }
        return processedZDArray;
    }
    private ZoneData[,] assignMandatoryZones (ZoneData[,] zdArray, Dictionary<ZoneType, ZoneList> mandatoryZoneLib) {
        /* Ensures that every mandatory zone is assigned to a unique tile.
         * Zone positions of a matching zonetype are chosen first.
         * If no empty zones of a matching zonetype are found, then for each tile that matches in zonetype, we check adjacent (even in other zonetypes) and try to place one there.
         * If none of the above works or is applicable, we pick random empty zone.
         */
        ZoneData[,] processedZDArray = zdArray;
        foreach (ZoneType currentZType in mandatoryZoneLib.Keys) {
            List<int> prefabIndexQueue = new List<int>();
            for (int i = 0; i < mandatoryZoneLib[currentZType].zonePrefabList.Count; i++) {
                prefabIndexQueue.Add(i);
            }
            while (prefabIndexQueue.Count > 0) {
                Vector2 newZonePos = getRandomUnchosenPoint_MandatoryZone(processedZDArray, currentZType);
                if (newZonePos.x >= 0 || newZonePos.y >= 0) { //If we have a valid zonePosition
                    processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZoneType = currentZType;
                    processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZoneFunction = ZoneFunction.Mandatory;
                    int prefabIndex = prefabIndexQueue[Random.Range(0, prefabIndexQueue.Count - 1)];
                    processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZonePrefabIndex = prefabIndex;
                    prefabIndexQueue.Remove(prefabIndex);
                }
                else {
                    newZonePos = getRandomAdjacentPoint_MandatoryZone(processedZDArray, currentZType);
                    if (newZonePos.x >= 0 || newZonePos.y >= 0) { //If we have a valid zonePosition
                        processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZoneType = currentZType;
                        processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZoneFunction = ZoneFunction.Mandatory;
                        int prefabIndex = prefabIndexQueue[Random.Range(0, prefabIndexQueue.Count - 1)];
                        processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZonePrefabIndex = prefabIndex;
                        prefabIndexQueue.Remove(prefabIndex);
                    }
                    else {
                        newZonePos = getRandomEmptyOrFillerPoint_MandatoryZone(processedZDArray);
                        processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZoneType = currentZType;
                        processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZoneFunction = ZoneFunction.Mandatory;
                        int prefabIndex = prefabIndexQueue[Random.Range(0, prefabIndexQueue.Count - 1)];
                        processedZDArray[(int)newZonePos.x, (int)newZonePos.y].z_ZonePrefabIndex = prefabIndex;
                        prefabIndexQueue.Remove(prefabIndex);
                    }
                }
            }
        }

        return processedZDArray;
    }
    private ZoneData[,] applyContinentFilter (ZoneData[,] zdArray, int iterations, float strength) {
        int currentIterations = 0;
        ZoneData[,] processedZDArray = zdArray;
        while (currentIterations < iterations) {
            List<Vector2> toFilter = new List<Vector2>();
            for (int yIndex = 0; yIndex < processedZDArray.GetLength(0)-currentIterations; yIndex++) {
                for (int xIndex = 0; xIndex < processedZDArray.GetLength(1)-currentIterations; xIndex++) {
                    if (yIndex == currentIterations || xIndex == currentIterations || yIndex == processedZDArray.GetLength(0)-currentIterations-1 || xIndex == processedZDArray.GetLength(1)-currentIterations-1) {
                        toFilter.Add(new Vector2(yIndex, xIndex));
                    }
                }
            }
            foreach (Vector2 zDataPos in toFilter) {
                if (strength >= Random.value) {
                    processedZDArray[(int)zDataPos.x, (int)zDataPos.y].z_ZoneFunction = ZoneFunction.Empty;
                }
            }
            currentIterations++;
        }

        return processedZDArray;
    }

    // --- [Helpers] ---
    private Vector2 getRandomEmptyOrFillerPoint_MandatoryZone (ZoneData[,] zones) {
        List<ZoneData> possibleZones = new List<ZoneData>();
        foreach (ZoneData zd in zones) {
            if (zd.z_ZoneFunction == ZoneFunction.Unset || zd.z_ZoneFunction == ZoneFunction.Empty || zd.z_ZoneFunction == ZoneFunction.Filler) {
                possibleZones.Add(zd);
            }
        }
        if (possibleZones.Count > 0) {
            return possibleZones[Random.Range(0, possibleZones.Count)].z_ZonePosition;
        }
        else {
            throw new System.Exception("No possible zone for current mandatory zone!"); //This should never happen if proper world size minimums are in place.
        }
    }
    private Vector2 getRandomUnchosenPoint_MandatoryZone (ZoneData[,] zones, ZoneType zType) {
        /* Gets a random, unchosen point from a list of all empty zones of the same zoneType as zType.
         * Otherwise, returns a new Vector2(-1,-1) - which will be interpreted as invalid.
         */
        List<ZoneData> possibleZones = new List<ZoneData>();
        foreach (ZoneData zd in zones) {
            if (zd.z_ZoneFunction == ZoneFunction.Unset && zd.z_ZoneType.name == zType.name) {
                possibleZones.Add(zd);
            }
        }

        if (possibleZones.Count > 0) {
            return possibleZones[Random.Range(0, possibleZones.Count)].z_ZonePosition;
        }
        return new Vector2(-1, -1);
    }
    private Vector2 getRandomAdjacentPoint_MandatoryZone (ZoneData[,] zones, ZoneType zType) {
        List<ZoneData> filteredZones = new List<ZoneData>();
        foreach (ZoneData zd in zones) {
            if (zd.z_ZoneFunction == ZoneFunction.Unset && zd.z_ZoneType.name == zType.name) {
                filteredZones.Add(zd);
            }
        }
        List<ZoneData> possibleZones = new List<ZoneData>();
        Vector2[] adjacentArray = new Vector2[8] { new Vector2(0, 1), new Vector2(0, -1), new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, -1), new Vector2(-1, 0), new Vector2(-1, 1), new Vector2(-1, -1) };
        foreach (ZoneData currentCenterZone in filteredZones) {
            foreach (Vector2 adjacentPos in adjacentArray) {
                try {
                    if (zones[(int)currentCenterZone.z_ZonePosition.x + (int)adjacentPos.x, (int)currentCenterZone.z_ZonePosition.y + (int)adjacentPos.y] != null) {
                        possibleZones.Add(zones[(int)currentCenterZone.z_ZonePosition.x + (int)adjacentPos.x, (int)currentCenterZone.z_ZonePosition.y + (int)adjacentPos.y]);
                    }
                }
                catch (System.IndexOutOfRangeException) {
                    continue;
                }
            }
        }

        if (possibleZones.Count > 0) {
            return possibleZones[Random.Range(0, possibleZones.Count)].z_ZonePosition;
        }
        return new Vector2(-1, -1);
    }
    private Vector2 pickRandomUnchosenPoint (ZoneData[,] zones) {
        var chosen = false;
        Vector2 randomVector = Vector2.zero;
        while (chosen == false) {
            randomVector = new Vector2((int)Random.Range(0, zones.GetLength(0)), (int)Random.Range(0, zones.GetLength(1)));
            if (zones[(int)randomVector.x, (int)randomVector.y].z_ZoneType.name == "Unspecified") {
                chosen = true;
            }
        }
        return randomVector;
    }
    #endregion
}
