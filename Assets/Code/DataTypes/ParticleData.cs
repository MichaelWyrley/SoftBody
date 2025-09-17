using UnityEngine;

[System.Serializable]
public struct ParticleData 
{

    public Vector3 position;
    public float pad0;
    public Vector3 velocity;
    public float pad1;
    public Vector3 force;
    public float pad2;
    public float mass;
    public float radius;
    public float collisionDamping;
    public float pad3;

    public ParticleData(Vector3 position, float mass, float radius, float collisionDamping){
        this.position = position;
        this.velocity = Vector3.zero;
        this.force = Vector3.zero;
        this.mass = mass;
        this.radius = radius;
        this.collisionDamping = collisionDamping;
        this.pad0 = 0f;
        this.pad1 = 0f;
        this.pad2 = 0f;
        this.pad3 = 0f;


    }
    public ParticleData(Vector3 position){
        this.position = position;
        this.velocity = Vector3.zero;
        this.force = Vector3.zero;

        this.mass = 0.01f;
        this.radius = 0.005f; 
        this.collisionDamping = 1.1f;
        this.pad0 = 0f;
        this.pad1 = 0f;
        this.pad2 = 0f;
        this.pad3 = 0f;
    }

    public static int GetSize () {
        return sizeof(float) * 16; // 16 floats = 64 bytes
    }


    
}
