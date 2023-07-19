using AppResources = BD.WTTS.Client.Resources.Strings;

using BD.SteamClient.Models;
using BD.SteamClient.Services;
using BD.WTTS.UI.Views.Pages;
using WinAuth;
using AngleSharp.Text;

namespace BD.WTTS.UI.ViewModels;

public sealed partial class AuthenticatorPageViewModel : ViewModelBase
{
    const int MAX_SYNC_VALUE = 100;

    string? _currentAnswer;
    
    string? _currentPassword;

    DateTime _initializeTime;

    AuthenticatorItemModel? PrevSelectedAuth { get; set; }

    AuthenticatorItemModel? CurrentSelectedAuth { get; set; }

    public AuthenticatorPageViewModel()
    {
        Auths = new();
        BorderBottomIsActive = false;
        AuthenticatorItemModel.OnAuthenticatorItemIsSelectedChanged +=
            AuthenticatorItemModel_OnAuthenticatorItemIsSelectedChanged;
        Initialize();
    }

    private void AuthenticatorItemModel_OnAuthenticatorItemIsSelectedChanged(AuthenticatorItemModel sender)
    {
        if (sender.IsSelected)
        {
            CurrentSelectedAuth = sender;

            if (PrevSelectedAuth != null) PrevSelectedAuth.IsSelected = false;

            PrevSelectedAuth = sender;

            BorderBottomIsActive = true;
        }
        else
        {
            if (PrevSelectedAuth == CurrentSelectedAuth) PrevSelectedAuth = null;
            if (PrevSelectedAuth != null) return;
            BorderBottomIsActive = false;
            CurrentSelectedAuth = null;
        }
    }

    public async void Initialize()
    {
        //await AuthenticatorService.DeleteAllAuthenticatorsAsync();
        if (_initializeTime > DateTime.Now.AddSeconds(-1))
        {
            Toast.Show(ToastIcon.Warning, AppResources.Warning_DoNotOperateFrequently);
            return;
        }

        _initializeTime = DateTime.Now;

        Auths.Clear();
        BorderBottomIsActive = false;

        var sourceList = await AuthenticatorService.GetAllSourceAuthenticatorAsync();
        if (sourceList.Length < 1) return;

        AuthenticatorIsEmpty = false;

        (HasLocalPcEncrypt, HasPasswordEncrypt) = AuthenticatorService.HasEncrypt(sourceList);

        if (HasPasswordEncrypt && IsVerificationPass == false)
        {
            if (!await EnterPassword(sourceList[0])) return;
        }
        else
        {
            IsVerificationPass = true;
        }

        // var list = await AuthenticatorService.GetAllAuthenticatorsAsync(sourcelist,
        //     _currentPassword.Base64Encode_Nullable());

        var list = (await AuthenticatorService.GetAllAuthenticatorsAsync(sourceList,
            _currentPassword)).OrderBy(i => i.Index);

        foreach (var item in list)
        {
            Auths.Add(new AuthenticatorItemModel(item));
        }

        Toast.Show(ToastIcon.Success, AppResources.Success_AuthloadedSuccessfully);
    }

    public async Task<bool> EnterPassword(AccountPlatformAuthenticator sourceData)
    {
        var textViewmodel = new TextBoxWindowViewModel()
        {
            InputType = TextBoxWindowViewModel.TextBoxInputType.Password,
        };
        if (await IWindowManager.Instance.ShowTaskDialogAsync(textViewmodel, AppResources.Title_InputAuthPassword, isDialog: false,
                isCancelButton: true) &&
            textViewmodel.Value != null)
        {
            if (await AuthenticatorService.ValidatePassword(sourceData, textViewmodel.Value))
            {
                //_currentPassword = textViewmodel.Value.Base64DecodeToByteArray_Nullable();
                _currentPassword = textViewmodel.Value;
                IsVerificationPass = true;
                return true;
            }
            else Toast.Show(ToastIcon.Warning, AppResources.Warning_PasswordError);
        }

        IsVerificationPass = false;
        return false;
    }

