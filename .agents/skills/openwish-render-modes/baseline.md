# OpenWish render-mode benchmark

Chromium: 159.0.8154.6. Timings in milliseconds.
Each cell: interactive-ready / synthetic click / framework KiB (renderer). Values are medians, with the ready-time range in brackets. Framework transfer is per page, shared by roots on the same page.

## Desktop, cold browser cache

| Component | Auto | Server | WebAssembly |
| --- | ---: | ---: | ---: |
| `Pages/Events/AcceptInvite` | 148 [144-299] / 2.7 / 2569 (Server; n=3) | 91 [86-262] / 0.7 / 57 (Server; n=3) | 798 [788-946] / 1.4 / 3654 (WebAssembly; n=3) |
| `Pages/Events/EventDetails` | 163 [148-376] / 3.8 / 2569 (Server; n=3) | 103 [97-382] / 4.6 / 57 (Server; n=3) | 783 [777-1045] / 1.1 / 3654 (WebAssembly; n=3) |
| `Pages/Events/Index` | 140 [136-147] / 2.1 / 2569 (Server; n=3) | 86 [85-113] / 0.6 / 57 (Server; n=3) | 777 [770-829] / 1.1 / 3654 (WebAssembly; n=3) |
| `Pages/Events/ManageEvent` | 159 [152-218] / 2.2 / 2569 (Server; n=3) | 110 [110-167] / 1.0 / 57 (Server; n=3) | 951 [947-954] / 4.5 / 3654 (WebAssembly; n=3) |
| `Pages/Events/NewEvent` | 144 [143-150] / 1.7 / 2569 (Server; n=3) | 97 [96-111] / 0.8 / 57 (Server; n=3) | 847 [825-848] / 3.1 / 3654 (WebAssembly; n=3) |
| `Pages/GiftPlan/Index` | 136 [132-139] / 1.3 / 2569 (Server; n=3) | 86 [81-95] / 0.6 / 57 (Server; n=3) | 773 [773-784] / 1.1 / 3654 (WebAssembly; n=3) |
| `Pages/Home` | 156 [147-164] / 2.4 / 2569 (Server; n=3) | 106 [103-114] / 0.8 / 57 (Server; n=3) | 876 [869-894] / 3.6 / 3654 (WebAssembly; n=3) |
| `Pages/Social/FriendJoin` | 140 [130-145] / 1.9 / 2569 (Server; n=3) | 82 [81-105] / 0.7 / 57 (Server; n=3) | 781 [779-792] / 1.0 / 3654 (WebAssembly; n=3) |
| `Pages/Social/Friends` | 148 [140-296] / 1.3 / 2569 (Server; n=3) | 92 [92-202] / 0.8 / 57 (Server; n=3) | 812 [804-916] / 1.6 / 3654 (WebAssembly; n=3) |
| `Pages/WhatsNew` | 144 [137-162] / 1.9 / 2569 (Server; n=3) | 92 [90-110] / 0.7 / 57 (Server; n=3) | 787 [781-797] / 1.1 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/AddItem` | 168 [149-244] / 2.5 / 2569 (Server; n=3) | 96 [91-190] / 1.1 / 57 (Server; n=3) | 837 [835-939] / 4.1 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/Index` | 151 [143-154] / 2.0 / 2569 (Server; n=3) | 91 [90-102] / 0.7 / 57 (Server; n=3) | 798 [793-805] / 1.3 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/ManageWishlist` | 147 [144-208] / 2.6 / 2569 (Server; n=3) | 96 [94-162] / 0.7 / 57 (Server; n=3) | 798 [796-850] / 1.0 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/NewWishlist` | 155 [140-162] / 1.7 / 2569 (Server; n=3) | 95 [92-98] / 0.9 / 57 (Server; n=3) | 824 [816-831] / 2.3 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/QuickAdd` | 134 [130-163] / 1.2 / 2569 (Server; n=3) | 88 [83-96] / 0.7 / 57 (Server; n=3) | 774 [761-787] / 1.4 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/WishlistDetails` | 155 [148-197] / 1.9 / 2569 (Server; n=3) | 98 [97-149] / 0.8 / 57 (Server; n=3) | 804 [798-845] / 1.4 / 3654 (WebAssembly; n=3) |
| `Shared/NotificationFlyout` | 152 [140-160] / 0.8 / 2569 (Server; n=3) | 101 [99-111] / 0.6 / 57 (Server; n=3) | 876 [869-894] / 0.9 / 3654 (WebAssembly; n=3) |
| `Shared/ThemeToggle` | 152 [140-157] / 0.6 / 2569 (Server; n=3) | 100 [99-109] / 0.7 / 57 (Server; n=3) | 876 [869-894] / 0.7 / 3654 (WebAssembly; n=3) |

