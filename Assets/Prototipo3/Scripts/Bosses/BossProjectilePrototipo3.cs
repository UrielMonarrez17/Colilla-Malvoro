using UnityEngine;

namespace Prototipo3
{
    public enum BossProjectileKind
    {
        BadgerDebrisArc,
        DogSonicBarkWave,
        OwlRazorFeather
    }

    /// <summary>
    /// Controlador físico/visual de los proyectiles 3D de los 3 jefes:
    /// - Escombro parabólico de Marco Feral (con círculo de impacto en el suelo).
    /// - Onda de Ladrido Sónico del Perro Guardián (ralentiza y empuja a Kilo).
    /// - Pluma Afilada del Búho Rector (dardo veloz bloqueable por coberturas).
    /// </summary>
    public class BossProjectilePrototipo3 : MonoBehaviour
    {
        public BossProjectileKind kind = BossProjectileKind.OwlRazorFeather;
        public float speed = 13f;
        public float maxLifetime = 4.5f;
        public float hitRadius = 0.75f;
        public bool dealsDirectHitDamage = true;
        public float slowDuration = 2.6f;
        public float slowMultiplier = 0.50f;

        private Vector3 moveDirection = Vector3.forward;
        private Vector3 arcStart;
        private Vector3 arcEnd;
        private float arcDuration = 0.95f;
        private float arcHeight = 3.2f;
        private float elapsed = 0f;
        private GameObject impactRingObj;
        private Transform ownerRoot;
        private bool consumed = false;

        public void InitLinear(BossProjectileKind projKind, Vector3 dir, float spd, Transform owner)
        {
            kind = projKind;
            moveDirection = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
            speed = spd;
            ownerRoot = owner;
            transform.rotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        }

        public void InitArc(Vector3 startPos, Vector3 targetPos, float flightTime, float peakHeight, Transform owner)
        {
            kind = BossProjectileKind.BadgerDebrisArc;
            arcStart = startPos;
            arcEnd = targetPos;
            arcDuration = Mathf.Max(0.35f, flightTime);
            arcHeight = peakHeight;
            ownerRoot = owner;
            maxLifetime = arcDuration + 0.25f;

            // Crear anillo indicador de peligro en el suelo donde caerá el escombro
            impactRingObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            impactRingObj.name = "DebrisImpactTelegraph";
            impactRingObj.transform.position = new Vector3(arcEnd.x, arcEnd.y + 0.04f, arcEnd.z);
            impactRingObj.transform.localScale = new Vector3(2.5f, 0.02f, 2.5f);
            Collider c = impactRingObj.GetComponent<Collider>();
            if (c != null) Destroy(c);
            Renderer r = impactRingObj.GetComponent<Renderer>();
            if (r != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                Material m = new Material(sh);
                Color warnCol = new Color(1f, 0.30f, 0.05f, 0.42f);
                m.color = warnCol;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", warnCol);
                if (m.HasProperty("_Surface"))
                {
                    m.SetFloat("_Surface", 1f);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.renderQueue = 3600;
                }
                r.material = m;
            }
        }

        private void Update()
        {
            if (consumed) return;
            if (PortraitDialogueUIPrototipo3.IsConversationActive || UI.FeralGameUI.BlocksGameplay) return;

            elapsed += Time.deltaTime;
            if (elapsed >= maxLifetime)
            {
                CleanupAndDestroy();
                return;
            }

            if (kind == BossProjectileKind.BadgerDebrisArc)
            {
                float t = Mathf.Clamp01(elapsed / arcDuration);
                Vector3 pos = Vector3.Lerp(arcStart, arcEnd, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
                transform.position = pos;
                transform.Rotate(280f * Time.deltaTime, 190f * Time.deltaTime, 120f * Time.deltaTime, Space.Self);

                if (t >= 1f)
                {
                    DetonateDebrisImpact();
                }
                return;
            }

            float step = speed * Time.deltaTime;
            Vector3 nextPos = transform.position + moveDirection * step;

            if (kind == BossProjectileKind.DogSonicBarkWave)
            {
                // La onda sónica se expande conforme avanza
                float grow = 1f + elapsed * 1.45f;
                transform.localScale = new Vector3(grow, 1f, 1f);
                hitRadius = 1.1f * grow;
            }
            else
            {
                // Pluma afilada: comprobar colisión contra paredes / coberturas sólidas
                if (Physics.Raycast(transform.position, moveDirection, out RaycastHit wallHit, step + 0.25f, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (wallHit.collider != null &&
                        (ownerRoot == null || !wallHit.collider.transform.IsChildOf(ownerRoot)) &&
                        !wallHit.collider.CompareTag("Enemy") &&
                        !wallHit.collider.CompareTag("Player"))
                    {
                        CleanupAndDestroy();
                        return;
                    }
                }
            }

            transform.position = nextPos;
            CheckPlayerHit();
        }

        private void CheckPlayerHit()
        {
            if (SafeRefugeZone.IsPlayerInsideSafeRefuge) return;

            HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            if (hc == null || hc.IsDead) return;

            Vector3 pPos = hc.transform.position + Vector3.up * 0.28f;
            float distXZ = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(pPos.x, pPos.z));
            float dy = Mathf.Abs(transform.position.y - pPos.y);

            if (distXZ <= hitRadius && dy <= 1.35f)
            {
                consumed = true;
                if (hc is HamsterControllerPrototipo3 hcp && slowDuration > 0f)
                {
                    hcp.ApplyExternalSlow(slowMultiplier, slowDuration);
                }

                // Si Kilo está haciendo Dash justo al cruzar una onda sónica, la esquiva sin perder vida
                if (dealsDirectHitDamage && !(kind == BossProjectileKind.DogSonicBarkWave && hc.IsDashing))
                {
                    hc.ReceiveDamage();
                    BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.95f, 0.2f, 0.1f), 0.28f);
                }
                CleanupAndDestroy();
            }
        }

        private void DetonateDebrisImpact()
        {
            consumed = true;
            if (!SafeRefugeZone.IsPlayerInsideSafeRefuge)
            {
                HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (hc != null && !hc.IsDead)
                {
                    float d = Vector3.Distance(new Vector3(arcEnd.x, hc.transform.position.y, arcEnd.z), hc.transform.position);
                    if (d <= 1.65f)
                    {
                        if (hc is HamsterControllerPrototipo3 hcp) hcp.ApplyExternalSlow(0.55f, 2.0f);
                        hc.ReceiveDamage();
                        BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(1f, 0.35f, 0.05f), 0.32f);
                    }
                }
            }
            CleanupAndDestroy();
        }

        private void CleanupAndDestroy()
        {
            if (impactRingObj != null) Destroy(impactRingObj);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (impactRingObj != null) Destroy(impactRingObj);
        }
    }
}
