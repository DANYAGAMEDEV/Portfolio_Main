using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    public string spawnerFunction; //spawnTraders, spawnMercenaries, etc.
    public float spawnAreaWidth = 5f; 
    public float spawnAreaHeight = 5f;
    public float minDistanceBetweenCharacters = 0.5f; 

    public (Vector3 spawnPosition, bool isPossibleToSpawn) GetRandomSpawnPosition()
    {
        float x = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
        float z = Random.Range(-spawnAreaHeight / 2, spawnAreaHeight / 2);

        Vector3 spawnPosition = new Vector3(x, 0, z);
        Collider[] colliders = Physics.OverlapSphere(spawnPosition, minDistanceBetweenCharacters);

        int count = new int(); bool isPossibleToSpawn = true;

        if (isPossibleToSpawn == true)
        {
            if (count >= 10) { isPossibleToSpawn = false; Debug.Log("Spawner error. Is not possible to spawn, " + spawnerFunction + "spawner position is " + transform.position); }

            while (colliders.Length > 1)
            {
                count += 1;
                x = Random.Range(-spawnAreaWidth / 2, spawnAreaWidth / 2);
                z = Random.Range(-spawnAreaHeight / 2, spawnAreaHeight / 2);
                spawnPosition = new Vector3(x, 0, z);
                colliders = Physics.OverlapSphere(spawnPosition, minDistanceBetweenCharacters);
            }
        }

        spawnPosition += transform.position;
        return (spawnPosition, isPossibleToSpawn);
    }
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnAreaWidth, 0.2f, spawnAreaHeight));
    } // Draw spawn in the Editor
}
