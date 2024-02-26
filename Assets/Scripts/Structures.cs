using System;
using System.Collections.Generic;
using UnityEngine;

public class Structures {

    //Not sure if im going to use this method yet
    public static List<Tuple<Vector3Int, BlockID>> tree = new () {
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 0, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 1, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 2, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 3, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-2, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, 2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(2, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 4, -2), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 5, 0), BlockID.Wood),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 5, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 5, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(-1, 5, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 5, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 5, 1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 5, 0), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(1, 5, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 5, -1), BlockID.Leaf),
        new Tuple<Vector3Int, BlockID>(new Vector3Int(0, 6, 0), BlockID.Leaf)
    };
}