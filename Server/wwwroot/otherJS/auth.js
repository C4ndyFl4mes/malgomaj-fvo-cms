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
            return {
                IsSuccess: false,
                Error: payload ?? {
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

export async function refresh()
{
    try {
        await fetch("/api/user/refresh", {
            method: "POST",
            credentials: "include"
        });
    } catch {
        // Ignorerar för tillfället...
    }
}