    public async Task SetPasswordProtection()
    {
        if (Auths.Count < 1 || IsVerificationPass == false)
        {
            Toast.Show(ToastIcon.Warning, AppResources.Warning_RefuseOperate);
            return;
        }

        string? newPassword;
        var textViewmodel = new TextBoxWindowViewModel()
        {
            InputType = TextBoxWindowViewModel.TextBoxInputType.Password,
        };
        if (await IWindowManager.Instance.ShowTaskDialogAsync(textViewmodel, AppResources.Title_InputAuthPassword, isDialog: false,
                isCancelButton: true) &&
            textViewmodel.Value != null)
        {
            newPassword = textViewmodel.Value;
            textViewmodel.Value = null;
            if (!(await IWindowManager.Instance.ShowTaskDialogAsync(textViewmodel, AppResources.Title_PasswordConfirm, isDialog: false) &&
                  textViewmodel.Value == newPassword)) return;
        }
        else return;

        if (await AuthenticatorService.SwitchEncryptionAuthenticators(HasLocalPcEncrypt, Auths.Select(i => i.AuthData),
                newPassword))
        {
            Toast.Show(ToastIcon.Success, AppResources.Success_AuthPasswordSetSuccessfully);
            //_currentPassword = newPassword.Base64DecodeToByteArray_Nullable();
            _currentPassword = newPassword;
        }
        else Toast.Show(ToastIcon.Error, AppResources.Error_TokenPasswordSetFailed);

        HasPasswordEncrypt = true;
    }

    public async Task RemovePasswordProtection()
    {
        if (Auths.Count < 1 || IsVerificationPass == false)
        {
            Toast.Show(ToastIcon.Error, AppResources.Warning_RefuseOperate);
            return;
        }

        if (await AuthenticatorService.SwitchEncryptionAuthenticators(HasLocalPcEncrypt, Auths.Select(i => i.AuthData)))
        {
            Toast.Show(ToastIcon.Success, AppResources.Success_AuthPasswordRemovedSuccessfully);
            _currentPassword = null;
        }
        else Toast.Show(ToastIcon.Error, AppResources.Error_TokenPasswordRemovedFailed);

        HasPasswordEncrypt = false;
        IsVerificationPass = true;
    }

    public async Task ToggleLocalProtection()
    {
        if (Auths.Count < 1 || IsVerificationPass == false)
        {
            Toast.Show(ToastIcon.Error, AppResources.Warning_RefuseOperate);
            return;
        }

        bool newStatus = HasLocalPcEncrypt == false;

        if (await AuthenticatorService.SwitchEncryptionAuthenticators(newStatus, Auths.Select(i => i.AuthData), _currentPassword))
            Toast.Show(ToastIcon.Success, AppResources.Success_AuthProtectSuccessfully_.Format(newStatus ? "开启" : "关闭"));
        else Toast.Show(ToastIcon.Error, AppResources.Error_AuthProtectFailed_.Format(newStatus ? "开启" : "关闭"));

        HasLocalPcEncrypt = newStatus;
    }

    public async Task EncryptHelp()
    {
        var messageViewmodel = new MessageBoxWindowViewModel();
        messageViewmodel.Content += Strings.LocalAuth_ProtectionAuth_Info + "\r\n\r\n";
        messageViewmodel.Content += Strings.LocalAuth_ProtectionAuth_EnablePassword + ":\r\n\r\n";
        messageViewmodel.Content += Strings.LocalAuth_ProtectionAuth_EnablePasswordTip + "\r\n\r\n";
        messageViewmodel.Content += Strings.LocalAuth_ProtectionAuth_IsOnlyCurrentComputerEncrypt + ":\r\n\r\n";
        messageViewmodel.Content += Strings.LocalAuth_ProtectionAuth_IsOnlyCurrentComputerEncryptTip + "\r\n\r\n";
        await IWindowManager.Instance.ShowTaskDialogAsync(messageViewmodel, AppResources.Title_AuthEncryption);
    }

    public void ReLockAuthenticator()
    {
        if (!HasPasswordEncrypt)
        {
            Toast.Show(ToastIcon.Warning, AppResources.Warning_NotAuthProvided);
            return;
        }

        Auths.Clear();
        BorderBottomIsActive = false;
        IsVerificationPass = false;
    }

