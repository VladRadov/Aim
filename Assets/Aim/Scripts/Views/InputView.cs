using System;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Aim.Views
{
    public sealed class InputView : MonoBehaviour
    {
        [SerializeField] InputActionAsset inputActions;

        readonly Subject<Vector2> _lookSubject = new();
        readonly Subject<Unit> _attackSubject = new();
        readonly CompositeDisposable _disposables = new();

        InputAction _lookAction;
        InputAction _attackAction;

        public IObservable<Vector2> LookStream => _lookSubject;
        public IObservable<Unit> AttackStream => _attackSubject;

        void Awake()
        {
            var playerMap = inputActions.FindActionMap("Player");
            _lookAction = playerMap.FindAction("Look");
            _attackAction = playerMap.FindAction("Attack");
        }

        void OnEnable()
        {
            _lookAction.Enable();
            _attackAction.Enable();
            LockCursor();

            Observable.EveryUpdate()
                .Subscribe(_ =>
                {
                    var lookDelta = _lookAction.ReadValue<Vector2>();
                    if (lookDelta != Vector2.zero)
                        _lookSubject.OnNext(lookDelta);

                    if (_attackAction.WasPressedThisFrame())
                        _attackSubject.OnNext(Unit.Default);

                    if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                        UnlockCursor();

                    if (Mouse.current != null &&
                        Mouse.current.leftButton.wasPressedThisFrame &&
                        Cursor.lockState != CursorLockMode.Locked)
                        LockCursor();
                })
                .AddTo(_disposables);
        }

        void OnDisable()
        {
            _lookAction.Disable();
            _attackAction.Disable();
            _disposables.Clear();
        }

        void OnDestroy()
        {
            _lookSubject.Dispose();
            _attackSubject.Dispose();
            _disposables.Dispose();
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
