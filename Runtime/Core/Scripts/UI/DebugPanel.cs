using System.Diagnostics;
using TMPro;
using UnityEngine;

namespace Twinny.Helpers
{

    /// <summary>
    /// This class manages the Degub Panel on UI
    /// </summary>
public class DebugPanel : MonoBehaviour
{

        #region Singleton Instance
        private static DebugPanel _instance;
        public static DebugPanel Instance { get { return _instance; } }
        #endregion

        #region Fields

        [SerializeField] bool _startVisible = true;
        public bool visible {  get => _startVisible; set { _startVisible = value; _debugPanel.SetActive(value); }}
        [SerializeField] bool _startFolded = true;

        [SerializeField] bool _followUser = true;
        [Range(0.1f, 1f)]
        [SerializeField] float _followSpeed = 1f;

        [Header("Components")]
        [SerializeField] private GameObject _debugPanel;
        [SerializeField] private TextMeshProUGUI TMP_DebugText;
        [SerializeField] private bool _showMessages = false;
        [SerializeField] private bool _showWarnings = true;
        [SerializeField] private bool _showErrors = true;
        private Transform _transform;
        private Transform _mainCameraTransform;
        
        public Animator animator;
        #endregion


        #region MonoBehaviour Methods
        private void OnEnable()
        {
            Application.logMessageReceived += Debug;
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= Debug;
        }

        private void Awake()
        {
            if (!_instance) _instance = this;
            else
                Destroy(gameObject);

            _transform = transform;
        }

        private void Start()
        {
            animator = GetComponent<Animator>();
        
            _mainCameraTransform = Camera.main.transform;

            _debugPanel.SetActive(_startVisible);


            animator?.SetBool("folded", _startFolded);

        }

        private void Update()
        {
            if (_followUser) FollowUser();
        }

        private void OnDestroy()
        {

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sends a debug log to Debug Panel
        /// </summary>
        /// <param name="condition">Message</param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>

        public static void Debug(string condition, string stackTrace = "", LogType type = LogType.Log)
        {
            if(Instance == null) return;

            string msg = null;

            // Se o stackTrace não for fornecido, tente gerar um a partir de uma exceção
            if (string.IsNullOrEmpty(stackTrace))
            {
                stackTrace = Instance.GetStackTrace();
            }


            switch (type)
            {
                case LogType.Error:
                    msg = Instance._showErrors ? $"\n<color=#ff0000>{condition}\n{stackTrace}</color>":null;
                    break;
                case LogType.Warning:
                    msg = Instance._showWarnings ? $"\n<color=#F77E00>{condition}</color>" : null;
                    break;
                case LogType.Log:
                    msg = Instance._showMessages ? $"\n<color=#ffffff>{condition}</color>" : null;
                    break;
                    default:
                    msg = $"\n<color=#8184BE>{condition}</color>";
                    break;
                        

            }

            
            if(msg != null)
            Instance.TMP_DebugText.text += msg;
        }


        /// <summary>
        /// This Method captures a stack trace manually
        /// </summary>
        /// <returns></returns>
        private string GetStackTrace()
        {
            var stackTrace = new StackTrace(true); // "true" permite incluir a linha do código
            return stackTrace.ToString(); // Retorna o stack trace com número de linha e nome do arquivo
        }

        /// <summary>
        /// This method makes HUD follow the user
        /// </summary>
        /// 
        private void FollowUser()
        {
            Quaternion targetRotation = Quaternion.Euler(0f, _mainCameraTransform.eulerAngles.y, 0f);

            _transform.rotation = Quaternion.Slerp(_transform.rotation, targetRotation, (10f * _followSpeed) * Time.deltaTime);

            Vector3 desiredPosition = _mainCameraTransform.position;
            desiredPosition.y = 0f;

            _transform.position = desiredPosition;
        }


        [ContextMenu("Set Fold|Unfold")]
        public void SetFold()
        {
            bool folded = animator.GetBool("folded");
            animator.SetBool("folded", !folded);
        }


        #endregion

    }
}

