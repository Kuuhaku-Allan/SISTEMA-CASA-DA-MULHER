(function () {
    const explicitBaseUrl = window.CASA_MULHER_API_BASE_URL || window.API_BASE_URL;

    function getCodespacesApiUrl(location) {
        const host = location.hostname || "";

        if (!host.endsWith(".app.github.dev")) {
            return "";
        }

        const apiHost = host.includes("-5500.")
            ? host.replace("-5500.", "-5001.")
            : host.replace(/-\d+\./, "-5001.");

        return `${location.protocol}//${apiHost}`;
    }

    function getLocalApiUrl(location) {
        const host = location.hostname || "";

        if (host === "localhost" || host === "127.0.0.1") {
            return "http://localhost:5001";
        }

        return "";
    }

    const apiBaseUrl = explicitBaseUrl
        || getCodespacesApiUrl(window.location)
        || getLocalApiUrl(window.location)
        || "http://localhost:5001";

    window.CasaMulherConfig = Object.assign({}, window.CasaMulherConfig, {
        apiBaseUrl
    });

    window.API_BASE_URL = apiBaseUrl;
})();
