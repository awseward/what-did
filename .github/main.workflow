workflow "push-heroku-prod" {
  on = "push"
  resolves = ["verify-production"]
}

action "master-filter" {
  uses = "actions/bin/filter@master"
  args = "branch master"
}

action "build" {
  uses = "./action-fake/"
  needs = ["master-filter"]

  args = ["bundle:web"]
}

action "heroku-container-login" {
  uses = "actions/heroku@master"
  needs = ["master-filter"]
  secrets = ["HEROKU_API_KEY"]

  args = "container:login"
}

action "push-production" {
  uses = "actions/heroku@master"
  needs = ["build", "heroku-container-login"]
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did"
  }

  args = ["container:push", "web", "--recursive", "--app", "$HEROKU_APP"]
}

action "release-production" {
  needs = ["push-production"]
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did"
  }

  args = ["container:release", "web", "--app", "$HEROKU_APP"]
}

action "verify-production" {
  needs = ["release-production"]
  uses = "actions/heroku@master"
  secrets = ["HEROKU_API_KEY"]
  env = {
    HEROKU_APP = "what-did"
  }

  args = ["apps:info", "$HEROKU_APP"]
}
