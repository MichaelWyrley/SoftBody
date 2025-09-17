using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Triangles
{
	public Vector3 posA;
	public Vector3 posB;
	public Vector3 posC;

	public Triangles(Vector3 posA, Vector3 posB, Vector3 posC)
	{
		this.posA = posA;
		this.posB = posB;
		this.posC = posC;
	}

    public static int GetSize () {
        return sizeof (float) * 9;
    }
}