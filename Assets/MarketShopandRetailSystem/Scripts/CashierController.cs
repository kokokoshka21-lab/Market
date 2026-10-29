using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace MarketShopandRetailSystem
{
    public class CashierController : MonoBehaviour
    {
        private NavMeshAgent agent;
        public string Name;
        private Animator animator;
        private Transform target;
        private float lastCheckTime = 0;
        public bool isworking = false;
        private bool gotMySalaryToday = false;
        private CashRegister currentCashRegister;


        public bool IsAttending(CashRegister register)
        {
            return register != null && register.pointPlayerStand != null && isActiveAndEnabled
                && currentCashRegister == register && agent != null && agent.isOnNavMesh
                && Vector3.Distance(transform.position, register.pointPlayerStand.position) <= agent.stoppingDistance + 0.2f;
        }

        private void ReleaseRegister()
        {
            if (currentCashRegister != null && currentCashRegister.CurrentCashier == this)
                currentCashRegister.CurrentCashier = null;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            ReleaseRegister();
            currentCashRegister = null;
            target = null;
            isworking = false;
        }

        private void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            isworking = false;
            target = null;
        }

        IEnumerator ScanAndGetPayment()
        {
            isworking = true;
            animator.SetTrigger("Work");
            for (int i = 0; i < currentCashRegister.productsofCustomer.Count; i++)
            {
                currentCashRegister.Scan(currentCashRegister.productsofCustomer[i]);
                yield return new WaitForSeconds(0.6f);
            }
            if(currentCashRegister != null)
            {
                currentCashRegister.Click_Button_Process_ByCashier();
            }
            yield return new WaitForSeconds(1);
            ReleaseRegister();
            target = null;
            currentCashRegister = null;
            isworking = false;
        }

        private void Start()
        {
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }

        [SerializeField] float faceEpsilon = 2f;

        void FaceTargetSmooth(Transform target)
        {
            if (target == null) return;

            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            float angle = Quaternion.Angle(transform.rotation, targetRot);

            if (angle > faceEpsilon)
            {
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    1000
                );
            }
        }

        void Update()
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
            if (isworking)
            {
                if(target != null)
                {
                    return;
                }
                else
                {
                    isworking = false;
                    target = null;
                    return;
                }
            }
            if (DayNightManager.Instance != null)
            {
                if (!DayNightManager.Instance.isDark && !gotMySalaryToday)
                {
                    gotMySalaryToday = true;
                    AdvancedGameManager.Instance.Spend(AdvancedGameManager.Instance.cashierDailySalary);
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
                        if(currentCashRegister.CurrentCashier != null && currentCashRegister.CurrentCashier != this)
                        {
                            agent.isStopped = true;
                            agent.updateRotation = true;
                            target = null;
                            currentCashRegister = null;
                        }
                        else
                        {
                            agent.isStopped = true;
                            agent.updateRotation = false;
                            FaceTargetSmooth(currentCashRegister.transform);
                            currentCashRegister.CurrentCashier = this;
                            if (currentCashRegister.isBusy)
                            {
                                StartCoroutine(ScanAndGetPayment());
                            }
                        }
                    }
                    else if (agent.hasPath && (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid))
                    {
                        agent.isStopped = true;
                        target = null;
                        currentCashRegister = null;
                    }
                    else if (target != null && agent.pathStatus == NavMeshPathStatus.PathComplete)
                    {
                        agent.updateRotation = true;
                        agent.SetDestination(target.position);
                    }
                }
                else
                {
                    InventoryManager.Instance.CheckCurrentEquipmentList();
                    CashRegister cashRegister = InventoryManager.Instance.CurrentEquipmentList.Where(x => x.CompareTag("Item") && x.GetComponent<CashRegister>() != null && !x.GetComponent<CashRegister>().selfService && !x.GetComponent<CashRegister>().isBusy && (x.GetComponent<CashRegister>().CurrentCashier == null || x.GetComponent<CashRegister>().CurrentCashier == this)).OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).Select(x => x.GetComponent<CashRegister>()).FirstOrDefault();
                    if (cashRegister != null && !cashRegister.isBusy)
                    {
                        target = cashRegister.pointPlayerStand.transform;
                        NavMeshPath path = new NavMeshPath();
                        bool pathFound = NavMesh.CalculatePath(transform.position, target.position, NavMesh.AllAreas, path);
                        if (pathFound && path.status == NavMeshPathStatus.PathComplete)
                        {
                            currentCashRegister = cashRegister;
                            agent.isStopped = false;
                            agent.SetDestination(target.position);
                        }
                    }
                }
            }
        }
    }
}
