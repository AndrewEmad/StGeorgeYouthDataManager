// Firebase Messaging Service Worker
// This file handles background push notifications from Firebase Cloud Messaging.

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

    if (payload.notification) {
        return;
    }

    const notificationTitle = payload.data?.title || 'تنبيه جديد'; // Changed default title to be more generic for admin
    const notificationOptions = {
        body: payload.data?.body || 'لديك إشعار جديد في تطبيق الإدارة',
        icon: '/assets/icons/icon-192x192.png', // Adjusted path to assets/
        badge: '/assets/icons/icon-72x72.png',
        dir: 'rtl',
        lang: 'ar'
    };

    self.registration.showNotification(notificationTitle, notificationOptions);
});
