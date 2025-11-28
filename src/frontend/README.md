Frontend files
=========================




For production serving 


## Simple webstarter 
from https://github.com/lttr/simple-npm-webapp-starter





## Features

- live preview
- dependency manager
- basic task runner
- concats multiple css files into one
- concats multiple javascript files into one

-> _Everything on one page of configuration._


## Quickstart

Clone or download this repo. Cd into the folder.

```
npm install
```
downloads the dependencies and install tools.

```
npm start
```
builds the application, begins watching the files for changes and starts the 
local server and opens browser with the app.

## Project structure

```
❯ tree app/ dist/
app/
├── css
│   ├── main.css
│   └── vendor
│       └── bootstrap.min.css
├── img
│   └── npm.png
├── index.html
└── js
    ├── main.js
    └── vendor
        └── bootstrap.min.js
dist/
├── css
│   ├── app.css
│   └── vendor.min.css
├── img
│   └── npm.png
├── index.html
└── js
    ├── app.js
    └── vendor.min.js
```