    public async Task SyncAuthenticators()
    {
        var response = await IMicroServiceClient.Instance.AuthenticatorClient.GetAuthenticators();
        if (response.Code != ApiRspCode.OK)
        {
            switch (response.Code)
            {
                case ApiRspCode.Unauthorized:
                    Toast.Show(ToastIcon.Error, AppResources.Error_PleaseLoginWattToolKit);
                    break;
            }

            return;
        }

        #region 安全问题

        if (string.IsNullOrEmpty(_currentAnswer))
        {
            var answer = await VerifyIndependentPassword();
            if (!string.IsNullOrEmpty(answer)) _currentAnswer = answer;
            else return;
        }

        #endregion

        response.Content.ThrowIsNull();
        var cloudAuths = response.Content.Select(AuthenticatorService.ConvertToAuthenticatorDto).ToList();
        
        if (cloudAuths.Count >= Auths.Count)
        {
            int changes = 0;
            int additions = 0;
            foreach (var cloudAuth in cloudAuths)
            {
                //var cloudAuth = AuthenticatorService.ConvertToAuthenticatorDto(item);
                // var localAuth = (Auths.FirstOrDefault(i => i.AuthData.ServerId == cloudAuth.ServerId) ??
                //                  Auths.FirstOrDefault(i => i.AuthData.Index == cloudAuth.Index))?.AuthData;
                var localAuth = Auths.FirstOrDefault(i => i.AuthData.ServerId == cloudAuth.ServerId)?.AuthData;
                if (localAuth != null)
                {
                    bool isChanged = false;
                    if (localAuth.Name != cloudAuth.Name)
                    {
                        localAuth.Name = cloudAuth.Name;
                        isChanged = true;
                    }

                    if (localAuth.Index != cloudAuth.Index)
                    {
                        localAuth.Index = cloudAuth.Index;
                        isChanged = true;
                    }

                    if (isChanged)
                    {
                        localAuth.LastUpdate = DateTimeOffset.Now;
                        await AuthenticatorService.AddOrUpdateSaveAuthenticatorsAsync(localAuth, _currentPassword);
                        changes++;
                    }

                    continue;
                }

                additions++;
                await AuthenticatorService.AddOrUpdateSaveAuthenticatorsAsync(cloudAuth, _currentPassword);
            }

            string changeMessage = $"本地令牌新增 {additions} 个 数据更新 {changes} 个";
            if (changes == 0 && additions == 0)
            {
                changeMessage = "本地令牌已为最新数据";
            }
            Toast.Show(ToastIcon.Success, AppResources.Success_CloudSynchronizationSuccessful.Format(changeMessage));
            Initialize();
            return;
        }

        var pushItems = (from item in Auths
            let cloudAuth = cloudAuths.FirstOrDefault(i => i.ServerId == item.AuthData.ServerId)
            where cloudAuth == null
            select new UserAuthenticatorPushItem()
            {
                Id = item.AuthData.ServerId,
                TokenType = item.AuthData.Platform == AuthenticatorPlatform.HOTP
                    ? UserAuthenticatorTokenType.HOTP
                    : UserAuthenticatorTokenType.TOTP,
                Name = item.AuthData.Name,
                Order = item.AuthData.Index,
                Token = MemoryPackSerializer.Serialize(item.AuthData.ToExport()),
            }).ToArray();

        if ((cloudAuths.Count + pushItems.Length) > MAX_SYNC_VALUE)
        {
            Toast.Show(ToastIcon.Error, AppResources.Error_CloudAuthMaximumQuantity___.Format(MAX_SYNC_VALUE, cloudAuths.Count, pushItems.Length));
            return;
        }

        var syncResponse = await IMicroServiceClient.Instance.AuthenticatorClient.SyncAuthenticatorsToCloud(new()
        {
            Difference = pushItems, Answer = _currentAnswer,
        });

        if (!syncResponse.IsSuccess) throw new Exception(AppResources.Error_FailedToSynchronizeAuth);
        response = await IMicroServiceClient.Instance.AuthenticatorClient.GetAuthenticators();
        if (response.Content?.Length != Auths.Count) throw new Exception(AppResources.Error_DataNotUnified);

        foreach (var item in response.Content)
        {
            var localAuth = Auths.FirstOrDefault(i => i.AuthData.Index == item.Order)?.AuthData;
            // var localAuth = Auths
            //     .FirstOrDefault(i => item.Token!.SequenceEqual(MemoryPackSerializer.Serialize(i.AuthData.ToExport())))
            //     ?.AuthData;
            localAuth.ThrowIsNull(AppResources.Error_localAuthNotEmpty);
            localAuth.ServerId ??= item.Id;
            localAuth.LastUpdate = DateTimeOffset.Now;
            await AuthenticatorService.AddOrUpdateSaveAuthenticatorsAsync(localAuth, _currentPassword);
        }

        Initialize();
        Toast.Show(ToastIcon.Success, AppResources.Success_AuthUpload__.Format(syncResponse.Content?.Message, pushItems.Length));
    }

