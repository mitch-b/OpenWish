FROM mcr.microsoft.com/playwright:v1.65.0-noble

WORKDIR /opt/openwish-verifier

COPY package.json package-lock.json ./
RUN npm ci --omit=dev

COPY openwish-smoke.mjs ./
COPY integrated-client.mjs ./

ENTRYPOINT ["node", "openwish-smoke.mjs"]
