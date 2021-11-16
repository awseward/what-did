# what-did [![Tag](https://github.com/awseward/what-did/actions/workflows/tag.yml/badge.svg)](https://github.com/awseward/what-did/actions/workflows/tag.yml)

```sh
# Staging
heroku container:push web --recursive --app what-did-staging \
  && heroku container:release web --app what-did-staging

# Prod
heroku container:push web --recursive --app what-did \
  && heroku container:release web --app what-did
```
