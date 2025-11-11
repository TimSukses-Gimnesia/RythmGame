using UnityEngine;

public class MovingObstacle : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Kecepatan pergerakan obstacle (satuan/detik)")]
    public float speed = 3f; // Nilai 3f adalah default yang lebih lambat
    
    [Tooltip("Pilih tipe pergerakan: Horizontal (Kanan ke Kiri) atau Vertical (Atas ke Bawah)")]
    public ObstacleType type = ObstacleType.Horizontal;

    [Header("Path Extension")]
    [Tooltip("Jarak tambahan di luar titik lane agar obstacle muncul dan hilang di luar batas pandang.")]
    public float pathExtensionOffset = 8f; // Coba nilai 8f atau 10f agar jelas di luar layar

    public enum ObstacleType { Horizontal, Vertical }

    private PlayerMovement playerMovement;
    private Vector3 startPos; // Posisi Awal Path (Sudah Di-Offset)
    private Vector3 endPos;   // Posisi Akhir Path (Sudah Di-Offset)
    private float journeyTime = 0f;
    private float startTime;
    private bool isMoving = false;

    void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (playerMovement == null || playerMovement.movePosition.Count < 4)
        {
            Debug.LogError("PlayerMovement atau 4 posisi pergerakan tidak ditemukan! Obstacle dinonaktifkan.", this);
            enabled = false;
            return;
        }

        // --- 1. Tentukan Posisi Start & End dengan OFFSET ---
        if (type == ObstacleType.Horizontal)
        {
            // Horizontal: Bergerak dari Kanan ke Kiri
            Vector3 laneRight = playerMovement.movePosition[0].position;
            Vector3 laneLeft = playerMovement.movePosition[1].position;

            // Start: Posisi Kanan + Offset JAUH KE KANAN
            startPos = laneRight + (Vector3.right * pathExtensionOffset); 
            // End: Posisi Kiri + Offset JAUH KE KIRI
            endPos = laneLeft + (Vector3.left * pathExtensionOffset);
            
            transform.rotation = Quaternion.Euler(0f, 0f, 0f); 
        }
        else // ObstacleType.Vertical
        {
            // Vertical: Bergerak dari Atas ke Bawah
            Vector3 laneUp = playerMovement.movePosition[2].position;
            Vector3 laneDown = playerMovement.movePosition[3].position;

            // Start: Posisi Atas + Offset JAUH KE ATAS
            startPos = laneUp + (Vector3.up * pathExtensionOffset);
            // End: Posisi Bawah + Offset JAUH KE BAWAH
            endPos = laneDown + (Vector3.down * pathExtensionOffset);

            transform.rotation = Quaternion.Euler(0f, 0f, 90f); 
        }

        // --- 2. SET POSISI AWAL DAN MULAI PERGERAKAN ---
        // Setting ini akan menimpa posisi spawn dari ObstacleSpawner
        transform.position = startPos; 
        
        float distance = Vector3.Distance(startPos, endPos);
        if (speed > 0)
        {
            journeyTime = distance / speed;
            startTime = Time.time;
            isMoving = true;
        }
    }

    void Update()
    {
        if (!isMoving || journeyTime <= 0) return;

        float timeElapsed = Time.time - startTime;
        float t = timeElapsed / journeyTime;
        
        transform.position = Vector3.Lerp(startPos, endPos, t);

        if (t >= 1.0f)
        {
            Destroy(gameObject); 
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            HandlePlayerHit();
        }
    }

    private void HandlePlayerHit()
    {
        if (playerMovement != null)
        {
            float damage = playerMovement.maxHealth * 0.25f; 
            HitJudgement.health -= damage;
            Debug.Log($"Pemain terkena Obstacle! Health berkurang {damage}. Sisa health: {HitJudgement.health}");
        }
        Destroy(gameObject);
    }
}