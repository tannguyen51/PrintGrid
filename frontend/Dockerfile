FROM node:22-alpine AS build
ARG VITE_GOOGLE_CLIENT_ID
ENV VITE_GOOGLE_CLIENT_ID=$VITE_GOOGLE_CLIENT_ID
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime
# The official nginx image runs envsubst over /etc/nginx/templates/*.template at startup,
# which is how Railway points the SPA proxy at its own API service. The default keeps
# docker-compose working unchanged.
ENV API_UPSTREAM=http://api:8080
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/templates/default.conf.template
EXPOSE 80
