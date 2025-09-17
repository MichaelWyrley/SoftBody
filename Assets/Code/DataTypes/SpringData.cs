using UnityEngine;

[System.Serializable]
public struct SpringData
{
    public uint p1, p2;
    public float restLength;
    public float pad;


    public SpringData(uint p1, uint p2, float restLength) {
        this.p1 = p1;
        this.p2 = p2;
        this.restLength = restLength;
        this.pad = 0f;

    }

    public static int GetSize () {
        return sizeof(uint) * 2 + sizeof(float) * 2;
    }



}