    /// <summary>
    /// 验证安全问题
    /// </summary>
    /// <returns>成功返回正确答案，失败返回null</returns>
    /// <exception cref="Exception">后端异常信息</exception>
    async Task<string?> VerifyIndependentPassword()
    {
        string? question = null;
        string? answer = null;
        var passwordQuestionResponse =
            await IMicroServiceClient.Instance.AuthenticatorClient.GetIndependentPasswordQuestion();
        if (passwordQuestionResponse.Content == null)
        {
            var textViewModel = new TextBoxWindowViewModel();
            if (!await IWindowManager.Instance.ShowTaskDialogAsync(textViewModel, "设置安全问题",
                    subHeader: "首次同步，请为您的云令牌设置一个安全问题及密码。" +
                               "\r\n请先输入「安全问题」", isCancelButton: true)) return null;
            question = textViewModel.Value;
            textViewModel = new TextBoxWindowViewModel();
            if (!await IWindowManager.Instance.ShowTaskDialogAsync(textViewModel, "设置安全问题", subHeader: "请再输入「问题答案」",
                    isCancelButton: true)) return null;
            answer = textViewModel.Value;
            if (string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer)) return null;
            var setPassword =
                await IMicroServiceClient.Instance.AuthenticatorClient.SetIndependentPassword(new()
                {
                    PwdQuestion = question, Answer = answer,
                });
            if (!setPassword.IsSuccess) throw new Exception("设置安全问题失败");
        }

        question ??= passwordQuestionResponse.Content;
        var answerTextViewModel = new TextBoxWindowViewModel();
        if (string.IsNullOrEmpty(answer) && await IWindowManager.Instance.ShowTaskDialogAsync(answerTextViewModel,
                "请输入问题答案", subHeader: $"安全问题：「{question}」", isCancelButton: true))
            answer = answerTextViewModel.Value;

        if (string.IsNullOrEmpty(answer))
        {
            Toast.Show(ToastIcon.Error, "请输入安全问题答案");
            return null;
        }

        var verifyResponse =
            await IMicroServiceClient.Instance.AuthenticatorClient
                .VerifyIndependentPassword(new() { Answer = answer, });
        if (!verifyResponse.Content)
        {
            Toast.Show(ToastIcon.Error, "答案错误，请重试");
            return null;
        }

