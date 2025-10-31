FROM node:18-alpine


COPY . .

RUN npm install

RUN npm run build


# Replace dev redirects

EXPOSE 4000

ENTRYPOINT ["npm", "run", "serve"]