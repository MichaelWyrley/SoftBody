using UnityEngine;

[System.Serializable]
public struct ParticelData 
{

    public Vector3 position;
    public Vector3 velocity;
    public Vector3 force;
    public float mass;
    public float radius;
    public float collisionDamping;

    public ParticelData(Vector3 position, float mass, float radius, float collisionDamping){
        this.position = position;
        this.mass = mass;
        this.radius = radius;
        this.collisionDamping = collisionDamping;

        this.velocity = Vector3.zero;
        this.force = Vector3.zero;
    }
    public ParticelData(Vector3 position){
        this.position = position;
        this.velocity = Vector3.zero;
        this.force = Vector3.zero;

        this.mass = 1f;
        this.radius = 0.005f; 
        this.collisionDamping = 0.2f;
    }

    public static int GetSize () {
        return sizeof (float) * 3 + sizeof(float) * 9;
    }


    
}
