using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;

namespace MarketShopandRetailSystem
{
    public class CivilianController : MonoBehaviour
    {
        private NavMeshAgent agent;
        public string Name;
        private Animator animator;
        private bool hasPath = false;
        private Transform target;
        private float lastCheckTime = 0;
        public string[] Conversations;
        public string[] ThiefConversations;
        private float LastTimeSpeking = -5;
        public Status currentStatus;
        public List<GameObject> myBasket;
        public bool isThief = false;

        public void SpeakwithHero()
        {
            if(currentStatus == Status.IsEscaping)
            {
                // Thief was escaping and player cought him and started to talk:
                ApologizeAndPay();
            }
            else
            {
                // Normal NPC Talk
                LastTimeSpeking = Time.time;
                agent.isStopped = true;
                animator.SetTrigger("Talk" + UnityEngine.Random.Range(0, 2).ToString());
                string conversation = Conversations[Random.Range(0, Conversations.Length)];
                SpeechManager.instance.Show_Speach(conversation, Name, gameObject);
            }
        }

        public void ApologizeAndPay()
        {
            // Apologize!
            LastTimeSpeking = Time.time;
            agent.isStopped = true;
            animator.SetTrigger("Talk" + UnityEngine.Random.Range(0, 2).ToString());
            string conversation = ThiefConversations[Random.Range(0, ThiefConversations.Length)];
            SpeechManager.instance.Show_Speach(conversation, Name, gameObject);
            // Pay the Basket Products!
            int TotalPrice = 0;
            for (int i = 0; i < myBasket.Count; i++)
            {
                TotalPrice = TotalPrice + myBasket[i].GetComponent<SellableObject>().GetSellingPrice();
            }
            int money = SaveLoadManager.Instance.GetMoney();
            money = money + TotalPrice;
            SaveLoadManager.Instance.SetMoney(money);
            GameCanvas.Instance.UpdateStatus();
            AudioManager.Instance.Play_audioClip_CashRegisterResult(true);
            // Go to normal Status and Carry on
            GoAfterShopping();
        }

        private void OnEnable()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            StartCoroutine(SetTargetAndWalk());
        }

        public int MakePayment(int total)
        {
            List<int> banknotes = new List<int> { 5, 10, 20, 50, 100 };
            List<int> validBanknotes = banknotes.FindAll(note => note > total);
            int maxBanknote = banknotes[banknotes.Count - 1];
            for (float overpayMultiplier = 1.1f; overpayMultiplier <= 2.0f; overpayMultiplier += 0.1f)
            {
                float overpayValue = Mathf.Ceil(total * overpayMultiplier / 5) * 5;
                if (!validBanknotes.Contains(Mathf.RoundToInt(overpayValue)))
                {
                    validBanknotes.Add(Mathf.RoundToInt(overpayValue));
                }
            }
            int randomIndex = UnityEngine.Random.Range(0, validBanknotes.Count);
            return validBanknotes[randomIndex];
        }

        private void Start()
        {
            agent.enabled = false;
            transform.position = CityPointsManager.Instance.GetRandomSpawnPoint();
            agent.enabled = true;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        }
        bool isGrabbing = false;

        IEnumerator GrabIt()
        {
            isGrabbing = true;
            agent.isStopped = true;
            animator.SetTrigger("Grab");
            yield return new WaitForSeconds(2f);
            isGrabbing = false;
            InventoryManager.Instance.CheckCurrentEquipmentList();
            CashRegister cashRegister = CashRegister.FindCustomerRegisters().OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault();
            SellableObject[] sellableObjects = GameObject.FindObjectsOfType<SellableObject>().Where(x => x.isAvailable && x.GetSellingPrice() < x.BuyingPrice * AdvancedGameManager.Instance.ProductsProfitLimitForBeingExpensiveTimes && Vector3.Distance(x.transform.position, transform.position) < 30).ToArray();
            if (cashRegister != null)
            {
                if (sellableObjects.Length > 0)
                {
                    int decision = Random.Range(0, 2);
                    if (decision < 1 && AdvancedGameManager.Instance.isShopOpen)
                    {
                        SellableObject selectedObject = sellableObjects[Random.Range(0, sellableObjects.Length)];
                        target = selectedObject.transform;
                        currentStatus = Status.isShopping;
                        GoToTarget();
                    }
                    else
                    {
                        if (myBasket.Count > 0)
                        {
                            GoToQueueForPayment(cashRegister);
                        }
                        else
                        {
                            ClearMyBasket();
                            GoAfterShopping();
                        }

                    }
                }
                else
                {
                    if (myBasket.Count > 0)
                    {
                        GoToQueueForPayment(cashRegister);
                    }
                    else
                    {
                        ClearMyBasket();
                        GoAfterShopping();
                    }
                }
            }
            else
            {
                ClearMyBasket();
                GoAfterShopping();
            }
        }
        float lastTimeDirt = 0;

