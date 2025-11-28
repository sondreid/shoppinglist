  FROM node:18-alpine AS build
  WORKDIR /app
  COPY . .
  RUN npm install && npm run build
  FROM joseluisq/static-web-server:2
  COPY --from=build /app/dist /public
  ENV SERVER_PORT=4000
  EXPOSE 4000