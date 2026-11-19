export async function signin(request) {
    try {
        const response = await fetch("/api/auth/signin", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "same-origin",
            body: JSON.stringify(request)
        });

        const contentType = response.headers.get("content-type") || "";
        const payload = contentType.includes("application/json") ? await response.json() : null;

        if (!response.ok) {
            if (payload) {
                return {
                    IsSuccess: false,
                    Error: {
                        StatusCode: response.status,
                        Type: "HttpError",
                        Detail: payload.message
                    }
                };
            }

            return {
                IsSuccess: false,
                Error: {
                    StatusCode: response.status,
                    Type: "HttpError",
                    Detail: "Inloggningen misslyckades."
                }
            };
        }

        return {
            IsSuccess: true,
            Data: payload
        };
    } catch (error) {
        return {
            IsSuccess: false,
            Error: {
                StatusCode: 0,
                Type: "NetworkError",
                Detail: error?.message ?? "Nätverksfel."
            }
        };
    }
}

export async function signout() {
    try {
        const response = await fetch("/api/auth/signout", {
            method: "POST",
            headers: { "Content-Type": "application/json" }
        });

        const contentType = response.headers.get("content-type") || "";
        const payload = contentType.includes("application/json") ? await response.json() : null;

        if (!response.ok) {
            if (payload) {
                return {
                    IsSuccess: false,
                    Error: {
                        StatusCode: response.status,
                        Type: "HttpError",
                        Detail: payload.message
                    }
                };
            }
            
            return {
                IsSuccess: false,
                Error: payload ?? {
                    StatusCode: response.status,
                    Type: "HttpError",
                    Detail: "Utloggningen misslyckades."
                }
            };
        }

        return {
            IsSuccess: true,
            Data: payload
        };
    } catch (error) {
        return {
            IsSuccess: false,
            Error: {
                StatusCode: 0,
                Type: "NetworkError",
                Detail: error?.message ?? "Nätverksfel."
            }
        };
    }
}

export const authRefreshCycle = {
    start: function (intervalMin) {
        this.stop();

        this.timer = setInterval(async () => {
            try {
                const response = await fetch("/api/auth/refresh", {
                    method: "POST",
                    credentials: "same-origin"
                });

                if (response.status === 401) {
                    window.location.assign("/admin");
                }
            } catch (error) {
                console.error("Authentication refresh cycle failed: " + error);
            }
        }, intervalMin * 60 * 1000);
    },
    stop: function () {
        if (this.timer) {
            clearInterval(this.timer);
            this.timer = null;
        }
    }
};