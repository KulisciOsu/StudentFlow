import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

export default defineConfig(({ command }) => {
    const certPath = fileURLToPath(new URL('../../.certs/localhost.pem', import.meta.url));
    const keyPath = fileURLToPath(new URL('../../.certs/localhost.key', import.meta.url));
    if (command === 'serve' && (!existsSync(certPath) || !existsSync(keyPath))) {
        throw new Error('Nejdříve z kořene projektu spusťte node scripts/dev.mjs setup.');
    }

    return {
        plugins: [plugin()],
        server: command === 'serve' ? {
            host: 'localhost',
            port: 60357,
            strictPort: true,
            https: {
                cert: readFileSync(certPath),
                key: readFileSync(keyPath),
            },
            proxy: {
                '/api': {
                    target: 'https://localhost:7082',
                    // Pouze lokální proxy k vývojovému certifikátu ASP.NET Core.
                    secure: false,
                },
            },
        } : undefined,
    };
})
