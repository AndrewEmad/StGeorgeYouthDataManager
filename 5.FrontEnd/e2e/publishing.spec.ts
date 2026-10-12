import {test,expect} from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
import {noOverflow} from './fixtures';
for(const width of [320,360,390,430,768,1024,1440,1920])test(`combined publication entry page is RTL and usable at ${width}px`,async({page},info)=>{
 await page.setViewportSize({width,height:800});await page.goto('/YouthDataManager/');await expect(page.getByRole('heading',{name:'خدمة الشباب',exact:true})).toBeVisible();await expect(page.getByRole('link',{name:'الإدارة',exact:true})).toHaveAttribute('href','./admin/');await expect(page.getByRole('link',{name:'الخدام',exact:true})).toHaveAttribute('href','./servant/');expect(await page.locator('html').getAttribute('dir')).toBe('rtl');expect((await noOverflow(page)).scroll).toBeLessThanOrEqual(width+1);expect((await new AxeBuilder({page}).withTags(['wcag2a','wcag2aa','wcag21aa']).analyze()).violations).toEqual([]);await page.screenshot({path:info.outputPath('entry.png')});
});
