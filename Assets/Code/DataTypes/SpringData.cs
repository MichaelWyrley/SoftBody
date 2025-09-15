using UnityEngine;

[System.Serializable]
public struct SpringData
{
    private ParticelData p1, p2;
    private float restLength;


    public SpringData(ParticelData p1, ParticelData p2) {
        this.p1 = p1;
        this.p2 = p2;
        this.restLength = Vector3.Distance(p1.position, p2.position);

    }

    public static int GetSize () {
        return ParticelData.GetSize() * 2 + sizeof(float) * 1;
    }



}