## Desktop, warm browser cache

| Component | Auto | Server | WebAssembly |
| --- | ---: | ---: | ---: |
| `Pages/Events/AcceptInvite` | 834 [814-881] / 1.4 / 1 (WebAssembly; n=3) | 84 [82-104] / 0.7 / 0 (Server; n=3) | 803 [763-830] / 1.3 / 1 (WebAssembly; n=3) |
| `Pages/Events/EventDetails` | 829 [820-858] / 1.1 / 1 (WebAssembly; n=3) | 95 [92-129] / 2.8 / 0 (Server; n=3) | 766 [761-796] / 1.1 / 1 (WebAssembly; n=3) |
| `Pages/Events/Index` | 819 [813-847] / 1.3 / 1 (WebAssembly; n=3) | 84 [84-101] / 0.6 / 0 (Server; n=3) | 778 [746-791] / 1.5 / 1 (WebAssembly; n=3) |
| `Pages/Events/ManageEvent` | 1022 [984-1050] / 4.3 / 1 (WebAssembly; n=3) | 122 [110-123] / 1.3 / 0 (Server; n=3) | 936 [930-939] / 4.1 / 1 (WebAssembly; n=3) |
| `Pages/Events/NewEvent` | 885 [869-902] / 3.0 / 1 (WebAssembly; n=3) | 96 [91-98] / 0.7 / 0 (Server; n=3) | 815 [804-818] / 2.7 / 1 (WebAssembly; n=3) |
| `Pages/GiftPlan/Index` | 805 [795-808] / 1.1 / 1 (WebAssembly; n=3) | 80 [78-85] / 0.6 / 0 (Server; n=3) | 758 [752-758] / 1.2 / 1 (WebAssembly; n=3) |
| `Pages/Home` | 919 [901-940] / 3.3 / 1 (WebAssembly; n=3) | 98 [97-117] / 0.8 / 0 (Server; n=3) | 862 [850-864] / 3.6 / 1 (WebAssembly; n=3) |
| `Pages/Social/FriendJoin` | 821 [816-832] / 1.1 / 1 (WebAssembly; n=3) | 78 [78-79] / 0.6 / 0 (Server; n=3) | 764 [745-767] / 1.2 / 1 (WebAssembly; n=3) |
| `Pages/Social/Friends` | 851 [842-907] / 2.0 / 1 (WebAssembly; n=3) | 88 [87-96] / 0.4 / 0 (Server; n=3) | 799 [772-801] / 1.3 / 1 (WebAssembly; n=3) |
| `Pages/WhatsNew` | 821 [817-884] / 1.2 / 1 (WebAssembly; n=3) | 89 [86-89] / 0.6 / 0 (Server; n=3) | 766 [737-772] / 1.1 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/AddItem` | 902 [882-908] / 4.4 / 1 (WebAssembly; n=3) | 88 [87-92] / 1.1 / 0 (Server; n=3) | 826 [815-845] / 3.9 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/Index` | 824 [819-869] / 1.7 / 1 (WebAssembly; n=3) | 87 [84-88] / 0.7 / 0 (Server; n=3) | 767 [765-774] / 1.2 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/ManageWishlist` | 828 [821-833] / 1.2 / 1 (WebAssembly; n=3) | 93 [90-96] / 0.8 / 0 (Server; n=3) | 762 [755-819] / 1.0 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/NewWishlist` | 863 [844-948] / 2.7 / 1 (WebAssembly; n=3) | 95 [89-95] / 0.7 / 0 (Server; n=3) | 798 [783-810] / 2.3 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/QuickAdd` | 822 [810-917] / 1.6 / 1 (WebAssembly; n=3) | 81 [77-82] / 0.7 / 0 (Server; n=3) | 753 [752-760] / 1.3 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/WishlistDetails` | 841 [837-844] / 1.2 / 1 (WebAssembly; n=3) | 98 [98-99] / 1.0 / 0 (Server; n=3) | 786 [785-795] / 1.0 / 1 (WebAssembly; n=3) |
| `Shared/NotificationFlyout` | 919 [901-940] / 0.8 / 1 (WebAssembly; n=3) | 94 [90-111] / 0.4 / 0 (Server; n=3) | 862 [850-864] / 0.8 / 1 (WebAssembly; n=3) |
| `Shared/ThemeToggle` | 919 [901-940] / 0.7 / 1 (WebAssembly; n=3) | 94 [90-110] / 0.3 / 0 (Server; n=3) | 862 [850-864] / 0.6 / 1 (WebAssembly; n=3) |

