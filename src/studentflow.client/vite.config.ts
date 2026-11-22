/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { ensureDevCertificate } from '../../scripts/dev-certs.mjs';

export default defineConfig(({ command }) => {
    // Při spuštění z Visual Studia (npm run dev) chybějící certifikát vytvoří; build ho nepotřebuje.
    const certificate = command === 'serve'
        ? ensureDevCertificate(fileURLToPath(new URL('../..', import.meta.url)))
        : null;

    return {
        plugins: [plugin()],
        server: certificate ? {
            host: 'localhost',
            port: 60357,
            strictPort: true,
            https: {
                cert: readFileSync(certificate.certPath),
                key: readFileSync(certificate.keyPath),
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
