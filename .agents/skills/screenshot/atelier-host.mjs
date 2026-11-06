import { createApp } from "/app/src/app.js";

// Import in test mode for synthetic login, then exercise the real production session/cookie configuration.
// This host is used only inside the isolated verification network; it is not a deployment entry point.
process.env.NODE_ENV = "production";
const app = createApp();
for (const path of ["/settings", "/wishlist"]) {
  app.get(path, (_request, response) => response.type("html").send("<h1>The Atelier test landing</h1>"));
}
app.listen(3001, "0.0.0.0", () => console.log("The Atelier verification host is ready."));
