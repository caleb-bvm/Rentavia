// Verificación visual opcional con Playwright; no es una dependencia de la web.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

(async () => {
    const root = path.resolve(__dirname, '..');
    const config = Object.fromEntries(fs.readFileSync(path.join(root, '.env'), 'utf8')
        .split(/\r?\n/).filter(line => line && !line.startsWith('#') && line.includes('='))
        .map(line => [line.slice(0, line.indexOf('=')), line.slice(line.indexOf('=') + 1)]));
    const browser = await chromium.launch({ headless: true,
        ...(fs.existsSync(chromium.executablePath()) ? {} : { channel: 'msedge' }) });
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    const output = path.join(root, 'docs', 'evidencias', 'ui');
    fs.mkdirSync(output, { recursive: true });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    const base = process.env.RENTAVIA_URL || 'http://127.0.0.1:5295';
    await page.goto(base + '/Cuenta/Ingresar');
    await page.screenshot({ path: path.join(output, '01-acceso.png'), fullPage: true });
    await page.locator('#Correo').fill(config.ADMIN_EMAIL);
    await page.locator('#Clave').fill(config.ADMIN_PASSWORD);
    await Promise.all([page.waitForURL(base + '/'), page.getByRole('button', { name: 'Entrar al panel' }).click()]);
    for (const [url, name] of [['/', '02-panel'], ['/Disponibilidad', '03-disponibilidad'], ['/Reservas', '04-reservas'], ['/Recomendaciones', '05-recomendador']]) {
        await page.goto(base + url);
        await page.screenshot({ path: path.join(output, name + '.png'), fullPage: true });
    }
    await page.locator('#Pasajeros').fill('4');
    await page.locator('#Maletas').fill('2');
    await page.locator('#Preferencias').fill('Viaje familiar de demostración');
    await Promise.all([
        page.waitForResponse(response => response.request().method() === 'POST' && response.url().includes('/Recomendaciones')),
        page.getByRole('button', { name: 'Obtener recomendaciones' }).click()
    ]);
    await page.getByRole('heading', { name: 'Reglas (sin IA)' }).waitFor();
    await page.screenshot({ path: path.join(output, '07-recomendacion-respaldo.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(base + '/Disponibilidad');
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth);
    await page.screenshot({ path: path.join(output, '06-movil.png'), fullPage: true });
    fs.writeFileSync(path.join(output, 'resultado.json'), JSON.stringify({ fechaUtc: new Date().toISOString(), erroresJavaScript: errors, desbordamientoMovil: overflow }, null, 2));
    await browser.close();
    if (errors.length || overflow) throw new Error('La verificación visual encontró un problema.');
    console.log('Capturas de escritorio y móvil completas; sin errores JavaScript ni desbordamiento.');
})().catch(error => { console.error(error.message); process.exitCode = 1; });
