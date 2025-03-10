mergeInto(LibraryManager.library, {
    SetAccessToken: function (token) {
        var tokenStr = Pointer_stringify(token);
        UnityInstance.SendMessage('OneDriveUploader', 'SetAccessToken', tokenStr);
    }
});