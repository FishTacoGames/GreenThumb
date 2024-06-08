using System;
using System.Collections.Generic;
using UnityEngine;

namespace FishTacoGames
{
    [Serializable]
    public struct GreenThumbCellProfile
    {
        /// <summary>
        /// a list of index values, each representing the index of a terrain tree, used for storing tree positions
        /// </summary>
        public List<Vector3> positionList;
        public List<int> indexOfPositionsList;
        public int profileIndex; // the index assigned to this cell, only unique to this terrain
        public Transform cellMesh;
        public Bounds cellBounds;
        public GreenThumbCellProfile(List<int> indexesList, int cellIndex, List<Vector3> TList, Transform parentTransform, Bounds boundsC)
        {
            positionList = TList;
            indexOfPositionsList = indexesList;
            profileIndex = cellIndex;
            cellMesh = parentTransform;
            cellBounds = boundsC;
        }
    }
}