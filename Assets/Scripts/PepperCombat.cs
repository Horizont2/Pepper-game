using System.Collections;
using UnityEngine;

public class PepperCombat : MonoBehaviour
{
    [Header("Combat Settings")]
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float attackRate = 2f;
    [SerializeField] private float attackRange = 0.6f;
    [SerializeField] private float attackDuration = 0.4f; // Час, поки гравець "заморожений" під час атаки

    [Header("Game Feel Settings")]
    [SerializeField] private float hitStopDuration = 0.05f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeMagnitude = 0.15f;

    [Header("Animation Names")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackAnimName = "Pepper_Attack";

    [Header("References")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private LayerMask enemyLayers;

    // Цю змінну читатиме PepperController, щоб знати, чи можна рухатись
    public bool IsAttacking { get; private set; }

    private float nextAttackTime = 0f;
    private int attackAnimHash;

    private void Start()
    {
        if (animator != null)
        {
            attackAnimHash = Animator.StringToHash(attackAnimName);
        }
    }

    private void Update()
    {
        // Якщо вже атакуємо — блокуємо будь-які нові спроби удару
        if (IsAttacking) return;

        if (Time.time >= nextAttackTime)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                StartCoroutine(AttackRoutine());
            }
        }
    }

    private IEnumerator AttackRoutine()
    {
        // 1. Починаємо атаку (блокуємо рух)
        IsAttacking = true;

        // 2. Програємо анімацію
        if (animator != null)
        {
            animator.Play(attackAnimHash, 0, 0f);
        }

        // 3. Шукаємо ворогів у зоні ураження
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        bool hitSuccessful = false;

        foreach (Collider2D enemy in hitEnemies)
        {
            HealthComponent health = enemy.GetComponent<HealthComponent>();
            if (health != null)
            {
                health.TakeDamage(attackDamage);

                // Якщо використовуєш скрипт відкидання, розкоментуй рядок нижче:
                // enemy.GetComponent<KnockbackFeedback>()?.ApplyKnockback(transform); 

                hitSuccessful = true;
            }
        }

        // 4. Ефекти удару (Game Feel)
        if (hitSuccessful && GameFeelManager.Instance != null)
        {
            GameFeelManager.Instance.TriggerHitImpact(hitStopDuration, shakeDuration, shakeMagnitude);
        }

        // 5. ЧЕКАЄМО завершення атаки (час, поки Перець стоїть на місці)
        yield return new WaitForSeconds(attackDuration);

        // 6. Закінчуємо атаку, дозволяємо рух і встановлюємо кулдаун
        IsAttacking = false;
        nextAttackTime = Time.time + 1f / attackRate;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}