        return answer;
    }

    public async Task KeepDisplay()
    {
        // var test1 = await IMicroServiceClient.Instance.Accelerate.Query();
        // var test2 = await IMicroServiceClient.Instance.Accelerate.GetInfoByIds();
        // var test3 = await IMicroServiceClient.Instance.Accelerate.GM();
        //
        // var test4 = await IMicroServiceClient.Instance.Script.GM();
        // var test5 = await IMicroServiceClient.Instance.Script.GetInfoByIds();
        // var test6 = await IMicroServiceClient.Instance.Script.Query();
        // var test5 = await IMicroServiceClient.Instance.AuthenticatorClient.GetAuthenticatorDeleteBackups();
        // var test6 = await IMicroServiceClient.Instance.AuthenticatorClient.ResetIndependentPassword(new ()
        // {
        //     Answer = "123",
        //     NewPwdQuestion = "Test",
        //     NewAnswer = "123",
        // });
        // Guid test = test5.Content[0].Id;
        // var test7 = await IMicroServiceClient.Instance.AuthenticatorClient.RecoverAuthenticatorsFromDeleteBackups(new()
        // {
        //     Answer = "123", Id = new[] { test, },
        // });
    }

    public async Task DeleteAuthAsync()
    {
        if (CurrentSelectedAuth == null) return;
        var messageViewmodel =
            new MessageBoxWindowViewModel { Content = Strings.LocalAuth_DeleteAuthTip2 };
        if (await IWindowManager.Instance.ShowTaskDialogAsync(messageViewmodel, AppResources.Title_DeleteAuth, isDialog: false,
                isCancelButton: true))
        {
            if (CurrentSelectedAuth.AuthData.ServerId != null)
            {
                if (string.IsNullOrEmpty(_currentAnswer))
                {
                    var answer = await VerifyIndependentPassword();
                    if (!string.IsNullOrEmpty(answer)) _currentAnswer = answer;
                }
                if (_currentAnswer != null)
                {
                    var response = await IMicroServiceClient.Instance.AuthenticatorClient.SyncAuthenticatorsToCloud(new()
                    {
                        Difference = new[]
                        {
                            new UserAuthenticatorPushItem()
                            {
                                Id = CurrentSelectedAuth.AuthData.ServerId, IsDeleted = true,
                            },
                        },
                        Answer = _currentAnswer,
                    });
                    if (response.IsSuccess)
                        Toast.Show(ToastIcon.Success, AppResources.Success_DelCloudData);
                    else
                        Toast.Show(ToastIcon.Warning, AppResources.Error_DelCloudData);
                }
            }
            AuthenticatorService.DeleteAuth(CurrentSelectedAuth.AuthData);
            Auths.Remove(CurrentSelectedAuth);
            Toast.Show(ToastIcon.Success, AppResources.Success_LocalAuthDelSuccessful);
        }
    }

    public async Task EditAuthNameAsync()
    {
        if (CurrentSelectedAuth == null) return;
        string? newName = null;

        var textViewmodel = new TextBoxWindowViewModel
        {
            InputType = TextBoxWindowViewModel.TextBoxInputType.TextBox, Value = CurrentSelectedAuth.AuthName
        };
        if (await IWindowManager.Instance.ShowTaskDialogAsync(textViewmodel, AppResources.Title_PleaseEnterNewAuthName, isDialog: false,
                isCancelButton: true))
        {
            newName = textViewmodel.Value;
        }

        if (string.IsNullOrEmpty(newName)) return;

        if (CurrentSelectedAuth.AuthData.ServerId != null)
        {
            if (string.IsNullOrEmpty(_currentAnswer))
            {
                var answer = await VerifyIndependentPassword();
                if (!string.IsNullOrEmpty(answer)) _currentAnswer = answer;
            }
            if (_currentAnswer != null)
            {
                var response = await IMicroServiceClient.Instance.AuthenticatorClient.SyncAuthenticatorsToCloud(new()
                {
                    Difference = new[]
                    {
                        new UserAuthenticatorPushItem() { Id = CurrentSelectedAuth.AuthData.ServerId, Name = newName, },
                    },
                    Answer = _currentAnswer,
                });
                if (response.IsSuccess)
                    Toast.Show(ToastIcon.Success, AppResources.Success_UpdateCloudData);
                else
                    Toast.Show(ToastIcon.Warning, AppResources.Error_UpdateCloudData);
            }
        }
        CurrentSelectedAuth.AuthName = newName;
        await AuthenticatorService.SaveEditAuthNameAsync(CurrentSelectedAuth.AuthData, newName);
        Toast.Show(ToastIcon.Success, AppResources.Success_LocalAuthUpdateSuccessful);
    }

    public async Task OpenSteamLoginImportWindow()
    {
        if (VerifyMaxValue())
            await IWindowManager.Instance.ShowTaskDialogAsync(new SteamLoginImportViewModel(_currentPassword),
                AppResources.SteamLoginImport,
                pageContent: new SteamLoginImportPage(), isOkButton: false);
        Initialize();
    }

    public async Task OpenSteamOtherImportWindow()
    {
        if (VerifyMaxValue())
            await IWindowManager.Instance.ShowTaskDialogAsync(new SteamOtherImportViewModel(_currentPassword), "令牌导入",
                pageContent: new SteamOtherImportPage(), isOkButton: false);
        Initialize();
    }

    public async Task OpenGeneralAuthenticatorImportWindow()
    {
        if (VerifyMaxValue())
            await IWindowManager.Instance.ShowTaskDialogAsync(new GeneralAuthenticatorImportViewModel(_currentPassword),
                AppResources.UniversalAuthImport, pageContent: new GeneralAuthenticatorImportPage(), isOkButton: false);
        Initialize();
    }

    bool VerifyMaxValue()
    {
        if (Auths.Count >= IAccountPlatformAuthenticatorRepository.MaxValue)
        {
            Toast.Show(ToastIcon.Info, AppResources.Info_AuthMaximumQuantity);
            return false;
        }

        return true;
    }
    
    public async Task OpenExportWindow()
    {
        await IWindowManager.Instance.ShowTaskDialogAsync(new AuthenticatorExportViewModel(), AppResources.ExportAuth,
            pageContent: new AuthenticatorExportPage(), isOkButton: false);
    }

    public void ShowQrCode()
    {
        if (CurrentSelectedAuth == null) return;
        var dto = CurrentSelectedAuth.AuthData.ToExport();
        var bytes = Serializable.SMP(dto);

        var bytes_compress_br = bytes.CompressByteArrayByBrotli();

        var (result, stream, e) = QRCodeHelper.Create(bytes_compress_br);
        switch (result)
        {
            case QRCodeHelper.QRCodeCreateResult.DataTooLong:
                Toast.Show(ToastIcon.Error, Strings.AuthLocal_ExportToQRCodeTooLongErrorTip);
                break;
            case QRCodeHelper.QRCodeCreateResult.Exception:
                Toast.Show(ToastIcon.Error, e!.Message);
                Log.Error(nameof(AuthenticatorPageViewModel), e, nameof(ShowQrCode));
                break;
        }

        QrCodeStream = stream;
    }

    public async Task ShowReplyWindow()
    {
        if (CurrentSelectedAuth == null || CurrentSelectedAuth.AuthData.Platform != AuthenticatorPlatform.Steam)
        {
            Toast.Show(ToastIcon.Warning, AppResources.Warning_TransactionOnlySteamAuth);
            return;
        }

        var authData = CurrentSelectedAuth.AuthData;
        await IWindowManager.Instance.ShowTaskDialogAsync(new SteamTradePageViewModel(ref authData),
            AppResources.ConfirmTransaction,
            pageContent: new SteamTradePage(), isOkButton: false);
        CurrentSelectedAuth.AuthData = authData;
    }

    public async Task ShowAuthJsonData()
    {
        if (CurrentSelectedAuth == null) return;
        if (CurrentSelectedAuth.AuthData.Platform == AuthenticatorPlatform.Steam)
            await IWindowManager.Instance.ShowTaskDialogAsync(new ShowSteamDataViewModel(CurrentSelectedAuth.AuthData),
                AppResources.LocalAuth_ShowAuthInfo,
                pageContent: new ShowSteamDataPage(), isCancelButton: true);
        else
        {
            var temp = CurrentSelectedAuth.AuthData.Value?.SecretKey
                .ThrowIsNull().ToHexString();
            await IWindowManager.Instance.ShowTaskDialogAsync(
                new TextBoxWindowViewModel()
                {
                    InputType = TextBoxWindowViewModel.TextBoxInputType.TextBox, Value = temp,
                }, AppResources.ModelContent_SecretKey_.Format(CurrentSelectedAuth.AuthName), isDialog: false, isOkButton: false);
        }
    }
    
    public async void ExportAuthWithSdaFile(object sender)
    {
        if (sender is not AuthenticatorItemModel authenticatorItemModel) return;
        
        if (authenticatorItemModel.AuthData.Value is SteamAuthenticator steamAuthenticator)
        {
            if (string.IsNullOrEmpty(steamAuthenticator.SteamData)) return;

            var steamdata = JsonSerializer.Deserialize(steamAuthenticator.SteamData,
                ImportFileModelJsonContext.Default.SdaFileModel);

            steamdata.ThrowIsNull();
            if (steamAuthenticator.SecretKey == null) return;
            var sdafilemodel = new SdaFileModel
            {
                DeviceId = steamAuthenticator.DeviceId ?? string.Empty,
                FullyEnrolled = steamdata.FullyEnrolled,
                Session = steamdata.Session,
                SerialNumber = steamAuthenticator.Serial ?? string.Empty,
                RevocationCode = steamdata.RevocationCode,
                Uri = steamdata.Uri,
                ServerTime = steamdata.ServerTime,
                AccountName = steamdata.AccountName,
                TokenGid = steamdata.TokenGid,
                IdentitySecret = steamdata.IdentitySecret,
                Secret1 = steamdata.Secret1,
                Status = steamdata.Status,
                SharedSecret = Convert.ToBase64String(steamAuthenticator.SecretKey),
            };

            var jsonString = JsonSerializer.Serialize(sdafilemodel, ImportFileModelJsonContext.Default.SdaFileModel);

            //...导出至文件目录

            if (Essentials.IsSupportedSaveFileDialog)
            {
                FilePickerFileType? fileTypes;
                if (IApplication.IsDesktop())
                {
                    fileTypes = new[] { ("maFile Files", new[] { FileEx.maFile, }), };
                }
                else
                {
                    fileTypes = null;
                }

                var exportFile = await FilePicker2.SaveAsync(new PickOptions
                {
                    FileTypes = fileTypes,
                    InitialFileName = $"{steamAuthenticator.AccountName}{FileEx.maFile}",
                    PickerTitle = "Watt Toolkit",
                });
                if (exportFile == null) return;

                var filestream = exportFile.OpenWrite();
                // if (filestream == null)
                // {
                //     Toast.Show(ToastIcon.Error, Strings.LocalAuth_ProtectionAuth_PathError);
                //     return;
                // }

                if (filestream.CanSeek && filestream.Position != 0) filestream.Position = 0;

                var data = Encoding.UTF8.GetBytes(jsonString);

                await filestream.WriteAsync(data);

                await filestream.FlushAsync();
                await filestream.DisposeAsync();

                Toast.Show(ToastIcon.Success, Strings.ExportedToPath_.Format(exportFile.ToString()));
            }
        }
        else
        {
            Toast.Show(ToastIcon.Warning, "Mafile格式仅支持Steam令牌");
        }
    }
    
    public async Task UnbindingSteamAuthAsync(object sender)
    {
        if (sender is not AuthenticatorItemModel authenticatorItemModel) return;
        
        if (authenticatorItemModel.AuthData.Platform != AuthenticatorPlatform.Steam)
        {
            Toast.Show(ToastIcon.Warning, "解绑功能目前仅支持 Steam 令牌");
            return;
        }

        if (await IWindowManager.Instance.ShowTaskDialogAsync(
                new MessageBoxWindowViewModel() { Content = "您确定要从您的 Steam 账号中解绑此令牌吗，解绑后此令牌将失效，您可以在解绑后删除此令牌。" },
                isDialog: false, isCancelButton: true))
        {
            if (authenticatorItemModel.AuthData.Value is SteamAuthenticator steamAuthenticator)
            {
                string? password;
                var textViewmodel = new TextBoxWindowViewModel()
                {
                    InputType = TextBoxWindowViewModel.TextBoxInputType.Password,
                };
                if (await IWindowManager.Instance.ShowTaskDialogAsync(textViewmodel, "请输入该Steam账号登陆密码",
                        isDialog: false, isCancelButton: true))
                {
                    password = textViewmodel.Value;
                }
                else return;

                if (string.IsNullOrEmpty(password)) return;

                var steamAccountService = Ioc.Get<ISteamAccountService>();
                SteamLoginState loginState = new() { Username = steamAuthenticator.AccountName, Password = password, };
                await steamAccountService.DoLoginV2Async(loginState);
                if (!loginState.Success)
                {
                    loginState.TwofactorCode = steamAuthenticator.CurrentCode;
                    await steamAccountService.DoLoginV2Async(loginState);
                }

                if (string.IsNullOrEmpty(loginState.AccessToken))
                {
                    Toast.Show(ToastIcon.Warning, "解绑令牌失败：登陆未成功");
                    return;
                }

                if (await steamAuthenticator.RemoveAuthenticatorAsync(loginState.AccessToken))
                {
                    // var response = await IMicroServiceClient.Instance.AuthenticatorClient.SyncAuthenticatorsToCloud(new()
                    // {
                    //     Difference = new[]
                    //     {
                    //         new UserAuthenticatorPushItem()
                    //         {
                    //             Id = CurrentSelectedAuth.AuthData.ServerId, IsDeleted = true,
                    //         },
                    //     },
                    // });
                    // Toast.Show(ToastIcon.Warning, $"云令牌数据删除{(response.IsSuccess ? "成功" : "失败")}");
                    // AuthenticatorService.DeleteAuth(CurrentSelectedAuth.AuthData);
                    // Auths.Remove(CurrentSelectedAuth);
                    Toast.Show(ToastIcon.Success, "令牌解绑成功");
                    return;
                }

                Toast.Show(ToastIcon.Error, "解绑令牌失败");
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        AuthenticatorItemModel.OnAuthenticatorItemIsSelectedChanged -=
            AuthenticatorItemModel_OnAuthenticatorItemIsSelectedChanged;
        base.Dispose(disposing);
    }
}