        void Update()
        {
            animator.SetFloat("Speed", agent.velocity.magnitude);
            if (isGrabbing) return;
            if (Time.time < LastTimeSpeking + 4)
            {
                Vector3 lookDirection = HeroPlayerScript.Instance.transform.position - transform.position;
                lookDirection.Normalize();
                agent.isStopped = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), 2 * Time.deltaTime);
            }
            if (Time.time < LastTimeSpeking + 4) return;

            if (Time.time > lastCheckTime + 0.5f)
            {
                lastCheckTime = Time.time;
                if (isInShop && Time.time > lastTimeDirt + 10)
                {
                    lastTimeDirt = Time.time;
                    if (Random.Range(0, 3) == 0 && currentStatus != Status.isInQueue)
                    {
                        AdvancedGameManager.Instance.CreateDust(transform);
                    }
                }
                if (myBasket.Count == 0 && currentStatus == Status.isInQueue)
                {
                    GoAfterShopping();
                }
                if (agent.hasPath)
                {
                    float distance = 0;
                    if (currentStatus == Status.isShopping)
                    {
                        if (target == null || target.GetComponent<SellableObject>().putPoint == null)
                        {
                            if (myBasket.Count > 0)
                            {
                                InventoryManager.Instance.CheckCurrentEquipmentList();
                                CashRegister cashRegister = CashRegister.FindCustomerRegisters().OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault();
                                if (cashRegister != null)
                                {
                                    GoToQueueForPayment(cashRegister);
                                }
                                else
                                {
                                    ClearMyBasket();
                                    GoAfterShopping();
                                }
                            }
                        }
                        else
                        {
                            distance = Vector3.Distance(transform.position, target.GetComponent<SellableObject>().putPoint.MainContainer.transform.position) - 0.5f;
                        }
                    }
                    else
                    {
                        if (target != null)
                        {
                            distance = Vector3.Distance(transform.position, target.position);
                        }
                        else
                        {
                            if (currentStatus == Status.isInQueue)
                            {
                                ClearMyBasket();
                                GoAfterShopping();
                            }
                        }
                    }

                    if (distance <= agent.stoppingDistance)
                    {
                        hasPath = false;
                        agent.isStopped = true;
                        if (currentStatus == Status.isGoingHome)
                        {
                            gameObject.SetActive(false);
                            return;
                        }

                        if (currentStatus == Status.isWalkingAround)
                        {
                            InventoryManager.Instance.CheckCurrentEquipmentList();
                            CashRegister cashRegisters = CashRegister.FindCustomerRegisters().OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault();
                            SellableObject[] sellableObjects = GameObject.FindObjectsOfType<SellableObject>().Where(x => x.isAvailable && x.GetSellingPrice() < x.BuyingPrice * AdvancedGameManager.Instance.ProductsProfitLimitForBeingExpensiveTimes).ToArray();

                            if (cashRegisters != null && sellableObjects != null && sellableObjects.Length > 0)
                            {
                                int decision = Random.Range(0, 100);
                                if (decision <= AdvancedGameManager.Instance.NPCShoppingPercentageRate && AdvancedGameManager.Instance.isShopOpen)
                                {
                                    SellableObject selectedObject = sellableObjects[Random.Range(0, sellableObjects.Length)];
                                    target = selectedObject.transform;
                                    currentStatus = Status.isShopping;
                                    GoToTarget();
                                }
                                else
                                {
                                    StartCoroutine(SetTargetAndWalk());
                                }
                            }
                            else
                            {
                                StartCoroutine(SetTargetAndWalk());
                            }
                        }
                        else if (currentStatus == Status.isShopping)
                        {
                            if (target != null && target.GetComponent<SellableObject>().putPoint != null)
                            {
                                myBasket.Add(target.gameObject);
                                target.GetComponent<SellableObject>().putPoint.GetComponentInParent<ArrayableArea>().RemoveItem(target.GetComponent<SellableObject>().putPoint.ID);
                                target.GetComponent<SellableObject>().isAvailable = false;
                                target.GetComponent<SellableObject>().putPoint.Floor.CheckPriceOnTag();
                                target.parent = this.transform;
                                target.gameObject.SetActive(false);
                                StartCoroutine(GrabIt());
                            }
                            else
                            {
                                if (myBasket.Count == 0)
                                {
                                    GoAfterShopping();
                                }
                                else
                                {
                                    InventoryManager.Instance.CheckCurrentEquipmentList();
                                    CashRegister cashRegister = CashRegister.FindCustomerRegisters().OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault();
                                    if (cashRegister != null)
                                    {
                                        GoToQueueForPayment(cashRegister);
                                    }
                                    else
                                    {
                                        ClearMyBasket();
                                        GoAfterShopping();
                                    }
                                }
                            }
                        }
                        else if (currentStatus == Status.isInQueue)
                        {
                            CashRegister currentCashRegister = target.GetComponentInParent<CashRegister>();
                            if (currentCashRegister != null && !currentCashRegister.isBusy)
                            {
                                for (int i = 0; i < myBasket.Count; i++)
                                {
                                    if (myBasket[i] == null)
                                    {
                                        myBasket.RemoveAt(i);
                                        i--;
                                    }
                                }
                                currentCashRegister.PutProductsYouGrabbed(this, myBasket);
                            }
                        }
                        else if(currentStatus == Status.IsEscaping)
                        {
                            // Escaped and reached the destination without being cought! The products are mine!
                            ClearMyBasket();
                            GoAfterShopping();
                        }
                    }
                    else
                    {
                        agent.SetDestination(agent.destination);
                        agent.isStopped = false;
                    }
                }
                else if (currentStatus == Status.isInQueue)
                {
                    CashRegister currentCashRegister = target.GetComponentInParent<CashRegister>();
                    if (currentCashRegister != null && !currentCashRegister.isBusy)
                    {
                        for (int i = 0; i < myBasket.Count; i++)
                        {
                            if (myBasket[i] == null)
                            {
                                myBasket.RemoveAt(i);
                                i--;
                            }
                        }
                        if (myBasket.Count > 0)
                        {
                            currentCashRegister.PutProductsYouGrabbed(this, myBasket);
                        }
                        else
                        {
                            ClearMyBasket();
                            GoAfterShopping();
                        }
                    }
                }
                if (hasPath && agent.hasPath && (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid) && (currentStatus == Status.isInQueue || currentStatus == Status.isShopping))
                {
                    ClearMyBasket();
                    GoAfterShopping();
                }
            }
        }

        public void ClearMyBasket()
        {
            for (int i = 0; i < myBasket.Count; i++)
            {
                Destroy(myBasket[i].gameObject);
            }
            myBasket.Clear();
        }

        public void GoToHome(Transform homeDoor)
        {
            currentStatus = Status.isGoingHome;
            hasPath = false;
            SetTargetAndWalkToHome(homeDoor);
        }

        public void OutFromHome()
        {
            gameObject.SetActive(true);
            currentStatus = Status.isWalkingAround;
            hasPath = false;
            StartCoroutine(SetTargetAndWalk());
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player") && HeroPlayerScript.Instance.currentCashRegister == null && currentStatus == Status.isWalkingAround)
            {
                StartCoroutine(SetTargetAndWalk());
            }
            if (other.CompareTag("Shop"))
            {
                isInShop = false;
            }
        }

        public void GoSomewhere()
        {
            if (currentStatus == Status.isWalkingAround)
            {
                target = CityPointsManager.Instance.GetRandomTargetPoint();
                agent.speed = Random.Range(1.1f, 1.2f);
                agent.isStopped = false;
                agent.SetDestination(target.position);
                hasPath = true;
            }
        }

        public void GoAfterShopping()
        {
            ClearMyBasket();
            currentStatus = Status.isWalkingAround;
            lastCheckTime = Time.time;
            target = CityPointsManager.Instance.GetRandomTargetPoint();
            agent.speed = Random.Range(1.1f, 1.2f);
            agent.isStopped = false;
            agent.SetDestination(target.position);
            hasPath = true;
        }

        public IEnumerator SetTargetAndWalk()
        {
            if (gameObject.activeSelf)
            {
                if (currentStatus == Status.isGoingHome)
                {
                    agent.speed = Random.Range(1.1f, 1.2f);
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                    hasPath = true;
                }
                else
                {
                    currentStatus = Status.isWalkingAround;
                    float time = Random.Range(2, 4);
                    agent.isStopped = true;
                    lastCheckTime = Time.time + time;
                    yield return new WaitForSeconds(time);
                    agent.isStopped = false;
                    target = CityPointsManager.Instance.GetRandomTargetPoint();
                    agent.speed = Random.Range(1.1f, 1.2f);
                    agent.isStopped = false;
                    agent.SetDestination(target.position);
                    hasPath = true;
                }
            }
        }

        bool isInShop = false;
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Shop") && !isInShop)
            {
                isInShop = true;
                lastTimeDirt = Time.time;
            }
        }


        public void GoToTarget(bool isEscaping = false)
        {
            if (gameObject.activeSelf)
            {
                agent.isStopped = false;
                if(isEscaping)
                {
                    agent.speed = Random.Range(2.5f, 3.5f);
                }
                else
                {
                    agent.speed = Random.Range(1.1f, 1.2f);
                }
                agent.isStopped = false;
                if (currentStatus == Status.isShopping)
                {
                    if (target == null || target.GetComponent<SellableObject>().putPoint == null)
                    {
                        if (myBasket.Count > 0)
                        {
                            InventoryManager.Instance.CheckCurrentEquipmentList();
                            CashRegister cashRegister = CashRegister.FindCustomerRegisters().OrderBy(x => Vector3.Distance(transform.position, x.transform.position)).FirstOrDefault();
                            if (cashRegister != null)
                            {
                                GoToQueueForPayment(cashRegister);
                            }
                            else
                            {
                                ClearMyBasket();
                                GoAfterShopping();
                            }
                        }
                    }
                    else
                    {
                        agent.SetDestination(target.GetComponent<SellableObject>().putPoint.MainContainer.transform.position);
                    }
                }
                else
                {
                    agent.SetDestination(target.position);
                }
                hasPath = true;
            }
        }

        public void GoToQueueForPayment(CashRegister cashRegister)
        {
            if(!isThief)
            {
                // Choose once on entering checkout, rather than changing lanes on every update.
                CashRegister selected = CashRegister.SelectForCustomer(CashRegister.FindCustomerRegisters(),
                    transform.position, AdvancedGameManager.Instance.selfServiceCheckoutChance, Random.value);
                if (selected == null) return;
                target = selected.pointCustomersCome.transform;
                currentStatus = Status.isInQueue;
                GoToTarget();
            }
            else
            {
                // I am a thief! Escape without paying! Leave the shop!
                // Lets select a destination out of shop!
                target = CityPointsManager.Instance.GetRandomTargetPoint();
                currentStatus = Status.IsEscaping;
                GoToTarget(true);
            }
        }

        private void SetTargetAndWalkToHome(Transform home)
        {
            if (gameObject.activeSelf)
            {
                agent.isStopped = false;
                target = home;
                agent.speed = Random.Range(1.1f, 1.2f);
                agent.isStopped = false;
                agent.SetDestination(target.position);
                hasPath = true;
            }
        }
    }

    public enum Status
    {
        isWalkingAround,
        isGoingHome,
        isShopping,
        isInQueue,
        IsEscaping
    }
}