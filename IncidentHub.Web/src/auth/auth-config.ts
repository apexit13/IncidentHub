export const authConfig = {
  domain: import.meta.env.VITE_AUTH0_DOMAIN,
  clientId: import.meta.env.VITE_AUTH0_CLIENT_ID,
  audience: import.meta.env.VITE_AUTH0_AUDIENCE,
  apiUrl: import.meta.env.VITE_API_URL,
 scope: import.meta.env.VITE_AUTH0_SCOPE || "openid profile email"
};