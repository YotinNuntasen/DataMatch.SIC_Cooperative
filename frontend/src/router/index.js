import { createRouter, createWebHistory } from "vue-router";
import store from "../store";

import LoginPage from "../views/LoginPage.vue";
import DataMatching from "../views/DataMatching.vue";
import ResultsPreview from "../views/ResultsPreview.vue";
import AuthCallback from "../views/AuthCallback.vue";

const routes = [
  {
    path: "/",
    name: "Login",
    component: LoginPage,
  },
  {
    path: "/login",
    redirect: "/",
  },
  {
    path: "/matching",
    name: "DataMatching",
    component: DataMatching,
    meta: { requiresAuth: true },
  },
  {
    path: "/results",
    name: "Results",
    component: ResultsPreview,
    meta: { requiresAuth: true },
  },
  {
    path: "/auth-callback",
    name: "AuthCallback",
    component: AuthCallback,
  },
];

const router = createRouter({
  history: createWebHistory(process.env.BASE_URL),
  routes,
});

// Authentication Guard
router.beforeEach(async (to, from, next) => {
  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth);

  const isAuthenticated = store.getters["auth/isAuthenticated"];

  if (requiresAuth && !isAuthenticated) {
    // Try to restore session
    console.log("Route Guard: Access denied. Redirecting to login.");
    next({ name: "Login" });
  } else {
    next();
  }
});

export default router;