## Mobile, cold browser cache

| Component | Auto | Server | WebAssembly |
| --- | ---: | ---: | ---: |
| `Pages/Events/AcceptInvite` | 1044 [1029-1057] / 10.1 / 2569 (Server; n=3) | 1049 [1039-1059] / 7.2 / 57 (Server; n=3) | 6905 [6587-7007] / 5.1 / 3654 (WebAssembly; n=3) |
| `Pages/Events/EventDetails` | 1084 [1066-1127] / 21.6 / 2569 (Server; n=3) | 1078 [1078-1084] / 11.2 / 57 (Server; n=3) | 6950 [6701-7066] / 5.4 / 3654 (WebAssembly; n=3) |
| `Pages/Events/Index` | 1045 [1039-1064] / 11.6 / 2569 (Server; n=3) | 1042 [1031-1057] / 5.4 / 57 (Server; n=3) | 6840 [6636-6977] / 6.7 / 3654 (WebAssembly; n=3) |
| `Pages/Events/ManageEvent` | 1271 [1263-1313] / 8.4 / 2569 (Server; n=3) | 1250 [1233-1253] / 9.6 / 57 (Server; n=3) | 8088 [7466-8091] / 21.0 / 3654 (WebAssembly; n=3) |
| `Pages/Events/NewEvent` | 1125 [1079-1177] / 9.6 / 2569 (Server; n=3) | 1088 [1084-1105] / 8.6 / 57 (Server; n=3) | 7184 [6907-7307] / 16.4 / 3654 (WebAssembly; n=3) |
| `Pages/GiftPlan/Index` | 1044 [1036-1046] / 10.5 / 2569 (Server; n=3) | 1025 [1020-1027] / 5.8 / 57 (Server; n=3) | 6764 [6509-7359] / 5.8 / 3654 (WebAssembly; n=3) |
| `Pages/Home` | 1192 [1189-1208] / 6.8 / 2569 (Server; n=3) | 1166 [1164-1171] / 6.2 / 57 (Server; n=3) | 7487 [7224-7586] / 20.1 / 3654 (WebAssembly; n=3) |
| `Pages/Social/FriendJoin` | 1043 [1041-1051] / 5.8 / 2569 (Server; n=3) | 1029 [1015-1037] / 8.1 / 57 (Server; n=3) | 6827 [6684-7047] / 5.8 / 3654 (WebAssembly; n=3) |
| `Pages/Social/Friends` | 1064 [1056-1066] / 8.1 / 2569 (Server; n=3) | 1056 [1054-1057] / 6.7 / 57 (Server; n=3) | 6940 [6814-7016] / 7.0 / 3654 (WebAssembly; n=3) |
| `Pages/WhatsNew` | 1049 [1047-1067] / 7.6 / 2569 (Server; n=3) | 1045 [1040-1049] / 5.6 / 57 (Server; n=3) | 6816 [6644-6927] / 5.7 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/AddItem` | 1084 [1061-1087] / 11.6 / 2569 (Server; n=3) | 1049 [1044-1050] / 8.8 / 57 (Server; n=3) | 7148 [6912-7215] / 17.1 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/Index` | 1073 [1063-1146] / 9.1 / 2569 (Server; n=3) | 1074 [1068-1091] / 7.4 / 57 (Server; n=3) | 6841 [6639-6956] / 5.4 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/ManageWishlist` | 1066 [1058-1075] / 12.8 / 2569 (Server; n=3) | 1072 [1057-1083] / 6.9 / 57 (Server; n=3) | 6863 [6696-7186] / 6.2 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/NewWishlist` | 1079 [1067-1088] / 10.9 / 2569 (Server; n=3) | 1071 [1041-1080] / 7.1 / 57 (Server; n=3) | 7044 [6792-7273] / 12.4 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/QuickAdd` | 1062 [1047-1063] / 6.2 / 2569 (Server; n=3) | 1048 [1037-1051] / 6.3 / 57 (Server; n=3) | 6769 [6558-7134] / 5.8 / 3654 (WebAssembly; n=3) |
| `Pages/Wishlists/WishlistDetails` | 1091 [1084-1170] / 43.7 / 2569 (Server; n=3) | 1103 [1082-1113] / 16.5 / 57 (Server; n=3) | 7073 [6708-7161] / 5.9 / 3654 (WebAssembly; n=3) |
| `Shared/NotificationFlyout` | 1138 [1138-1147] / 5.9 / 2569 (Server; n=3) | 1133 [1128-1136] / 5.9 / 57 (Server; n=3) | 7487 [7224-7586] / 4.3 / 3654 (WebAssembly; n=3) |
| `Shared/ThemeToggle` | 1134 [1133-1144] / 5.6 / 2569 (Server; n=3) | 1129 [1126-1132] / 5.4 / 57 (Server; n=3) | 7487 [7224-7586] / 5.0 / 3654 (WebAssembly; n=3) |

## Mobile, warm browser cache

| Component | Auto | Server | WebAssembly |
| --- | ---: | ---: | ---: |
| `Pages/Events/AcceptInvite` | 4087 [4064-4106] / 6.2 / 1 (WebAssembly; n=3) | 689 [675-695] / 6.8 / 0 (Server; n=3) | 3745 [3622-3963] / 6.8 / 1 (WebAssembly; n=3) |
| `Pages/Events/EventDetails` | 3934 [3900-3986] / 6.0 / 1 (WebAssembly; n=3) | 739 [737-740] / 8.1 / 0 (Server; n=3) | 3836 [3580-4015] / 7.3 / 1 (WebAssembly; n=3) |
| `Pages/Events/Index` | 4033 [3944-4186] / 6.5 / 1 (WebAssembly; n=3) | 693 [693-708] / 7.2 / 0 (Server; n=3) | 3893 [3539-3908] / 5.4 / 1 (WebAssembly; n=3) |
| `Pages/Events/ManageEvent` | 5068 [4955-5706] / 22.1 / 1 (WebAssembly; n=3) | 899 [882-907] / 6.7 / 0 (Server; n=3) | 4858 [4359-4889] / 22.4 / 1 (WebAssembly; n=3) |
| `Pages/Events/NewEvent` | 4262 [4255-4288] / 15.4 / 1 (WebAssembly; n=3) | 770 [755-780] / 8.8 / 0 (Server; n=3) | 4169 [3759-4281] / 15.3 / 1 (WebAssembly; n=3) |
| `Pages/GiftPlan/Index` | 3959 [3931-3997] / 5.3 / 1 (WebAssembly; n=3) | 673 [666-688] / 5.7 / 0 (Server; n=3) | 3795 [3577-4014] / 6.0 / 1 (WebAssembly; n=3) |
| `Pages/Home` | 4517 [4507-4533] / 18.0 / 1 (WebAssembly; n=3) | 814 [794-819] / 6.6 / 0 (Server; n=3) | 4423 [4070-4475] / 18.0 / 1 (WebAssembly; n=3) |
| `Pages/Social/FriendJoin` | 3997 [3939-4010] / 6.5 / 1 (WebAssembly; n=3) | 689 [669-695] / 6.8 / 0 (Server; n=3) | 3768 [3637-3897] / 6.1 / 1 (WebAssembly; n=3) |
| `Pages/Social/Friends` | 4144 [4104-4154] / 8.0 / 1 (WebAssembly; n=3) | 704 [688-722] / 7.4 / 0 (Server; n=3) | 3918 [3730-3935] / 8.2 / 1 (WebAssembly; n=3) |
| `Pages/WhatsNew` | 4006 [3958-4068] / 6.5 / 1 (WebAssembly; n=3) | 704 [695-707] / 7.2 / 0 (Server; n=3) | 3739 [3628-3964] / 6.1 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/AddItem` | 4340 [4305-4363] / 20.6 / 1 (WebAssembly; n=3) | 710 [700-725] / 10.6 / 0 (Server; n=3) | 4116 [3972-4173] / 19.9 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/Index` | 3944 [3929-4043] / 9.3 / 1 (WebAssembly; n=3) | 725 [721-738] / 8.5 / 0 (Server; n=3) | 3760 [3613-4316] / 5.5 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/ManageWishlist` | 4029 [3953-4048] / 6.9 / 1 (WebAssembly; n=3) | 707 [694-719] / 8.5 / 0 (Server; n=3) | 3827 [3551-4110] / 6.8 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/NewWishlist` | 4140 [4125-4204] / 13.0 / 1 (WebAssembly; n=3) | 725 [698-736] / 8.9 / 0 (Server; n=3) | 4115 [3776-4258] / 13.0 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/QuickAdd` | 4012 [4007-4063] / 8.3 / 1 (WebAssembly; n=3) | 702 [670-710] / 6.8 / 0 (Server; n=3) | 3793 [3602-3996] / 6.4 / 1 (WebAssembly; n=3) |
| `Pages/Wishlists/WishlistDetails` | 4130 [4121-4164] / 6.7 / 1 (WebAssembly; n=3) | 753 [692-768] / 16.5 / 0 (Server; n=3) | 3764 [3685-4126] / 6.7 / 1 (WebAssembly; n=3) |
| `Shared/NotificationFlyout` | 4517 [4507-4533] / 5.3 / 1 (WebAssembly; n=3) | 781 [756-786] / 5.8 / 0 (Server; n=3) | 4423 [4070-4475] / 5.7 / 1 (WebAssembly; n=3) |
| `Shared/ThemeToggle` | 4517 [4507-4533] / 5.4 / 1 (WebAssembly; n=3) | 778 [751-782] / 6.2 / 0 (Server; n=3) | 4422 [4070-4474] / 5.2 / 1 (WebAssembly; n=3) |

