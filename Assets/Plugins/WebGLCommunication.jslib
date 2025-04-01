mergeInto(LibraryManager.library, {
    WebGLAlert: function(message) {
        alert(UTF8ToString(message));
    },
    BlockWindowClose: function() {
        window.onbeforeunload = function(e) {
            return "Are you sure you want to leave? Your data may not be saved.";
        };
    }
});
