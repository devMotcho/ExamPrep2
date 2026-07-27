# Testing Google OAuth locally

To manually test the Google OAuth flow in Postman without building a full frontend application, we need to generate a real Google ID Token that was minted specifically for our `ClientId`.

Since the `index.html` used to generate this token is ignored by Git to avoid cluttering the repository, developers should follow these steps to test the flow:

### 1. Create a temporary HTML client

Create a file named `OAuthTestClient.html` anywhere in the project (this file is included in `.gitignore` so you won't accidentally commit it).

Copy and paste the following snippet into `OAuthTestClient.html`. Be sure to replace `YOUR_GOOGLE_CLIENT_ID` with the actual Google Client ID from your `appsettings.json` or secrets.

```html
<!DOCTYPE html>
<html>
<head>
    <script src="https://accounts.google.com/gsi/client" async defer></script>
</head>
<body>
    <!-- Replace data-client_id below with your actual Client ID -->
    <div id="g_id_onload"
         data-client_id="YOUR_GOOGLE_CLIENT_ID"
         data-callback="handleCredentialResponse">
    </div>
    <div class="g_id_signin" data-type="standard"></div>

    <script>
        function handleCredentialResponse(response) {
            console.log("Copy this ID Token to Postman:");
            console.log(response.credential);
        }
    </script>
</body>
</html>
```

### 2. Generate the Token

1. Ensure the URL you are using to open this file (e.g. `http://localhost`) is whitelisted in your Google Cloud Console under **Authorized JavaScript origins**.
2. Open `OAuthTestClient.html` in your web browser.
3. Click the "Sign in with Google" button.
4. Once you complete the login, open your browser's Developer Tools (F12) and go to the **Console** tab.
5. You will see a long JWT token string starting with `ey...`. Copy this token.

### 3. Simulate the Frontend in Postman

Now, pretend Postman is the frontend sending the token to the backend.

1. Open Postman.
2. Set the method to **POST**.
3. Set the URL to your API endpoint: `https://localhost:<port>/api/oauth/google/login`
4. Under the **Headers** tab, ensure you have:
   - `Content-Type`: `application/json`
5. Under the **Body** tab, select `raw` and `JSON`, then paste:

```json
{
    "token": "ey.......... (paste your long token here) ............."
}
```

6. Send the request. You should receive a `200 OK` with a valid JWT access token from your API, and a `refresh_token` set as an HttpOnly cookie!
