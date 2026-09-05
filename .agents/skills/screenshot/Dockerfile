FROM mcr.microsoft.com/playwright:v1.63.0-noble

WORKDIR /opt/openwish-verifier

COPY package.json package-lock.json ./
RUN npm ci --omit=dev

COPY openwish-smoke.mjs ./

ENTRYPOINT ["node", "openwish-smoke.mjs"]
