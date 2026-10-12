const appRoot=new URL('./',self.location.href);
self.addEventListener('notificationclick',event=>{
  event.stopImmediatePropagation();event.notification.close();
  const candidate=event.notification.data?.url || event.notification.data?.FCM_MSG?.data?.url;
  let url;try{url=new URL(candidate || '#/dashboard',appRoot);}catch{url=new URL('#/dashboard',appRoot);}
  if(url.origin!==appRoot.origin || !url.pathname.startsWith(appRoot.pathname))url=new URL('#/dashboard',appRoot);
  event.waitUntil(clients.matchAll({type:'window',includeUncontrolled:true}).then(async windows=>{const existing=windows.find(w=>new URL(w.url).pathname.startsWith(appRoot.pathname));if(existing){await existing.navigate(url.href);return existing.focus();}return clients.openWindow(url.href);}));
});
// Firebase Messaging Service Worker
// This file handles background push notifications from Firebase Cloud Messaging.

importScripts('https://www.gstatic.com/firebasejs/12.19.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/12.19.0/firebase-messaging-compat.js');

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
        icon: new URL('logo.png',appRoot).href,
        badge: new URL('logo.png',appRoot).href,
        dir: 'rtl',
        lang: 'ar', data:{url:new URL('#/dashboard',appRoot).href}
    };

    self.registration.showNotification(notificationTitle, notificationOptions);
});
