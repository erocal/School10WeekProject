using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float lifeTime = 10f;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem deathParticlePrefab;
    [SerializeField] private float particleDestroyDelay = 2f;

    private EnemyPool pool;
    private bool isAlive;
    private Coroutine lifeCoroutine;

    public void SetPool(EnemyPool enemyPool)
    {
        pool = enemyPool;
    }

    public void BeginLife()
    {
        isAlive = true;

        if (lifeCoroutine != null)
        {
            StopCoroutine(lifeCoroutine);
        }

        lifeCoroutine = StartCoroutine(LifeTimer());
    }

    private IEnumerator LifeTimer()
    {
        yield return new WaitForSeconds(lifeTime);
        Die();
    }

    private void Die()
    {
        if (!isAlive) return;

        isAlive = false;

        Vector3 deathPosition = transform.position;
        Quaternion deathRotation = transform.rotation;

        if (deathParticlePrefab != null)
        {
            ParticleSystem particle = Instantiate(
                deathParticlePrefab,
                deathPosition,
                deathRotation
            );

            particle.Play();
            Destroy(particle.gameObject, particleDestroyDelay);
        }

        pool.ReturnEnemy(this);
    }
}