## Components without independent renderer choices

Static components retain SSR; inherited components follow their parent.
A static host can still contain independently interactive children.

### Static

- `OpenWish.Web/Account/Pages/AccessDenied.razor`
- `OpenWish.Web/Account/Pages/ConfirmEmail.razor`
- `OpenWish.Web/Account/Pages/ConfirmEmailChange.razor`
- `OpenWish.Web/Account/Pages/ExternalLogin.razor`
- `OpenWish.Web/Account/Pages/ForgotPassword.razor`
- `OpenWish.Web/Account/Pages/ForgotPasswordConfirmation.razor`
- `OpenWish.Web/Account/Pages/InvalidPasswordReset.razor`
- `OpenWish.Web/Account/Pages/InvalidUser.razor`
- `OpenWish.Web/Account/Pages/Lockout.razor`
- `OpenWish.Web/Account/Pages/Login.razor`
- `OpenWish.Web/Account/Pages/LoginWith2fa.razor`
- `OpenWish.Web/Account/Pages/LoginWithRecoveryCode.razor`
- `OpenWish.Web/Account/Pages/Manage/ChangePassword.razor`
- `OpenWish.Web/Account/Pages/Manage/ConnectedApps.razor`
- `OpenWish.Web/Account/Pages/Manage/DeletePersonalData.razor`
- `OpenWish.Web/Account/Pages/Manage/Disable2fa.razor`
- `OpenWish.Web/Account/Pages/Manage/Email.razor`
- `OpenWish.Web/Account/Pages/Manage/EnableAuthenticator.razor`
- `OpenWish.Web/Account/Pages/Manage/ExternalLogins.razor`
- `OpenWish.Web/Account/Pages/Manage/GenerateRecoveryCodes.razor`
- `OpenWish.Web/Account/Pages/Manage/GiftProfile.razor`
- `OpenWish.Web/Account/Pages/Manage/Index.razor`
- `OpenWish.Web/Account/Pages/Manage/InstallApp.razor`
- `OpenWish.Web/Account/Pages/Manage/PersonalData.razor`
- `OpenWish.Web/Account/Pages/Manage/ResetAuthenticator.razor`
- `OpenWish.Web/Account/Pages/Manage/SetPassword.razor`
- `OpenWish.Web/Account/Pages/Manage/TwoFactorAuthentication.razor`
- `OpenWish.Web/Account/Pages/Register.razor`
- `OpenWish.Web/Account/Pages/RegisterConfirmation.razor`
- `OpenWish.Web/Account/Pages/ResendEmailConfirmation.razor`
- `OpenWish.Web/Account/Pages/ResetPassword.razor`
- `OpenWish.Web/Account/Pages/ResetPasswordConfirmation.razor`
- `OpenWish.Web/Account/Shared/ExternalLoginPicker.razor`
- `OpenWish.Web/Account/Shared/ManageLayout.razor`
- `OpenWish.Web/Account/Shared/ManageNavMenu.razor`
- `OpenWish.Web/Account/Shared/ShowRecoveryCodes.razor`
- `OpenWish.Web/Account/Shared/StatusMessage.razor`
- `OpenWish.Web/App.razor`
- `OpenWish.Web/Layout/MainLayout.razor`
- `OpenWish.Web/Layout/NavMenu.razor`
- `OpenWish.Web/Pages/Error.razor`
- `OpenWish.Web/Pages/GuestEvent.razor`
- `OpenWish.Web/Pages/Integrations/ConnectApp.razor`
- `OpenWish.Web/Pages/NotFound.razor`
- `OpenWish.Web/Routes.razor`
- `OpenWish.Web/Shared/AppInstall.razor`
- `OpenWish.Web/Shared/IntegrationWishlistPicker.razor`

