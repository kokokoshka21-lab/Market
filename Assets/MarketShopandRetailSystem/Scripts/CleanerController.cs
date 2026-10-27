using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace MarketShopandRetailSystem
{
    public class CleanerController : MonoBehaviour
    {
        private NavMeshAgent agent;
        public string Name;
        private Animator animator;
        private Transform target;
        private float lastCheckTime = 0;
        public bool isCleaning = false;
        private bool gotMySalaryToday = false;

        private void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
        }

        IEnumerator CleanIt()
        {
            isCleaning = true;
            animator.SetTrigger("Clean");
            AudioManager.Instance.Play_Audio_Cleaning();
            yield return new WaitForSeconds(4);
            if(target != null)
            {
                target.GetComponent<ItemScript>().Interact();
            }
            yield return new WaitForSeconds(2);
            target = null;
            isCleaning = false;
        }

        private void Start()
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        bool IsPathValid(Vector3 targetPosition)
        {
            NavMeshPath path = new NavMeshPath();
            agent.CalculatePath(targetPosition, path);
            return path.status == NavMeshPathStatus.PathComplete;
        }

        // Update is called once per frame
        void Update()
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
            if (isCleaning) return;
            if (DayNightManager.Instance != null)
            {
                if (!DayNightManager.Instance.isDark && !gotMySalaryToday)
                {
                    gotMySalaryToday = true;
                    AdvancedGameManager.Instance.Spend(AdvancedGameManager.Instance.cleanerDailySalary);
                }
                else if (DayNightManager.Instance.isDark)
                {
                    gotMySalaryToday = false;
                }
            }
            if (Time.time > lastCheckTime + 0.5f)
            {
                lastCheckTime = Time.time;
                if (agent.hasPath && target != null)
                {
                    float distance = 0;
                    if (target != null)
                    {
                        distance = Vector3.Distance(transform.position, target.position);
                    }
                    if (distance <= agent.stoppingDistance)
                    {
                        agent.isStopped = true;
                        StartCoroutine(CleanIt());
                    }
                    else if (agent.hasPath && (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid))
                    {
                        agent.isStopped = true;
                        target = null;
                    }
                    else
                    {
                        agent.SetDestination(target.transform.position);
                    }
                }
                else
                {
                    Transform closestDirt = GameObject.FindObjectsByType<ItemScript>(FindObjectsSortMode.None).Where(x => x.interactionType == InteractionType.Clean).OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).Select(x => x.transform).FirstOrDefault();
                    if (closestDirt != null)
                    {
                        // Let's go there!
                        if (IsPathValid(closestDirt.position))
                        {
                            target = closestDirt.transform;
                            agent.isStopped = false;
                            agent.SetDestination(target.position);
                        }
                    }
                }
            }
        }
    }
}
