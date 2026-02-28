// Firebase Messaging Service Worker
// This file handles background push notifications from Firebase Cloud Messaging.
// Replace the firebase config below with your actual Firebase project config.

importScripts('https://www.gstatic.com/firebasejs/10.12.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/10.12.0/firebase-messaging-compat.js');

firebase.initializeApp({
    apiKey: "AIzaSyAd8mOVdDYhv9Jau5yYYjuLhH54-JZtHXk",
    authDomain: "stgeorgeyouthdbmanager.firebaseapp.com",
    projectId: "stgeorgeyouthdbmanager",
    storageBucket: "stgeorgeyouthdbmanager.firebasestorage.app",
    messagingSenderId: "573535792366",
    appId: "1:573535792366:web:f9a5c748717ded61b3d7ff",
    measurementId: "G-ZQR4DYHYPV"
});

const messaging = firebase.messaging();

messaging.onBackgroundMessage((payload) => {
    console.log('[firebase-messaging-sw.js] Received background message:', payload);

    // If the payload already has a notification object, Firebase SDK will show it automatically.
    // We only need to show it manually if it's a data-only payload.
    if (payload.notification) {
        return;
    }

    const notificationTitle = payload.data?.title || 'تذكير متابعة';
    const notificationOptions = {
        body: payload.data?.body || 'وقت متابعة المخدومين',
        icon: '/icons/icon-192x192.png',
        badge: '/icons/icon-72x72.png',
        dir: 'rtl',
        lang: 'ar'
    };

    self.registration.showNotification(notificationTitle, notificationOptions);
});