### Inherited

- `OpenWish.Web.Client/Diagnostics/SessionInformation.razor`
- `OpenWish.Web.Client/Event/EventCard.razor`
- `OpenWish.Web.Client/Event/EventForm.razor`
- `OpenWish.Web.Client/Event/EventInvitations.razor`
- `OpenWish.Web.Client/Event/EventReservedItems.razor`
- `OpenWish.Web.Client/Event/EventWishlistManager.razor`
- `OpenWish.Web.Client/Event/ExchangeReveal.razor`
- `OpenWish.Web.Client/Event/GiftExchangeDisplay.razor`
- `OpenWish.Web.Client/Event/GiftExchangeManager.razor`
- `OpenWish.Web.Client/Event/PendingInvitations.razor`
- `OpenWish.Web.Client/Event/RunEventAgain.razor`
- `OpenWish.Web.Client/Event/SecretSantaSetup.razor`
- `OpenWish.Web.Client/Event/SetupStep.razor`
- `OpenWish.Web.Client/Shared/Dialog.razor`
- `OpenWish.Web.Client/Shared/FocusOnFirstRender.razor`
- `OpenWish.Web.Client/Shared/GiftBoxArt.razor`
- `OpenWish.Web.Client/Shared/OpenWishIcon.razor`
- `OpenWish.Web.Client/Shared/PageHeader.razor`
- `OpenWish.Web.Client/Shared/PageSections.razor`
- `OpenWish.Web.Client/Social/CirclesPanel.razor`
- `OpenWish.Web.Client/Social/ConnectInPerson.razor`
- `OpenWish.Web.Client/Social/FriendList.razor`
- `OpenWish.Web.Client/Social/FriendRemoveConfirmationDialog.razor`
- `OpenWish.Web.Client/Social/FriendRequestList.razor`
- `OpenWish.Web.Client/Social/FriendSearch.razor`
- `OpenWish.Web.Client/Social/FriendSuggestions.razor`
- `OpenWish.Web.Client/Wishlist/DuplicateIdeaNotice.razor`
- `OpenWish.Web.Client/Wishlist/GiftProfilePanel.razor`
- `OpenWish.Web.Client/Wishlist/ItemComments.razor`
- `OpenWish.Web.Client/Wishlist/ItemReservation.razor`
- `OpenWish.Web.Client/Wishlist/ItemSourceBadge.razor`
- `OpenWish.Web.Client/Wishlist/ProductLinkImporter.razor`
- `OpenWish.Web.Client/Wishlist/ReservationBadge.razor`
- `OpenWish.Web.Client/Wishlist/WishlistCard.razor`
- `OpenWish.Web.Client/Wishlist/WishlistCardView.razor`
- `OpenWish.Web.Client/Wishlist/WishlistDeleteDialog.razor`
- `OpenWish.Web.Client/Wishlist/WishlistForm.razor`
- `OpenWish.Web.Client/Wishlist/WishlistItemCard.razor`
- `OpenWish.Web.Client/Wishlist/WishlistItemForm.razor`
- `OpenWish.Web.Client/Wishlist/WishlistItemList.razor`
- `OpenWish.Web.Client/Wishlist/WishlistItemModal.razor`
- `OpenWish.Web.Client/Wishlist/WishlistOrganizePanel.razor`
