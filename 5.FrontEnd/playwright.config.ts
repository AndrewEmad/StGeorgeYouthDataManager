import {defineConfig,devices} from '@playwright/test';
export default defineConfig({testDir:'./e2e',timeout:120000,expect:{timeout:10000},fullyParallel:true,workers:2,
 snapshotPathTemplate:'{testDir}/snapshots/{projectName}/{arg}{ext}',
 outputDir:process.env['YDM_E2E_OUTPUT'] || 'test-results/ui',reporter:[['list'],['html',{open:'never',outputFolder:process.env['YDM_E2E_OUTPUT'] ? 'playwright-report/pwa' : 'playwright-report/ui'}]],use:{actionTimeout:10000,baseURL:'http://127.0.0.1:4173',serviceWorkers:'block',locale:'ar-EG',timezoneId:'Africa/Cairo',reducedMotion:'reduce',trace:'retain-on-failure',screenshot:'only-on-failure'},
 projects:[{name:'chromium',use:{...devices['Desktop Chrome']}},{name:'firefox',use:{...devices['Desktop Firefox']}},{name:'webkit',use:{...devices['Desktop Safari']}}],
 webServer:{command:'node e2e/server.mjs',url:'http://127.0.0.1:4173/health',reuseExistingServer:!process.env.CI,timeout:30000}});
