const puppeteer = require('puppeteer');

(async () => {
  const browser = await puppeteer.launch();
  const page = await browser.newPage();
  
  page.on('console', msg => console.log('BROWSER LOG:', msg.text()));
  page.on('pageerror', err => console.log('BROWSER ERROR:', err.toString()));
  
  await page.goto('http://localhost:5281/', { waitUntil: 'networkidle2' });
  await page.waitForTimeout(6000); // wait for one slide transition
  
  const activeSlideIndex = await page.evaluate(() => {
     const slides = Array.from(document.querySelectorAll('[data-champong-hero-slide]'));
     return slides.findIndex(s => s.classList.contains('is-active'));
  });
  console.log('Active slide index after 6s:', activeSlideIndex);

  await browser.close();
})();
