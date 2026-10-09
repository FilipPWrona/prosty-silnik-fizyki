using UnityEngine;


public class CirclePhysics : MonoBehaviour
{
    public float speed = 5f;
    public float radius = 1f;


    public Vector2 velocity;

    // Parametry systemu fizyki (wspólne - odczytywane z pierwszego obiektu)
    [Header("System")]
    public Vector2 gravity = new Vector2(0f, -9.81f);
    public float airFrictionK = 0.5f;
    public float collisionMargin = 0.01f;

    private static CirclePhysics[] circles = null;
    private static int lastUpdateFrame = -1;


    void Start()
    {
        circles = FindObjectsOfType<CirclePhysics>();
        radius = Random.Range(.2f, 1f);
        speed = Random.Range(1f, 5f);

        // set size according to radius
        transform.localScale = radius * 2f * Vector3.one;

        // Initialize a random velocity for the circle
        velocity = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized * speed;
    }


    void Update()
    {
        // Jeden obiekt zarządza całym systemem w danej klatce
        // Pozostałe instancje pomijają Update (trójkąt n*(n-1)/2 wymaga jednego przejścia)
        if (Time.frameCount == lastUpdateFrame) return;
        lastUpdateFrame = Time.frameCount;

        float dt = Time.deltaTime;

        // Odczytaj parametry systemu z tego obiektu
        Vector2 grav = gravity;
        float fricK = airFrictionK;
        float margin = collisionMargin;
        float fricFactor = Mathf.Exp(-fricK * dt);

        // Faza 1: Grawitacja + tarcie o powietrze + ruch
        for (int i = 0; i < circles.Length; i++)
        {
            CirclePhysics c = circles[i];

            // Grawitacja - stałe przyspieszenie dodawane do prędkości
            c.velocity += grav * dt;

            // Tarcie o powietrze - dążenie prędkości do (0,0): v *= exp(-k·dt)
            c.velocity *= fricFactor;

            // Move the circle based on its velocity
            c.transform.Translate(c.velocity * dt);
        }


        // Faza 2: Kolizje między obiektami - trójkąt n*(n-1)/2
        for (int i = 0; i < circles.Length; i++)
        {
            for (int j = i + 1; j < circles.Length; j++)
            {
                ResolveCollision(circles[i], circles[j], margin);
            }
        }


        // Faza 3: Odbicia od ścian ekranu
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            for (int i = 0; i < circles.Length; i++)
            {
                CheckScreenBoundaries(circles[i], mainCamera);
            }
        }
    }


    static void ResolveCollision(CirclePhysics a, CirclePhysics b, float margin)
    {
        Vector2 offset = (Vector2)b.transform.position - (Vector2)a.transform.position;
        float distance = offset.magnitude;
        float minDistance = a.radius + b.radius;


        if (distance < minDistance && distance > 0.0001f)
        {
            // Handle collision by reflecting velocities
            Vector2 normal = offset / distance;

            // collision solving - push object away by the half of overlap
            // dodatkowy margines zapobiega "ślizganiu" się obiektów
            float overlap = minDistance - distance + margin;

            Vector2 push = overlap * normal * 0.5f;
            a.transform.position -= (Vector3)push;
            b.transform.position += (Vector3)push;

            // Odbij prędkości tylko gdy obiekty zbliżają się do siebie
            float relVelAlongNormal = Vector2.Dot(b.velocity - a.velocity, normal);
            if (relVelAlongNormal < 0f)
            {
                a.velocity = Vector2.Reflect(a.velocity, normal);
                b.velocity = Vector2.Reflect(b.velocity, normal);
            }
        }
    }


    static void CheckScreenBoundaries(CirclePhysics c, Camera mainCamera)
    {
        Vector2 position = c.transform.position;
        Vector2 cameraPos = mainCamera.transform.position;

        float cameraHalfWidth = mainCamera.orthographicSize * mainCamera.aspect;
        float cameraHalfHeight = mainCamera.orthographicSize;

        float minX = cameraPos.x - cameraHalfWidth;
        float maxX = cameraPos.x + cameraHalfWidth;
        float minY = cameraPos.y - cameraHalfHeight;
        float maxY = cameraPos.y + cameraHalfHeight;

        // Lewa ściana
        if (position.x - c.radius < minX)
        {
            position.x = minX + c.radius;
            if (c.velocity.x < 0f)
            {
                c.velocity.x = -c.velocity.x;
            }
        }
        // Prawa ściana
        else if (position.x + c.radius > maxX)
        {
            position.x = maxX - c.radius;
            if (c.velocity.x > 0f)
            {
                c.velocity.x = -c.velocity.x;
            }
        }

        // Dolna ściana
        if (position.y - c.radius < minY)
        {
            position.y = minY + c.radius;
            if (c.velocity.y < 0f)
            {
                c.velocity.y = -c.velocity.y;
            }
        }
        // Górna ściana
        else if (position.y + c.radius > maxY)
        {
            position.y = maxY - c.radius;
            if (c.velocity.y > 0f)
            {
                c.velocity.y = -c.velocity.y;
            }
        }

        c.transform.position = new Vector3(position.x, position.y, c.transform.position.z);
    }
}
