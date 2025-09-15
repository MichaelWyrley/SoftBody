using UnityEngine;

[System.Serializable]
public struct GroundData
{

    public int triangle_begin;
    public int triangle_count;
    public Vector3 bounds_min;
    public Vector3 bounds_max;

    public GroundData(int triangle_begin, int triangle_count, Vector3 bounds_min, Vector3 bounds_max)
	{
        this.triangle_begin = triangle_begin;
        this.triangle_count = triangle_count;
        this.bounds_min = bounds_min;
        this.bounds_max = bounds_max;

	}

    public static int GetSize () {
        return sizeof (float) * 6 + sizeof (int) * 2;
    }

}
