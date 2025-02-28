﻿using System;
using System.Reactive;
using ReactiveUI;
using UnitystationLauncher.Services;

namespace UnitystationLauncher.ViewModels
{
    public class ForgotPasswordViewModel : ViewModelBase
    {
        private readonly Lazy<LoginViewModel> _loginVm;
        private readonly AuthService _authService;
        private bool _isFormVisible;
        private bool _isSuccessVisible;
        string _email = "";

        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        public ReactiveCommand<Unit, Unit> Submit { get; }
        public ReactiveCommand<Unit, LoginViewModel> DoneButton { get; }

        public bool IsFormVisible
        {
            get => _isFormVisible;
            set => this.RaiseAndSetIfChanged(ref _isFormVisible, value);
        }

        public bool IsSuccessVisible
        {
            get => _isSuccessVisible;
            set => this.RaiseAndSetIfChanged(ref _isSuccessVisible, value);
        }

        public ForgotPasswordViewModel(AuthService authService, Lazy<LoginViewModel> loginVm)
        {
            IsFormVisible = true;
            IsSuccessVisible = false;
            _authService = authService;
            _loginVm = loginVm;

            var inputValidation = this.WhenAnyValue(
                x => x.Email,
                (e) => !string.IsNullOrWhiteSpace(e) &&
                e.Contains("@") && e.Contains("."));

            Submit = ReactiveCommand.Create(
                TrySendResetPassword, inputValidation);

            DoneButton = ReactiveCommand.Create(ReturnToLogin);
        }

        void TrySendResetPassword()
        {
            _authService.SendForgotPasswordEmail(Email);
            IsFormVisible = false;
            IsSuccessVisible = true;
        }

        public LoginViewModel ReturnToLogin()
        {
            return _loginVm.Value;
        }
    }
}
