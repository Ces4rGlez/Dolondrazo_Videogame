using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;

    [Header("Suavizado y Offset")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0.5f, -10f);

    [Header("Límites del Nivel")]
    [SerializeField] private float minX = 0f;
    [SerializeField] private float maxX = 24.5f;
    [SerializeField] private float fixedY = 0f;
    [SerializeField] private bool lockYAxis = true;

    [Header("Control de Oleadas")]
    public bool isLocked = false;

    private void Awake()
    {
        ExtendBackground();
    }

    private void Start()
    {
        // Si no se asignó en el Inspector, buscar al jugador automáticamente
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }

        ExtendBackground();
    }

    private void ExtendBackground()
    {
        GameObject bg = GameObject.Find("Fondo_Calzada");
        if (bg != null && GameObject.Find("Fondo_Calzada_Ext") == null)
        {
            SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
            float width = 38.66f;
            if (sr != null && sr.sprite != null)
            {
                width = (sr.sprite.rect.width / sr.sprite.pixelsPerUnit) * bg.transform.localScale.x;
            }

            GameObject ext = Instantiate(bg, bg.transform.position + new Vector3(width - 0.05f, 0f, 0f), bg.transform.rotation);
            ext.name = "Fondo_Calzada_Ext";
        }
    }

    private void LateUpdate()
    {
        if (target == null || isLocked) return;

        // Seguir al jugador en X respetando los límites
        float clampedX = Mathf.Clamp(target.position.x + offset.x, minX, maxX);
        float targetY = lockYAxis ? fixedY : target.position.y + offset.y;

        Vector3 desiredPosition = new Vector3(clampedX, targetY, offset.z);
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }

    public void SetLock(bool state)
    {
        isLocked = state;
    }

    public void SetMaxX(float newMaxX)
    {
        maxX = newMaxX;
    }

    public void SetMinX(float newMinX)
    {
        minX = newMinX;
    }
}

