using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation.Samples
{
    /// <summary>
    /// Listens for touch events and performs an AR raycast from the screen touch point.
    /// AR raycasts will only hit detected trackables like feature points and planes.
    ///
    /// If a raycast hits a trackable, the <see cref="placedPrefab"/> is instantiated
    /// and moved to the hit position.
    /// </summary>
    [RequireComponent(typeof(ARRaycastManager))]
    public class PlaceOnPlane : PressInputBase
    {
        [SerializeField]
        [Tooltip("Instantiates this prefab on a plane at the touch location.")]
        GameObject m_PlacedPrefab;

        /// <summary>
        /// The prefab to instantiate on touch.
        /// </summary>
        public GameObject placedPrefab
        {
            get { return m_PlacedPrefab; }
            set { m_PlacedPrefab = value; }
        }

        /// <summary>
        /// The object instantiated as a result of a successful raycast intersection with a plane.
        /// </summary>
        public GameObject spawnedObject { get; private set; }

        static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        ARRaycastManager m_RaycastManager;

        bool m_Pressed;

        protected override void OnPress(Vector3 position) => m_Pressed = true;

        protected override void OnPressCancel() => m_Pressed = false;

        private GameObject[] cats;      //只有一只猫，一直使用cats[0]
        private AudioSource audioSource;
        private Animator animator;
        bool isMoving = false;          //正在移动
        public float walkSpeed = 0.2f;  //移动速度
        Vector3 targetPosition;         //目标位置
        Vector3 lookatDirection;        //从开始位置到目标位置的 方向


        protected override void Awake()
        {
            base.Awake();
            m_RaycastManager = GetComponent<ARRaycastManager>();
        }

        void Update()
        {
            if (Pointer.current == null || m_Pressed == false)      //没有点击
            {
                if (spawnedObject != null && isMoving)      //如果已生成物体，并正在移动，则继续移动
                {
                    GoToTarget(spawnedObject, targetPosition, lookatDirection);
                }
                return;
            }

            var touchPosition = Pointer.current.position.ReadValue();

            if (m_RaycastManager.Raycast(touchPosition, s_Hits, TrackableType.PlaneWithinPolygon))      //查找 点击到的平面
            {
                // Raycast hits are sorted by distance, so the first one
                // will be the closest hit.
                var hitPose = s_Hits[0].pose;

                if (spawnedObject == null)      //如果没有生成物体，在点击位置处 生成物体
                {
                    spawnedObject = Instantiate(m_PlacedPrefab, hitPose.position, hitPose.rotation);
                    spawnedObject.transform.Rotate(new Vector3(0, 80, 0));      //cat头原来朝后，转向前面

                    cats = GameObject.FindGameObjectsWithTag("Cat");
                    animator = cats[0].GetComponent<Animator>();

                    audioSource = cats[0].GetComponent<AudioSource>();
                    audioSource.Play(0);
                }
                else        //已生成物体，向点击位置 移动
                {
                    isMoving = true;
                    targetPosition = hitPose.position;
                    lookatDirection = (targetPosition - spawnedObject.transform.position).normalized;

                    audioSource.Play(0);
                    animator.Play("Walk");
                    GoToTarget(spawnedObject, targetPosition, lookatDirection);
                }
            }
        }

        void GoToTarget(GameObject spawnedObj, Vector3 TargetPos, Vector3 lookDirection)        //物体从目前位置移动到目标位置，lookDirection是 从开始位置到目标位置的方向
        {
            Vector3 currentPosition = spawnedObj.transform.position;
            if (currentPosition == TargetPos)       //如果已到达目标位置
            {
               isMoving = false;
               animator = cats[0].GetComponent<Animator>();
               animator.Play("Idle_A");
            }
            else
            {
                spawnedObj.transform.rotation = Quaternion.LookRotation(lookDirection);     //不能用 spawnedObj.transform.LookAt(lookDirection); 会出现改变方向时 方向失控的问题

                var step = walkSpeed * Time.deltaTime;
                spawnedObj.transform.position = Vector3.MoveTowards(currentPosition, TargetPos, step);
            }
        }
    }
}
